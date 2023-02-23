#pragma target 5.0
#include "UnityCG.cginc"

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float4 _Color;
float g_flOutlineWidth;
float g_flCornerAdjust;

float4x4 _InverseRotation;
float4 _GlobalClipCenter;
float4 _GlobalClipExtents;

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
struct VS_INPUT
{
    float4 vPositionOs : POSITION;
    float3 vNormalOs : NORMAL;
};

struct PS_INPUT
{
    float4 vPositionOs : TEXCOORD0;
    float3 vNormalOs : TEXCOORD1;
    float3 clipPos : TEXCOORD2;
    float4 vPositionPs : SV_POSITION;
};

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
PS_INPUT MainVs(VS_INPUT i)
{
    PS_INPUT o;
    o.vPositionOs.xyzw = i.vPositionOs.xyzw;
    o.vNormalOs.xyz = i.vNormalOs.xyz;
#if UNITY_VERSION >= 540
    o.vPositionPs = UnityObjectToClipPos(i.vPositionOs.xyzw);
#else
    o.vPositionPs = UnityObjectToClipPos(i.vPositionOs.xyzw);
#endif

    o.clipPos = (float3)0;

    return o;
}

PS_INPUT MainVsExtrude(VS_INPUT i)
{
    PS_INPUT o;
    o.vPositionOs.xyzw = i.vPositionOs.xyzw;
    o.vNormalOs.xyz = i.vNormalOs.xyz;
#if UNITY_VERSION >= 540
    o.vPositionPs = UnityObjectToClipPos(i.vPositionOs.xyzw);
#else
    o.vPositionPs = UnityObjectToClipPos(i.vPositionOs.xyzw);
#endif

    float3 vNormalVs = mul((float3x3)UNITY_MATRIX_IT_MV, o.vNormalOs.xyz);
    float2 vOffsetPs = TransformViewToProjection(vNormalVs.xy);
    vOffsetPs.xy = normalize(vOffsetPs.xy);
    o.vPositionPs.xy += vOffsetPs.xy * o.vPositionPs.w * g_flOutlineWidth * 0.5;

    o.clipPos = (float3)0;

    return o;
}

PS_INPUT MainVsClip(VS_INPUT i)
{
    PS_INPUT o;
    o.vPositionOs.xyzw = i.vPositionOs.xyzw;
    o.vNormalOs.xyz = i.vNormalOs.xyz;
#if UNITY_VERSION >= 540
    o.vPositionPs = UnityObjectToClipPos(i.vPositionOs.xyzw);
#else
    o.vPositionPs = UnityObjectToClipPos(i.vPositionOs.xyzw);
#endif

    o.clipPos = mul(_InverseRotation, mul(unity_ObjectToWorld, i.vPositionOs));

    return o;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
PS_INPUT Extrude(PS_INPUT vertex)
{
    PS_INPUT extruded = vertex;

    // Offset along normal in projection space
    float3 vNormalVs = mul((float3x3)UNITY_MATRIX_IT_MV, vertex.vNormalOs.xyz);
    float2 vOffsetPs = TransformViewToProjection(vNormalVs.xy);
    vOffsetPs.xy = normalize(vOffsetPs.xy);

    // Calculate position
#if UNITY_VERSION >= 540
    extruded.vPositionPs = UnityObjectToClipPos(vertex.vPositionOs.xyzw);
#else
    extruded.vPositionPs = UnityObjectToClipPos(vertex.vPositionOs.xyzw);
#endif
    extruded.vPositionPs.xy += vOffsetPs.xy * extruded.vPositionPs.w * g_flOutlineWidth;

    return extruded;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
[maxvertexcount(18)]
void ExtrudeGs(triangle PS_INPUT inputTriangle[3], inout TriangleStream<PS_INPUT> outputStream)
{
    float3 a = normalize(inputTriangle[0].vPositionOs.xyz - inputTriangle[1].vPositionOs.xyz);
    float3 b = normalize(inputTriangle[1].vPositionOs.xyz - inputTriangle[2].vPositionOs.xyz);
    float3 c = normalize(inputTriangle[2].vPositionOs.xyz - inputTriangle[0].vPositionOs.xyz);

    inputTriangle[0].vNormalOs = inputTriangle[0].vNormalOs + normalize(a - c)  * g_flCornerAdjust;
    inputTriangle[1].vNormalOs = inputTriangle[1].vNormalOs + normalize(-a + b)  * g_flCornerAdjust;
    inputTriangle[2].vNormalOs = inputTriangle[2].vNormalOs + normalize(-b + c) * g_flCornerAdjust;

    PS_INPUT extrudedTriangle0 = Extrude(inputTriangle[0]);
    PS_INPUT extrudedTriangle1 = Extrude(inputTriangle[1]);
    PS_INPUT extrudedTriangle2 = Extrude(inputTriangle[2]);

    outputStream.Append(inputTriangle[0]);
    outputStream.Append(extrudedTriangle0);
    outputStream.Append(inputTriangle[1]);
    outputStream.Append(extrudedTriangle0);
    outputStream.Append(extrudedTriangle1);
    outputStream.Append(inputTriangle[1]);

    outputStream.Append(inputTriangle[1]);
    outputStream.Append(extrudedTriangle1);
    outputStream.Append(extrudedTriangle2);
    outputStream.Append(inputTriangle[1]);
    outputStream.Append(extrudedTriangle2);
    outputStream.Append(inputTriangle[2]);

    outputStream.Append(inputTriangle[2]);
    outputStream.Append(extrudedTriangle2);
    outputStream.Append(inputTriangle[0]);
    outputStream.Append(extrudedTriangle2);
    outputStream.Append(extrudedTriangle0);
    outputStream.Append(inputTriangle[0]);
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
fixed4 MainPs(PS_INPUT IN) : SV_Target
{
    return _Color;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
fixed4 NullPs(PS_INPUT IN) : SV_Target
{
    return float4(1.0, 0.0, 1.0, 1.0);
}
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
fixed4 MainPsClip(PS_INPUT IN) : SV_Target
{
    float3 diff = abs(IN.clipPos - _GlobalClipCenter);
    if (diff.x > _GlobalClipExtents.x || diff.y > _GlobalClipExtents.y || diff.z > _GlobalClipExtents.z)
        discard;

    return _Color;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
fixed4 NullPsClip (PS_INPUT IN) : SV_Target
{
    float3 diff = abs(IN.clipPos - _GlobalClipCenter);
    if (diff.x > _GlobalClipExtents.x || diff.y > _GlobalClipExtents.y || diff.z > _GlobalClipExtents.z)
        discard;

    return float4(1.0, 0.0, 1.0, 1.0);
}
