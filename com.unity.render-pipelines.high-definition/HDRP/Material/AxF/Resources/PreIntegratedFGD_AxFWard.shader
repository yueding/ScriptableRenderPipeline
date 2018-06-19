Shader "Hidden/HDRenderPipeline/PreIntegratedFGD_AxFWard"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "../../../ShaderVariables.hlsl"

            // ==============================================================================================
            // Pre-Integration Code
            //

            // ----------------------------------------------------------------------------
            // Importance sampling BSDF functions
            // ----------------------------------------------------------------------------
            void SampleWardDir( real2   u,
                                real3   V,
                                real3x3 localToWorld,
                                real    roughness,
                            out real3   L,
                            out real    NdotL,
                            out real    NdotH,
                            out real    VdotH,
                                bool    VeqN = false )
            {
                // Ward NDF sampling
                real    cosTheta = sqrt((1.0 - u.x) / (1.0 + (roughness * roughness - 1.0) * u.x));
                real    phi      = TWO_PI * u.y;

                real3   localH = SphericalToCartesian(phi, cosTheta);

                NdotH = cosTheta;

                real3   localV;
                if ( VeqN ) {
                    // localV == localN
                    localV = real3(0.0, 0.0, 1.0);
                    VdotH  = NdotH;
                } else {
                    localV = mul( V, transpose(localToWorld) );
                    VdotH  = saturate( dot(localV, localH) );
                }

                // Compute { localL = reflect(-localV, localH) }
                real3   localL = -localV + 2.0 * VdotH * localH;
                NdotL = localL.z;

                L = mul( localL, localToWorld );
            }

//            // weightOverPdf return the weight (without the diffuseAlbedo term) over pdf. diffuseAlbedo term must be apply by the caller.
//            void ImportanceSampleLambert(real2   u,
//                                         real3x3 localToWorld,
//                                     out real3   L,
//                                     out real    NdotL,
//                                     out real    weightOverPdf)
//            {
//                real3 N = localToWorld[2];
//
//                L     = SampleHemisphereCosine(u.x, u.y, N);
//                NdotL = saturate(dot(N, L));
//
//                // Importance sampling weight for each sample
//                // pdf = N.L / PI
//                // weight = fr * (N.L) with fr = diffuseAlbedo / PI
//                // weight over pdf is:
//                // weightOverPdf = (diffuseAlbedo / PI) * (N.L) / (N.L / PI)
//                // weightOverPdf = diffuseAlbedo
//                // diffuseAlbedo is apply outside the function
//
//                weightOverPdf = 1.0;
//            }

            // weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
            void ImportanceSampleWard(  real2   u,
                                        real3   V,
                                        real3x3 localToWorld,
                                        real    roughness,
                                        real    NdotV,
                                    out real3   L,
                                    out real    VdotH,
                                    out real    NdotL,
                                    out real    weightOverPdf)
            {
                real    NdotH;
                SampleWardDir( u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH );

                // Importance sampling weight for each sample
                // pdf = D(H) * (N.H) / (4 * (L.H))
                // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
                // weight over pdf is:
                // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
                // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
                // Remind (L.H) == (V.H)
                // F is apply outside the function

                real Vis = V_SmithJointGGX(NdotL, NdotV, roughness);
                weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
            }

            float4  IntegrateWardAndLambertDiffuseFGD( float3 V, float3 N, float roughness, uint sampleCount = 8192 ) {
                float   NdotV    = ClampNdotV( dot(N, V) );
                float4  acc      = float4(0.0, 0.0, 0.0, 0.0);
                float2  randNum  = InitRandom( V.xy * 0.5 + 0.5 );  // Add some jittering on Hammersley2d

                float3x3    localToWorld = GetLocalFrame( N );

                for ( uint i = 0; i < sampleCount; ++i ) {
                    float2  u = frac( randNum + Hammersley2d( i, sampleCount ) );

                    float   VdotH;
                    float   NdotL;
                    float   weightOverPdf;

                    float3  L; // Unused
                    ImportanceSampleWard(   u, V, localToWorld, roughness, NdotV,
                                            L, VdotH, NdotL, weightOverPdf );

                    if ( NdotL > 0.0 ) {
                        // Integral{BSDF * <N,L> dw} =
                        // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
                        // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
                        // (1 - F0) * x + F0 * y = lerp(x, y, F0)
                        acc.x += weightOverPdf * pow( 1 - VdotH, 5 );
                        acc.y += weightOverPdf;
                    }

                    // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
                    ImportanceSampleLambert( u, localToWorld, L, NdotL, weightOverPdf );

                    if ( NdotL > 0.0 ) {
                        float   LdotV = dot(L, V);
                        float   disneyDiffuse = DisneyDiffuseNoPI( NdotV, NdotL, LdotV, RoughnessToPerceptualRoughness(roughness) );

                        acc.z += disneyDiffuse * weightOverPdf;
                    }
                }

                acc /= sampleCount;

                // Remap from the [0.5, 1.5] to the [0, 1] range.
                acc.z -= 0.5;

                return acc;
            }

            // ==============================================================================================
            //
            struct Attributes {
                uint vertexID : SV_VertexID;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target {
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in lit.hlsl, i.e here we use perceptualRoughness, must be the same in shader
                float   NdotV               = input.texCoord.x;
                float   perceptualRoughness = input.texCoord.y;
                float3  V                   = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);
                float3  N                   = float3(0.0, 0.0, 1.0);

                // Pre integrate GGX with smithJoint visibility as well as DisneyDiffuse
                float4 preFGD = IntegrateWardAndLambertDiffuseFGD( V, N, PerceptualRoughnessToRoughness(perceptualRoughness) );

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
