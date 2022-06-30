using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
using DigitalOpus.MB.Core;

/// <summary>
/// This script should be added to each example scene for 2017+
/// It is hard to add because it destroys itself and is configured to [ExecuteInEditMode]
/// 
/// You need to set the MB_ALLOW_ADD_MIGRATE_MAT_SCRIPT in order to add this script and save the scene.
/// 
/// </summary>
[ExecuteInEditMode]
public class MB_MigrateMaterialsToDifferentPipeline : MonoBehaviour
{
#if UNITY_EDITOR
    void Start(){
#if UNITY_2018_4_OR_NEWER
        MBVersion.PipelineType pipelineType = MBVersion.DetectPipeline();
        int materialPipeline = GetMaterialPipelineType();
        // Do not prompt if example materials cannot be found, are using an unsupported shader, or are already suitable for the current pipeline
        if (materialPipeline != -1 && pipelineType != (MBVersion.PipelineType)materialPipeline)
        {
            if(pipelineType != MBVersion.PipelineType.Unsupported)
            {
                bool convert = EditorUtility.DisplayDialog(
                    "Different Pipline Detected",
                    "Would you like to try converting the default Mesh Baker example scene materials to suit the " + pipelineType + " pipeline?",
                    "Yes",
                    "No");

                if (convert){
                    MigrateMaterials();
                    if(pipelineType == MBVersion.PipelineType.HDRP)
                    {
                        Debug.LogWarning("If the Mesh Baker Example Scenes appear dark, try adding an Empty GameObject to the scene, adding a 'Volume' component to it, and assigning that volume one of the default profiles.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Mesh Baker's example scenes only support the Default Pipeline, URP, and HDRP.");
            }
        }
#endif
#if !MB_ALLOW_ADD_MIGRATE_MAT_SCRIPT
        // Add the MB_ALLOW_ADD_MIGRATE_MAT_SCRIPT to script defines to add this to scenes.
        DestroyImmediate(this.gameObject);
#endif
    }

    public string[][] shaderMap_Unsupported_Default_URP_HDRP = new string[][]
    {
        new string[] { "", "Legacy Shaders/Self-Illumin/Bumped Diffuse",    "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Transparent/VertexLit",          "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Lightmapped/Specular",           "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Diffuse",                        "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Bumped Specular",                "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Mobile/Diffuse",                                "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Universal Render Pipeline/Lit",                 "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Standard",                                      "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Nature/Tree Creator Leaves Fast",               "Universal Render Pipeline/Nature/SpeedTree8",  "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Bumped Diffuse",                 "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Reflective/Diffuse",             "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "Legacy Shaders/Specular",                       "Universal Render Pipeline/Lit",                "HDRP/Lit"},
        new string[] { "", "MeshBaker/Examples/UnlitTextureArray",          "Universal Render Pipeline/Lit",                "HDRP/Lit"}
    };

    string[][] propNameMap_Unsupported_Default_URP_HDRP = new string[][]
    {
        new string[]{ "_Color",     "_Color",       "_BaseColor",   "_BaseColor"}, // (Color)
        new string[]{ "_MainTex",   "_MainTex",     "_BaseMap",     "_BaseColorMap"}, // (Texture)
        new string[]{ "_Albedo",    "_Albedo",      "_BaseMap",     "_BaseColorMap" }, // (Texture)
        new string[]{ "_BumpMap",   "_BumpMap",     "_BumpMap",     "_NormalMap"}, // (Texture)
        new string[]{ "_Illum",     "_Illum" ,      "_EmissionMap", "_EmissiveColorMap"}, // (Texture)
        new string[]{ "_LightMap",  "_LightMap" ,   "",             ""} // No mapping, not going to assign lightmap to the new shader
    };

    [ContextMenu("Create Unity Package For All Mats In Examples")]
    void CreatePackageForExampleMaterials()
    {
        // Check if examples folder is "Assets/MeshBaker/Examples"
        // Get all materials in examples.
        string unityPackageFilename;
        string examplesPathRelative = GetRelativeExamplesPath();
        if(examplesPathRelative == null) {
            Debug.LogError("Cannot package example materials as no Mesh Baker Example scenes were found. Renaming example scenes may cause this error.");
            return;
        }

        MBVersion.PipelineType pipelineType = MBVersion.DetectPipeline();
        if (pipelineType == MBVersion.PipelineType.Default)
        {
            unityPackageFilename = examplesPathRelative + "/Examples_Default.unitypackage";
        } else if (pipelineType == MBVersion.PipelineType.HDRP)
        {
            unityPackageFilename = examplesPathRelative + "/Examples_HDRP.unitypackage";
        } else if (pipelineType == MBVersion.PipelineType.URP)
        {
            unityPackageFilename = examplesPathRelative + "/Examples_URP.unitypackage";
        } else
        {
            Debug.LogError("Unknown pipeline type.");
            return;
        }

        string[] matGIDs = AssetDatabase.FindAssets("t:Material", new string[] { examplesPathRelative });
        string[] assetPathNames = new string[matGIDs.Length];
        List<string> assetPathNamesList = new List<string>(); 
        for (int i = 0; i < matGIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(matGIDs[i]);
            if (!path.Contains("SceneBasicTextureArrayAssets") && !path.EndsWith(".fbx")) // don't do texture array assets.
            {
                assetPathNamesList.Add(path);
                // Debug.Log(path);
            }
        }
        assetPathNames = assetPathNamesList.ToArray();

        Debug.Log("Found " + assetPathNames.Length + " materials in " + examplesPathRelative);

        AssetDatabase.ExportPackage(assetPathNames, unityPackageFilename);

        Debug.Log("Create Unity Package: " + unityPackageFilename);
    }

    [ContextMenu("Migrate Materials In Examples Folder")]
    void MigrateMaterials()
    {
        HashSet<string> shaderNames = new HashSet<string>();

        string examplesPathRelative = GetRelativeExamplesPath();
        if(examplesPathRelative == null) {
            Debug.LogError("Cannot convert example materials as no Mesh Baker Example scenes were found. Renaming example scenes may cause this error.");
            return;
        }

        Debug.Log("Found Mesh Baker Examples at: " + examplesPathRelative);

        MBVersion.PipelineType pipelineType = MBVersion.DetectPipeline();

        Material[] mats;
        {
            string[] matGIDs = AssetDatabase.FindAssets("t:Material", new string[] { examplesPathRelative });
            List<Material> matList = new List<Material>();
            for (int i = 0; i < matGIDs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(matGIDs[i]);
                if (!path.Contains("SceneBasicTextureArrayAssets") && !path.EndsWith(".fbx")) // don't do texture array assets.
                {
                    matList.Add(AssetDatabase.LoadAssetAtPath<Material>(path));
                    string shaderName = matList[matList.Count - 1].shader.name;
                    shaderNames.Add(shaderName);

                    if (MapDefault2OtherShader(shaderName, pipelineType) == null)
                    {
                        Debug.LogError("Could not find mapping for shader " + shaderName + " in mat " + path);
                        return;
                    }
                }
            }
            mats = matList.ToArray();
        }

        string nn = "";
        foreach (string n in shaderNames)
        {
            if (MapDefault2OtherShader(n, pipelineType) == null)
            {
                Debug.LogError("Could not find mapping for shader " + n);
                return;
            }

            nn += "'"+n+"' will map to '" + MapDefault2OtherShader(n, pipelineType) + "',\n";
        }

        Debug.Log(examplesPathRelative + " Found Mats: " + mats.Length + " Shaders Found " + nn);
        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];
            RemapTextures_Default2OtherPipeline(m, pipelineType);
            EditorUtility.SetDirty(m);
        }

        AssetDatabase.SaveAssets();
    }

    string MapDefault2OtherShader(string defaultName, MBVersion.PipelineType pipelineType)
    {
        // pipelineType
        // Unsupported = 0
        // Default = 1
        // URP = 2
        // HDRP = 3
        for (int i = 0; i < shaderMap_Unsupported_Default_URP_HDRP.Length; i++)
        {
            if (defaultName == shaderMap_Unsupported_Default_URP_HDRP[i][(int)MBVersion.PipelineType.Default])
            {
                return shaderMap_Unsupported_Default_URP_HDRP[i][(int)pipelineType];
            }
        }

        return null;
    }

    [ContextMenu("Find Examples Filepath")]
    string GetRelativeExamplesPath(){
        // One of the example scenes, it should always be found directly in the root Examples directory
        string[] targetGID = AssetDatabase.FindAssets("BatchPrepareObjectsForDynamicBatching t:SceneAsset");
        if (targetGID.Length != 1) {
            Debug.LogWarning("Could not find Mesh Baker Example Scenes");
            return null;
        }
        string relativeAssetPath = AssetDatabase.GUIDToAssetPath(targetGID[0]);

        // Parent directory should therefore give us a relative path to Examples, even if it is not in the default MeshBaker/Examples location
        string path = System.IO.Directory.GetParent(relativeAssetPath).ToString().Replace('\\', '/');
        
        // Debug.Log("Found Mesh Baker Examples at " + path);
        return path;
    }

    // Use a known material inside the examples to find out what pipeline the examples materials are set to
    // Returns as an int to handle the case of the examples directory missing or being altered, otherwise result can be cast to PipelineType
    int GetMaterialPipelineType(){
        string relativePath = GetRelativeExamplesPath();
        if(relativePath == null){
            return -1;
        }

        Material targetMat = AssetDatabase.LoadAssetAtPath<Material>(relativePath + "/Assets/Materials/firstFloorPlatform.mat");
        if(targetMat == null){
            Debug.LogWarning("Cannot get example material pipeline type. Contents of Mesh Baker Examples directory have changed.");
            return -1;
        }

        string shaderName = targetMat.shader.name;
        if(shaderName == "Legacy Shaders/Self-Illumin/Bumped Diffuse"){
            return 1;
        }
        if(shaderName == "Universal Render Pipeline/Lit"){
            return 2;
        }
        if(shaderName == "HDRP/Lit"){
            return 3;
        }

        return 0;
    }

    [ContextMenu("Test Transparency")]
    Material HandleHDRPTreeTransparency(Material m)
    {
        // Surface Type
        // 0 = Opaque
        // 1 = Transparent
        m.SetFloat("_SurfaceType", 1.0f);

        // Preserve Specular Lighting Checkbox
        // 0 = Unchecked
        // 1 = Checked
        m.SetFloat("_EnableBlendModePreserveSpecularLighting", 0.0f);

        return m;
    }

    void RemapTextures_Default2OtherPipeline(Material m, MBVersion.PipelineType pipelineType)
    {
#if !UNITY_2018_4_OR_NEWER
        Debug.LogError("Must use Unity 2018.4 or greater");
        return;
#else
        if(m.shader == null) {
            Debug.LogError("Null shader found in material " + m + ", skipping");
            return;
        }
        string oldShaderName = m.shader.name;
        string newShaderName = MapDefault2OtherShader(m.shader.name, pipelineType);
        if(newShaderName == null) {
            Debug.LogError("Could not find mapping for shader " + newShaderName + " in " + AssetDatabase.GetAssetPath(m) + ", skipping");
            return;
        }

        Dictionary<string, string> propMap = new Dictionary<string, string>();
        for (int i = 0; i < propNameMap_Unsupported_Default_URP_HDRP.Length; i++)
        {
            // pipelineType
            // Unsupported = 0
            // Default = 1
            // URP = 2
            // HDRP = 3
            propMap[propNameMap_Unsupported_Default_URP_HDRP[i][(int)MBVersion.PipelineType.Default]] = propNameMap_Unsupported_Default_URP_HDRP[i][(int)pipelineType];
        }

        Dictionary<string, Texture> texMap = new Dictionary<string, Texture>();
        Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
        for (int i = 0; i < propNameMap_Unsupported_Default_URP_HDRP.Length; i++)
        {
            string p = propNameMap_Unsupported_Default_URP_HDRP[i][(int)MBVersion.PipelineType.Default];
            if(m.HasProperty(p))
            {
                // So far the only property that we map of type (Color) is the main color, if this changes we might want to make a colorPropertyMap up top
                if(p == "_Color")
                {
                    Color c = m.GetColor(p);
                    if (m.HasProperty(p))
                    {
                        Debug.Assert(propMap.ContainsKey(p), "No mapping for " + p + " mat " + m + " " + m.shader);
                        //Debug.Log("Will Map " + p + " to " + colorMap[p]);
                        colorMap[propMap[p]] = c;
                    }
                }
                else
                {
                    Texture t = m.GetTexture(p);
                    if (t != null)
                    {
                        Debug.Assert(propMap.ContainsKey(p), "No mapping for " + p + " mat " + m + " " + m.shader);
                        //Debug.Log("Will Map " + p + " to " + propMap[p]);
                        texMap[propMap[p]] = t;
                    }
                }
            }
        }

        m.shader = Shader.Find(newShaderName);

        // Can be removed when HDRP adds a nice tree shader
        if(pipelineType == MBVersion.PipelineType.HDRP && oldShaderName == "Nature/Tree Creator Leaves Fast")
        {
            m = HandleHDRPTreeTransparency(m);
        }
        
        foreach (string p in texMap.Keys)
        {
            // Debug.Log("Mat " + m + " setting prop " + p + " to " + texMap[p]);
            m.SetTexture(p, texMap[p]);
        }
        foreach (string p in colorMap.Keys)
        {
            // Debug.Log("Mat " + m + " setting prop " + p + " to " + colorMap[p]);
            m.SetColor(p, colorMap[p]);
        }
#endif
    }
#endif
}
