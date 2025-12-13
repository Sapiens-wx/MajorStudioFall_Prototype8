Shader "Hidden/Outline_Then_InkSpike_Merged"
{
    Properties
    {
        // Source
        [HideInInspector]_MainTex ("Source", 2D) = "white" {}

        // ----- OUTLINE PROPS -----
        _Scale                    ("Outline Thickness (pixels)", Float) = 1.0
        _DepthThreshold           ("Base Depth Threshold", Float) = 0.2
        _NormalThreshold          ("Normal Threshold", Float) = 0.2
        _DepthNormalThresholdScale("Depth/Normal Threshold Scale", Float) = 0.5
        _OutlineColor             ("Outline Color", Color) = (0,0,0,1)

        // ----- INK SPIKE BW + RADIAL BLUR -----
        _Threshold ("Luminance Threshold", Range(0,1)) = 0.5
        _Invert    ("Invert (0/1)", Range(0,1)) = 0
        _Tint      ("Tint", Color) = (1,1,1,1)

        _Center ("Center (xy=UV, z=UseCustom 0/1)", Vector) = (0.5, 0.5, 0, 0)

        _Spikes      ("Spike Count", Range(2, 256)) = 64
        _SpikeLength ("Spike Length (UV)", Range(0, 0.25)) = 0.08
        _SpikeSharp  ("Spike Sharpness", Range(1, 16)) = 6
        _SpikeSpeed  ("Spike Speed", Range(0, 10)) = 1.5
        _InnerRadius ("Warp Start Radius", Range(0, 1)) = 0.05
        _OuterRadius ("Warp End Radius", Range(0, 1)) = 0.75

        _SpikeChaos   ("Spike Chaos", Range(0, 4)) = 1.5
        _SpikeJitter  ("Spike Jitter Speed", Range(0, 40)) = 18
        _SpikeAmpVar  ("Spike Amp Variation", Range(0, 1)) = 0.6
        _SpikeFreqVar ("Spike Freq Variation", Range(0, 2)) = 1.0

        _BlurWidth ("Blur Width", Range(0,0.5)) = 0.13
        _Samples   ("Samples", Range(1,64)) = 20
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // ----- GLOBALS -----
            sampler2D _MainTex;
            float4    _MainTex_TexelSize;

            // Depth/normal tex from camera
            sampler2D _CameraDepthNormalsTexture;

            // Outline params
            float  _Scale;
            float  _DepthThreshold;
            float  _NormalThreshold;
            float  _DepthNormalThresholdScale;
            float4 _OutlineColor;

            // Ink spike params
            float  _Threshold;
            float  _Invert;
            float4 _Tint;

            float4 _Center;

            float  _Spikes;
            float  _SpikeLength;
            float  _SpikeSharp;
            float  _SpikeSpeed;
            float  _InnerRadius;
            float  _OuterRadius;

            float  _SpikeChaos;
            float  _SpikeJitter;
            float  _SpikeAmpVar;
            float  _SpikeFreqVar;

            float  _BlurWidth;
            float  _Samples;

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
            };

            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.posCS = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                return o;
            }

            // ---------------------------------------------------------
            // COMMON HELPERS
            // ---------------------------------------------------------
            float luminance(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // ---------------------------------------------------------
            // OUTLINE: SAMPLE DEPTH & NORMAL 4-CORNER KERNEL
            // ---------------------------------------------------------
            void SampleDepthNormalCorners(float2 uv,
                                          out float depthBL, out float3 nBL,
                                          out float depthTR, out float3 nTR,
                                          out float depthTL, out float3 nTL,
                                          out float depthBR, out float3 nBR)
            {
                float2 texel    = _MainTex_TexelSize.xy;
                float  halfStep = _Scale * 0.5;

                float2 offsetBL = texel * float2(-halfStep, -halfStep);
                float2 offsetTR = texel * float2( halfStep,  halfStep);
                float2 offsetTL = texel * float2(-halfStep,  halfStep);
                float2 offsetBR = texel * float2( halfStep, -halfStep);

                float4 dnBL = tex2D(_CameraDepthNormalsTexture, uv + offsetBL);
                float4 dnTR = tex2D(_CameraDepthNormalsTexture, uv + offsetTR);
                float4 dnTL = tex2D(_CameraDepthNormalsTexture, uv + offsetTL);
                float4 dnBR = tex2D(_CameraDepthNormalsTexture, uv + offsetBR);

                DecodeDepthNormal(dnBL, depthBL, nBL);
                DecodeDepthNormal(dnTR, depthTR, nTR);
                DecodeDepthNormal(dnTL, depthTL, nTL);
                DecodeDepthNormal(dnBR, depthBR, nBR);
            }

            float ComputeOutlineMask(float2 uv)
            {
                float depthBL, depthTR, depthTL, depthBR;
                float3 nBL, nTR, nTL, nBR;

                SampleDepthNormalCorners(uv,
                                         depthBL, nBL,
                                         depthTR, nTR,
                                         depthTL, nTL,
                                         depthBR, nBR);

                // Depth edge (gradient magnitude)
                float dDiff1    = depthTR - depthBL;
                float dDiff2    = depthBR - depthTL;
                float edgeDepth = sqrt(dDiff1 * dDiff1 + dDiff2 * dDiff2);

                // Normal edge
                float3 nDiff1    = nTR - nBL;
                float3 nDiff2    = nBR - nTL;
                float  edgeNormal = sqrt(dot(nDiff1, nDiff1) + dot(nDiff2, nDiff2));

                // View direction in view space ~ (0,0,1)
                float3 viewDir = float3(0.0, 0.0, 1.0);

                // Use one sample as representative normal
                float  nDotV        = abs(dot(normalize(nBL), viewDir)); // 1 front-on, 0 grazing
                float  grazingAmount= 1.0 - nDotV;                        // 0 front-on, 1 grazing
                float  depthScale   = 1.0 + grazingAmount * _DepthNormalThresholdScale;

                float  effectiveDepthThreshold = _DepthThreshold * depthScale;

                float depthMask  = (edgeDepth  > effectiveDepthThreshold) ? 1.0 : 0.0;
                float normalMask = (edgeNormal > _NormalThreshold)       ? 1.0 : 0.0;

                float edge = max(depthMask, normalMask);
                return edge;
            }

            // ---------------------------------------------------------
            // INK SPIKE B/W + WARP
            // ---------------------------------------------------------
            float3 InkSpikeBW(float2 uv, float2 center)
            {
                float2 d = uv - center;
                float  r = length(d);
                float2 dir = (r > 1e-6) ? (d / r) : float2(1, 0);

                // polar
                float ang = atan2(dir.y, dir.x); // -pi..pi
                float env = smoothstep(_InnerRadius, _OuterRadius, r);

                // map angle -> spike index
                float ang01     = (ang + UNITY_PI) / (2.0 * UNITY_PI);
                float spikeIdxF = floor(ang01 * _Spikes);
                float local01   = frac(ang01 * _Spikes);

                // per-spike random
                float h0 = hash11(spikeIdxF + 1.23);
                float h1 = hash11(spikeIdxF + 9.87);

                float spikeSpeed = _SpikeSpeed * lerp(0.4, 2.8, h0) * (1.0 + _SpikeFreqVar * (h1 - 0.5));
                float phase      = 6.28318 * h1;

                float t = _Time.y;

                // violent jitter
                float jitter = (hash21(float2(spikeIdxF, floor(t * _SpikeJitter))) - 0.5) * 2.0;
                jitter *= _SpikeChaos;

                float local = local01 + 0.15 * jitter * env;

                float baseWave = abs(sin(local * UNITY_PI + phase + t * spikeSpeed + jitter));
                float micro    = abs(sin(local * UNITY_PI * 3.0 + phase * 2.0 + t * (spikeSpeed * 2.7) + jitter * 1.7));

                float chaosLerp = saturate(_SpikeChaos / 4.0);
                float wave      = saturate(lerp(baseWave, baseWave * micro, chaosLerp));

                float ampBreath = lerp(1.0 - _SpikeAmpVar, 1.0 + _SpikeAmpVar,
                                       abs(sin(t * spikeSpeed * 1.3 + phase + jitter)));

                float spike = pow(wave, _SpikeSharp);

                float offset = spike * ampBreath * _SpikeLength * env;
                float2 warpedUV = uv + dir * offset;

                float3 col = tex2D(_MainTex, warpedUV).rgb;
                float  lum = luminance(col);

                float bw = step(_Threshold, lum);
                bw = lerp(bw, 1.0 - bw, saturate(_Invert));

                return bw.xxx * _Tint.rgb;
            }

            float3 ComputeImpact(float2 uv)
            {
                float2 center = (_Center.z > 0.5) ? _Center.xy : float2(0.5, 0.5);

                float2 d = uv - center;

                float  blurStart = 1.0 - _BlurWidth;
                float  stepSize  = _BlurWidth / max(1.0, (_Samples - 1.0));

                float3 acc = 0.0;

                const int MAX_SAMPLES = 64;
                int ns = (int)round(_Samples);
                ns = clamp(ns, 1, MAX_SAMPLES);

                for (int s = 0; s < MAX_SAMPLES; s++)
                {
                    if (s >= ns) break;

                    float scale = blurStart + (float)s * stepSize;
                    float2 suv  = d * scale + center;

                    acc += InkSpikeBW(suv, center);
                }

                acc /= (float)ns;
                return acc;
            }

            // ---------------------------------------------------------
            // FRAGMENT: OUTLINE FIRST, THEN IMPACT, THEN COMPOSITE
            // ---------------------------------------------------------
            float4 frag (Interpolators i) : SV_Target
            {
                // 1) Depth/normal outline mask
                float edgeMask = ComputeOutlineMask(i.uv);  // 0 or 1

                // 2) Impact color (InkSpike + radial blur)
                float3 impactCol = ComputeImpact(i.uv);

                // 3) Composite: outlines override impact where edgeMask=1
                float3 outlineCol = _OutlineColor.rgb;
                float3 finalCol   = lerp(impactCol, outlineCol, edgeMask);

                return float4(finalCol, 1.0);
            }

            ENDCG
        }
    }

    Fallback Off
}
