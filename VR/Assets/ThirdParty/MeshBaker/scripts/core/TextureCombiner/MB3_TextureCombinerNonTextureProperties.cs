using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DigitalOpus.MB.Core
{
    public class MB3_TextureCombinerNonTextureProperties
    {
        public interface MaterialProperty
        {
            string PropertyName { get; set; }
            MaterialPropertyValueAveraged GetAverageCalculator();
            object GetDefaultValue();
        }

        public class MaterialPropertyFloat : MaterialProperty
        {
            public string PropertyName { get; set; }
            MaterialPropertyValueAveragedFloat _averageCalc;
            float _defaultValue;

            public MaterialPropertyFloat(string name, float defValue)
            {
                _averageCalc = new MaterialPropertyValueAveragedFloat();
                _defaultValue = defValue;
                PropertyName = name;
            }

            public MaterialPropertyValueAveraged GetAverageCalculator()
            {
                return _averageCalc; 
            }

            public object GetDefaultValue()
            {
                return _defaultValue;
            }
        }

        public class MaterialPropertyColor : MaterialProperty
        {
            public string PropertyName { get; set; }
            MaterialPropertyValueAveragedColor _averageCalc;
            Color _defaultValue;

            public MaterialPropertyColor(string name, Color defaultVal)
            {
                _averageCalc = new MaterialPropertyValueAveragedColor();
                _defaultValue = defaultVal;
                PropertyName = name;
            }

            public MaterialPropertyValueAveraged GetAverageCalculator()
            {
                return _averageCalc;
            }

            public object GetDefaultValue()
            {
                return _defaultValue;
            }
        }

        public interface MaterialPropertyValueAveraged
        {
            void TryGetPropValueFromMaterialAndBlendIntoAverage(Material mat, MaterialProperty property);
            object GetAverage();
            int NumValues();
            void SetAverageValueOrDefaultOnMaterial(Material mat, MaterialProperty property);
        }

        public class MaterialPropertyValueAveragedFloat : MaterialPropertyValueAveraged
        {
            public float averageVal;
            public int numValues;

            public void TryGetPropValueFromMaterialAndBlendIntoAverage(Material mat, MaterialProperty property)
            {
                if (mat.HasProperty(property.PropertyName))
                {
                    float v = mat.GetFloat(property.PropertyName);
                    averageVal = averageVal * ((float)numValues) / (numValues + 1) + v / (numValues + 1);
                    numValues++;
                }
            }

            public object GetAverage()
            {
                return averageVal;
            }

            public int NumValues()
            {
                return numValues;
            }

            public void SetAverageValueOrDefaultOnMaterial(Material mat, MaterialProperty property)
            {
                if (mat.HasProperty(property.PropertyName))
                {
                    if (numValues > 0)
                    {
                        mat.SetFloat(property.PropertyName, averageVal);
                    } else
                    {
                        mat.SetFloat(property.PropertyName, (float)property.GetDefaultValue());
                    }
                }
            }
        }

        public class MaterialPropertyValueAveragedColor : MaterialPropertyValueAveraged
        {
            public Color averageVal;
            public int numValues;

            public void TryGetPropValueFromMaterialAndBlendIntoAverage(Material mat, MaterialProperty property)
            {
                if (mat.HasProperty(property.PropertyName))
                {
                    Color v = mat.GetColor(property.PropertyName);
                    averageVal = averageVal * ((float)numValues) / (numValues + 1) + v / (numValues + 1);
                    numValues++;
                }
            }

            public object GetAverage()
            {
                return averageVal;
            }

            public int NumValues()
            {
                return numValues;
            }

            public void SetAverageValueOrDefaultOnMaterial(Material mat, MaterialProperty property)
            {
                if (mat.HasProperty(property.PropertyName))
                {
                    if (numValues > 0)
                    {
                        mat.SetColor(property.PropertyName, averageVal);
                    }
                    else
                    {
                        mat.SetColor(property.PropertyName, (Color) property.GetDefaultValue());
                    }
                }
            }
        }

        public struct TexPropertyNameColorPair
        {
            public string name;
            public Color color;

            public TexPropertyNameColorPair(string nm, Color col)
            {
                name = nm;
                color = col;
            }
        }

        private interface NonTextureProperties
        {
            bool NonTexturePropertiesAreEqual(Material a, Material b);
            Texture2D TintTextureWithTextureCombiner(Texture2D t, MB_TexSet sourceMaterial, ShaderTextureProperty shaderPropertyName);
            void AdjustNonTextureProperties(Material resultMat, List<ShaderTextureProperty> texPropertyNames, MB2_EditorMethodsInterface editorMethods);
            Color GetColorForTemporaryTexture(Material matIfBlender, ShaderTextureProperty texProperty);
            Color GetColorAsItWouldAppearInAtlasIfNoTexture(Material matIfBlender, ShaderTextureProperty texProperty);
        }

        private class NonTexturePropertiesDontBlendProps : NonTextureProperties
        {
            MB3_TextureCombinerNonTextureProperties _textureProperties;

            public NonTexturePropertiesDontBlendProps(MB3_TextureCombinerNonTextureProperties textureProperties)
            {
                _textureProperties = textureProperties;
            }

            public bool NonTexturePropertiesAreEqual(Material a, Material b)
            {
                Debug.Assert(_textureProperties._considerNonTextureProperties == false);
                return true;
            }

            public Texture2D TintTextureWithTextureCombiner(Texture2D t, MB_TexSet sourceMaterial, ShaderTextureProperty shaderPropertyName)
            {
                Debug.Assert(_textureProperties._considerNonTextureProperties == false);
                Debug.LogError("TintTextureWithTextureCombiner should never be called if resultMaterialTextureBlender is null");
                return t;
            }

            public void AdjustNonTextureProperties(Material resultMat, List<ShaderTextureProperty> texPropertyNames, MB2_EditorMethodsInterface editorMethods)
            {
                Debug.Assert(_textureProperties._considerNonTextureProperties == false);
                if (resultMat == null || texPropertyNames == null) return;
                for (int nonTexPropIdx = 0; nonTexPropIdx < _textureProperties._nonTextureProperties.Length; nonTexPropIdx++)
                {
                    MaterialProperty nonTexProperty = _textureProperties._nonTextureProperties[nonTexPropIdx];
                    if (resultMat.HasProperty(nonTexProperty.PropertyName))
                    {
                        nonTexProperty.GetAverageCalculator().SetAverageValueOrDefaultOnMaterial(resultMat, nonTexProperty);
                    }
                }

                if (editorMethods != null)
                {
                    editorMethods.CommitChangesToAssets();
                }
            }

            public Color GetColorAsItWouldAppearInAtlasIfNoTexture(Material matIfBlender, ShaderTextureProperty texProperty)
            {
                Debug.Assert(false, "Should never be called");
                return Color.white;
            }

            public Color GetColorForTemporaryTexture(Material matIfBlender, ShaderTextureProperty texProperty)
            {
                Debug.Assert(_textureProperties._considerNonTextureProperties == false);
                if (texProperty.isNormalMap)
                {
                    return NEUTRAL_NORMAL_MAP_COLOR;
                }

                else if (_textureProperties.textureProperty2DefaultColorMap.ContainsKey(texProperty.name))
                {
                    return _textureProperties.textureProperty2DefaultColorMap[texProperty.name];
                }

                return new Color(1f, 1f, 1f, 0f);
            }
        }

        private class NonTexturePropertiesBlendProps : NonTextureProperties
        {
            MB3_TextureCombinerNonTextureProperties _textureProperties;
            TextureBlender resultMaterialTextureBlender;

            public NonTexturePropertiesBlendProps(MB3_TextureCombinerNonTextureProperties textureProperties, TextureBlender resultMats)
            {
                resultMaterialTextureBlender = resultMats;
                _textureProperties = textureProperties;
            }

            public bool NonTexturePropertiesAreEqual(Material a, Material b)
            {
                Debug.Assert(resultMaterialTextureBlender != null);
                return resultMaterialTextureBlender.NonTexturePropertiesAreEqual(a, b);
            }

            public Texture2D TintTextureWithTextureCombiner(Texture2D t, MB_TexSet sourceMaterial, ShaderTextureProperty shaderPropertyName)
            {
                Debug.Assert(resultMaterialTextureBlender != null);
                resultMaterialTextureBlender.OnBeforeTintTexture(sourceMaterial.matsAndGOs.mats[0].mat, shaderPropertyName.name);
                if (_textureProperties.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(string.Format("Blending texture {0} mat {1} with non-texture properties using TextureBlender {2}", t.name, sourceMaterial.matsAndGOs.mats[0].mat, resultMaterialTextureBlender));
                for (int i = 0; i < t.height; i++)
                {
                    Color[] cs = t.GetPixels(0, i, t.width, 1);
                    for (int j = 0; j < cs.Length; j++)
                    {
                        cs[j] = resultMaterialTextureBlender.OnBlendTexturePixel(shaderPropertyName.name, cs[j]);
                    }
                    t.SetPixels(0, i, t.width, 1, cs);
                }
                t.Apply();
                return t;
            }

            public void AdjustNonTextureProperties(Material resultMat, List<ShaderTextureProperty> texPropertyNames, MB2_EditorMethodsInterface editorMethods)
            {
                if (resultMat == null || texPropertyNames == null) return;

                //try to use a texture blender if we can find one to set the non-texture property values
                if (_textureProperties.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Adjusting non texture properties using TextureBlender for shader: " + resultMat.shader.name);
                resultMaterialTextureBlender.SetNonTexturePropertyValuesOnResultMaterial(resultMat);

                if (editorMethods != null)
                {
                    editorMethods.CommitChangesToAssets();
                }
            }

            public Color GetColorAsItWouldAppearInAtlasIfNoTexture(Material matIfBlender, ShaderTextureProperty texProperty)
            {
                resultMaterialTextureBlender.OnBeforeTintTexture(matIfBlender, texProperty.name);
                Color c = GetColorForTemporaryTexture(matIfBlender, texProperty);
                return resultMaterialTextureBlender.OnBlendTexturePixel(texProperty.name, c);
            }

            public Color GetColorForTemporaryTexture(Material matIfBlender, ShaderTextureProperty texProperty)
            {
                return resultMaterialTextureBlender.GetColorIfNoTexture(matIfBlender, texProperty);
            }
        }

        public static Color NEUTRAL_NORMAL_MAP_COLOR = new Color(.5f, .5f, 1f);

        TexPropertyNameColorPair[] defaultTextureProperty2DefaultColorMap = new TexPropertyNameColorPair[]
        {
            new TexPropertyNameColorPair("_MainTex", new Color(1f, 1f, 1f, 0f)),
            new TexPropertyNameColorPair("_MetallicGlossMap", new Color(0f, 0f, 0f, 1f)),
            new TexPropertyNameColorPair("_ParallaxMap", new Color(0f, 0f, 0f, 0f)),
            new TexPropertyNameColorPair("_OcclusionMap",  new Color(1f, 1f, 1f, 1f)),
            new TexPropertyNameColorPair("_EmissionMap", new Color(0f, 0f, 0f, 0f)),
            new TexPropertyNameColorPair("_DetailMask", new Color(0f, 0f, 0f, 0f)),
        };

        MB3_TextureCombinerNonTextureProperties.MaterialProperty[] _nonTextureProperties = new MB3_TextureCombinerNonTextureProperties.MaterialProperty[]
        {
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyColor("_Color", Color.white),
            //new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_Cutoff"),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_Glossiness", .5f),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_GlossMapScale", 1f),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_Metallic", 0f),
            //new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_SpecularHightlights"),
            //new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_GlossyReflections"),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_BumpScale", .1f),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_Parallax", .02f),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyFloat("_OcclusionStrength", 1f),
            new MB3_TextureCombinerNonTextureProperties.MaterialPropertyColor("_EmissionColor", Color.black),
        };

        MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;
        bool _considerNonTextureProperties = false;
        private TextureBlender resultMaterialTextureBlender;
        private TextureBlender[] textureBlenders = new TextureBlender[0];
        private Dictionary<string, Color> textureProperty2DefaultColorMap = new Dictionary<string, Color>();
        private NonTextureProperties _nonTexturePropertiesBlender;

        public MB3_TextureCombinerNonTextureProperties(MB2_LogLevel ll, bool considerNonTextureProps)
        {
            _considerNonTextureProperties = considerNonTextureProps;
            textureProperty2DefaultColorMap = new Dictionary<string, Color>();
            for (int i = 0; i < defaultTextureProperty2DefaultColorMap.Length; i++)
            {
                textureProperty2DefaultColorMap.Add(defaultTextureProperty2DefaultColorMap[i].name,
                                                    defaultTextureProperty2DefaultColorMap[i].color);
                _nonTexturePropertiesBlender = new NonTexturePropertiesDontBlendProps(this);
            }
        }

        internal void CollectAverageValuesOfNonTextureProperties(Material resultMaterial, Material mat)
        {
            for (int i = 0; i < _nonTextureProperties.Length; i++)
            {
                MB3_TextureCombinerNonTextureProperties.MaterialProperty prop = _nonTextureProperties[i];
                if (resultMaterial.HasProperty(prop.PropertyName))
                {
                    prop.GetAverageCalculator().TryGetPropValueFromMaterialAndBlendIntoAverage(mat, prop);
                }
            }
        }

        internal void LoadTextureBlendersIfNeeded(Material resultMaterial)
        {
            if (_considerNonTextureProperties)
            {
                LoadTextureBlenders();
                FindBestTextureBlender(resultMaterial);
            }
        }

#if UNITY_WSA && !UNITY_EDITOR
        //not defined for WSA runtime
#else 
        private static bool InterfaceFilter(Type typeObj, System.Object criteriaObj)
        {
            return typeObj.ToString() == criteriaObj.ToString();
        }
#endif

        private void FindBestTextureBlender(Material resultMaterial)
        {
            Debug.Assert(_considerNonTextureProperties);
            resultMaterialTextureBlender = FindMatchingTextureBlender(resultMaterial.shader.name);
            if (resultMaterialTextureBlender != null)
            {
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Using Consider Non-Texture Properties found a TextureBlender for result material. Using: " + resultMaterialTextureBlender);
            }
            else
            {
                if (LOG_LEVEL >= MB2_LogLevel.error) Debug.LogWarning("Using _considerNonTextureProperties could not find a TextureBlender that matches the shader on the result material. Using the Fallback Texture Blender.");
                resultMaterialTextureBlender = new TextureBlenderFallback();
            }
            _nonTexturePropertiesBlender = new NonTexturePropertiesBlendProps(this, resultMaterialTextureBlender);
        }

        private void LoadTextureBlenders()
        {
#if UNITY_WSA && !UNITY_EDITOR
        //not defined for WSA runtime
#else   
            Debug.Assert(_considerNonTextureProperties);
            string qualifiedInterfaceName = "DigitalOpus.MB.Core.TextureBlender";
            var interfaceFilter = new TypeFilter(InterfaceFilter);
            List<Type> types = new List<Type>();
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Collections.IEnumerable typesIterator = null;
                try
                {
                    typesIterator = ass.GetTypes();
                }
                catch (Exception e)
                {
                    //Debug.Log("The assembly that I could not read types for was: " + ass.GetName());
                    //suppress error
                    e.Equals(null);
                }
                if (typesIterator != null)
                {
                    foreach (Type ty in ass.GetTypes())
                    {
                        var myInterfaces = ty.FindInterfaces(interfaceFilter, qualifiedInterfaceName);
                        if (myInterfaces.Length > 0)
                        {
                            types.Add(ty);
                        }
                    }
                }
            }

            TextureBlender fallbackTB = null;
            List<TextureBlender> textureBlendersList = new List<TextureBlender>();
            foreach (Type tt in types)
            {
                if (!tt.IsAbstract && !tt.IsInterface)
                {
                    TextureBlender instance = (TextureBlender) System.Activator.CreateInstance(tt);
                    if (instance is TextureBlenderFallback)
                    {
                        fallbackTB = instance;
                    }
                    else
                    {
                        textureBlendersList.Add(instance);
                    }
                }
            }

            if (fallbackTB != null) textureBlendersList.Add(fallbackTB); // must come last in list
            textureBlenders = textureBlendersList.ToArray();
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                Debug.Log(string.Format("Loaded {0} TextureBlenders.", textureBlenders.Length));
            }
#endif
        }

        internal bool NonTexturePropertiesAreEqual(Material a, Material b)
        {
            return _nonTexturePropertiesBlender.NonTexturePropertiesAreEqual(a, b);
        }

        internal Texture2D TintTextureWithTextureCombiner(Texture2D t, MB_TexSet sourceMaterial, ShaderTextureProperty shaderPropertyName)
        {
            return _nonTexturePropertiesBlender.TintTextureWithTextureCombiner(t, sourceMaterial, shaderPropertyName);
        }

        //If we are switching from a Material that uses color properties to
        //using atlases don't want some properties such as _Color to be copied
        //from the original material because the atlas texture will be multiplied
        //by that color
        internal void AdjustNonTextureProperties(Material resultMat, List<ShaderTextureProperty> texPropertyNames, MB2_EditorMethodsInterface editorMethods)
        {
            if (resultMat == null || texPropertyNames == null) return;
            _nonTexturePropertiesBlender.AdjustNonTextureProperties(resultMat, texPropertyNames, editorMethods);
        }

        internal Color GetColorAsItWouldAppearInAtlasIfNoTexture(Material matIfBlender, ShaderTextureProperty texProperty)
        {
            return _nonTexturePropertiesBlender.GetColorAsItWouldAppearInAtlasIfNoTexture(matIfBlender, texProperty);
        }

        internal Color GetColorForTemporaryTexture(Material matIfBlender, ShaderTextureProperty texProperty)
        {
            return _nonTexturePropertiesBlender.GetColorForTemporaryTexture(matIfBlender, texProperty);
        }

        private TextureBlender FindMatchingTextureBlender(string shaderName)
        {
            for (int i = 0; i < textureBlenders.Length; i++)
            {
                if (textureBlenders[i].DoesShaderNameMatch(shaderName))
                {
                    return textureBlenders[i];
                }
            }
            return null;
        }


    }
}
