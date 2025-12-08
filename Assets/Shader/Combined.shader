Shader "Hidden/InkSpikeBW_RadialBlur_Merged_BuiltIn"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

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

        // NEW: chaos controls
        _SpikeChaos   ("Spike Chaos", Range(0, 4)) = 1.5
        _SpikeJitter  ("Spike Jitter Speed", Range(0, 40)) = 18
        _SpikeAmpVar  ("Spike Amp Variation", Range(0, 1)) = 0.6
        _SpikeFreqVar ("Spike Freq Variation", Range(0, 2)) = 1.0

        _BlurWidth ("Blur Width", Range(0,0.5)) = 0.13
        _Samples   ("Samples", Range(1,64)) = 20
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _Threshold;
            float _Invert;
            float4 _Tint;

            float4 _Center;

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

            float _BlurWidth;
            float _Samples;

            struct MeshData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Interpolators { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.posCS = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

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

            float3 InkSpikeBW(float2 uv, float2 center)
            {
                float2 d = uv - center;
                float r = length(d);
                float2 dir = (r > 1e-6) ? (d / r) : float2(1, 0);

                // base polar
                float ang = atan2(dir.y, dir.x); // -pi..pi
                float env = smoothstep(_InnerRadius, _OuterRadius, r);

                // which spike am I on?
                // map ang to [0,1) then to spike index
                float ang01 = (ang + UNITY_PI) / (2.0 * UNITY_PI);
                float spikeIdxF = floor(ang01 * _Spikes);
                float local01 = frac(ang01 * _Spikes); // position within spike sector 0..1

                // per-spike random
                float h0 = hash11(spikeIdxF + 1.23);
                float h1 = hash11(spikeIdxF + 9.87);

                // make each spike have its own speed + phase + amplitude
                float spikeSpeed = _SpikeSpeed * lerp(0.4, 2.8, h0) * (1.0 + _SpikeFreqVar * (h1 - 0.5));
                float phase = 6.28318 * h1;

                // violent jitter (fast-changing), but stable per spike
                float t = _Time.y;
                float jitter = (hash21(float2(spikeIdxF, floor(t * _SpikeJitter))) - 0.5) * 2.0; // -1..1
                jitter *= _SpikeChaos;

                // sideways twitch (optional): wobble the local coordinate slightly
                float local = local01 + 0.15 * jitter * env;

                // layered waveform: sharp peaks + chaotic modulation
                float baseWave = abs(sin(local * UNITY_PI + phase + t * spikeSpeed + jitter));
                float micro    = abs(sin(local * UNITY_PI * 3.0 + phase * 2.0 + t * (spikeSpeed * 2.7) + jitter * 1.7));

                // combine into something spikier + more erratic
                float wave = saturate(lerp(baseWave, baseWave * micro, saturate(_SpikeChaos / 4.0)));

                // per-spike amplitude breathing (each spike gets shorter/longer differently)
                float ampBreath = lerp(1.0 - _SpikeAmpVar, 1.0 + _SpikeAmpVar, abs(sin(t * spikeSpeed * 1.3 + phase + jitter)));

                float spike = pow(wave, _SpikeSharp);

                float offset = spike * ampBreath * _SpikeLength * env;
                float2 warpedUV = uv + dir * offset;

                float3 col = tex2D(_MainTex, warpedUV).rgb;
                float lum = luminance(col);

                float bw = step(_Threshold, lum);
                bw = lerp(bw, 1.0 - bw, saturate(_Invert));

                return (bw.xxx) * _Tint.rgb;
            }

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
