using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    public class TextureBlenderURPLit : TextureBlender
    {
        static Color NeutralNormalMap = new Color(.5f, .5f, 1f);

        private enum Prop
        {
            doColor,
            doSpecular,
            doMetallic,
            doEmission,
            doBump,
            doNone,
        }

        private enum WorkflowMode
        {
            unknown,
            metallic,
            specular,
        }

        private enum SmoothnessTextureChannel
        {
            unknown,
            albedo,
            metallicSpecular,
        }

        // This is used to cache the non texture property values. If all non-texutre property values are the same for a property for all source textures
        // then the source value will be re-used
        TextureBlenderMaterialPropertyCacheHelper sourceMaterialPropertyCache = new TextureBlenderMaterialPropertyCacheHelper();

        WorkflowMode m_workflowMode = WorkflowMode.unknown;

        SmoothnessTextureChannel m_smoothnessTextureChannel = SmoothnessTextureChannel.unknown;

        // These are cached values read in OnBeforeTintTexture and used when blending pixels.
        Color m_tintColor;

        float m_smoothness; // shared by both metallic maps and spec maps
        
        Color m_specColor;   // Used if no spec map
        bool m_hasSpecGlossMap;

        float m_metallic;  // Used if no metallic map
        bool m_hasMetallicGlossMap;

        float m_bumpScale;
        
        bool m_shaderDoesEmission;
        Color m_emissionColor;

        // This just makes things more efficient so we arn't doing a string comparison for each pixel.
        Prop propertyToDo = Prop.doNone;

        // These are the property values that will be assigned to the result material if
        // generating an atlas for those properties. 
        Color m_generatingTintedAtlaColor = Color.white;
        float m_generatingTintedAtlasMetallic = 0f;
        Color m_generatingTintedAtlaSpecular = Color.black;
        float m_generatingTintedAtlasMetallic_smoothness = 1f;
        float m_generatingTintedAtlasSpecular_somoothness = 1f;
        float m_generatingTintedAtlaBumpScale = 1f;
        Color m_generatingTintedAtlaEmission = Color.white;

        // These are the default property values that will be assigned to the result materials if 
        // none of the source materials have a value for these properties.
        Color m_notGeneratingAtlasDefaultColor = Color.white;
        float m_notGeneratingAtlasDefaultMetallic = 0;
        float m_notGeneratingAtlasDefaultSmoothness_MetallicWorkflow = 0f;
        float m_notGeneratingAtlasDefaultSmoothness_SpecularWorkflow = 1f;
        Color m_notGeneratingAtlasDefaultSpecularColor = new Color(.2f,.2f,.2f,1f);
        Color m_notGeneratingAtlasDefaultEmisionColor = Color.black;

        public bool DoesShaderNameMatch(string shaderName)
		{
			return  shaderName.Equals("Universal Render Pipeline/Lit") ||
                    shaderName.Equals("Universal Render Pipeline/Simple Lit") ||
                    shaderName.Equals("Universal Render Pipeline/Baked Lit");
		}

        private WorkflowMode _MapFloatToWorkflowMode(float workflowMode)
        {
            if (workflowMode == 0f)
            {
                return WorkflowMode.specular;
            }
            else
            {
                return WorkflowMode.metallic;
            }
        }

        private float _MapWorkflowModeToFloat(WorkflowMode workflowMode)
        {
            if (workflowMode == WorkflowMode.specular)
            {
                return 0f;
            } else
            {
                return 1f;
            }
        }

        private SmoothnessTextureChannel _MapFloatToTextureChannel(float texChannel)
        {
            if (texChannel == 0f)
            {
                return SmoothnessTextureChannel.metallicSpecular;
            }
            else
            {
                return SmoothnessTextureChannel.albedo;
            }
        }

        private float _MapTextureChannelToFloat(SmoothnessTextureChannel workflowMode)
        {
            if (workflowMode == SmoothnessTextureChannel.metallicSpecular)
            {
                return 0f;
            }
            else
            {
                return 1f;
            }
        }

        public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
        {
            if (m_workflowMode == WorkflowMode.unknown)
            {
                if (sourceMat.HasProperty("_WorkflowMode"))
                {
                    m_workflowMode = _MapFloatToWorkflowMode(sourceMat.GetFloat("_WorkflowMode"));
                }
            } else
            {
                if (sourceMat.HasProperty("_WorkflowMode") && _MapFloatToWorkflowMode(sourceMat.GetFloat("_WorkflowMode")) != m_workflowMode)
                {
                    Debug.LogError("Using the Universal Render Pipeline TextureBlender to blend non-texture-propertyes. Some of the source materials used different 'WorkflowModes'. These "+
                        " cannot be blended properly. Results will be unpredictable.");
                }
            }

            if (m_smoothnessTextureChannel == SmoothnessTextureChannel.unknown)
            {
                if (sourceMat.HasProperty("_SmoothnessTextureChannel"))
                {
                    m_smoothnessTextureChannel = _MapFloatToTextureChannel(sourceMat.GetFloat("_SmoothnessTextureChannel"));
                }
            } else
            {
                if (sourceMat.HasProperty("_SmoothnessTextureChannel") && _MapFloatToTextureChannel(sourceMat.GetFloat("_SmoothnessTextureChannel")) != m_smoothnessTextureChannel)
                {
                    Debug.LogError("Using the Universal Render Pipeline TextureBlender to blend non-texture-properties. Some of the source materials store smoothness in the Albedo texture alpha" +
                        " and some source materials store smoothness in the Metallic/Specular texture alpha channel. The result material can only read smoothness from one or the other. Results will be unpredictable.");
                }
            }

            if (shaderTexturePropertyName.Equals("_BaseMap"))
            {
                propertyToDo = Prop.doColor;
                if (sourceMat.HasProperty("_BaseColor"))
                {
                    m_tintColor = sourceMat.GetColor("_BaseColor");
                }
                else
                {
                    m_tintColor = m_generatingTintedAtlaColor;
                }
            }
            else if (shaderTexturePropertyName.Equals("_SpecGlossMap"))
            {
                propertyToDo = Prop.doSpecular;
                m_specColor = m_generatingTintedAtlaSpecular;
                if (sourceMat.GetTexture("_SpecGlossMap") != null)
                {
                    m_hasSpecGlossMap = true;
                }
                else
                {
                    m_hasSpecGlossMap = false;
                }

                if (sourceMat.HasProperty("_SpecColor"))
                {
                    m_specColor = sourceMat.GetColor("_SpecColor");
                } else
                {
                    m_specColor = new Color(0f, 0f, 0f, 1f);
                }

                if (sourceMat.HasProperty("_Smoothness") && m_workflowMode == WorkflowMode.specular)
                {
                    m_smoothness = sourceMat.GetFloat("_Smoothness");
                    Debug.LogError("TODO smooth " + sourceMat + "  " + m_smoothness);
                }
                else if (m_workflowMode == WorkflowMode.specular)
                {
                    m_smoothness = 1f;
                }
            }
            else if (shaderTexturePropertyName.Equals("_MetallicGlossMap"))
            {
                propertyToDo = Prop.doMetallic;
                if (sourceMat.GetTexture("_MetallicGlossMap") != null)
                {
                    m_hasMetallicGlossMap = true;
                }
                else
                {
                    m_hasMetallicGlossMap = false;
                }

                if (sourceMat.HasProperty("_Metallic"))
                {
                    m_metallic = sourceMat.GetFloat("_Metallic");
                }
                else
                {
                    m_metallic = 0f;
                }

                if (sourceMat.HasProperty("_Smoothness") && m_workflowMode == WorkflowMode.metallic)
                {
                    m_smoothness = sourceMat.GetFloat("_Smoothness");
                }
                else if (m_workflowMode == WorkflowMode.metallic)
                {
                    m_smoothness = 0f;
                }
                

            }
            else if (shaderTexturePropertyName.Equals("_BumpMap"))
            {
                propertyToDo = Prop.doBump;
                if (sourceMat.HasProperty(shaderTexturePropertyName))
                {
                    if (sourceMat.HasProperty("_BumpScale"))
                        m_bumpScale = sourceMat.GetFloat("_BumpScale");
                }
                else
                {
                    m_bumpScale = m_generatingTintedAtlaBumpScale;
                }
            } else if (shaderTexturePropertyName.Equals("_EmissionMap"))
            {
                propertyToDo = Prop.doEmission;
                m_shaderDoesEmission = sourceMat.IsKeywordEnabled("_EMISSION");
                if (sourceMat.HasProperty("_EmissionColor")) {
                    m_emissionColor = sourceMat.GetColor("_EmissionColor");
                } else
                {
                    m_generatingTintedAtlaColor = m_notGeneratingAtlasDefaultEmisionColor;
                }
            } else
            {
                propertyToDo = Prop.doNone;
            }
        }

        public Color OnBlendTexturePixel(string propertyToDoshaderPropertyName, Color pixelColor)
        {
            if (propertyToDo == Prop.doColor)
            {
                return new Color(pixelColor.r * m_tintColor.r, pixelColor.g * m_tintColor.g, pixelColor.b * m_tintColor.b, pixelColor.a * m_tintColor.a);
            }
            else if (propertyToDo == Prop.doMetallic)
            {
                if (m_hasMetallicGlossMap)
                {
                    return pixelColor = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a * m_smoothness);
                }
                else
                {
                    return new Color(m_metallic, 0, 0, m_smoothness);
                }
            }
            else if (propertyToDo == Prop.doSpecular)
            {
                if (m_hasSpecGlossMap)
                {
                    return pixelColor = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a * m_smoothness);
                }
                else
                {
                    Color c = m_specColor;
                    c.a = m_smoothness;
                    return c;
                }
            }
            else if (propertyToDo == Prop.doBump)
            {
                return Color.Lerp(NeutralNormalMap, pixelColor, m_bumpScale);
            }
            else if (propertyToDo == Prop.doEmission)
            {
                if (m_shaderDoesEmission)
                {
                    return new Color(pixelColor.r * m_emissionColor.r, pixelColor.g * m_emissionColor.g, pixelColor.b * m_emissionColor.b, pixelColor.a * m_emissionColor.a);
                }
                else
                {
                    return Color.black;
                }
            }
            return pixelColor;
        }

        public bool NonTexturePropertiesAreEqual(Material a, Material b)
        {
            if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaColor, "_BaseColor"))
            {
                return false;
            }

            if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaSpecular, "_SpecColor"))
            {
                return false;
            }

            if (m_workflowMode == WorkflowMode.specular){
                bool aHasSpecTex = a.HasProperty("_SpecGlossMap") && a.GetTexture("_SpecGlossMap") != null;
                bool bHasSpecTex = b.HasProperty("_SpecGlossMap") && b.GetTexture("_SpecGlossMap") != null;

                if (aHasSpecTex && bHasSpecTex)
                {
                    if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_SpecularWorkflow, "_Smoothness"))
                    {
                        Debug.LogError("Are equal A");
                        return false;
                    }
                }
                else if (!aHasSpecTex && !bHasSpecTex)
                {
                    if (!TextureBlenderFallback._compareColor(a, b, m_notGeneratingAtlasDefaultSpecularColor, "_SpecColor") &&
                        !TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_SpecularWorkflow, "_Smoothness"))
                    {
                        Debug.LogError("Are equal B");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("Are equal C");
                    return false;
                }
            }

            if (m_workflowMode == WorkflowMode.metallic) {
                bool aHasMetallicTex = a.HasProperty("_MetallicGlossMap") && a.GetTexture("_MetallicGlossMap") != null;
                bool bHasMetallicTex = b.HasProperty("_MetallicGlossMap") && b.GetTexture("_MetallicGlossMap") != null;
                if (aHasMetallicTex && bHasMetallicTex)
                {
                    if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_MetallicWorkflow, "_Smoothness"))
                    {
                        return false;
                    }
                }
                else if (!aHasMetallicTex && !bHasMetallicTex)
                {
                    if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultMetallic, "_Metallic") &&
                        !TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_MetallicWorkflow, "_Smoothness"))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (!TextureBlenderFallback._compareFloat(a, b, m_generatingTintedAtlaBumpScale, "_BumpScale"))
            {
                return false;
            }

            if (a.IsKeywordEnabled("_EMISSION") != b.IsKeywordEnabled("_EMISSION"))
            {
                return false;
            }
            if (a.IsKeywordEnabled("_EMISSION"))
            {
                if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaEmission, "_EmissionColor"))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial)
        {
            if (m_workflowMode != WorkflowMode.unknown)
            {
                resultMaterial.SetFloat("_WorkflowMode", _MapWorkflowModeToFloat(m_workflowMode));
            }

            if (m_smoothnessTextureChannel != SmoothnessTextureChannel.unknown)
            {
                resultMaterial.SetFloat("_SmoothnessTextureChannel", _MapTextureChannelToFloat(m_smoothnessTextureChannel));
            }

            if (resultMaterial.GetTexture("_BaseMap") != null)
            {
                resultMaterial.SetColor("_BaseColor", m_generatingTintedAtlaColor);
            }
            else
            {
                resultMaterial.SetColor("_BaseColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_BaseColor", m_notGeneratingAtlasDefaultColor));
            }

            if (m_workflowMode == WorkflowMode.specular)
            {
                if (resultMaterial.GetTexture("_SpecGlossMap") != null)
                {
                    Debug.LogError("Setting A " + m_smoothness);
                    resultMaterial.SetColor("_SpecColor", m_generatingTintedAtlaSpecular);
                    resultMaterial.SetFloat("_Smoothness", m_generatingTintedAtlasSpecular_somoothness);
                }
                else
                {
                    Debug.LogError("Setting B " + m_smoothness);
                    resultMaterial.SetColor("_SpecColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_SpecColor", m_notGeneratingAtlasDefaultSpecularColor));
                    resultMaterial.SetFloat("_Smoothness", m_smoothness);
                }
            }

            if (m_workflowMode == WorkflowMode.metallic)
            {
                if (resultMaterial.GetTexture("_MetallicGlossMap") != null)
                {
                    resultMaterial.SetFloat("_Metallic", m_generatingTintedAtlasMetallic);
                    resultMaterial.SetFloat("_Smoothness", m_generatingTintedAtlasMetallic_smoothness);
                }
                else
                {
                    resultMaterial.SetFloat("_Metallic", (float)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Metallic", m_notGeneratingAtlasDefaultMetallic));
                    resultMaterial.SetFloat("_Smoothness", m_smoothness);
                }
            }

            if (resultMaterial.GetTexture("_BumpMap") != null)
            {
                resultMaterial.SetFloat("_BumpScale", m_generatingTintedAtlaBumpScale);
            }
            else
            {
                resultMaterial.SetFloat("_BumpScale", m_generatingTintedAtlaBumpScale);
            }

            if (resultMaterial.GetTexture("_EmissionMap") != null)
            {
                resultMaterial.EnableKeyword("_EMISSION");
                resultMaterial.SetColor("_EmissionColor", Color.white);
            }
            else
            {
                resultMaterial.DisableKeyword("_EMISSION");
                resultMaterial.SetColor("_EmissionColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_EmissionColor", m_notGeneratingAtlasDefaultEmisionColor));
            }
        }


        public Color GetColorIfNoTexture(Material mat, ShaderTextureProperty texPropertyName)
        {
            if (texPropertyName.name.Equals("_BumpMap"))
            {
                return new Color(.5f, .5f, 1f);
            }
            else if (texPropertyName.name.Equals("_BaseMap"))
            {
                if (mat != null && mat.HasProperty("_BaseColor"))
                {
                    try
                    { //need try because can't garantee _Color is a color
                        Color c = mat.GetColor("_BaseColor");
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_BaseColor", c);
                    }
                    catch (Exception) { }
                    return Color.white;
                }
            }
            else if (texPropertyName.name.Equals("_SpecGlossMap"))
            {
                if (mat != null && mat.HasProperty("_SpecColor"))
                {
                    try
                    { //need try because can't garantee _Color is a color
                        Color c = mat.GetColor("_SpecColor");
                        /*
                        if (mat.HasProperty("_Glossiness"))
                        {
                            try
                            {
                                c.a = mat.GetFloat("_Glossiness");
                            }
                            catch (Exception) { }
                        }
                        */
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_SpecColor", c);
                        //sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Glossiness", c.a);
                    }
                    catch (Exception) { }
                }
                return new Color(0f, 0f, 0f, .5f);
            }
            else if (texPropertyName.name.Equals("_MetallicGlossMap"))
            {
                if (mat != null && mat.HasProperty("_Metallic"))
                {
                    try
                    { //need try because can't garantee _Metallic is a float
                        float v = mat.GetFloat("_Metallic");
                        Color c = new Color(v, v, v);
                        
                        if (mat.HasProperty("_Smoothness"))
                        {
                            try
                            {
                                c.a = mat.GetFloat("_Smoothness");
                            }

                            catch (Exception) { }
                        }
                        
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Metallic", v);
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Smoothness", c.a);
                    }
                    catch (Exception) { }
                    return new Color(0f, 0f, 0f, .5f);
                }
                else
                {
                    return new Color(0f, 0f, 0f, .5f);
                }
            }
            else if (texPropertyName.name.Equals("_OcclusionMap"))
            {
                return new Color(1f, 1f, 1f, 1f);
            }
            else if (texPropertyName.name.Equals("_EmissionMap"))
            {
                if (mat != null)
                {
                    if (mat.IsKeywordEnabled("_EMISSION"))
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            try
                            {
                                Color c = mat.GetColor("_EmissionColor");
                                sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_EmissionColor", c);
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            return Color.black;
                        }
                    }
                    else
                    {
                        return Color.black;
                    }
                }
            }
            else if (texPropertyName.name.Equals("_DetailMask"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            return new Color(1f, 1f, 1f, 0f);
        }

    }
}
