Shader "Custom/ImpactUnlit"
{
    Properties
    {
        _BaseColor   ("Base Color", Color)   = (1, 1, 1, 1)
        _ImpactColor ("Impact Color", Color) = (1, 0, 0, 1)

        // These will mostly be driven by script, but exposed for debug
        _HitPower     ("Hit Power", Float)        = 0
        _HitStartTime ("Hit Start Time", Float)   = 0
        _HitPointOS   ("Hit Point (Object)", Vector) = (0, 0, 0, 0)
        _ImpactRadius ("Impact Radius", Float)    = 0.2
        _DecayTime    ("Decay Time", Float)       = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _ImpactColor;

            float _HitPower;
            float _HitStartTime;
            float3 _HitPointOS;
            float _ImpactRadius;
            float _DecayTime;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(float4(v.positionOS, 1.0));
                o.positionHCS = TransformWorldToHClip(worldPos.xyz);
                o.positionOS  = v.positionOS;
                return o;
            }

            float4 frag(Varyings i) : SV_TARGET
            {
                // Distance from this pixel’s object-space position to the impact point
                float dist = distance(i.positionOS, _HitPointOS);
                float radial = 0.0;

                if (_ImpactRadius > 0.0001)
                {
                    radial = saturate(1.0 - dist / _ImpactRadius); // 1 at center, 0 at edge
                }

                // Time since hit
                float timeSinceHit = max(0.0, _Time.y - _HitStartTime);

                // Protect against zero/very small decay time
                float decayT = max(_DecayTime, 0.0001);
                float timeFactor = saturate(1.0 - timeSinceHit / decayT);

                // Final impact strength
                float hitStrength = _HitPower * radial * timeFactor;

                float3 baseCol   = _BaseColor.rgb;
                float3 impactCol = _ImpactColor.rgb;

                float3 finalCol = lerp(baseCol, impactCol, hitStrength);

                return float4(finalCol, 1.0);
            }

            ENDHLSL
        }
    }
}
