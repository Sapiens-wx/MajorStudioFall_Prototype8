Shader "Hidden/UltraImpact_Outline_InkSpike_Blur_BuiltIn"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        // ----- LUMINANCE / TINT -----
        _Threshold ("Luminance Threshold", Range(0,1)) = 0.5
        _Invert    ("Invert (0/1)", Range(0,1)) = 0
        _Tint      ("Tint", Color) = (1,1,1,1)

        // ----- CENTER / RADIAL BLUR -----
        _Center ("Center (xy=UV, z=UseCustom 0/1)", Vector) = (0.5, 0.5, 0, 0)

        _BlurWidth ("Blur Width", Range(0,0.5)) = 0.13
        _Samples   ("Samples", Range(1,64)) = 20

        // ----- SPIKE WARP -----
        _Spikes      ("Spike Count", Range(2, 256)) = 64
        _SpikeLength ("Spike Length (UV)", Range(0, 0.25)) = 0.08
        _SpikeSharp  ("Spike Sharpness", Range(1, 16)) = 6
        _SpikeSpeed  ("Spike Speed", Range(0, 10)) = 1.5
        _InnerRadius ("Warp Start Radius", Range(0, 1)) = 0.05
        _OuterRadius ("Warp End Radius", Range(0, 1)) = 0.75

        // Chaos controls
        _SpikeChaos   ("Spike Chaos", Range(0, 4)) = 1.5
        _SpikeJitter  ("Spike Jitter Speed", Range(0, 40)) = 18
        _SpikeAmpVar  ("Spike Amp Variation", Range(0, 1)) = 0.6
        _SpikeFreqVar ("Spike Freq Variation", Range(0, 2)) = 1.0

        // ----- OUTLINE (DEPTH + NORMAL) -----
        _Scale                    ("Outline Thickness (pixels)", Float) = 1.0
        _DepthThreshold           ("Base Depth Threshold", Float) = 0.2
        _NormalThreshold          ("Normal Threshold", Float) = 0.2
        _DepthNormalThresholdScale("Depth/Normal Threshold Scale", Float) = 0.5
        _OutlineColor             ("Outline Color (RGB+A strength)", Color) = (0,0,0,1)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        // Fullscreen blit, we output final color; no extra blending needed.
        // (If used as overlay on something else, you could add Blend SrcAlpha OneMinusSrcAlpha.)

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;

            float _Threshold;
            float _Invert;
            float4 _Tint;

            float4 _Center;

            float _BlurWidth;
            float _Samples;

            float _Spikes;
            float _SpikeLength;
            float _SpikeSharp;
            float _SpikeSpeed;
            float _InnerRadius;
            float _OuterRadius;

            float _SpikeChaos;
            float _SpikeJitter;
            float _SpikeAmpVar;
            float _SpikeFreqVar;

            // ----- Outline uniforms -----
            sampler2D _CameraDepthNormalsTexture;

            float     _Scale;
            float     _DepthThreshold;
            float     _NormalThreshold;
            float     _DepthNormalThresholdScale;
            float4    _OutlineColor;

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

            // -----------------------------
            // Helpers
            // -----------------------------

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

            // -----------------------------
            // OUTLINE: depth+normal edge mask
            // -----------------------------

            void SampleDepthNormalCorners(float2 uv,
                                          out float depthBL, out float3 nBL,
                                          out float depthTR, out float3 nTR,
                                          out float depthTL, out float3 nTL,
                                          out float depthBR, out float3 nBR)
            {
                float2 texel = _MainTex_TexelSize.xy;
                float  halfScale = _Scale * 0.5;

                float2 offsetBL = texel * float2(-halfScale, -halfScale);
                float2 offsetTR = texel * float2( halfScale,  halfScale);
                float2 offsetTL = texel * float2(-halfScale,  halfScale);
                float2 offsetBR = texel * float2( halfScale, -halfScale);

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

                // Depth edge
                float dDiff1 = depthTR - depthBL;
                float dDiff2 = depthBR - depthTL;
                float edgeDepth = sqrt(dDiff1 * dDiff1 + dDiff2 * dDiff2);

                // Normal edge
                float3 nDiff1 = nTR - nBL;
                float3 nDiff2 = nBR - nTL;
                float edgeNormal = sqrt(dot(nDiff1, nDiff1) + dot(nDiff2, nDiff2));

                // View direction in view space ~ (0,0,1)
                float3 viewDir = float3(0.0, 0.0, 1.0);
                float nDotV = abs(dot(normalize(nBL), viewDir));
                float grazingAmount = 1.0 - nDotV;
                float depthScale = 1.0 + grazingAmount * _DepthNormalThresholdScale;

                float effectiveDepthThreshold = _DepthThreshold * depthScale;

                float depthMask  = edgeDepth  > effectiveDepthThreshold ? 1.0 : 0.0;
                float normalMask = edgeNormal > _NormalThreshold       ? 1.0 : 0.0;

                float edge = max(depthMask, normalMask);
                return edge;
            }

            float3 ApplyOutline(float2 uv, float3 sceneColor)
            {
                float edge = ComputeOutlineMask(uv);

                // Use outline alpha as intensity
                float outlineAlpha = edge * _OutlineColor.a;

                float3 finalRGB = lerp(sceneColor, _OutlineColor.rgb, outlineAlpha);
                return finalRGB;
            }

            // -----------------------------
            // Ink spike + BW, now using outlined color
            // -----------------------------

            float3 InkSpikeBW(float2 uv, float2 center)
            {
                float2 d = uv - center;
                float r = length(d);
                float2 dir = (r > 1e-6) ? (d / r) : float2(1, 0);

                // base polar
                float ang = atan2(dir.y, dir.x); // -pi..pi
                float env = smoothstep(_InnerRadius, _OuterRadius, r);

                // which spike?
                float ang01 = (ang + UNITY_PI) / (2.0 * UNITY_PI);
                float spikeIdxF = floor(ang01 * _Spikes);
                float local01 = frac(ang01 * _Spikes); // 0..1 within spike

                // per-spike random
                float h0 = hash11(spikeIdxF + 1.23);
                float h1 = hash11(spikeIdxF + 9.87);

                // each spike: unique speed / phase / amp
                float spikeSpeed = _SpikeSpeed * lerp(0.4, 2.8, h0) * (1.0 + _SpikeFreqVar * (h1 - 0.5));
                float phase = 6.28318 * h1;

                // violent jitter
                float t = _Time.y;
                float jitter = (hash21(float2(spikeIdxF, floor(t * _SpikeJitter))) - 0.5) * 2.0; // -1..1
                jitter *= _SpikeChaos;

                // sideways twitch
                float local = local01 + 0.15 * jitter * env;

                // layered waveform
                float baseWave = abs(sin(local * UNITY_PI + phase + t * spikeSpeed + jitter));
                float micro    = abs(sin(local * UNITY_PI * 3.0 + phase * 2.0 + t * (spikeSpeed * 2.7) + jitter * 1.7));

                float wave = saturate(lerp(baseWave, baseWave * micro, saturate(_SpikeChaos / 4.0)));

                // amplitude breathing
                float ampBreath = lerp(1.0 - _SpikeAmpVar, 1.0 + _SpikeAmpVar,
                                       abs(sin(t * spikeSpeed * 1.3 + phase + jitter)));

                float spike = pow(wave, _SpikeSharp);

                float offset = spike * ampBreath * _SpikeLength * env;
                float2 warpedUV = uv - dir * offset;

                // SAMPLE SCENE COLOR, THEN APPLY OUTLINE AT WARPED UV
                float3 col = tex2D(_MainTex, warpedUV).rgb;
                col = ApplyOutline(warpedUV, col);

                float lum = luminance(col);
                float bw = step(_Threshold, lum);
                bw = lerp(bw, 1.0 - bw, saturate(_Invert));

                return (bw.xxx) * _Tint.rgb;
            }

            // -----------------------------
            // Final radial blur over InkSpikeBW
            // -----------------------------

            float4 frag (Interpolators i) : SV_Target
            {
                float2 center = (_Center.z > 0.5) ? _Center.xy : float2(0.5, 0.5);

                float2 uv = i.uv;
                float2 d = uv - center;

                float blurStart = 1.0 - _BlurWidth;
                float precompute = _BlurWidth / max(1.0, (_Samples - 1.0));

                float3 acc = 0.0;

                const int MAX_SAMPLES = 64;
                int ns = (int)round(_Samples);
                ns = clamp(ns, 1, MAX_SAMPLES);

                for (int s = 0; s < MAX_SAMPLES; s++)
                {
                    if (s >= ns) break;

                    float scale = blurStart + (float)s * precompute;
                    float2 suv = d * scale + center;

                    acc += InkSpikeBW(suv, center);
                }

                acc /= (float)ns;
                return float4(acc, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
