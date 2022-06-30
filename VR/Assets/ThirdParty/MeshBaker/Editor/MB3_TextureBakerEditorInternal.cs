//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//---------------------------------------------- 
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DigitalOpus.MB.Core;

using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{
    public class MB3_TextureBakerEditorInternal
    {

        public class MultiMatSubmeshInfo
        {
            public Shader shader;
            public GameObjectFilterInfo.StandardShaderBlendMode stdBlendMode = GameObjectFilterInfo.StandardShaderBlendMode.NotApplicable;

            public MultiMatSubmeshInfo(Shader s, Material m)
            {
                shader = s;
                if (m.shader.name.StartsWith("Standard") && m.HasProperty("_Mode"))
                {
                    stdBlendMode = (GameObjectFilterInfo.StandardShaderBlendMode)m.GetFloat("_Mode");
                }
                else
                {
                    stdBlendMode = GameObjectFilterInfo.StandardShaderBlendMode.NotApplicable;
                }
            }

            public override bool Equals(object obj)
            {
                MultiMatSubmeshInfo b = (MultiMatSubmeshInfo)obj;
                return (stdBlendMode == b.stdBlendMode && shader == b.shader);
            }

            public override int GetHashCode()
            {
                return shader.GetHashCode() ^ (int)stdBlendMode;
            }
        }

        public class MeshSubmeshMaterial
        {
            public Mesh mesh;
            public Material srcMat;
            public int submesh;
            public bool obUVs;
            public GameObject srcGameObj;

            public MeshSubmeshMaterial(Mesh mesh, Material srcMat, int submesh, bool obUVs, GameObject srcGo)
            {
                this.mesh = mesh;
                this.srcMat = srcMat;
                this.submesh = submesh;
                this.obUVs = obUVs;
                this.srcGameObj = srcGo;
            }

            public override bool Equals(object obj)
            {
                var material = obj as MeshSubmeshMaterial;
                return material != null &&
                       EqualityComparer<Mesh>.Default.Equals(mesh, material.mesh) &&
                       EqualityComparer<Material>.Default.Equals(srcMat, material.srcMat) &&
                       submesh == material.submesh &&
                       obUVs == material.obUVs;
            }

            public override int GetHashCode()
            {
                var hashCode = -1560254091;
                hashCode = hashCode * -1521134295 + EqualityComparer<Mesh>.Default.GetHashCode(mesh);
                hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(srcMat);
                hashCode = hashCode * -1521134295 + submesh.GetHashCode();
                hashCode = hashCode * -1521134295 + obUVs.GetHashCode();
                return hashCode;
            }
        }

        //add option to exclude skinned mesh renderer and mesh renderer in filter
        //example scenes for multi material

        internal static GUIContent insertContent = new GUIContent("+", "add a material");
        internal static GUIContent deleteContent = new GUIContent("-", "delete a material");
        internal static GUILayoutOption buttonWidth = GUILayout.MaxWidth(20f);
        public static GUIContent noneContent = new GUIContent("");

        //private SerializedObject textureBaker;
        internal SerializedProperty logLevel, textureBakeResults, maxTilingBakeSize, maxAtlasSize,
            doMultiMaterial, doMultiMaterialSplitAtlasesIfTooBig, doMultiMaterialIfOBUVs, considerMeshUVs, resultMaterial, resultMaterials, atlasPadding,
            resizePowerOfTwoTextures, customShaderProperties, texturePropNamesToIgnore, objsToMesh, texturePackingAlgorithm, layerTexturePackerFastMesh, maxAtlasWidthOverride, maxAtlasHeightOverride, useMaxAtlasWidthOverride, useMaxAtlasHeightOverride,
            forcePowerOfTwoAtlas, considerNonTextureProperties, sortOrderAxis, resultType,
            resultMaterialsTexArray, textureArrayOutputFormats;

        internal bool resultMaterialsFoldout = true;
        internal bool showInstructions = false;
        internal bool showContainsReport = true;

        internal MB_EditorStyles editorStyles = new MB_EditorStyles();

        Color buttonColor = new Color(.8f, .8f, 1f, 1f);

        internal static GUIContent
            createPrefabAndMaterialLabelContent = new GUIContent("Create Empty Assets For Combined Material", "Creates a material asset and a 'MB2_TextureBakeResult' asset. You should set the shader on the material. Mesh Baker uses the Texture properties on the material to decide what atlases need to be created. The MB2_TextureBakeResult asset should be used in the 'Texture Bake Result' field."),
            logLevelContent = new GUIContent("Log Level"),
            openToolsWindowLabelContent = new GUIContent("Open Tools For Adding Objects", "Use these tools to find out what can be combined, discover possible problems with meshes, and quickly add objects."),
            fixOutOfBoundsGUIContent = new GUIContent("Consider Mesh UVs", "(Previously called 'fix out of bounds UVs') Textures copied to the atlas will be clipped to the mesh UV rectangle as well as the texture material tiling. This can have two effects:\n\n" +
                                                        "1) If the mesh only uses a small rectangle of it's source texture (atlas) then only that small rectangle will be copied to the atlas.\n\n" +
                                                        "2) If the mesh has uvs outside the 0,1 range (tiling) then this tiling will be copied to the atlas."),
            resizePowerOfTwoGUIContent = new GUIContent("Resize Power-Of-Two Textures", "Shrinks textures so they have a clear border of width 'Atlas Padding' around them. Improves texture packing efficiency."),
            customShaderPropertyNamesGUIContent = new GUIContent("Custom Shader Property Names", "Mesh Baker has a list of common texture properties that it looks for in shaders to generate atlases. Custom shaders may have texture properties not on this list. Add them here and Mesh Baker will generate atlases for them."),
            gc_texturePropNamesToIgnore = new GUIContent("Texture Properties To Ignore", "A list of material texture properties to ignore. Atlases will not be generated for these properties. Some materials have textures that are not positioned on the mesh using UV coordinates (for example cell shader color ramps or LUTs). Usually atlases should not be created for these properties."),
            combinedMaterialsGUIContent = new GUIContent("Combined Materials", "Use the +/- buttons to add multiple combined materials. You will also need to specify which materials on the source objects map to each combined material."),
            textureArrayCombinedMaterialFoldoutGUIContent = new GUIContent("Texture Array Combined Materials", "Use the +/- buttons to add texture array materials. You will also need to specify which materials on the source objects map to each texture array slice."),
            maxTilingBakeSizeGUIContent = new GUIContent("Max Tiling Bake Size", "This is the maximum size tiling textures will be baked to."),
            maxAtlasSizeGUIContent = new GUIContent("Max Atlas Size", "This is the maximum size of the atlas. If the atlas is larger than this, textures being added will be shrunk."),

            objectsToCombineGUIContent = new GUIContent("Objects To Be Combined", "These can be prefabs or scene objects. They must be game objects with Renderer components, not the parent objects. Materials on these objects will baked into the combined material(s)"),
            textureBakeResultsGUIContent = new GUIContent("Texture Bake Result", "This asset contains a mapping of materials to UV rectangles in the atlases. It is needed to create combined meshes or adjust meshes so they can use the combined material(s). Create it using 'Create Empty Assets For Combined Material'. Drag it to the 'Texture Bake Result' field to use it."),
            texturePackingAgorithmGUIContent = new GUIContent("Texture Packer", "Unity's PackTextures: Atlases are always a power of two. Can crash when trying to generate large atlases. \n\n " +
                                                              "Mesh Baker Texture Packer: Atlases will be most efficient size and shape (not limited to a power of two). More robust for large atlases. \n\n" +
                                                              "Mesh Baker Texture Packer Fast: Same as Mesh Baker Texture Packer but creates atlases on the graphics card using RenderTextures instead of the CPU. Source textures can be compressed. May not be pixel perfect. \n\n" +
                                                              "Mesh Baker Texture Packer Horizontal (Experimental): Packs all images vertically to allow horizontal-only UV-tiling.\n\n" +
                                                              "Mesh Baker Texture Packer Vertical (Experimental): Packs all images horizontally other to allow vertical-only UV-tiling.\n\n" +
                                                              "Mesh Baker Texture Packer Fast V2 (Experimental): A rewrite of 'Mesh Baker Texture Packer Fast' that is compatible with URP and HDRP and even faster.\n\n"),

            layerTexturePackerFastMeshGUIContent = new GUIContent("Atlas Render Layer", "Bad 'Atlas Render Layer' value. The atlas is rendered using a MeshRenderer in the scene. This MeshRenderer needs to be on a layer that is not used by any other renderers. If there are other renderers on this layer, those renderers could render in front of the atlas, which would ruin it."),
            configAtlasMultiMatsFromObjsContent = new GUIContent("Build Source To Combined Mapping From \n Objects To Be Combined", "This will group the materials on your source objects by shader and create one source to combined mapping for each shader found. For example if combining trees, then all the materials with the same bark shader will be grouped togther and all the materials with the same leaf material will be grouped together. You can adjust the results afterwards. \n\nIf Consider Mesh UVs is NOT checked, then submeshes with UVs outside 0,0..1,1 will be mapped to their own submesh regardless of shader."),
            configAtlasTextureSlicesFromObjsContent = new GUIContent("Build Texture Array Slices From \n Objects To Be Combined", "This will group the materials on your source objects by shader and create slices. Objects with out-of-bounds UVs will be put on their own slice. You can adjust the results afterwards."),
            forcePowerOfTwoAtlasContent = new GUIContent("Force Power-Of-Two Atlas", "Forces atlas x and y dimensions to be powers of two with aspect ratio 1:1,1:2 or 2:1. Unity recommends textures be a power of two for everything but GUI textures."),
            considerNonTexturePropertiesContent = new GUIContent("Blend Non-Texture Properties", "Will blend non-texture properties such as _Color, _Glossiness with the textures. Objects with different non-texture property values will be copied into different parts of the atlas even if they use the same textures. This feature requires that TextureBlenders " +
                                                            "exist for the result material shader. It is easy to extend Mesh Baker by writing custom TextureBlenders. Default TextureBlenders exist for: \n" +
                                                             "  - Standard \n" +
                                                             "  - Diffuse \n" +
                                                             "  - Bump Diffuse\n"),
            gc_SortAlongAxis = new GUIContent("Sort Along Axis", "Transparent materials often require that triangles be rendered far to near. Game Objects will be sorted along this axis. Triangles will be added to the combined mesh in this order."),
            gc_DoMultiMaterialSplitAtlasesIfTooBig = new GUIContent("Split Atlases If Textures Don't Fit", ""),
            gc_DoMultiMaterialSplitAtlasesIfOBUVs = new GUIContent("Put Meshes With Out Of Bounds UV On Submesh", ""),
            gc_overrideMaxAtlasWidth = new GUIContent("Override Max Atlas Width", "Set the maximum width of the atlas to this."),
            gc_overrideMaxAtlasHeight = new GUIContent("Override Max Atlas Height", "Set the maximum height of the atlas to this."),
            gc_useMaxAtlasWidthOverride = new GUIContent("Use Max Width Override", "Force the atlas width to not exceed the override value"),
            gc_useMaxAtlasHeightOverride = new GUIContent("Use Max Height Override", "Force the atlas width to not exceed the override value"),
            gc_atlasPadding = new GUIContent("Atlas Padding", "Number of pixels to pad around the edge of the atlas.");

        protected string layerTexturePackerFastMeshMessage;


        [MenuItem("GameObject/Create Other/Mesh Baker/TextureBaker", false, 100)]
        public static void CreateNewTextureBaker()
        {
            MB3_TextureBaker[] mbs = (MB3_TextureBaker[])Editor.FindObjectsOfType(typeof(MB3_TextureBaker));
            Regex regex = new Regex(@"\((\d+)\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            int largest = 0;
            try
            {
                for (int i = 0; i < mbs.Length; i++)
                {
                    Match match = regex.Match(mbs[i].name);
                    if (match.Success)
                    {
                        int val = Convert.ToInt32(match.Groups[1].Value);
                        if (val >= largest)
                            largest = val + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex == null) ex = null; //Do nothing supress compiler warning
            }
            GameObject nmb = new GameObject("TextureBaker (" + largest + ")");
            nmb.transform.position = Vector3.zero;
            nmb.AddComponent<MB3_MeshBakerGrouper>();
            MB3_TextureBaker tb = nmb.AddComponent<MB3_TextureBaker>();
            tb.packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
        }

        void _init(SerializedObject textureBaker)
        {
            //textureBaker = new SerializedObject(target);
            logLevel = textureBaker.FindProperty("LOG_LEVEL");
            doMultiMaterial = textureBaker.FindProperty("_doMultiMaterial");
            doMultiMaterialSplitAtlasesIfTooBig = textureBaker.FindProperty("_doMultiMaterialSplitAtlasesIfTooBig");
            doMultiMaterialIfOBUVs = textureBaker.FindProperty("_doMultiMaterialSplitAtlasesIfOBUVs");
            considerMeshUVs = textureBaker.FindProperty("_fixOutOfBoundsUVs");
            resultMaterial = textureBaker.FindProperty("_resultMaterial");
            resultMaterials = textureBaker.FindProperty("resultMaterials");
            atlasPadding = textureBaker.FindProperty("_atlasPadding");
            resizePowerOfTwoTextures = textureBaker.FindProperty("_resizePowerOfTwoTextures");
            customShaderProperties = textureBaker.FindProperty("_customShaderProperties");
            texturePropNamesToIgnore = textureBaker.FindProperty("_texturePropNamesToIgnore");
            objsToMesh = textureBaker.FindProperty("objsToMesh");
            maxTilingBakeSize = textureBaker.FindProperty("_maxTilingBakeSize");
            maxAtlasSize = textureBaker.FindProperty("_maxAtlasSize");
            maxAtlasWidthOverride = textureBaker.FindProperty("_maxAtlasWidthOverride");
            maxAtlasHeightOverride = textureBaker.FindProperty("_maxAtlasHeightOverride");
            useMaxAtlasWidthOverride = textureBaker.FindProperty("_useMaxAtlasWidthOverride");
            useMaxAtlasHeightOverride = textureBaker.FindProperty("_useMaxAtlasHeightOverride");
            textureBakeResults = textureBaker.FindProperty("_textureBakeResults");
            texturePackingAlgorithm = textureBaker.FindProperty("_packingAlgorithm");
            layerTexturePackerFastMesh = textureBaker.FindProperty("_layerTexturePackerFastMesh");
            forcePowerOfTwoAtlas = textureBaker.FindProperty("_meshBakerTexturePackerForcePowerOfTwo");
            considerNonTextureProperties = textureBaker.FindProperty("_considerNonTextureProperties");
            sortOrderAxis = textureBaker.FindProperty("sortAxis");
            resultType = textureBaker.FindProperty("_resultType");
            resultMaterialsTexArray = textureBaker.FindProperty("resultMaterialsTexArray");
            textureArrayOutputFormats = textureBaker.FindProperty("textureArrayOutputFormats");
        }

        public void OnEnable(SerializedObject textureBaker)
        {
            _init(textureBaker);
            if (editorStyles == null) editorStyles = new MB_EditorStyles();
            editorStyles.Init();

        }

        public void OnDisable()
        {
            editorStyles.DestroyTextures();
        }

        public void DrawGUI(SerializedObject textureBaker, MB3_TextureBaker momm, UnityEngine.Object[] targets, System.Type editorWindow)
        {
            if (textureBaker == null)
            {
                return;
            }
            textureBaker.Update();


            showInstructions = EditorGUILayout.Foldout(showInstructions, "Instructions:");
            if (showInstructions)
            {
                EditorGUILayout.HelpBox("1. Add scene objects or prefabs to combine. For best results these should use the same shader as Combined Mesh Material.\n\n" +
                                        "2. Create Empty Assets for Combined Mesh Material(s)\n\n" +
                                        "3. Check that shader on Combined Mesh Material(s) are correct.\n\n" +
                                        "4. Bake materials into Combined Mesh Material(s).\n\n" +
                                        "5. Look at warnings/errors in console. Decide if action needs to be taken.\n\n" +
                                        "6. You are now ready to bake combined meshes or batch bake prefabs.", UnityEditor.MessageType.None);

            }

            EditorGUILayout.PropertyField(logLevel, logLevelContent);
            EditorGUILayout.Separator();

            // Selected objects
            EditorGUILayout.BeginVertical(editorStyles.editorBoxBackgroundStyle);
            EditorGUILayout.LabelField("Objects To Be Combined", EditorStyles.boldLabel);
            if (GUILayout.Button(openToolsWindowLabelContent))
            {
                MB3_MeshBakerEditorWindow mmWin = (MB3_MeshBakerEditorWindow) EditorWindow.GetWindow(editorWindow);
                mmWin.SetTarget((MB3_MeshBakerRoot) momm);
            }

            object[] objs = MB3_EditorMethods.DropZone("Drag & Drop Renderers or Parents\n" + "HERE\n" +
                "to add objects to be combined", 300, 50);
            MB3_EditorMethods.AddDroppedObjects(objs, momm);

            EditorGUILayout.PropertyField(objsToMesh, objectsToCombineGUIContent, true);
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Objects In Scene"))
            {
                List<MB3_TextureBaker> selectedBakers = _getBakersFromTargets(targets);
                List<GameObject> obsToCombine = new List<GameObject>();

                foreach (MB3_TextureBaker baker in selectedBakers) obsToCombine.AddRange(baker.GetObjectsToCombine());
                Selection.objects = obsToCombine.ToArray();
                if (momm.GetObjectsToCombine().Count > 0)
                {
                    SceneView.lastActiveSceneView.pivot = momm.GetObjectsToCombine()[0].transform.position;
                }

            }
            if (GUILayout.Button(gc_SortAlongAxis))
            {
                MB3_MeshBakerRoot.ZSortObjects sorter = new MB3_MeshBakerRoot.ZSortObjects();
                sorter.sortAxis = sortOrderAxis.vector3Value;
                sorter.SortByDistanceAlongAxis(momm.GetObjectsToCombine());
            }
            EditorGUILayout.PropertyField(sortOrderAxis, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();


            // output
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(resultType);

            if (GUILayout.Button(createPrefabAndMaterialLabelContent))
            {
                List<MB3_TextureBaker> selectedBakers = _getBakersFromTargets(targets);
                string newPrefabPath = EditorUtility.SaveFilePanelInProject("Asset name", "", "asset", "Enter a name for the baked texture results");
                if (newPrefabPath != null)
                {
                    for (int i = 0; i < selectedBakers.Count; i++)
                    {
                        CreateCombinedMaterialAssets(selectedBakers[i], newPrefabPath, i == 0 ? true : false);
                    }
                }
            }

            EditorGUILayout.PropertyField(textureBakeResults, textureBakeResultsGUIContent);
            if (textureBakeResults.objectReferenceValue != null)
            {
                showContainsReport = EditorGUILayout.Foldout(showContainsReport, "Shaders & Materials Contained");
                if (showContainsReport)
                {
                    EditorGUILayout.HelpBox(((MB2_TextureBakeResults)textureBakeResults.objectReferenceValue).GetDescription(), MessageType.Info);
                }
            }

            // Confusion warning (don't use resultType.enumValueIndex. It is the index in the list of display names. NOT the enum value)
            if (resultType.intValue == (int) MB2_TextureBakeResults.ResultType.textureArray)
            {
                MB_TextureBakerConfigureTextureArrays.DrawTextureArrayConfiguration(momm, textureBaker, this);
            }
            else
            {
                EditorGUILayout.PropertyField(doMultiMaterial, new GUIContent("Multiple Combined Materials"));

                if (momm.doMultiMaterial)
                {
                    MB_TextureBakerEditorConfigureMultiMaterials.DrawMultipleMaterialsMappings(momm, textureBaker, this);
                }
                else
                {
                    EditorGUILayout.PropertyField(resultMaterial, new GUIContent("Combined Mesh Material"));
                }
            }

            // settings
            int labelWidth = 200;
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(editorStyles.editorBoxBackgroundStyle);
            EditorGUILayout.LabelField("Material Bake Options", EditorStyles.boldLabel);

            DrawPropertyFieldWithLabelWidth(atlasPadding, gc_atlasPadding, labelWidth);
            DrawPropertyFieldWithLabelWidth(maxAtlasSize, maxAtlasSizeGUIContent, labelWidth);
            DrawPropertyFieldWithLabelWidth(resizePowerOfTwoTextures, resizePowerOfTwoGUIContent, labelWidth);
            DrawPropertyFieldWithLabelWidth(maxTilingBakeSize, maxTilingBakeSizeGUIContent, labelWidth);
            EditorGUI.BeginDisabledGroup(momm.doMultiMaterial);
            DrawPropertyFieldWithLabelWidth(considerMeshUVs, fixOutOfBoundsGUIContent, labelWidth);
            EditorGUI.EndDisabledGroup();
            if (texturePackingAlgorithm.intValue == (int)MB2_PackingAlgorithmEnum.MeshBakerTexturePacker ||
                texturePackingAlgorithm.intValue == (int)MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Fast ||
                texturePackingAlgorithm.intValue == (int)MB2_PackingAlgorithmEnum.MeshBakerTexturePaker_Fast_V2_Beta)
            {
                DrawPropertyFieldWithLabelWidth(forcePowerOfTwoAtlas, forcePowerOfTwoAtlasContent, labelWidth);
            }
            DrawPropertyFieldWithLabelWidth(considerNonTextureProperties, considerNonTexturePropertiesContent, labelWidth);
            if (texturePackingAlgorithm.intValue == (int)MB2_PackingAlgorithmEnum.UnitysPackTextures)
            {
                EditorGUILayout.HelpBox("Unity's texture packer has memory problems and frequently crashes the editor.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(texturePackingAlgorithm, texturePackingAgorithmGUIContent);
            if (MB3_TextureCombinerPipeline.USE_EXPERIMENTAL_HOIZONTALVERTICAL)
            {
            	// Confusion warning (don't use texturePackingAlgorithm.enumValueIndex. It is the index in the list of display names. NOT the enum value)
                if (texturePackingAlgorithm.intValue == (int)MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Horizontal)
                {

                    EditorGUILayout.PropertyField(useMaxAtlasWidthOverride, gc_useMaxAtlasWidthOverride);
                    if (!useMaxAtlasWidthOverride.boolValue) EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(maxAtlasWidthOverride, gc_overrideMaxAtlasWidth);
                    if (!useMaxAtlasWidthOverride.boolValue) EditorGUI.EndDisabledGroup();
                }
                else if (texturePackingAlgorithm.intValue == (int)MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Vertical)
                {
                    EditorGUILayout.PropertyField(useMaxAtlasHeightOverride, gc_useMaxAtlasHeightOverride);
                    if (!useMaxAtlasHeightOverride.boolValue) EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(maxAtlasHeightOverride, gc_overrideMaxAtlasHeight);
                    if (!useMaxAtlasHeightOverride.boolValue) EditorGUI.EndDisabledGroup();
                }
            }

            if (texturePackingAlgorithm.intValue == (int) MB2_PackingAlgorithmEnum.MeshBakerTexturePaker_Fast_V2_Beta)
            {
                // layer field
                int newValueLayerTexturePackerFastMesh = EditorGUILayout.LayerField(layerTexturePackerFastMeshGUIContent, layerTexturePackerFastMesh.intValue);
                bool isNewValue = newValueLayerTexturePackerFastMesh == layerTexturePackerFastMesh.intValue;
                layerTexturePackerFastMesh.intValue = newValueLayerTexturePackerFastMesh;
                if (isNewValue)
                {
                    Renderer[] rs = GameObject.FindObjectsOfType<Renderer>();
                    int numRenderersOnLayer = 0;
                    for (int i = 0; i < rs.Length; i++)
                    {
                        if (rs[i].gameObject.layer == newValueLayerTexturePackerFastMesh) numRenderersOnLayer++;
                    }

                    string layerName = LayerMask.LayerToName(layerTexturePackerFastMesh.intValue);
                    if (layerName != null && layerName.Length > 0 && numRenderersOnLayer == 0)
                    {
                        layerTexturePackerFastMeshMessage = null;
                    } else
                    {
                        layerTexturePackerFastMeshMessage = layerTexturePackerFastMeshGUIContent.tooltip;
                    }
                }

                string scriptDefinesErrMessage = ValidatePlayerSettingsDefineSymbols();
                
                if (layerTexturePackerFastMesh.intValue == -1 || 
                    (layerTexturePackerFastMeshMessage != null && layerTexturePackerFastMeshMessage.Length > 0) ||
                    (scriptDefinesErrMessage != null))
                {
                    EditorGUILayout.HelpBox(layerTexturePackerFastMeshMessage + "\n\n" + scriptDefinesErrMessage, MessageType.Error);
                }
            }

            EditorGUILayout.PropertyField(customShaderProperties, customShaderPropertyNamesGUIContent, true);
            EditorGUILayout.PropertyField(texturePropNamesToIgnore, gc_texturePropNamesToIgnore, true);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
            Color oldColor = GUI.backgroundColor;
            GUI.color = buttonColor;
            if (GUILayout.Button("Bake Materials Into Combined Material"))
            {
                List<MB3_TextureBaker> selectedBakers = _getBakersFromTargets(targets);
                foreach (MB3_TextureBaker tb in selectedBakers)
                {
                    tb.CreateAtlases(updateProgressBar, true, new MB3_EditorMethods());
                    EditorUtility.ClearProgressBar();
                    if (tb.textureBakeResults != null) EditorUtility.SetDirty(momm.textureBakeResults);
                }
            }
            GUI.backgroundColor = oldColor;
            textureBaker.ApplyModifiedProperties();
            if (GUI.changed)
            {
                textureBaker.SetIsDifferentCacheDirty();
            }
        }

        public void DrawPropertyFieldWithLabelWidth(SerializedProperty prop, GUIContent content, int labelWidth)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelWidth));
            EditorGUILayout.PropertyField(prop, noneContent);
            EditorGUILayout.EndHorizontal();
        }

        public void updateProgressBar(string msg, float progress)
        {
            EditorUtility.DisplayProgressBar("Combining Meshes", msg, progress);
        }

        public static void CreateCombinedMaterialAssets(MB3_TextureBaker target, string pth, bool allowOverwrite=true)
        {
            MB3_TextureBaker mom = (MB3_TextureBaker)target;
            string baseName = Path.GetFileNameWithoutExtension(pth);
            if (baseName == null || baseName.Length == 0) return;
            string folderPath = Path.GetDirectoryName(pth) + "/";

            List<string> matNames = new List<string>();
            if (mom.resultType == MB2_TextureBakeResults.ResultType.textureArray)
            {
                for (int i = 0; i < mom.resultMaterialsTexArray.Length; i++)
                {
                    string nm = folderPath + baseName + "-mat" + i + ".mat";
                    if (!allowOverwrite) nm = AssetDatabase.GenerateUniqueAssetPath(nm);
                    matNames.Add(nm);
                    AssetDatabase.CreateAsset(new Material(Shader.Find("Diffuse")), matNames[i]);
                    mom.resultMaterialsTexArray[i].combinedMaterial = (Material) AssetDatabase.LoadAssetAtPath(matNames[i], typeof(Material));
                }
            }
            else
            {
                if (mom.doMultiMaterial)
                {
                    for (int i = 0; i < mom.resultMaterials.Length; i++)
                    {
                        string nm = folderPath + baseName + "-mat" + i + ".mat";
                        if (!allowOverwrite) nm = AssetDatabase.GenerateUniqueAssetPath(nm);
                        matNames.Add(nm);
                        AssetDatabase.CreateAsset(new Material(Shader.Find("Diffuse")), matNames[i]);
                        mom.resultMaterials[i].combinedMaterial = (Material)AssetDatabase.LoadAssetAtPath(matNames[i], typeof(Material));
                    }
                }
                else
                {
                    string nm = folderPath + baseName + "-mat.mat";
                    if (!allowOverwrite) nm = AssetDatabase.GenerateUniqueAssetPath(nm);
                    matNames.Add(nm);
                    Material newMat = null;
                    if (mom.GetObjectsToCombine().Count > 0 && mom.GetObjectsToCombine()[0] != null)
                    {
                        Renderer r = mom.GetObjectsToCombine()[0].GetComponent<Renderer>();
                        if (r == null)
                        {
                            Debug.LogWarning("Object " + mom.GetObjectsToCombine()[0] + " does not have a Renderer attached to it.");
                        }
                        else
                        {
                            if (r.sharedMaterial != null)
                            {
                                newMat = new Material(r.sharedMaterial);
                                //newMat.shader = r.sharedMaterial.shader;					
                                MB3_TextureBaker.ConfigureNewMaterialToMatchOld(newMat, r.sharedMaterial);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("If you add objects to be combined before creating the Combined Material Assets, then Mesh Baker will create a result material that is a duplicate of the material on the first object to be combined. This saves time configuring the shader.");
                    }
                    if (newMat == null)
                    {
                        newMat = new Material(Shader.Find("Diffuse"));
                    }
                    AssetDatabase.CreateAsset(newMat, matNames[0]);
                    mom.resultMaterial = (Material)AssetDatabase.LoadAssetAtPath(matNames[0], typeof(Material));
                }
            }

            //create the MB2_TextureBakeResults
            string nmm = pth;
            if (!allowOverwrite) nmm = AssetDatabase.GenerateUniqueAssetPath(nmm);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MB2_TextureBakeResults>(), nmm);
            mom.textureBakeResults = (MB2_TextureBakeResults)AssetDatabase.LoadAssetAtPath(nmm, typeof(MB2_TextureBakeResults));
            AssetDatabase.Refresh();
        }

        List<MB3_TextureBaker> _getBakersFromTargets(UnityEngine.Object[] targs)
        {
            List<MB3_TextureBaker> outList = new List<MB3_TextureBaker>(targs.Length);
            for (int i = 0; i < targs.Length; i++)
            {
                outList.Add((MB3_TextureBaker) targs[i]);
            }

            return outList;
        }

        public static string ValidatePlayerSettingsDefineSymbols()
        {
            // Check that the needed defines exist or are present when they should not be.
            MBVersion.PipelineType pipelineType = MBVersion.DetectPipeline();
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string scriptDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            string s = "";
            if (pipelineType == MBVersion.PipelineType.HDRP)
            {
                if (!scriptDefines.Contains(MBVersion.MB_USING_HDRP))
                {
                    s += "The GraphicsSettings -> Render Pipeline Asset is configured to use HDRP. Please add 'MB_USING_HDRP' to Player Settings -> Scripting Define Symbols for all the build platforms " + 
                        " that you are targeting. If there are compile errors check that the MeshBakerCore.asmdef file has references for:\n\n" +
                        "   Unity.RenderPipelines.HighDefinition.Runtime\n" +
                        "   Unity.RenderPipelines.HighDefinition.Config.Runtime (Unity 2019.3+)\n";
                }

                /*
                Type tp = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData");
                if (tp == null)
                {
                    s += "The class 'HDAdditionalCameraData' cannot be found by the MeshBaker assembly. Ensure that the following assemblies are referenced by the MeshBaker.asmdef file: \n" +
                        "    Unity.RenderPipelines.HighDefinition.Runtime\n" +
                        "    Unity.RenderPipelines.HighDefinition.Config.Runtime (Unity 2019.3+)\n\n"+
                        "Or download the HDRP version of the package from the asset store.";
                }
                */
            }
            else
            {
                if (scriptDefines.Contains(MBVersion.MB_USING_HDRP))
                {
                    s += "Please remove 'MB_USING_HDRP' from Player Settings -> Scripting Define Symbols for the current build platform. If this define is present there may be compile errors because Mesh Baker tries to access classes which only exist in the HDRP API.";
                }
            }

            if (s.Length > 0)
            {
                return s;
            }
            else
            {
                return null;
            }
        }
    }
}