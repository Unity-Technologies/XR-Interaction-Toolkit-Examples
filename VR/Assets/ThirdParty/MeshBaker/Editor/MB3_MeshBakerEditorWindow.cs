//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    public class MB3_MeshBakerEditorWindow : EditorWindow
    {
        MB3_MeshBakerEditorWindowAddObjectsTab addObjectsTab;
        MB3_MeshBakerEditorWindowAnalyseSceneTab analyseSceneTab;
        Vector2 scrollPos = Vector2.zero;
        int selectedTab = 0;
        GUIContent[] tabs = new GUIContent[] { new GUIContent("Analyse Scene & Generate Bakers"), new GUIContent("Search For Meshes To Add") };

        [MenuItem("Window/Mesh Baker/Mesh Baker")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(MB3_MeshBakerEditorWindow));
        }

        public void SetTarget(MB3_MeshBakerRoot targ)
        {
            if (addObjectsTab == null) addObjectsTab = new MB3_MeshBakerEditorWindowAddObjectsTab();
            addObjectsTab.target = targ;
        }

        void OnGUI()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

            if (selectedTab == 0)
            {
                analyseSceneTab.drawTabAnalyseScene(position);
            }
            else
            {
                addObjectsTab.drawTabAddObjectsToBakers();
            }

            EditorGUILayout.EndScrollView();
        }

        void OnEnable()
        {
            if (addObjectsTab == null) addObjectsTab = new MB3_MeshBakerEditorWindowAddObjectsTab();
            if (analyseSceneTab == null) analyseSceneTab = new MB3_MeshBakerEditorWindowAnalyseSceneTab();
            addObjectsTab.OnEnable();
        }

        void OnDisable()
        {
            addObjectsTab.OnDisable();
        }
    }
}