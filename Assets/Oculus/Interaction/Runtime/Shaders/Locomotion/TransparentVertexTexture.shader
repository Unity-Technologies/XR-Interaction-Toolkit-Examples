Shader "Unlit/TransparentVertexTexture"
{
    Properties
    {
        _Color("Color",COLOR) = (1,1,1,1)
        _FadeLimit("Fade Limit",VECTOR) = (0,0,1,1)
        _FadeSign("Fade Sign",Range(-1,1)) = 1
        _Fade("Fade",Range(0,1)) = 1
        _Highlight("Highlight Strength",Range(0,1)) = 0
        _HighlightColor("Highlight Color", COLOR) = (1,1,1,1)

        _OffsetFactor("Offset Factor", float) = 0
        _OffsetUnits("Offset Units", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Offset[_OffsetFactor],[_OffsetUnits]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            
            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 vertex : SV_POSITION;
                float4 color : TEXCOORD1;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _Color;
            half4 _FadeLimit;
            half _FadeSign;
            half _Fade;
            half _Highlight;
            half4 _HighlightColor;

            float _OffsetFactor;
            float _OffsetUnits;

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = lerp(v.color * _Color, _HighlightColor, _Highlight);
                return o;
            }

            half4 frag(VertexOutput i) : SV_Target
            {
                half4 color = i.color;
                half lowLimit = smoothstep(_FadeLimit.x, _FadeLimit.y, i.uv.y);
                half highLimit = smoothstep(_FadeLimit.z, _FadeLimit.w, i.uv.y);
                color.a *= saturate(lowLimit - _FadeSign * highLimit) * _Fade;

                return color;
            }
            ENDCG
        }
    }
}
