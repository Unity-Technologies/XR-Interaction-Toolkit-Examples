using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    public class TextureBlenderStandardSpecular : TextureBlender
    {
        static Color NeutralNormalMap = new Color(.5f, .5f, 1f);

        private enum Prop
        {
            doColor,
            doSpecular,
            doEmission,
            doBump,
            doNone,
        }

        // This is used to cache the non texture property values. If all non-texutre property values are the same for a property for all source textures
        // then the source value will be re-used
        TextureBlenderMaterialPropertyCacheHelper sourceMaterialPropertyCache = new TextureBlenderMaterialPropertyCacheHelper();

        // These are cached values read in OnBeforeTintTexture and used when blending pixels.
        Color m_tintColor;
        float m_glossiness;
        float m_SpecGlossMapScale;
        Color m_specColor;
        bool m_hasSpecGlossMap;
        float m_bumpScale;
        bool m_shaderDoesEmission;
        Color m_emissionColor;

        // This just makes things more efficient so we arn't doing a string comparison for each pixel.
        Prop propertyToDo = Prop.doNone;

        // These are the property values that will be assigned to the result material if
        // generating an atlas for those properties. 
        Color m_generatingTintedAtlaColor = Color.white;
        Color m_generatingTintedAtlaSpecular = Color.black;
        float m_generatingTintedAtlaGlossiness = 1f;
        float m_generatingTintedAtlaSpecGlossMapScale = 1f;
        float m_generatingTintedAtlaBumpScale = 1f;
        Color m_generatingTintedAtlaEmission = Color.white;

        // These are the default property values that will be assigned to the result materials if 
        // none of the source materials have a value for these properties.
        Color m_notGeneratingAtlasDefaultColor = Color.white;
        Color m_notGeneratingAtlasDefaultSpecularColor = new Color(0f,0f,0f,1f);
        float m_notGeneratingAtlasDefaultGlossiness = .5f;
        Color m_notGeneratingAtlasDefaultEmisionColor = Color.black;

        public bool DoesShaderNameMatch(string shaderName)
		{
			return shaderName.Equals("Standard (Specular setup)");
		}

        public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
        {
            if (shaderTexturePropertyName.Equals("_MainTex"))
            {
                propertyToDo = Prop.doColor;
                if (sourceMat.HasProperty("_Color"))
                {
                    m_tintColor = sourceMat.GetColor("_Color");
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

                if (sourceMat.HasProperty("_GlossMapScale"))
                {
                    m_SpecGlossMapScale = sourceMat.GetFloat("_GlossMapScale");
                }
                else
                {
                    m_SpecGlossMapScale = 1f;
                }

                if (sourceMat.HasProperty("_Glossiness"))
                {
                    m_glossiness = sourceMat.GetFloat("_Glossiness");
                }
                else
                {
                    m_glossiness = 0f;
                }
            } else if (shaderTexturePropertyName.Equals("_BumpMap"))
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
            else if (propertyToDo == Prop.doSpecular)
            {
                if (m_hasSpecGlossMap)
                {
                    return pixelColor = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a * m_SpecGlossMapScale);
                }
                else
                {
                    Color c = m_specColor;
                    c.a = m_glossiness;
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
            if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaColor, "_Color"))
            {
                return false;
            }

            if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaSpecular, "_SpecColor"))
            {
                return false;
            }

            bool aHasSpecTex = a.HasProperty("_SpecGlossMap") && a.GetTexture("_SpecGlossMap") != null;
            bool bHasSpecTex = b.HasProperty("_SpecGlossMap") && b.GetTexture("_SpecGlossMap") != null;

            if (aHasSpecTex && bHasSpecTex)
            {
                if (!TextureBlenderFallback._compareFloat(a, b, m_generatingTintedAtlaSpecGlossMapScale, "_GlossMapScale"))
                {
                    return false;
                }
            }
            else if (!aHasSpecTex && !bHasSpecTex)
            {
                if (!TextureBlenderFallback._compareFloat(a, b, m_generatingTintedAtlaGlossiness, "_Glossiness"))
                {
                    return false;
                }
            }
            else
            {
                return false;
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
            if (resultMaterial.GetTexture("_MainTex") != null)
            {
                resultMaterial.SetColor("_Color", m_generatingTintedAtlaColor);
            }
            else
            {
                resultMaterial.SetColor("_Color", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Color", m_notGeneratingAtlasDefaultColor));
            }

            if (resultMaterial.GetTexture("_SpecGlossMap") != null) {
                resultMaterial.SetColor("_SpecColor", m_generatingTintedAtlaSpecular);
                resultMaterial.SetFloat("_GlossMapScale", m_generatingTintedAtlaSpecGlossMapScale);
                resultMaterial.SetFloat("_Glossiness", m_generatingTintedAtlaGlossiness);
            } else {
                resultMaterial.SetColor("_SpecColor", (Color) sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_SpecColor", m_notGeneratingAtlasDefaultSpecularColor));
                resultMaterial.SetFloat("_Glossiness", (float) sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Glossiness", m_notGeneratingAtlasDefaultGlossiness));
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
            else if (texPropertyName.name.Equals("_MainTex"))
            {
                if (mat != null && mat.HasProperty("_Color"))
                {
                    try
                    { //need try because can't garantee _Color is a color
                        Color c = mat.GetColor("_Color");
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Color", c);
                        return c;
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
                        if (mat.HasProperty("_Glossiness"))
                        {
                            try
                            {
                                c.a = mat.GetFloat("_Glossiness");
                            }
                            catch (Exception) { }
                        }
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_SpecColor", c);
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Glossiness", c.a);
                        return c;
                    }
                    catch (Exception) { }
                }
                return new Color(0f, 0f, 0f, .5f);
            }
            else if (texPropertyName.name.Equals("_ParallaxMap"))
            {
                return new Color(0f, 0f, 0f, 0f);
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
                                return c;
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
