Shader "Custom/ManualUVTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UVRect ("UV Rect (x,y,width,height)", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST; // 不用也行
            float4 _UVRect;      // 手动 UV（x,y,w,h）

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // 原始 UV（我们将用它手动改写）
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // 手动计算 UV
                o.uv = v.uv * _UVRect.zw + _UVRect.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
