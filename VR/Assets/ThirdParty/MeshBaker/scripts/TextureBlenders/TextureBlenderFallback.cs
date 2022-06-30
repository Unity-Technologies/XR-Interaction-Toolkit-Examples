using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    public class TextureBlenderFallback : TextureBlender
    {
        bool m_doTintColor = false;
        Color m_tintColor;

        Color m_defaultColor = Color.white;

		public bool DoesShaderNameMatch(string shaderName)
        {
            return true; //matches everything
        }

        public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
        {
            if (shaderTexturePropertyName.Equals("_MainTex"))
            {
                m_doTintColor = true;
                m_tintColor = Color.white;
                if (sourceMat.HasProperty("_Color"))
                {
                    m_tintColor = sourceMat.GetColor("_Color");
                }
				else if (sourceMat.HasProperty("_TintColor"))
				{
					m_tintColor = sourceMat.GetColor("_TintColor");
				}
            } else
            {
                m_doTintColor = false;
            }
        }

        public Color OnBlendTexturePixel(string shaderPropertyName, Color pixelColor)
        {
            if (m_doTintColor)
            {
                return new Color(pixelColor.r * m_tintColor.r, pixelColor.g * m_tintColor.g, pixelColor.b * m_tintColor.b, pixelColor.a * m_tintColor.a);
            }
            return pixelColor;
        }

        public bool NonTexturePropertiesAreEqual(Material a, Material b)
        {
			if (a.HasProperty("_Color"))
			{
            if (_compareColor(a, b, m_defaultColor, "_Color"))
            {
                return true;
            }
				//return false;
			}
			else if (a.HasProperty("_TintColor"))
			{
				if (_compareColor(a, b, m_defaultColor, "_TintColor"))
				{
					return true;
				}
				//return false;
			}
            return false;
        }

        public void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial)
        {
           if (resultMaterial.HasProperty("_Color"))
            {
                resultMaterial.SetColor("_Color", m_defaultColor);
            }
			else if (resultMaterial.HasProperty("_TintColor"))
			{
				resultMaterial.SetColor("_TintColor", m_defaultColor);
			}
        }

        public Color GetColorIfNoTexture(Material mat, ShaderTextureProperty texProperty)
        {
            if (texProperty.isNormalMap)
            {
                return new Color(.5f, .5f, 1f);
            }
            else if (texProperty.name.Equals("_MainTex"))
            {
                if (mat != null && mat.HasProperty("_Color"))
                {
                    try
                    { //need try because can't garantee _Color is a color
                        return mat.GetColor("_Color");
                    }
                    catch (Exception) { }
                }
				else if (mat != null && mat.HasProperty("_TintColor"))
				{
					try
					{ //need try because can't garantee _TintColor is a color
						return mat.GetColor("_TintColor");
					}
					catch (Exception) { }
				}
            }
            else if (texProperty.name.Equals("_SpecGlossMap"))
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
                        Debug.LogWarning(c);
                        return c;
                    }
                    catch (Exception) { }
                }
            }
            else if (texProperty.name.Equals("_MetallicGlossMap"))
            {
                if (mat != null && mat.HasProperty("_Metallic"))
                {
                    try
                    { //need try because can't garantee _Metallic is a float
                        float v = mat.GetFloat("_Metallic");
                        Color c = new Color(v, v, v);
                        if (mat.HasProperty("_Glossiness"))
                        {
                            try
                            {
                                c.a = mat.GetFloat("_Glossiness");
                            }
                            catch (Exception) { }
                        }
                        return c;
                    }
                    catch (Exception) { }
                }
            }
            else if (texProperty.name.Equals("_ParallaxMap"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            else if (texProperty.name.Equals("_OcclusionMap"))
            {
                return new Color(1f, 1f, 1f, 1f);
            }
            else if (texProperty.name.Equals("_EmissionMap"))
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_EmissionScaleUI"))
                    {
                        //Standard shader has weird behavior if EmissionMap has never
                        //been set then no EmissionColorUI color picker. If has ever
                        //been set then is EmissionColorUI color picker.
                        if (mat.HasProperty("_EmissionColor") &&
                            mat.HasProperty("_EmissionColorUI"))
                        {
                            try
                            {
                                Color c1 = mat.GetColor("_EmissionColor");
                                Color c2 = mat.GetColor("_EmissionColorUI");
                                float f = mat.GetFloat("_EmissionScaleUI");
                                if (c1 == new Color(0f, 0f, 0f, 0f) &&
                                    c2 == new Color(1f, 1f, 1f, 1f))
                                {
                                    //is virgin Emission values
                                    return new Color(f, f, f, f);
                                }
                                else { //non virgin Emission values
                                    return c2;
                                }
                            }
                            catch (Exception) { }

                        }
                        else {
                            try
                            { //need try because can't garantee _Color is a color
                                float f = mat.GetFloat("_EmissionScaleUI");
                                return new Color(f, f, f, f);
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            else if (texProperty.name.Equals("_DetailMask"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            return new Color(1f, 1f, 1f, 0f);
        }

        public static bool _compareColor(Material a, Material b, Color defaultVal, string propertyName)
        {
            Color aColor = defaultVal;
            Color bColor = defaultVal;
            if (a.HasProperty(propertyName))
            {
                aColor = a.GetColor(propertyName);
            }
            if (b.HasProperty(propertyName))
            {
                bColor = b.GetColor(propertyName);
            }
            if (aColor != bColor)
            {
                return false;
            }
            return true;
        }

        public static bool _compareFloat(Material a, Material b, float defaultVal, string propertyName)
        {
            float aFloat = defaultVal;
            float bFloat = defaultVal;
            if (a.HasProperty(propertyName))
            {
                aFloat = a.GetFloat(propertyName);
            }
            if (b.HasProperty(propertyName))
            {
                bFloat = b.GetFloat(propertyName);
            }
            if (aFloat != bFloat)
            {
                return false;
            }
            return true;
        }
    }
}
