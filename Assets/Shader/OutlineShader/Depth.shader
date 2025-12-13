Shader "Hidden/Outline/Step1_DepthOnly"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Scale          ("Outline Thickness (pixels)", Float) = 1.0
        _DepthThreshold ("Depth Threshold", Float) = 0.2
        _OutlineColor   ("Outline Color", Color) = (0,0,0,1)
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
            float4    _MainTex_TexelSize; // x = 1/width, y = 1/height
            sampler2D _CameraDepthNormalsTexture;

            float     _Scale;
            float     _DepthThreshold;
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

            float ComputeDepthEdge(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy;
                float  halfScale = _Scale * 0.5;

                // Diagonal sample offsets (Roberts cross)
                float2 offsetBL = texel * float2(-halfScale, -halfScale);
                float2 offsetTR = texel * float2( halfScale,  halfScale);
                float2 offsetTL = texel * float2(-halfScale,  halfScale);
                float2 offsetBR = texel * float2( halfScale, -halfScale);

                float4 dnBL = tex2D(_CameraDepthNormalsTexture, uv + offsetBL);
                float4 dnTR = tex2D(_CameraDepthNormalsTexture, uv + offsetTR);
                float4 dnTL = tex2D(_CameraDepthNormalsTexture, uv + offsetTL);
                float4 dnBR = tex2D(_CameraDepthNormalsTexture, uv + offsetBR);

                float depthBL, depthTR, depthTL, depthBR;
                float3 dummyN;

                DecodeDepthNormal(dnBL, depthBL, dummyN);
                DecodeDepthNormal(dnTR, depthTR, dummyN);
                DecodeDepthNormal(dnTL, depthTL, dummyN);
                DecodeDepthNormal(dnBR, depthBR, dummyN);

                // Roberts cross on depth
                float diff1 = depthTR - depthBL;
                float diff2 = depthBR - depthTL;
                float edgeDepth = sqrt(diff1 * diff1 + diff2 * diff2);

                return edgeDepth;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float edgeDepth = ComputeDepthEdge(i.uv);

                float edge = edgeDepth > _DepthThreshold ? 1.0 : 0.0;

                return float4(_OutlineColor.rgb * edge, 1.0);
            }
            ENDCG
        }
    }
}
