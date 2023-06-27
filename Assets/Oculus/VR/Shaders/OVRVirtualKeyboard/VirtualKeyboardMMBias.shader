Shader "Oculus/VirtualKeyboard/OVRVirtualKeyboardMMBias"
{
    Properties
    {
        [PerRendererData]  _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1.000000,1.000000,1.000000,1.000000)
        [HideInInspector]  _RendererColor ("RendererColor", Color) = (1.000000,1.000000,1.000000,1.000000)
        _MainTexMMBias("Mipmap Bias", Float) = 0.000000
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }
        LOD 100

        Pass
        {
            Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }

            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR0;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 color : COLOR0;
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RendererColor;
            half _MainTexMMBias;

            // Perform multiple samples per fragment to reduce aliasing
            // Source: https://developer.oculus.com/blog/common-rendering-mistakes-how-to-find-them-and-how-to-fix-them/
            fixed4 tex2DmultisampleBias(sampler2D tex, float2 uv, half bias)
            {
              float2 dx = ddx(uv) * 0.25;
              float2 dy = ddy(uv) * 0.25;

              float4 sample0 = tex2Dbias(tex, half4(uv + dx + dy, 0.0, bias));
              float4 sample1 = tex2Dbias(tex, half4(uv + dx - dy, 0.0, bias));
              float4 sample2 = tex2Dbias(tex, half4(uv - dx + dy, 0.0, bias));
              float4 sample3 = tex2Dbias(tex, half4(uv - dx - dy, 0.0, bias));

              return (sample0 + sample1 + sample2 + sample3) * 0.25;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color * _RendererColor;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2DmultisampleBias(_MainTex, i.uv, _MainTexMMBias) * i.color;
                return col;
            }
            ENDCG
        }
    }
}
