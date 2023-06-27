// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using UnityEngine;

 namespace VRBuilder.Editor
 {
     /// <summary>
     /// Modal Window that helps setting the 'API Compatibility Level' to '.Net 4.x'.
     /// </summary>
     internal class DotNetWindow : EditorWindow
     {
         private bool abortBuilding;
         private BuildTargetGroup buildTargetGroup;
         private ApiCompatibilityLevel currentLevel;
         private readonly Vector2 fixedSize = new Vector2(400f, 160);

         public bool ShouldAbortBuilding()
         {
             return abortBuilding;
         }

         private void OnEnable()
         {
             abortBuilding = false;
             minSize = maxSize = fixedSize;
             titleContent = new GUIContent("API Compatibility Level*");

             GatherCurrentSettings();
         }

         private void OnGUI()
         {
             EditorGUILayout.Space();
             EditorGUILayout.HelpBox($"This Unity project uses {currentLevel} but some features require .NET 4.X support.\n\nThe built application might not work as expected.", MessageType.Warning);
             EditorGUILayout.Space(20f);
             EditorGUILayout.BeginHorizontal();
             GUILayout.FlexibleSpace();
             {
                 if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fix & Continue", "Sets the 'API Compatibility Level' to '.Net 4.x' and continues building the application."), GUILayout.Width(110f)))
                 {
                     PlayerSettings.SetApiCompatibilityLevel(buildTargetGroup, ApiCompatibilityLevel.NET_4_6);
                     Close();
                 }

                 if (GUILayout.Button(EditorGUIUtility.TrTextContent("Ignore", "Ignores this warning and continues building the application."), GUILayout.Width(110f)))
                 {
                     Close();
                 }

                 if (GUILayout.Button(EditorGUIUtility.TrTextContent("Abort", "Aborts the build immediately."), GUILayout.Width(110f)))
                 {
                     abortBuilding = true;
                     Close();
                 }
             }
             EditorGUILayout.EndHorizontal();
         }

         private void GatherCurrentSettings()
         {
             BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
             buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
             currentLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);
         }
     }
 }
