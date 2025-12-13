Shader "Hidden/Outline/Final"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Scale                    ("Outline Thickness (pixels)", Float) = 1.0
        _DepthThreshold           ("Base Depth Threshold", Float) = 0.2
        _NormalThreshold          ("Normal Threshold", Float) = 0.2
        _DepthNormalThresholdScale("Depth/Normal Threshold Scale", Float) = 0.5
        _OutlineColor             ("Outline Color (RGB+A strength)", Color) = (0,0,0,1)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        // We output a color that is already a blend of scene + outline,
        // but this makes it usable as an overlay if you want.
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            sampler2D _CameraDepthNormalsTexture;

            float     _Scale;
            float     _DepthThreshold;
            float     _NormalThreshold;
            float     _DepthNormalThresholdScale;
            float4    _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

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

            fixed4 frag(v2f i) : SV_Target
            {
                float4 srcColor = tex2D(_MainTex, i.uv);

                float depthBL, depthTR, depthTL, depthBR;
                float3 nBL, nTR, nTL, nBR;

                SampleDepthNormalCorners(i.uv,
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

                // View-space view direction
                float3 viewDir = float3(0.0, 0.0, 1.0);
                float nDotV = abs(dot(normalize(nBL), viewDir));
                float grazingAmount = 1.0 - nDotV;
                float depthScale = 1.0 + grazingAmount * _DepthNormalThresholdScale;

                float effectiveDepthThreshold = _DepthThreshold * depthScale;

                float depthMask  = edgeDepth  > effectiveDepthThreshold ? 1.0 : 0.0;
                float normalMask = edgeNormal > _NormalThreshold       ? 1.0 : 0.0;

                float edge = max(depthMask, normalMask);

                // Use _OutlineColor.a as an intensity for blending
                float outlineAlpha = edge * _OutlineColor.a;

                float3 finalRGB = lerp(srcColor.rgb, _OutlineColor.rgb, outlineAlpha);

                return float4(finalRGB, srcColor.a);
            }
            ENDCG
        }
    }
}
