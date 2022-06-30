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
using DigitalOpus.MB.Core;

using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{

    public class MB_MeshBakerSettingsEditor
    {
        private static GUIContent
            gc_renderTypeGUIContent = new GUIContent("Renderer", "The type of renderer to add to the combined mesh."),
            gc_lightmappingOptionGUIContent = new GUIContent("Lightmapping UVs", "preserve current lightmapping: Use this if all source objects are lightmapped and you want to preserve it. All source objects must use the same lightmap. DOES NOT WORK IN UNITY 5.\n\n" +
                                                                             "generate new UV Layout: Use this if you want to bake a lightmap after the combined mesh has been generated\n\n" +
                                                                             "copy UV2 unchanged: Use this if UV2 is being used for something other than lightmaping.\n\n" +
                                                                             "ignore UV2: A UV2 channel will not be generated for the combined mesh\n\n" +
                                                                             "copy UV2 unchanged to separate rects: Use this if your meshes include a custom lightmap that you want to use with the combined mesh.\n\n"),
            gc_clearBuffersAfterBakeGUIContent = new GUIContent("Clear Buffers After Bake", "Frees memory used by the MeshCombiner. Set to false if you want to update the combined mesh at runtime."),
            gc_doNormGUIContent = new GUIContent("Include Normals"),
            gc_doTanGUIContent = new GUIContent("Include Tangents"),
            gc_doColGUIContent = new GUIContent("Include Colors"),
            gc_doBlendShapeGUIContent = new GUIContent("Include Blend Shapes"),
            gc_doUVGUIContent = new GUIContent("Include UV"),
            gc_doUV3GUIContent = new GUIContent("Include UV3"),
            gc_doUV4GUIContent = new GUIContent("Include UV4"),

            gc_doUV5GUIContent = new GUIContent("Include UV5"),
            gc_doUV6GUIContent = new GUIContent("Include UV6"),
            gc_doUV7GUIContent = new GUIContent("Include UV7"),
            gc_doUV8GUIContent = new GUIContent("Include UV8"),

            gc_uv2HardAngleGUIContent = new GUIContent("  UV2 Hard Angle", "Angles greater than 'hard angle' in degrees will be split."),
            gc_uv2PackingMarginUV3GUIContent = new GUIContent("  UV2 Packing Margin", "The margin between islands in the UV layout measured in UV coordinates (0..1) not pixels"),
            gc_PivotLocationType = new GUIContent("Pivot Location Type", "Centers the verticies of the mesh about the render bounds center and translates the game object. This makes the combined meshes easier to work with. There is a performance and memory allocation cost to this so if you are frequently baking meshes at runtime disable it."),
            gc_PivotLocation = new GUIContent("Pivot Location"),
            gc_OptimizeAfterBake = new GUIContent("Optimize After Bake", "This does the same thing that 'Optimize' does on the ModelImporter."),
            gc_AssignMeshCusomizer = new GUIContent("Assign To Mesh Customizer", "This is a custom script that can be used to alter data just before channels are assigned to the mesh." +
                                                " It could be used for example to inject a Texture Array slice index into mesh coordinate (uv.z or colors.a)."),
            gc_smrNoExtraBonesWhenCombiningMeshRenderers = new GUIContent("No Extra Bones For Mesh Renderers", "If a MeshRenderer is a child of another bone in the hierarchy (e.g. a sword is a child of a hand bone), then the MeshRenderer's vertices " +
                                                " will be merged with the parent bone's vertices eliminating the extra bone (the sword becomes part of the hand). This reduces the bone count, but the MeshRenderer can never be moved independently of its parent.\n\n" +
                                                "If you want some bones merged and some bones Independent, then move the 'Independent' bones in the hierarchy so that they are not descendants of other bones before baking. They can be re-parented after baking."),
            gc_smrMergeBlendShapesWithSameNames = new GUIContent("Merge Blend Shapes With Same Names", "Enable this if you are combining multiple skinned meshes on a single rig (mixing and matching different body part variations and clothes) and the body parts have the " +
                                                " same blend shape names. The combined mesh will preserve the original blend shape names. All blend shapes with the same name will activate in lockstep\n\n" +
                                                "Disable this if you are combining multiple characters into a single combined skinned mesh and want to be able to activate the blend shapes on the different characters independently. " +
                                                " the blend shapes will be re-named. Use the MB_BlendShape2CombinedMap component to control which blend shape activate.");


        private SerializedProperty doNorm, doTan, doUV, doUV3, doUV4, doUV5, doUV6, doUV7, doUV8, doCol, doBlendShapes, lightmappingOption, renderType, clearBuffersAfterBake, uv2OutputParamsPackingMargin, uv2OutputParamsHardAngle, pivotLocationType, pivotLocation, optimizeAfterBake, assignToMeshCustomizer;
        private SerializedProperty smrNoExtraBonesWhenCombiningMeshRenderers, smrMergeBlendShapesWithSameNames;
        private MB_EditorStyles editorStyles = new MB_EditorStyles();

        /// <summary>
        /// This is the regular init method that should usually be used
        /// </summary>
        /// <param name="meshBakerSettingsData">Should be the MB_IMeshBakerSettingsData implementor</param>
        public void OnEnable(SerializedProperty meshBakerSettingsData)
        {
            _InitCommon(meshBakerSettingsData);
            clearBuffersAfterBake = meshBakerSettingsData.FindPropertyRelative("_clearBuffersAfterBake");
        }

        /// <summary>
        /// This is necessary for backward compatibility. MeshCombinerCommon does not contain the 
        /// clearBuffersAfterBake field. It is stored in the MeshBaker and is serialized in many, many
        /// scenes. This Init method is only used for MeshCombinerCommon.
        /// </summary>
        /// <param name="combiner"></param>
        /// <param name="meshBaker"></param>
        public void OnEnable(SerializedProperty combiner, SerializedObject meshBaker)
        {
            _InitCommon(combiner);
            clearBuffersAfterBake = meshBaker.FindProperty("clearBuffersAfterBake");
        }

        public void OnDisable()
        {
            editorStyles.DestroyTextures();
        }

        private void _InitCommon(SerializedProperty combiner)
        {
            renderType = combiner.FindPropertyRelative("_renderType");
            lightmappingOption = combiner.FindPropertyRelative("_lightmapOption");
            doNorm = combiner.FindPropertyRelative("_doNorm");
            doTan = combiner.FindPropertyRelative("_doTan");
            doUV = combiner.FindPropertyRelative("_doUV");
            doUV3 = combiner.FindPropertyRelative("_doUV3");
            doUV4 = combiner.FindPropertyRelative("_doUV4");
            doUV5 = combiner.FindPropertyRelative("_doUV5");
            doUV6 = combiner.FindPropertyRelative("_doUV6");
            doUV7 = combiner.FindPropertyRelative("_doUV7");
            doUV8 = combiner.FindPropertyRelative("_doUV8");
            doCol = combiner.FindPropertyRelative("_doCol");
            doBlendShapes = combiner.FindPropertyRelative("_doBlendShapes");
            uv2OutputParamsPackingMargin = combiner.FindPropertyRelative("_uv2UnwrappingParamsPackMargin");
            uv2OutputParamsHardAngle = combiner.FindPropertyRelative("_uv2UnwrappingParamsHardAngle");
            pivotLocationType = combiner.FindPropertyRelative("_pivotLocationType");
            pivotLocation = combiner.FindPropertyRelative("_pivotLocation");
            optimizeAfterBake = combiner.FindPropertyRelative("_optimizeAfterBake");
            assignToMeshCustomizer = combiner.FindPropertyRelative("_assignToMeshCustomizer");
            smrNoExtraBonesWhenCombiningMeshRenderers = combiner.FindPropertyRelative("_smrNoExtraBonesWhenCombiningMeshRenderers");
            smrMergeBlendShapesWithSameNames = combiner.FindPropertyRelative("_smrMergeBlendShapesWithSameNames");
            editorStyles.Init();
        }

        public void DrawGUI(MB_IMeshBakerSettings momm, bool settingsEnabled, bool doingTextureArrays)
        {
            EditorGUILayout.BeginVertical(editorStyles.editorBoxBackgroundStyle);
            GUI.enabled = settingsEnabled;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(doNorm, gc_doNormGUIContent);
            EditorGUILayout.PropertyField(doTan, gc_doTanGUIContent);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(doUV, gc_doUVGUIContent);
            EditorGUILayout.PropertyField(doUV3, gc_doUV3GUIContent);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(doUV4, gc_doUV4GUIContent);
            EditorGUILayout.PropertyField(doUV5, gc_doUV5GUIContent);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(doUV6, gc_doUV6GUIContent);
            EditorGUILayout.PropertyField(doUV7, gc_doUV7GUIContent);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(doUV8, gc_doUV8GUIContent);
            EditorGUILayout.PropertyField(doCol, gc_doColGUIContent);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(doBlendShapes, gc_doBlendShapeGUIContent);

            if (momm.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping)
            {
                if (MBVersion.GetMajorVersion() == 5)
                {
                    EditorGUILayout.HelpBox("The best choice for Unity 5 is to Ignore_UV2 or Generate_New_UV2 layout. Unity's baked GI will create the UV2 layout it wants. See manual for more information.", MessageType.Warning);
                }
            }

            if (momm.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout)
            {
                EditorGUILayout.HelpBox("Generating new lightmap UVs can split vertices which can push the number of vertices over the 64k limit.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(lightmappingOption, gc_lightmappingOptionGUIContent);
            if (momm.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout)
            {
                EditorGUILayout.PropertyField(uv2OutputParamsHardAngle, gc_uv2HardAngleGUIContent);
                EditorGUILayout.PropertyField(uv2OutputParamsPackingMargin, gc_uv2PackingMarginUV3GUIContent);
                EditorGUILayout.Separator();
            }

            UnityEngine.Object obj = null;
            obj = assignToMeshCustomizer.objectReferenceValue;
            

            EditorGUILayout.PropertyField(renderType, gc_renderTypeGUIContent);



            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(clearBuffersAfterBake, gc_clearBuffersAfterBakeGUIContent);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(pivotLocationType, gc_PivotLocationType);
            // Confusion warning (don't use pivotLocationType.enumValueIndex. It is the index in the list of display names. NOT the enum value)
            if (pivotLocationType.intValue == (int) MB_MeshPivotLocation.customLocation)
            {
                EditorGUILayout.PropertyField(pivotLocation, gc_PivotLocation);
            }
            EditorGUILayout.PropertyField(optimizeAfterBake, gc_OptimizeAfterBake);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skinned Mesh Renderer Settings", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup((MB_RenderType)renderType.intValue != MB_RenderType.skinnedMeshRenderer);
            EditorGUILayout.PropertyField(smrNoExtraBonesWhenCombiningMeshRenderers, gc_smrNoExtraBonesWhenCombiningMeshRenderers);
            EditorGUILayout.PropertyField(smrMergeBlendShapesWithSameNames, gc_smrMergeBlendShapesWithSameNames);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);

            // Don't use a PropertyField because we may not be able to use the assigned object. It may not implement requried interface.

            if (doingTextureArrays && assignToMeshCustomizer.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("The Textures were baked into Texture Arrays. You probaly need to a customizer here" +
                    " to embed the slice index in the mesh. You can use one of the included customizers or write your own. " +
                    "See the example customizers in MeshBaker/Scripts/AssignToMeshCustomizers.", MessageType.Error);
            }
            obj = EditorGUILayout.ObjectField(gc_AssignMeshCusomizer, obj, typeof(UnityEngine.Object), true);
            if (obj == null || !(obj is IAssignToMeshCustomizer))
            {
                assignToMeshCustomizer.objectReferenceValue = null;
            }
            else
            {
                assignToMeshCustomizer.objectReferenceValue = obj;
            }

            GUI.enabled = true;
            EditorGUILayout.EndVertical();

        }
    }
}
