Shader "Hidden/RadialInkSpikeBW_BuiltIn"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        _Threshold    ("Luminance Threshold", Range(0,1)) = 0.5
        _Invert       ("Invert (0/1)", Range(0,1)) = 0

        _Center       ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)

        _Spikes       ("Spike Count", Range(2, 256)) = 64
        _SpikeLength  ("Spike Length (UV)", Range(0, 0.25)) = 0.08
        _SpikeSharp   ("Spike Sharpness", Range(1, 16)) = 6
        _SpikeSpeed   ("Spike Speed", Range(0, 10)) = 1.5

        _InnerRadius  ("Warp Start Radius", Range(0, 1)) = 0.05
        _OuterRadius  ("Warp End Radius", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _Threshold;
            float _Invert;

            float4 _Center;

            float _Spikes;
            float _SpikeLength;
            float _SpikeSharp;
            float _SpikeSpeed;

            float _InnerRadius;
            float _OuterRadius;

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
                o.uv = v.uv;
                return o;
            }

            float luminance(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            fixed4 frag (Interpolators i) : SV_Target
            {
                float2 uv = i.uv;

                // Radial coordinates from center
                float2 center = _Center.xy;
                float2 d = uv - center;

                // Avoid NaNs at exact center
                float r = length(d);
                float2 dir = (r > 1e-6) ? (d / r) : float2(1, 0);

                // Angle controls spikes
                float ang = atan2(dir.y, dir.x); // -pi..pi

                // Envelope: where the warp happens (0 inside inner radius, 1 by outer radius)
                float env = smoothstep(_InnerRadius, _OuterRadius, r);

                // Spiky waveform (0..1), sharpened
                float wave = abs(sin(ang * _Spikes + _Time.y * _SpikeSpeed));
                float spike = pow(wave, _SpikeSharp);

                // Outward UV warp along radial direction
                float offset = spike * _SpikeLength * env;
                float2 warpedUV = uv + dir * offset;

                // Sample + luminance + threshold to pure BW
                float3 col = tex2D(_MainTex, warpedUV).rgb;
                float lum = luminance(col);

                float bw = step(_Threshold, lum);
                bw = lerp(bw, 1.0 - bw, saturate(_Invert));

                return fixed4(bw.xxx, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
