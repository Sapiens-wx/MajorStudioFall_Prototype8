Shader "Hidden/Outline/Step2_NormalOnly"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Scale           ("Outline Thickness (pixels)", Float) = 1.0
        _NormalThreshold ("Normal Threshold", Float) = 0.2
        _OutlineColor    ("Outline Color", Color) = (0,0,0,1)
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            sampler2D _CameraDepthNormalsTexture;

            float     _Scale;
            float     _NormalThreshold;
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

            float ComputeNormalEdge(float2 uv)
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

                float depthDummy;
                float3 nBL, nTR, nTL, nBR;

                DecodeDepthNormal(dnBL, depthDummy, nBL);
                DecodeDepthNormal(dnTR, depthDummy, nTR);
                DecodeDepthNormal(dnTL, depthDummy, nTL);
                DecodeDepthNormal(dnBR, depthDummy, nBR);

                // Roberts cross on normals
                float3 diff1 = nTR - nBL;
                float3 diff2 = nBR - nTL;
                float edgeNormal = sqrt(dot(diff1, diff1) + dot(diff2, diff2));

                return edgeNormal;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float edgeNormal = ComputeNormalEdge(i.uv);

                float edge = edgeNormal > _NormalThreshold ? 1.0 : 0.0;

                return float4(_OutlineColor.rgb * edge, 1.0);
            }
            ENDCG
        }
    }
}
