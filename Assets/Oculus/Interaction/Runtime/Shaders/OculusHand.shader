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

Shader "Interaction/OculusHand"
{
    Properties
    {
        [Header(General)]
        _ColorTop("Color Top", Color) = (0.1960784, 0.2039215, 0.2117647, 1)
        _ColorBottom("Color Bottom", Color) = (0.1215686, 0.1254902, 0.1294117, 1)
        _Opacity("Opacity", Range(0 , 1)) = 0.8

        [Header(Fresnel)]
        _FresnelPower("FresnelPower", Range(0 , 5)) = 0.16

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.5377358,0.5377358,0.5377358,1)
        _OutlineJointColor("Outline Joint Error Color", Color) = (1,0,0,1)
        _OutlineWidth("Outline Width", Range(0 , 0.005)) = 0.00134
        _OutlineOpacity("Outline Opacity", Range(0 , 1)) = 0.4

        [Header(Wrist)]
        _WristFade("Wrist Fade", Range(0 , 1)) = 0.5

        [Header(Finger Glow)]
        _FingerGlowMask("Finger Glow Mask", 2D) = "white" {}

        [HideInInspector] _texcoord("", 2D) = "white" {}
        _GlowColor("GlowColor", Color) = (1,1,1,1)
        [HideInInspector] _ThumbGlowValue("", Float) = 0
        [HideInInspector] _IndexGlowValue("", Float) = 0
        [HideInInspector] _MiddleGlowValue("", Float) = 0
        [HideInInspector] _RingGlowValue("", Float) = 0
        [HideInInspector] _PinkyGlowValue("", Float) = 0
        [HideInInspector] _GenerateGlow("", Int) = 0
        [HideInInspector] _OcclusionEnabled("", Int) = 0 
    }

    CGINCLUDE
    #include "UnityCG.cginc"

#pragma target 2.0

    // CBUFFER named UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost of each drawcall.
    CBUFFER_START(UnityPerMaterial)
    // General
    uniform float4 _ColorTop;
    uniform float4 _ColorBottom;
    uniform float _Opacity;
    uniform float _FresnelPower;

    // Outline
    uniform float4 _OutlineColor;
    uniform half4 _OutlineJointColor;
    uniform float _OutlineWidth;
    uniform float _OutlineOpacity;

    // Wrist
    uniform half _WristFade; 

    // Finger Glow
    uniform sampler2D _FingerGlowMask;

    uniform float _ThumbGlowValue;
    uniform float _IndexGlowValue;
    uniform float _MiddleGlowValue;
    uniform float _RingGlowValue;
    uniform float _PinkyGlowValue;

    uniform int _FingerGlowIndex;
    
    uniform int _GenerateGlow;
    uniform float3 _GlowColor;
    
    uniform float3 _GlowPosition;
    uniform float _GlowParameter;
    uniform float _GlowMaxLength;
    uniform int _GlowType;

    //Finger Masks
    //Finger Tip To Knuckles
    uniform float4 _ThumbLine;
    uniform float4 _IndexLine;
    uniform float4 _MiddleLine;
    uniform float4 _RingLine;
    uniform float4 _PinkyLine;
    //Finger Tip To Palm
    uniform float4 _PalmThumbLine;
    uniform float4 _PalmIndexLine;
    uniform float4 _PalmMiddleLine;
    uniform float4 _PalmRingLine;
    uniform float4 _PalmPinkyLine;

    CBUFFER_END
    ENDCG

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"
        }

        Cull Back
        AlphaToMask Off

        Pass
        {
            Name "Depth"
            ZWrite On
            ColorMask 0
        }

        Pass
        {
            Name "HandOutline"
            Tags
            {
                "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"
            }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex outlineVertex
#pragma fragment outlineFragment
#include "OculusHandOutlineCG.cginc"
            ENDCG
        }

        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "10.0.0" }
            Name "HandOutlineURP"
            Tags
            {
                "LightMode" = "UniversalForwardOnly" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline"
            }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex outlineVertex
#pragma fragment outlineFragment
#include "OculusHandOutlineCG.cginc"
            ENDCG
        }

        Pass
        {
            Name "HandFill"
            Tags
            {
                "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Stencil 
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex baseVertex
#pragma fragment baseFragment
#include "OculusHandFillCG.cginc"
            ENDCG
        }

        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "10.0.0" }
            Name "HandFillURP"
            Tags
            {
                "LightMode" = "UniversalForward" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex baseVertex
#pragma fragment baseFragment
#include "OculusHandFillCG.cginc"
            ENDCG
        }
    }
}