Shader "Oculus/Cubemap Blit" {
    Properties{
        _MainTex("Base (RGB) Trans (A)", CUBE) = "white" {}
        _face("Face", Int) = 0
        _premultiply("Cubemap Blit", Int) = 0
    }
    SubShader{
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Pass{
            ZWrite Off
            ColorMask RGBA

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    half3 cubedir : TEXCOORD0;
                };

                samplerCUBE _MainTex;
                float4 _MainTex_ST;
                int _face;
                int _premultiply;

                v2f vert (appdata_t va)
                {
                    v2f vo;
                    vo.vertex = UnityObjectToClipPos(va.vertex);

                    //Face bases, assuming +x, -x, +z, -z, +y, -y with origin at bottom-left.
                    float3 o[6] = { {1.0, -1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, 1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, -1.0, 1.0}, { 1.0, -1.0, -1.0} };
                    float3 u[6] = { {0.0,  0.0, -1.0}, { 0.0,  0.0,  1.0}, { 1.0, 0.0,  0.0}, { 1.0,  0.0,  0.0}, { 1.0,  0.0, 0.0}, {-1.0,  0.0,  0.0} };
                    float3 v[6] = { {0.0,  1.0,  0.0}, { 0.0,  1.0,  0.0}, { 0.0, 0.0, -1.0}, { 0.0,  0.0,  1.0}, { 0.0,  1.0, 0.0}, { 0.0,  1.0,  0.0} };

                    //Map the input UV to the corresponding face basis.
                    vo.cubedir = o[_face] + 2.0*va.texcoord.x * u[_face] + 2.0*(1.0 - va.texcoord.y) * v[_face];

                    return vo;
                }

                fixed4 frag (v2f vi) : COLOR
                {
                    fixed4 col = texCUBE(_MainTex, vi.cubedir);

                    if (_premultiply)
                        col.rgb *= col.a;

                    return col;
                }
            ENDCG
        }
    }
}
