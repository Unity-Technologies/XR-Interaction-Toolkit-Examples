Shader "XRContent/TransparentPulse"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        _PulseSpeed ("Pulse Speed", Float) = 30.0
        _PulseMaxAlpha ("Max Pulse Alpha", Range (0, 1)) = 0.5
        _PulseMinAlpha ("Min Pulse Alpha", Range (0, 1)) = 0
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100
        ZWrite On
        Offset -1,-1
        Blend One OneMinusSrcAlpha

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

            sampler2D _MainTex;

            float4 _MainTex_ST;
            fixed4 _Color;
            float _PulseSpeed;
            float _PulseMinAlpha;
            float _PulseMaxAlpha;

            float _DistanceFadeFactor;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                float pulsePhase = 0.5 * (sin(_Time * _PulseSpeed) + 1.0); // Map time to sin wave from 0 - 1
                float pulseAlpha = _PulseMinAlpha + (pulsePhase)*(_PulseMaxAlpha - _PulseMinAlpha); // Remap wave to min/max alpha range
                col *= pulseAlpha;
                return col;
            }
            ENDCG
        }
    }
}
