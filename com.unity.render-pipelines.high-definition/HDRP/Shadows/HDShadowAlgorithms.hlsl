// Various shadow algorithms
// There are two variants provided, one takes the texture and sampler explicitly so they can be statically passed in.
// The variant without resource parameters dynamically accesses the texture when sampling.

#ifdef PUNCTUAL_SHADOW_PCF_5X5
#define PUNCTUAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_5x5(sd.textureSize, sd.textureSizeRcp, posTC, sampleBias, tex, samp)
#elif PUNCTUAL_SHADOW_PCF_7X7
#define PUNCTUAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_7x7(sd.textureSize, sd.textureSizeRcp, posTC, sampleBias, tex, samp)
#else // PUNCTUAL_SHADOW_PCSS
#define PUNCTUAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp) SampleShadow_PCSS(posTC, sd.scaleOffset, sampleBias, sd.shadowSoftness, sd.blockerSampleCount, sd.filterSampleCount, tex, samp, s_point_clamp_sampler)
#endif

#ifdef DIRECTIONAL_SHADOW_PCF_5X5
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_5x5(sd.textureSize, sd.textureSizeRcp, posTC, sampleBias, tex, samp)
#elif DIRECTIONAL_SHADOW_PCF_7X7
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_7x7(sd.textureSize, sd.textureSizeRcp, posTC, sampleBias, tex, samp)
#else // DIRECTIONAL_SHADOW_PCSS
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp) SampleShadow_PCSS(posTC, sd.scaleOffset, sampleBias, sd.shadowSoftness, sd.blockerSampleCount, sd.filterSampleCount, tex, samp, s_point_clamp_sampler)
#endif

real4 EvalShadow_WorldToShadow(real4x4 viewProjection, real3 positionWS)
{
    return mul(viewProjection, real4(positionWS, 1));
}

// function called by spot, point and directional eval routines to calculate shadow coordinates
real3 EvalShadow_GetTexcoordsAtlas(real4x4 viewProjection, real4 scaleOffset, real3 positionWS, out real3 posNDC, bool perspProj)
{
    real4 posCS = EvalShadow_WorldToShadow(viewProjection, positionWS);
    posNDC = perspProj ? (posCS.xyz / posCS.w) : posCS.xyz;
    // calc TCs
    real3 posTC = real3(posNDC.xy * 0.5 + 0.5, posNDC.z);
    posTC.xy = posTC.xy * scaleOffset.xy + scaleOffset.zw;

    return posTC;
}

real3 EvalShadow_GetTexcoordsAtlas(real4x4 viewProjection, real4 scaleOffset, real3 positionWS, bool perspProj)
{
    real3 ndc;
    return EvalShadow_GetTexcoordsAtlas(viewProjection, scaleOffset, positionWS, ndc, perspProj);
}

real2 EvalShadow_GetTexcoordsAtlas(real4x4 viewProjection, real4 scaleOffset, real2 shadowmapSize, real2 shadowmapSizeRcp, real3 positionWS, out real2 closestSampleNDC, bool perspProj)
{
    real4 posCS = EvalShadow_WorldToShadow(viewProjection, positionWS);
    real2 posNDC = perspProj ? (posCS.xy / posCS.w) : posCS.xy;
    // calc TCs
    real2 posTC = posNDC * 0.5 + 0.5;
    closestSampleNDC = (floor(posTC * shadowmapSize) + 0.5) * shadowmapSizeRcp * 2.0 - 1.0.xx;
    return posTC * scaleOffset.xy + scaleOffset.zw;
}

uint2 EvalShadow_GetIntTexcoordsAtlas(real4x4 viewProjection, real4 scaleOffset, real2 shadowmapSize, real2 shadowmapSizeRcp, real2 atlasSize, real3 positionWS, out real2 closestSampleNDC, bool perspProj)
{
    real2 texCoords = EvalShadow_GetTexcoordsAtlas(viewProjection, scaleOffset, shadowmapSize, shadowmapSizeRcp, positionWS, closestSampleNDC, perspProj);
    return uint2(texCoords * atlasSize.xy);
}

//
//  Biasing functions
//

// helper function to get the world texel size
real EvalShadow_WorldTexelSize(real4 viewBias, real L_dist, bool perspProj)
{
    return perspProj ? (viewBias.w * L_dist) : viewBias.w;
}

// used to scale down view biases to mitigate light leaking across shadowed corners
real EvalShadow_ReceiverBiasWeightFlag(int flag)
{
    return (flag & HDSHADOWFLAG_EDGE_LEAK_FIXUP) ? 1.0 : 0.0;
}

bool EvalShadow_ReceiverBiasWeightUseNormalFlag(int flag)
{
    return (flag & HDSHADOWFLAG_EDGE_TOLERANCE_NORMAL) ? true : false;
}

real3 EvalShadow_ReceiverBiasWeightPos(real3 positionWS, real3 normalWS, real3 L, real worldTexelSize, real tolerance, bool useNormal)
{
#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0
    return positionWS + L * worldTexelSize * tolerance;
#else
    return positionWS + (useNormal ? normalWS : L) * worldTexelSize * tolerance;
#endif
}

real EvalShadow_ReceiverBiasWeight(real4x4 viewProjection, real4 scaleOffset, real4 viewBias, real edgeTolerance, int flags, Texture2D tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj)
{
    real3 pos = EvalShadow_ReceiverBiasWeightPos(positionWS, normalWS, L, EvalShadow_WorldTexelSize(viewBias, L_dist, perspProj), edgeTolerance, EvalShadow_ReceiverBiasWeightUseNormalFlag(flags));
    return lerp(1.0, SAMPLE_TEXTURE2D_SHADOW(tex, samp, EvalShadow_GetTexcoordsAtlas(viewProjection, scaleOffset, pos, perspProj)).x, EvalShadow_ReceiverBiasWeightFlag(flags));
}

// receiver bias either using the normal to weight normal and view biases, or just light view biasing
real3 EvalShadow_ReceiverBias(real4 viewBias, real4 normalBias, real3 positionWS, real3 normalWS, real3 L, real L_dist, real lightviewBiasWeight, bool perspProj)
{
#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0 // only light vector based biasing
    real viewBiasScale = viewBias.z;
    return positionWS + L * viewBiasScale * lightviewBiasWeight * EvalShadow_WorldTexelSize(viewBias, L_dist, perspProj);
#else // biasing based on the angle between the normal and the light vector
    real viewBiasMin   = viewBias.x;
    real viewBiasMax   = viewBias.y;
    real viewBiasScale = viewBias.z;
    real normalBiasMin   = normalBias.x;
    real normalBiasMax   = normalBias.y;
    real normalBiasScale = normalBias.z;

    real  NdotL       = dot(normalWS, L);
    real  sine        = sqrt(saturate(1.0 - NdotL * NdotL));
    real  tangent     = abs(NdotL) > 0.0 ? (sine / NdotL) : 0.0;
           sine        = clamp(sine    * normalBiasScale, normalBiasMin, normalBiasMax);
           tangent     = clamp(tangent * viewBiasScale * lightviewBiasWeight, viewBiasMin, viewBiasMax);
    real3 view_bias   = L        * tangent;
    real3 normal_bias = normalWS * sine;
    return positionWS + (normal_bias + view_bias) * EvalShadow_WorldTexelSize(viewBias, L_dist, perspProj);
#endif
}

// sample bias used by wide PCF filters to offset individual taps
real2 EvalShadow_SampleBias_Persp(real3 positionWS, real3 normalWS, real3 tcs) { return 0.0.xx; }
real2 EvalShadow_SampleBias_Ortho(real3 normalWS)                                { return 0.0.xx; }


//
//  Point shadows
//
real EvalShadow_PunctualDepth(HDShadowData sd, Texture2D tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist)
{
    /* bias the world position */
    real recvBiasWeight = EvalShadow_ReceiverBiasWeight(sd.viewProjection, sd.scaleOffset, sd.viewBias, sd.edgeTolerance, sd.flags, tex, samp, positionWS, normalWS, L, L_dist, true);
    positionWS = EvalShadow_ReceiverBias(sd.viewBias, sd.normalBias, positionWS, normalWS, L, L_dist, recvBiasWeight, true);
    /* get shadowmap texcoords */
    real3 posTC = EvalShadow_GetTexcoordsAtlas(sd.viewProjection, sd.scaleOffset, positionWS, true);
    /* get the per sample bias */
    real2 sampleBias = EvalShadow_SampleBias_Persp(positionWS, normalWS, posTC);
    /* sample the texture */
    return PUNCTUAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp);
}

//
//  Directional shadows (cascaded shadow map)
//

#define kMaxShadowCascades 4

int EvalShadow_GetSplitIndex(HDShadowContext shadowContext, int index, real3 positionWS, out real alpha, out int cascadeCount)
{
    int   i = 0;
    real  relDistance = 0.0;
    real3 wposDir, splitSphere;

    HDShadowData sd = shadowContext.shadowDatas[index];
    HDDirectionalShadow dsd = shadowContext.directionalShadowData;

    // find the current cascade
    for (; i < kMaxShadowCascades; i++)
    {
        real4  sphere  = dsd.sphereCascades[i];
                wposDir = -sphere.xyz + positionWS;
        real   distSq  = dot(wposDir, wposDir);
        relDistance = distSq / sphere.w;
        if (relDistance > 0.0 && relDistance <= 1.0)
        {
            splitSphere = sphere.xyz;
            wposDir    /= sqrt(distSq);
            break;
        }
    }
    int shadowSplitIndex = i < kMaxShadowCascades ? i : -1;

    real3 cascadeDir = dsd.cascadeDirection.xyz;
    cascadeCount     = dsd.cascadeDirection.w;
    real border      = dsd.cascadeBorders[shadowSplitIndex];
          alpha      = border <= 0.0 ? 0.0 : saturate((relDistance - (1.0 - border)) / border);
    real  cascDot    = dot(cascadeDir, wposDir);
          alpha      = lerp(alpha, 0.0, saturate(-cascDot * 4.0));

    return shadowSplitIndex;
}

real EvalShadow_CascadedDepth_Blend(HDShadowContext shadowContext, Texture2D tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, int index, real3 L)
{
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, alpha, cascadeCount);

    if (shadowSplitIndex < 0)
        return 0.0;

    HDShadowData sd = shadowContext.shadowDatas[index + shadowSplitIndex];

    /* normal based bias */
    real3 orig_pos = positionWS;
    real recvBiasWeight = EvalShadow_ReceiverBiasWeight(sd.viewProjection, sd.scaleOffset, sd.viewBias, sd.edgeTolerance, sd.flags, tex, samp, positionWS, normalWS, L, 1.0, false);
    positionWS = EvalShadow_ReceiverBias(sd.viewBias, sd.normalBias, positionWS, normalWS, L, 1.0, recvBiasWeight, false);

    /* get shadowmap texcoords */
    real3 posTC = EvalShadow_GetTexcoordsAtlas(sd.viewProjection, sd.scaleOffset, positionWS, false);
    /* evalute the first cascade */
    real2 sampleBias = EvalShadow_SampleBias_Ortho(normalWS);
    real  shadow     = DIRECTIONAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp);
    real  shadow1    = 1.0;

    shadowSplitIndex++;
    if (shadowSplitIndex < cascadeCount)
    {
        shadow1 = shadow;

        if (alpha > 0.0)
        {
            sd = shadowContext.shadowDatas[index + shadowSplitIndex];
            positionWS = EvalShadow_ReceiverBias(sd.viewBias, sd.normalBias, orig_pos, normalWS, L, 1.0, recvBiasWeight, false);
            real3 posNDC;
            posTC = EvalShadow_GetTexcoordsAtlas(sd.viewProjection, sd.scaleOffset, positionWS, posNDC, false);
            /* sample the texture */
            sampleBias = EvalShadow_SampleBias_Ortho(normalWS);

            UNITY_BRANCH
            if (all(abs(posNDC.xy) <= (1.0 - sd.textureSizeRcp.zw * 0.5)))
                shadow1 = DIRECTIONAL_FILTER_ALGORITHM(sd, posTC, sampleBias, tex, samp);
        }
    }
    shadow = lerp(shadow, shadow1, alpha);
    return shadow;
}

real EvalShadow_hash12(real2 pos)
{
    real3 p3  = frac(pos.xyx * real3(443.8975, 397.2973, 491.1871));
           p3 += dot(p3, p3.yzx + 19.19);
    return frac((p3.x + p3.y) * p3.z);
}

real EvalShadow_SampleClosestDistance_Punctual(HDShadowData sd, Texture2D tex, SamplerState sampl, real3 positionWS, real3 L, real3 lightPositionWS)
{
    real4 closestNDC = { 0,0,0,1 };
    real2 texelIdx = EvalShadow_GetTexcoordsAtlas(sd.viewProjection, sd.scaleOffset, sd.textureSize.zw, sd.textureSizeRcp.zw, positionWS, closestNDC.xy, true);

    // sample the shadow map
    closestNDC.z = SAMPLE_TEXTURE2D_LOD(tex, sampl, texelIdx, 0).x;

    // reconstruct depth position
    real4 closestWS = mul(closestNDC, sd.shadowToWorld);
    real3 occluderPosWS = closestWS.xyz / closestWS.w;

    return distance(occluderPosWS, lightPositionWS);
}

real3 EvalShadow_GetClosestSample_Cascade(HDShadowContext shadowContext, Texture2D tex, real3 positionWS, real3 normalWS, int index, real4 L)
{
    // load the right shadow data for the current face
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, alpha, cascadeCount);

    if (shadowSplitIndex < 0)
        return 0.0;

    HDShadowData sd = shadowContext.shadowDatas[index + shadowSplitIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoordsAtlas(sd.viewProjection, sd.scaleOffset, sd.textureSize.zw, sd.textureSizeRcp.zw, sd.textureSize.xy, positionWS, closestNDC.xy, false);

    // load the texel
    closestNDC.z = LOAD_TEXTURE2D_LOD(tex, texelIdx, 0).x;

    // reconstruct depth position
    real4 closestWS = mul(closestNDC, sd.shadowToWorld);
    return closestWS.xyz / closestWS.w;
}

real EvalShadow_SampleClosestDistance_Cascade(HDShadowContext shadowContext, Texture2D tex, SamplerState sampl,
                                               real3 positionWS, real3 normalWS, int index, real4 L, out real3 nearPlanePositionWS)
{
    real alpha;
    int  cascadeCount;
    int shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, alpha, cascadeCount);
    
    HDShadowData sd = shadowContext.shadowDatas[index + shadowSplitIndex];

    real4 closestNDC = { 0,0,0,1 };
    real2 texelIdx = EvalShadow_GetTexcoordsAtlas(sd.viewProjection, sd.scaleOffset, sd.textureSize.zw, sd.textureSizeRcp.zw, positionWS, closestNDC.xy, false);

    // sample the shadow map
    closestNDC.z = SAMPLE_TEXTURE2D_LOD(tex, sampl, texelIdx, 0).x;

    // reconstruct depth position
    real4 closestWS = mul(closestNDC, sd.shadowToWorld);
    real3 occluderPosWS = closestWS.xyz / closestWS.w;

    // TODO: avoid the matrix multiplication here.
    real4 nearPlanePos = mul(real4(0,0,1,1), sd.shadowToWorld); // Note the reversed Z
    nearPlanePositionWS = nearPlanePos.xyz / nearPlanePos.w;

    return distance(occluderPosWS, nearPlanePositionWS);
}
