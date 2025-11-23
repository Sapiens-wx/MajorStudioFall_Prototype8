Shader "Unlit/Checker"
{
    Properties
    {
        scale ("scale", Float) = 1
        col1 ("color 1", Color) = (1,1,1,1)
        col2 ("color 2", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            fixed4 col1, col2;
            float scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.uv*=scale;
                i.uv=frac(i.uv);
                half b=step(0.5, i.uv.x)+step(0.5, i.uv.y);
                b=step(0.8, b)*step(b, 1.2);
                fixed4 col=b*col1+(1-b)*col2;
                return col;
            }
            ENDCG
        }
    }
}
