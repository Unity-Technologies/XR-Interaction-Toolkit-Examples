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
    [CustomEditor(typeof(MB3_MeshCombinerSettings))]
    public class MB3_MeshBakerSettingsAssetEditor : Editor
    {

        private SerializedObject settingsSerializedObj;
        private SerializedProperty mbSettings;
        private MB_MeshBakerSettingsEditor meshBakerSettingsEditor;

        public void OnEnable()
        {
            settingsSerializedObj = new SerializedObject(target);
            mbSettings = settingsSerializedObj.FindProperty("data");
            meshBakerSettingsEditor = new MB_MeshBakerSettingsEditor();
            meshBakerSettingsEditor.OnEnable(mbSettings);
        }

        public override void OnInspectorGUI()
        {
            MB3_MeshCombinerSettings tbg = (MB3_MeshCombinerSettings)target;
            settingsSerializedObj.Update();
            EditorGUILayout.HelpBox("This asset can be shared by many Mesh Bakers and MultiMeshBakers. Drag this " +
                " asset to the 'Use Shared Settings' field of any Mesh Baker", MessageType.Info);
            meshBakerSettingsEditor.DrawGUI(tbg.data, true, false);
            settingsSerializedObj.ApplyModifiedProperties();
        }
    }
}
