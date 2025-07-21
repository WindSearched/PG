Shader "Unlit/TextureListBlend"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Count ("Texture Count", Range(0,8)) = 0

        _Tex0 ("Texture 0", 2D) = "white" {}
        _Tex1 ("Texture 1", 2D) = "white" {}
        _Tex2 ("Texture 2", 2D) = "white" {}
        _Tex3 ("Texture 3", 2D) = "white" {}
        _Tex4 ("Texture 4", 2D) = "white" {}
        _Tex5 ("Texture 5", 2D) = "white" {}
        _Tex6 ("Texture 6", 2D) = "white" {}
        _Tex7 ("Texture 7", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            float _Count;

            sampler2D _Tex0;
            sampler2D _Tex1;
            sampler2D _Tex2;
            sampler2D _Tex3;
            sampler2D _Tex4;
            sampler2D _Tex5;
            sampler2D _Tex6;
            sampler2D _Tex7;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // alpha-composite helper: dst = dst*(1 - src.a) + src.rgb*src.a
            inline float3 AlphaComposite(float3 dst, float3 src, float srcA)
            {
                return dst * (1.0 - srcA) + src * srcA;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float count = _Count;
                float3 outRGB = _BaseColor.rgb * _BaseColor.a; // treat base color as bottom (optional)
                float outA = _BaseColor.a;

                if (count > 0.5)
                {
                    float4 s = tex2D(_Tex0, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 1.5)
                {
                    float4 s = tex2D(_Tex1, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 2.5)
                {
                    float4 s = tex2D(_Tex2, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 3.5)
                {
                    float4 s = tex2D(_Tex3, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 4.5)
                {
                    float4 s = tex2D(_Tex4, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 5.5)
                {
                    float4 s = tex2D(_Tex5, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 6.5)
                {
                    float4 s = tex2D(_Tex6, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }
                if (count > 7.5)
                {
                    float4 s = tex2D(_Tex7, i.uv);
                    outRGB = AlphaComposite(outRGB, s.rgb, s.a);
                    outA = outA + s.a * (1 - outA);
                }

                return fixed4(outRGB, outA);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
