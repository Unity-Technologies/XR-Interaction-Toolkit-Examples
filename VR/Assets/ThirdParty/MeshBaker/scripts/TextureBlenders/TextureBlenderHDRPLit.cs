using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    // Only works for Standard and Specular material types, blends basic properties: tint, metallic, smoothness, specular color, emmisive color    
    public class TextureBlenderHDRPLit : TextureBlender
    {

        private enum Prop
        {
            doColor,
            doMask,
            doSpecular,
            doEmission,
            doNone,
        }
        
        // TODO add support for other material types besides standard and specular
        private enum MaterialType
        {
            unknown,
            subsurfaceScattering,
            standard,
            anisotropy,
            iridescence,
            specularColor,
            translucent,
        }        

        // This is used to cache the non texture property values. If all non-texutre property values are the same for a property for all source textures
        // then the source value will be re-used
        TextureBlenderMaterialPropertyCacheHelper sourceMaterialPropertyCache = new TextureBlenderMaterialPropertyCacheHelper();

        MaterialType m_materialType = MaterialType.unknown;

        // These are cached values read in OnBeforeTintTexture and used when blending pixels.
        Color m_tintColor;
        bool m_hasMaskMap;
        float m_smoothness; // Used if no mask map
        float m_metallic;  // Used if no mask map
        bool m_hasSpecMap;
        Color m_specularColor;
        Color m_emissiveColor;

        // This just makes things more efficient so we arn't doing a string comparison for each pixel.
        Prop propertyToDo = Prop.doNone;

        // These are the property values that will be assigned to the result material if
        // generating an atlas for those properties. 
        Color m_generatingTintedAtlaColor = Color.white;
        Color m_generatingTintedAtlaSpecular = Color.white;
        Color m_generatingTintedAtlaEmission = Color.white;

        // These are the default property values that will be assigned to the result materials if 
        // none of the source materials have a value for these properties.
        Color m_notGeneratingAtlasDefaultColor = Color.white;
        float m_notGeneratingAtlasDefaultMetallic = 0;
        float m_notGeneratingAtlasDefaultSmoothness = 0.5f;
        Color m_notGeneratingAtlasDefaultSpecular = Color.white;
        Color m_notGeneratingAtlasDefaultEmissiveColor = Color.black;

        public bool DoesShaderNameMatch(string shaderName)
		{
			return  shaderName.Equals("HDRP/Lit");
		}

        private MaterialType _MapFloatToMaterialType(float materialType)
        {
            if (materialType == 0f)
            {
                return MaterialType.subsurfaceScattering;
            }
            if (materialType == 1f)
            {
                return MaterialType.standard;
            }
            if (materialType == 2f)
            {
                return MaterialType.anisotropy;
            }
            if (materialType == 3f)
            {
                return MaterialType.iridescence;
            }
            if (materialType == 4f)
            {
                return MaterialType.specularColor;
            }
            if (materialType == 5f)
            {
                return MaterialType.translucent;
            }
            else
            {
                return MaterialType.unknown;
            }
        }

        private float _MapMaterialTypeToFloat(MaterialType materialType)
        {
            if (materialType == MaterialType.subsurfaceScattering)
            {
                return 0f;
            }
            if (materialType == MaterialType.standard)
            {
                return 1f;
            }
            if (materialType == MaterialType.anisotropy)
            {
                return 2f;
            }
            if (materialType == MaterialType.iridescence)
            {
                return 3f;
            }
            if (materialType == MaterialType.specularColor)
            {
                return 4f;
            }
            if (materialType == MaterialType.translucent)
            {
                return 5f;
            }
            else
            {
                return -1f;
            }
        }
        
        public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
        {
            if (m_materialType == MaterialType.unknown)
            {
                if (sourceMat.HasProperty("_MaterialID"))
                {
                    m_materialType = _MapFloatToMaterialType(sourceMat.GetFloat("_MaterialID"));
                }
            }
            else
            {
                if (sourceMat.HasProperty("_MaterialID") && _MapFloatToMaterialType(sourceMat.GetFloat("_MaterialID")) != m_materialType)
                {
                    Debug.LogError("Using the High Definition Render Pipeline TextureBlender to blend non-texture-properties. Some of the source materials use different 'MaterialType'. These " +
                        " cannot be blended properly. Results will be unpredictable.");
                }
            }

            if (shaderTexturePropertyName.Equals("_BaseColorMap"))
            {
                propertyToDo = Prop.doColor;
                if (sourceMat.HasProperty("_BaseColor"))
                {
                    m_tintColor = sourceMat.GetColor("_BaseColor");
                }
                else
                {
                    m_tintColor = m_notGeneratingAtlasDefaultColor;
                }
            }            
            else if (shaderTexturePropertyName.Equals("_MaskMap"))
            {
                propertyToDo = Prop.doMask;
                if (sourceMat.HasProperty("_MaskMap") && sourceMat.GetTexture("_MaskMap") != null)
                {
                    m_hasMaskMap = true;
                }
                else
                {
                    m_hasMaskMap = false;

                    // No maskmap means sliders
                    if (m_materialType == MaterialType.standard && sourceMat.HasProperty("_Metallic"))
                    {
                        m_metallic = sourceMat.GetFloat("_Metallic");
                    }
                    else
                    {
                        m_metallic = m_notGeneratingAtlasDefaultMetallic;
                    }

                    if (sourceMat.HasProperty("_Smoothness"))
                    {
                        m_smoothness = sourceMat.GetFloat("_Smoothness");
                    }
                    else
                    {
                        m_smoothness = m_notGeneratingAtlasDefaultSmoothness;
                    }
                }                
            }
            else if (shaderTexturePropertyName.Equals("_SpecularColorMap") && m_materialType == MaterialType.specularColor)
            {
                propertyToDo = Prop.doSpecular;
                if (sourceMat.HasProperty("_SpecularColorMap") && sourceMat.GetTexture("_SpecularColorMap") != null)
                {
                    m_hasSpecMap = true;
                }
                else
                {
                    m_hasSpecMap = false;
                }
                if (sourceMat.HasProperty("_SpecularColor"))
                {
                    m_specularColor = sourceMat.GetColor("_SpecularColor");
                }
            }
            else if (shaderTexturePropertyName.Equals("_EmissiveColorMap"))
            {
                // Does not handle LDR Emissive colors
                propertyToDo = Prop.doEmission;
                if (sourceMat.HasProperty("_EmissiveColor")) {
                    m_emissiveColor = sourceMat.GetColor("_EmissiveColor");
                } else
                {
                    m_emissiveColor = m_notGeneratingAtlasDefaultEmissiveColor;
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
            else if (propertyToDo == Prop.doMask)
            {
                
                if (m_hasMaskMap)
                {
                    return new Color(pixelColor.r * m_metallic, pixelColor.g, pixelColor.b, pixelColor.a * m_smoothness);
                }
                else
                {
                    return new Color(m_metallic, 0, 0, m_smoothness);
                }
            }
            else if (propertyToDo == Prop.doSpecular)
            {
                if (m_hasSpecMap)
                {
                    return new Color(pixelColor.r * m_specularColor.r, pixelColor.g * m_specularColor.g, pixelColor.b * m_specularColor.g, pixelColor.a * m_specularColor.a);
                }
                else
                {
                    return m_specularColor;
                }
            }
            else if (propertyToDo == Prop.doEmission)
            {
                return new Color(pixelColor.r * m_emissiveColor.r, pixelColor.g * m_emissiveColor.g, pixelColor.b * m_emissiveColor.b, pixelColor.a * m_emissiveColor.a);
            }
            return pixelColor;
        }

        public bool NonTexturePropertiesAreEqual(Material a, Material b)
        {
            if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaColor, "_BaseColor"))
            {
                return false;
            }

            bool aHasMaskMapTex = a.HasProperty("_MaskMap") && a.GetTexture("_MaskMap") != null;
            bool bHasMaskMapTex = b.HasProperty("_MaskMap") && b.GetTexture("_MaskMap") != null;

            if (!aHasMaskMapTex && !bHasMaskMapTex)
            {
                if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultMetallic, "_Metallic") &&
                    !TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness, "_Smoothness"))
                {
                    return false;
                }
            }

            if (m_materialType == MaterialType.specularColor)
            {
                if (!TextureBlenderFallback._compareColor(a, b, m_notGeneratingAtlasDefaultSpecular, "_SpecularColor"))
                {
                    return false;
                }
            }

            if (!TextureBlenderFallback._compareColor(a, b, m_notGeneratingAtlasDefaultEmissiveColor, "_EmissiveColor"))
            {
                return false;
            }

            return true;
        }

        public void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial)
        {
            if (m_materialType != MaterialType.unknown)
            {
                resultMaterial.SetFloat("_MaterialID", _MapMaterialTypeToFloat(m_materialType));
            }

            if (resultMaterial.GetTexture("_BaseColorMap") != null)
            {
                resultMaterial.SetColor("_BaseColor", m_generatingTintedAtlaColor);
            }
            else
            {
                resultMaterial.SetColor("_BaseColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_BaseColor", m_notGeneratingAtlasDefaultColor));
            }

            if (resultMaterial.GetTexture("_MaskMap") != null)
            {
                // MaskMap atlas has been generated, no metallic and smoothness property
            }
            else
            {
                resultMaterial.SetFloat("_Metallic", (float)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Metallic", m_notGeneratingAtlasDefaultMetallic));
                resultMaterial.SetFloat("_Smoothness", m_notGeneratingAtlasDefaultSmoothness);
            }

            if (m_materialType == MaterialType.specularColor)
            {
                if (resultMaterial.GetTexture("_SpecularColorMap") != null)
                {
                    resultMaterial.SetColor("_SpecularColor", m_generatingTintedAtlaSpecular);
                    // Easier to set the Ambient Occlusion remap than to try to change the mask map after the fact, otherwise spec colors with no textures display improperly
                    resultMaterial.SetFloat("_AORemapMin", 1);
                }
                else
                {
                    resultMaterial.SetColor("_SpecularColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_SpecularColor", m_notGeneratingAtlasDefaultSpecular));
                    resultMaterial.SetFloat("_AORemapMin", 1);
                }
            }

            if (resultMaterial.GetTexture("_EmissiveColorMap") != null)
            {
                resultMaterial.SetColor("_EmissiveColor", m_generatingTintedAtlaEmission);
            }
            else
            {
                resultMaterial.SetColor("_EmissiveColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_EmissiveColor", m_notGeneratingAtlasDefaultEmissiveColor));
            }
        }


        public Color GetColorIfNoTexture(Material mat, ShaderTextureProperty texPropertyName)
        {
            if (texPropertyName.name.Equals("_BaseColorMap"))
            {
                if (mat != null && mat.HasProperty("_BaseColor"))
                {
                    try
                    { //need try because can't garantee _Color is a color
                        Color c = mat.GetColor("_BaseColor");
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_BaseColor", c);
                    }
                    catch (Exception) { }
                    return m_notGeneratingAtlasDefaultColor;
                }
            }            
            else if (texPropertyName.name.Equals("_Metallic"))
            {
                if (mat != null && mat.HasProperty("_Metallic"))
                {
                    try
                    { //need try because can't garantee _Metallic is a float
                        float v = mat.GetFloat("_Metallic");
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Metallic", v);
                    }
                    catch (Exception) { }
                    return new Color(0f, 0f, 0f, m_notGeneratingAtlasDefaultSmoothness);
                }
                else
                {
                    return new Color(0f, 0f, 0f, m_notGeneratingAtlasDefaultSmoothness);
                }
            }
            else if (texPropertyName.name.Equals("_Smoothness"))
            {
                if (mat != null && mat.HasProperty("_Smoothness"))
                {
                    try
                    { // need try because can't guarantee _Smoothness is a float
                        float a = mat.GetFloat("_Smoothness");
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Smoothness", a);
                    }
                    catch (Exception) { }
                    return new Color(0f, 0f, 0f, m_notGeneratingAtlasDefaultSmoothness);
                }
                else
                {
                    return new Color(0f, 0f, 0f, m_notGeneratingAtlasDefaultSmoothness);
                }
            }
            else if (texPropertyName.name.Equals("_SpecularColorMap"))
            {
                if (mat != null && mat.HasProperty("_SpecularColor"))
                {
                    try
                    { // need try because can't garantee _SpecularColor is a color
                        Color c = mat.GetColor("_SpecularColor");                        
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_SpecularColor", c);
                    }
                    catch (Exception) { }
                }
                return m_notGeneratingAtlasDefaultSpecular;
            }
            else if (texPropertyName.name.Equals("_EmissiveColorMap"))
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_EmissiveColor"))
                    {
                        try
                        { // need try because can't garantee _EmissiveColor is a color
                            Color c = mat.GetColor("_EmissiveColor");
                            sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_EmissiveColor", c);
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        return m_notGeneratingAtlasDefaultEmissiveColor;
                    }
                }
            }            
            return new Color(1f, 1f, 1f, 0f);
        }

    }
}
