using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    public class TextureBlenderLegacyBumpDiffuse : TextureBlender
    {
        bool doColor;
        Color m_tintColor;
        Color m_defaultTintColor = Color.white;
		
		public bool DoesShaderNameMatch(string shaderName)
		{
			if (shaderName.Equals ("Legacy Shaders/Bumped Diffuse")) {
				return true;
			} else if (shaderName.Equals ("Bumped Diffuse")) {
				return true;			
			}
			return false;
		}

        public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
        {
            if (shaderTexturePropertyName.EndsWith("_MainTex"))
            {
                doColor = true;
                m_tintColor = sourceMat.GetColor("_Color");
            } else
            {
                doColor = false;
            }
        }

        public Color OnBlendTexturePixel(string propertyToDoshaderPropertyName, Color pixelColor)
        {
            if (doColor)
            {
                return new Color(pixelColor.r * m_tintColor.r, pixelColor.g * m_tintColor.g, pixelColor.b * m_tintColor.b, pixelColor.a * m_tintColor.a);
            }
            return pixelColor;
        }

        public bool NonTexturePropertiesAreEqual(Material a, Material b)
        {
            return TextureBlenderFallback._compareColor(a, b, m_defaultTintColor, "_Color");
        }

        public void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial)
        {
            resultMaterial.SetColor("_Color", Color.white);
        }

        public Color GetColorIfNoTexture(Material m, ShaderTextureProperty texPropertyName)
        {
            if (texPropertyName.name.Equals("_BumpMap"))
            {
                return new Color(.5f, .5f, 1f);
            }
            if (texPropertyName.name.Equals("_MainTex"))
            {
                if (m != null && m.HasProperty("_Color"))
                {
                    try
                    { //need try because can't garantee _Color is a color
                        return m.GetColor("_Color");
                    }
                    catch (Exception) { }
                }
            }
            return new Color(1,1,1,0);
        }
    }
}
