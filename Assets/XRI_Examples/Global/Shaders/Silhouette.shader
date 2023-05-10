//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Used to show the outline of the object
//
// Used with permission (3/20/17)
// Copyright (C) 2017 Unity Technologies ApS - Fix for hard edges
//=============================================================================

Shader "XRContent/Outline"
{
    //-------------------------------------------------------------------------------------------------------------------------------------------------------------
    Properties
    {
        _Color( "Color", Color ) = ( .5, .5, .5, 1 )
        g_flOutlineWidth( "Outline width", Range ( .001, 0.03 ) ) = .005
        g_flCornerAdjust( "Corner Adjustment", Range(0, 2)) = .5
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------
    CGINCLUDE
        #include "Silhouette.cginc"
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Outline" "Queue" = "Overlay+5000" }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Render the object with stencil=1 to mask out the part that isn't the silhouette
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "Always" }
            ColorMask 0
            Cull Off
            ZWrite Off
            ZTest Off
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            CGPROGRAM
                #pragma vertex MainVs
                #pragma fragment NullPs
            ENDCG
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Render the outline by extruding along vertex normals and using the stencil mask previously rendered. Only render depth, so that the final pass executes
        // once per fragment (otherwise alpha blending will look bad).
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "Always" }
            Cull Off
            ZWrite Off
            ZTest Off
            Stencil
            {
                Ref 1
                Comp notequal
                Pass keep
                Fail keep
            }

            CGPROGRAM
                #pragma vertex MainVs
                #pragma geometry ExtrudeGs
                #pragma fragment MainPs
            ENDCG
        }

        Pass
        {
            Tags{ "LightMode" = "Always" }
            ColorMask 0
            Cull Off
            ZWrite Off
            ZTest Off
            Stencil
            {
                Ref 0
                Comp always
                Pass replace
            }

            CGPROGRAM
                #pragma vertex MainVs
                #pragma fragment NullPs
            ENDCG
        }
    }
}
