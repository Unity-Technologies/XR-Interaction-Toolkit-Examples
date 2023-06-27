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

#if UNITY_2020_2_OR_NEWER
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters; // AssetImporters namespace became non-experimental in 2020.2.0f1

/**
 * Asset processor that will read custom PBR properties from a Phong material description as an asset file is
 * imported. These custom properties are applied to standard lit shader properties on the material that is being
 * pre processed.
 * This is required since the Phong shader is the only one which the unity built-in FBXMaterialDescriptionPreprocessor
 * will convert to a "Standard Lit" shader when parsing an FBX file.
 * The advantage of performing this processing here, rather than leveraging custom materials in the unity package, is
 * that the Asset Import step will create a suitable material for either the URP or Builtin render pipeline.
 */
public class PhongPbrMaterialDescriptionPostprocessor : AssetPostprocessor
{
    private const uint Version = 1;

    // Required to be executed after every builtin unity processor to ensure the smoothness property is set properly
    private const int Order = 11;

    // Smoothness source (Albedo or Metallic map alpha)
    private static readonly int SmoothnessTextureChannelPropId = Shader.PropertyToID("_SmoothnessTextureChannel");
    private static readonly string SmoothnessTextureChannelCustomProp = "OVR_SmoothnessTextureChannel";

    // Smoothness map and scalar properties:
    private static readonly int SmoothnessValueBrpPropId = Shader.PropertyToID("_Glossiness");
    private static readonly int SmoothnessValueUrpPropId = Shader.PropertyToID("_Smoothness");
    private static readonly int SmoothnessScaleValuePropId = Shader.PropertyToID("_GlossMapScale");
    private static readonly string SmoothnessFactorCustomProp = "OVR_SmoothnessFactor";

    // Metallic map
    private static readonly int MetallicTexturePropId = Shader.PropertyToID("_MetallicGlossMap");
    private static readonly int MetallicValuePropId = Shader.PropertyToID("_Metallic");
    private static readonly string MetallicFactorPhongProp = "ReflectionFactor";

    // Parallax
    private static readonly int ParallaxTexturePropId = Shader.PropertyToID("_ParallaxMap");
    private static readonly int ParallaxStrengthPropId = Shader.PropertyToID("_Parallax");
    private static readonly string ParallaxCustomProp = "OVR_HeightMap";
    private static readonly string ParallaxStrengthCustomProp = "OVR_HeightFactor";

    // Occlusion
    private static readonly int OcclusionTexturePropId = Shader.PropertyToID("_OcclusionMap");
    private static readonly int OcclusionStrengthPropId = Shader.PropertyToID("_OcclusionStrength");
    private static readonly string OcclusionMapCustomProp = "OVR_OcclusionMap";
    private static readonly string OcclusionStrengthCustomProp = "OVR_OcclusionFactor";

    // Detail
    private static readonly int DetailTexturePropId = Shader.PropertyToID("_DetailMask");
    private static readonly string DetailMapCustomProp = "OVR_DetailMap";

    public override uint GetVersion()
    {
        return Version;
    }

    public override int GetPostprocessOrder()
    {
        return Order;
    }

    public void OnPreprocessMaterialDescription(MaterialDescription description, Material material,
        AnimationClip[] clips)
    {
        // Asset Processor supports only URP and Builtin standard shaders.
        bool isBirpStandardLit = material.shader.name == "Standard";
        bool isUrpStandardLit = material.shader.name == "Universal Render Pipeline/Lit";
        if (isBirpStandardLit || isUrpStandardLit)
        {
            var lowerCaseExtension = Path.GetExtension(assetPath).ToLower();
            if (lowerCaseExtension == ".fbx" || lowerCaseExtension == ".dae" || lowerCaseExtension == ".obj" ||
                lowerCaseExtension == ".blend" || lowerCaseExtension == ".mb" || lowerCaseExtension == ".ma" ||
                lowerCaseExtension == ".max")
            {
                ReadOvrPbrProperties(description, material, isUrpStandardLit);
            }
        }
    }

    private void ReadOvrPbrProperties(MaterialDescription description, Material material, bool isUrp)
    {
        if (!HasOvrProperty(description))
        {
            // Only overwrite values if an OVR property was detected. This
            // should prevent accidental conflicts with any other user defined AssetPostprocessor's
            // since we are reading from the Phong Reflection property, not a custom user defined
            // property.
            return;
        }

        Vector4 vectorProperty;
        float floatProperty;
        TexturePropertyDescription textureProperty;

        if (description.TryGetProperty(SmoothnessTextureChannelCustomProp, out floatProperty))
        {
            var isAlbedoAlphaChannel = floatProperty > float.Epsilon;
            material.SetFloat(SmoothnessTextureChannelPropId, isAlbedoAlphaChannel ? 1.0f : 0.0f);

            var isOpaque = (material.HasProperty("_Mode") && material.GetFloat("_Mode") ==  0.0) ||
                           (material.HasProperty("_Surface") && material.GetFloat("_Surface") ==  0.0);
            if (isAlbedoAlphaChannel && isOpaque)
            {
                material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            }
        }

        if (description.TryGetProperty(SmoothnessFactorCustomProp, out floatProperty))
        {
            // Note: setting both value and scale value does not seem to cause any problems.

            // For smoothness texture channel = metallic alpha
            material.SetFloat(SmoothnessValueBrpPropId, floatProperty);
            // For smoothness texture channel = albedo alpha
            material.SetFloat(SmoothnessScaleValuePropId, floatProperty);
            // For smoothness on URP metallic and albedo alpha
            material.SetFloat(SmoothnessValueUrpPropId, floatProperty);
        }

        if (description.TryGetProperty(ParallaxStrengthCustomProp, out floatProperty))
        {
            material.SetFloat(ParallaxStrengthPropId, floatProperty);
        }
        if (description.TryGetProperty(OcclusionStrengthCustomProp, out floatProperty))
        {
            material.SetFloat(OcclusionStrengthPropId, floatProperty);
        }

        if (description.TryGetProperty("DiffuseColor", out vectorProperty))
        {
            // URP uses _BaseColor; BIRP uses _Color
            var materialColorProp = isUrp ? "_BaseColor" : "_Color";
            var diffuseColor = material.GetColor(materialColorProp);

            if (!description.TryGetProperty("DiffuseFactor", out float diffuseFactorProperty))
            {
                diffuseFactorProperty = 1.0f;
            }
            diffuseColor.r = vectorProperty.x * diffuseFactorProperty;
            diffuseColor.g = vectorProperty.y * diffuseFactorProperty;
            diffuseColor.b = vectorProperty.z * diffuseFactorProperty;

            // Re-assign the diffuse color to work around incorrect color space behavior in the regular FBX importer:
            // in gamma mode, it seems to double-apply the gamma correction, so counteract one of the conversions.
            if (PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                diffuseColor = new Color(
                    Mathf.GammaToLinearSpace(diffuseColor.r),
                    Mathf.GammaToLinearSpace(diffuseColor.g),
                    Mathf.GammaToLinearSpace(diffuseColor.b));
            }
            material.SetColor(materialColorProp, diffuseColor);
        }

        if (description.TryGetProperty(MetallicFactorPhongProp, out float metallic))
        {
            material.SetFloat(MetallicValuePropId, metallic);
        }
        if (description.TryGetProperty(MetallicFactorPhongProp, out textureProperty))
        {
            material.SetTexture(MetallicTexturePropId, textureProperty.texture);
            material.SetTextureOffset(MetallicTexturePropId, textureProperty.offset);
            material.SetTextureScale(MetallicTexturePropId, textureProperty.scale);

            // BIRP uses _METALLICGLOSSMAP ; URP uses _METALLICSPECGLOSSMAP
            material.EnableKeyword(isUrp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
        }
        if (description.TryGetProperty(ParallaxCustomProp, out textureProperty))
        {
            material.SetTexture(ParallaxTexturePropId, textureProperty.texture);
            material.SetTextureOffset(ParallaxTexturePropId, textureProperty.offset);
            material.SetTextureScale(ParallaxTexturePropId, textureProperty.scale);
            material.EnableKeyword("_PARALLAXMAP");
        }
        if (description.TryGetProperty(OcclusionMapCustomProp, out textureProperty))
        {
            material.SetTexture(OcclusionTexturePropId, textureProperty.texture);
            material.SetTextureOffset(OcclusionTexturePropId, textureProperty.offset);
            material.SetTextureScale(OcclusionTexturePropId, textureProperty.scale);
        }
        if (description.TryGetProperty(DetailMapCustomProp, out textureProperty))
        {
            material.SetTexture(DetailTexturePropId, textureProperty.texture);
            material.SetTextureOffset(DetailTexturePropId, textureProperty.offset);
            material.SetTextureScale(DetailTexturePropId, textureProperty.scale);
        }


        // Apply emissive color as well as texture map, which standard doesn't do.
        if (
            description.TryGetProperty("EmissiveColor", out vectorProperty) &&
            vectorProperty.magnitude > vectorProperty.w
            || description.HasAnimationCurve("EmissiveColor.x"))
        {
            if (description.TryGetProperty("EmissiveFactor", out floatProperty))
                vectorProperty *= floatProperty;

            Color emissiveColor = vectorProperty;
            if (PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                // Re-assign the color to work around incorrect color space behavior in the regular FBX importer:
                // in gamma mode, it seems to double-apply the gamma correction, so counteract one of the conversions.
                emissiveColor = new Color(
                    Mathf.GammaToLinearSpace(emissiveColor.r),
                    Mathf.GammaToLinearSpace(emissiveColor.g),
                    Mathf.GammaToLinearSpace(emissiveColor.b));
            }
            material.SetColor("_EmissionColor", emissiveColor);
            if (floatProperty > 0.0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
    }

    private static bool HasOvrProperty(MaterialDescription description)
    {
        var propertyNames = new List<string>();
        description.GetFloatPropertyNames(propertyNames);
        foreach (var propertyName in propertyNames)
        {
            if (propertyName.StartsWith("OVR_"))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
