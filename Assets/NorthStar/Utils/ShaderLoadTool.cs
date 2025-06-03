// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
namespace NorthStar
{
    /// <summary>
    /// Provides a simple GUI for loading and processing shader data that was gathered during a playthrough via adb logcat 
    /// with the "Log Shader Compilation" option enabled in the Graphics settings
    /// 
    /// We used this way of gathering shader variants due to differences between editor and device shaders on Android causing
    /// issues with hitches. We later used this tool to help with PSO warmup to prevent hitching during gameplay
    /// </summary>
    public class ShaderLoadTool : EditorWindow
    {
        private TextAsset m_txtAsset;
        private ShaderVariantCollection m_variants;
        private List<ShaderVariantCollectionSO.ShaderData> m_shaderList = new();
        private ShaderVariantCollectionSO m_variantSO;
        private Dictionary<string, PassType> m_passMappings = new();

        private Vector2 m_scrollPos;

        [MenuItem("Tools/NorthStar/Shader load tool")]
        public static void GetWindow()
        {
            _ = GetWindow(typeof(ShaderLoadTool));
        }
        private void OnGUI()
        {
            m_txtAsset = EditorGUILayout.ObjectField(m_txtAsset, typeof(TextAsset), true) as TextAsset;
            m_variantSO = EditorGUILayout.ObjectField(m_variantSO, typeof(ShaderVariantCollectionSO), true) as ShaderVariantCollectionSO;


            if (m_txtAsset != null)
                if (GUILayout.Button("Load from Log file"))
                {
                    PopulateListFromText();
                }
            if (m_variantSO != null)
                if (GUILayout.Button("Load from SO"))
                {
                    if (m_variantSO != null)
                    {
                        m_shaderList = m_variantSO.Shaders;
                    }
                }

            if (m_shaderList.Count == 0)
                return;

            var passSet = new HashSet<string>();
            foreach (var shader in m_shaderList)
            {
                _ = passSet.Add(shader.Pass);
            }
            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);
            foreach (var pass in passSet)
            {
                if (!m_passMappings.ContainsKey(pass))
                    m_passMappings.Add(pass, new PassType());
                GUILayout.BeginHorizontal();
                GUILayout.Label(pass);
                m_passMappings[pass] = (PassType)EditorGUILayout.EnumPopup(m_passMappings[pass], GUILayout.Width(300));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            m_variants = EditorGUILayout.ObjectField(m_variants, typeof(ShaderVariantCollection), true) as ShaderVariantCollection;
            if (m_variants != null)
                if (GUILayout.Button("Export to collection"))
                    ExportToCollection();
            if (m_variantSO != null)
                if (GUILayout.Button("Export to SO"))
                {
                    ExportToSO();
                }
        }

        private void ExportToCollection()
        {
            if (m_variants == null)
                return;
            m_variants.Clear();

            foreach (var data in m_shaderList)
            {
                var variant = new ShaderVariantCollection.ShaderVariant();
                var shader = Shader.Find(data.Name);
                if (shader == null)
                    continue;
                variant.shader = shader;

                if (!m_passMappings.ContainsKey(data.Pass))
                    continue;
                variant.passType = m_passMappings[data.Pass];

                variant.keywords = (string[])data.Keywords.Clone();

                _ = m_variants.Add(variant);
            }

            EditorUtility.SetDirty(m_variants);
        }

        private void ExportToSO()
        {
            foreach (var data in m_shaderList)
            {
                if (m_passMappings.TryGetValue(data.Pass, out var passType))
                {
                    data.PassType = passType;
                }
            }
            m_variantSO.Shaders = m_shaderList;
            EditorUtility.SetDirty(m_variantSO);
        }

        private ShaderVariantCollectionSO.ShaderData ParseShaderData(string shaderLine)
        {
            var data = new ShaderVariantCollectionSO.ShaderData();
            var segments = shaderLine.Split(", ");

            for (var i = 0; i < segments.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        data.Name = segments[i]; break;
                    case 1:
                        {
                            var passStr = segments[i];
                            data.Pass = passStr.Replace("pass: ", "");
                            if (data.Pass.Contains("<Unnamed"))
                                data.Pass = $"{data.Name}: {data.Pass}";
                            break;
                        }
                    case 2:
                        break;
                    case 3:
                        {
                            var keywordsStr = segments[i];
                            keywordsStr = keywordsStr.Replace("keywords ", "");
                            var keywords = new List<string>();
                            if (keywordsStr != "<no keywords>")
                            {
                                foreach (var keyword in keywordsStr.Split(" "))
                                {
                                    keywords.Add(keyword);
                                }
                            }
                            data.Keywords = keywords.ToArray();
                            break;
                        }
                }
            }
            return data;
        }

        private void PopulateListFromText()
        {
            if (m_txtAsset == null)
                return;

            m_shaderList.Clear();

            var lines = m_txtAsset.text.Replace("\r", "").Split("\n");
            foreach (var line in lines)
            {
                var data = ParseShaderData(line.Trim());
                if (!data.Name.Contains("Hidden"))
                    m_shaderList.Add(data);
            }
        }
    }
}
#endif