Shader "Hidden/RadialThresholdBlur_BuiltIn"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,1)) = 0.4
        _Tint ("Tint", Color) = (1,1,1,1)
        _Flip ("Flip (0/1)", Range(0,1)) = 0
        _BlurWidth ("Blur Width", Range(0,0.5)) = 0.13
        _Samples ("Samples", Range(1,64)) = 20
        _Center ("Center (xy)", Vector) = (0.5, 0.5, 0, 0) // normalized screen UV
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
            float4 _Tint;
            float _Flip;
            float _BlurWidth;
            float _Samples;
            float4 _Center;

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

            float3 BlackWhite(float3 color)
            {
                float brightness = (color.r + color.g + color.b) / 3.0;

                float t = step(_Threshold, brightness); // 0 or 1

                // flip if _Flip > 0.5
                if (_Flip > 0.5) t = 1.0 - t;

                return (t.xxx) * _Tint.rgb;
            }

            float4 frag (Interpolators i) : SV_Target
            {
                float2 center = _Center.xy;

                float2 uv = i.uv;
                float2 d = uv - center;

                // match the shadertoy idea:
                // blurStart = 1.0 - blurWidth
                float blurStart = 1.0 - _BlurWidth;
                float precompute = _BlurWidth / max(1.0, (_Samples - 1.0));

                float3 acc = 0.0;

                const int MAX_SAMPLES = 64;
                int ns = (int)round(saturate(_Samples / 64.0) * 64.0);
                ns = max(1, ns);

                for (int s = 0; s < MAX_SAMPLES; s++)
                {
                    if (s >= ns) break;

                    float scale = blurStart + (float)s * precompute;
                    float2 suv = d * scale + center;

                    float3 col = tex2D(_MainTex, suv).rgb;
                    acc += BlackWhite(col);
                }

                acc /= (float)ns;

                return float4(acc, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
