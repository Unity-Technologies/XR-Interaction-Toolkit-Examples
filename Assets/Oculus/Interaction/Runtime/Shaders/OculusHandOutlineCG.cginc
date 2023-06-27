/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#pragma multi_compile_local __ CONFIDENCE

#pragma prefer_hlslcc gles
#pragma exclude_renderers d3d11_9x
#pragma target 2.0

//

struct OutlineVertexInput {
float4 vertex : POSITION;
  float3 normal : NORMAL;
  float4 texcoord : TEXCOORD0;
  float4 texcoord1 : TEXCOORD1;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVertexOutput {
  float4 vertex : SV_POSITION;
  half4 color : TEXCOORD1;
  float3 worldPos : TEXCOORD2;
  float4 texcoord1 : TEXCOORD3;
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

OutlineVertexOutput outlineVertex(OutlineVertexInput v) {
  OutlineVertexOutput o;
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  UNITY_TRANSFER_INSTANCE_ID(v, o);

  o.worldPos = mul(unity_ObjectToWorld, v.vertex);
  v.vertex.xyz += v.normal * _OutlineWidth;
  o.vertex = UnityObjectToClipPos(v.vertex);
  o.texcoord1 = v.texcoord1;
  half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);
  o.color.rgb = _OutlineColor;
  o.color.a = saturate(maskPixelColor.a + _WristFade) * _OutlineColor.a *
      _OutlineOpacity;
  return o;
}

#include "GlowFunctions.cginc"

void getFingerStrength(out float fingerStrength[5]) {
  float strengthValuesUniforms[5] = {
    _ThumbGlowValue,
    _IndexGlowValue,
    _MiddleGlowValue,
    _RingGlowValue,
    _PinkyGlowValue
  };
  fingerStrength = strengthValuesUniforms;
}

void getOutlineFingerLines(out float4 lines[5]) {
  float4 _lines[5] = {
    _PalmThumbLine,
    _PalmIndexLine,
    _PalmMiddleLine,
    _PalmRingLine,
    _PalmPinkyLine
};
  lines = _lines;
}

float4 getOutlineFingerLinesByIndex(int index) {
  if(index == 0) { return  _PalmThumbLine; }
  if(index == 1) { return  _PalmIndexLine; }
  if(index == 2) { return  _PalmMiddleLine; }
  if(index == 3) { return  _PalmRingLine; }
  if(index == 4) { return  _PalmPinkyLine; }
  return float4(0.0,0.0,0.0,0.0);
}

half4 applyGlow(int glowType, float3 color, float alpha, float2 texCoord, float3 worldPosition) {
  if (glowType == 31 || glowType == 32) {
    float4 gradientLine = getOutlineFingerLinesByIndex(_FingerGlowIndex);
    float2 gradientRadius = getPalmFingerRadiusByIndex(_FingerGlowIndex);
    bool useGlow;
    float2 fingerGradient = movingFingerGradient(texCoord, gradientLine, gradientRadius, _GlowParameter, _GlowMaxLength, useGlow);
    if (useGlow) {
      float param = 1.0 - fingerGradient.y;
      float glowValue = saturate(param * param * step(0.0, param));
      return half4(color + _GlowColor * glowValue, alpha);
    }else {
      return half4(color, alpha);
    }
  }
  if (glowType == 28 || glowType == 29) {
    float fingerStrength[5];
    getFingerStrength(fingerStrength);
    float4 lines[5];
    getOutlineFingerLines(lines);
    float2 fingerRadius[5];
    getPalmFingerRadius(fingerRadius);
    float glowValue = fingerLineGlow(texCoord, fingerStrength, 30, lines, fingerRadius);
    return half4(color + _GlowColor * glowValue, alpha);
  }
  else if (glowType == 16 || glowType == 18) {
    float gradient = invertedSphereGradient(_GlowPosition, worldPosition, _GlowMaxLength);
    float3 glowColor = _GlowColor * gradient * _GlowParameter;
    return float4(saturate(color + glowColor), alpha);
  }
  else if (glowType == 12 || glowType == 15) {
    float gradient = movingSphereGradient(_GlowPosition, worldPosition, _GlowMaxLength, _GlowParameter, 1.5);
    float3 glowColor = _GlowColor * gradient;
    return float4(saturate(color + glowColor), alpha);
  } 
  else {
    return float4(color, alpha);
  }
}

half4 outlineFragment(OutlineVertexOutput i) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(i);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  half4 fragColor;

  if (_GenerateGlow == 1) {
    fragColor = applyGlow(_GlowType, i.color.rgb, i.color.a, i.texcoord1, i.worldPos);
  } else {
    fragColor = i.color;
  }
  return fragColor;
}