Shader "Unlit/pulsingEffectShader"
{
    Properties
    {
		_TintColor("Tint Color", Color) = (1,1,1,1)
		_MaxTransparency("Min Transparency", Range(0.0,1.0)) = 0.0
		_MinTransparency("Max Transparency", Range(0.0,1.0)) = 1.0
		_Speed("Speed", Range(0.0,10.0)) = 1.0
    }
    SubShader
    {
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag         

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;                
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

			float4 _TintColor;
			float _MaxTransparency;
			float _MinTransparency;
			float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v)
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col;
				float alphaVal = clamp(lerp(_MaxTransparency, _MinTransparency,  (_SinTime.w) * _Speed),0.0,1.0);
				col.rgba = fixed4(_TintColor.rgb, alphaVal);				
				return col;
            }
            ENDCG
        }
    }
}
