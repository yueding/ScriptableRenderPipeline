// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
Shader "Hidden/TerrainEngine/Details/Vertexlit" 
{
    Properties 
    {
        _MainTex ("Main Texture", 2D) = "white" {  }
    }    
    SubShader 
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
        LOD 300
        // Lightmapped
        Pass
        {
            //Tags{ "LightMode" = "VertexLM"}
    
            // Here you will notice we now use HLSL rather than CG in SRP
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            
                        // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ _SHADOWS_ENABLED
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _SHADOWS_CASCADE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            
            #pragma vertex vert
            #pragma fragment frag
            
            // Here we include the Core.hlsl of the LWRP Shader Library,
            // this will inturn also link the Core.hlsl fromt the SRP Core Shader Library.
            #include "LWRP/ShaderLibrary/Core.hlsl"
            // We added this to access the lighting functions for LWRP,
            // here we use the refleciton funtions to get the relection probe data
            #include "LWRP/ShaderLibrary/Lighting.hlsl"
    
            float4 _MainTex_ST;
    
            struct appdata
            {
                float3 pos : POSITION;
                float3 uv1 : TEXCOORD1;
                float3 uv0 : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
    
            struct v2f
            {
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                half4 lightingFog : TEXCOORD2; // w=fog
                float4 shadowCoord :TEXCOORD3;
                float4 clipPos : SV_POSITION;
    
                UNITY_VERTEX_OUTPUT_STEREO
            };
    
            v2f vert(appdata IN)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
                o.uv0 = IN.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                o.uv1 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                
                float3 worldPos = TransformObjectToWorld(IN.pos);
                o.clipPos = TransformObjectToHClip(IN.pos);
                
                float3 worldNormal = IN.normal;
    
                o.lightingFog.w = ComputeFogFactor(o.clipPos.z);
            
            #if SHADOWS_SCREEN
                o.shadowCoord = ComputeShadowCoord(o.clipPos);
            #else
                o.shadowCoord = TransformWorldToShadowCoord(worldPos);
            #endif
           
                Light mainLight = GetMainLight();
                //MixRealtimeAndBakedGI(mainLight, worldNormal, bakedGI, half4(0, 0, 0, 0));
            
                half3 attenuatedLightColor = mainLight.color * mainLight.attenuation;
                half3 diffuseColor = LightingLambert(attenuatedLightColor, mainLight.direction, worldNormal);
            #ifdef _ADDITIONAL_LIGHTS
                int pixelLightCount = GetPixelLightCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetLight(i, worldPos);
                    light.attenuation *= LocalLightRealtimeShadowAttenuation(light.index, worldPos);
                    half3 attenuatedLightColor = light.color * light.attenuation;
                    diffuseColor += LightingLambert(attenuatedLightColor, light.direction, worldNormal);
                }
            #endif
            
                o.lightingFog.xyz = diffuseColor;
            
                return o;
            }
    
            sampler2D _MainTex;
    
            half4 frag(v2f IN) : SV_Target
            {
                half3 bakedGI = SampleLightmap(IN.uv0.xy, half3(0, 1, 0));
                
                half3 lighting = IN.lightingFog.rgb * MainLightRealtimeShadowAttenuation(IN.shadowCoord) + bakedGI;
                
                
            
                half4 col;
                half4 tex = tex2D(_MainTex, IN.uv1.xy);
                col.rgb = tex.rgb * lighting;
                col.a = 1.0f;
    
                ApplyFog(col.rgb, IN.lightingFog.w);
    
                return col;
            }
    
        ENDHLSL
        }
        
        Pass
        {
            Name "Meta"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex LightweightVertexMeta
            #pragma fragment LightweightFragmentMetaSimple

            #pragma shader_feature _EMISSION
            #pragma shader_feature _SPECGLOSSMAP

            #include "LWRP/ShaderLibrary/InputSurfaceSimple.hlsl"
            #include "LWRP/ShaderLibrary/LightweightPassMetaSimple.hlsl"

            ENDHLSL
        }
    }
    
    //Fallback "VertexLit"
}
