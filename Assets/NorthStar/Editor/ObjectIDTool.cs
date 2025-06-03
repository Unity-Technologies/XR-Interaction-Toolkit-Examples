// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Tool for finding objects by their guid
    /// </summary>
    public class ObjectIDTool : EditorWindow
    {
        private string m_id = "";
        private string m_message = "";
        private Vector2 m_scrollPos;

        private struct FileItem
        {
            public string Type;
            public string Name;
            public Object Obj;

            public FileItem(string type, string name, Object obj)
            {
                Type = type;
                Name = name;
                Obj = obj;
            }
        }

        private List<FileItem> m_objectHistory = new();

        [MenuItem("Tools/NorthStar/ObjectIDTool", priority = 0)]
        private static void CreateWindow()
        {
            _ = GetWindow(typeof(ObjectIDTool), false, nameof(ObjectIDTool));
        }

        private void OnGUI()
        {
            GUILayout.Label("Enter ID:");
            m_id = GUILayout.TextField(m_id);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            #region FileID

            if (GUILayout.Button("Use fileID", GUILayout.Width(120)))
            {
                if (long.TryParse(m_id, out var fileID))
                {
                    var result = GetObjectFromFileID(fileID);

                    if (result != null)
                    {
                        m_objectHistory.Add(new FileItem("FileID", result.name, result));
                        m_message = "Found object in scene";
                    }
                    else
                    {
                        m_message = "Object not found";
                    }
                }
                else
                {
                    m_message = "Input value is not a \"long\" type";
                }
            }
            #endregion
            GUILayout.FlexibleSpace();
            #region GUID
            if (GUILayout.Button("Use GUID", GUILayout.Width(120)))
            {
                var result = GetObjectFromGUID(m_id);
                if (result != null)
                {
                    m_objectHistory.Add(new FileItem("GUID", result.name, result));
                }
                else
                {
                    m_message = "Object not found";
                }
            }

            #endregion
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            #region Abort
            if (GUILayout.Button("Abort", GUILayout.Width(120)))
                Close();
            #endregion
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(m_message);

            GUILayout.Space(10);

            GUILayout.Label("History:");
            _ = GUILayout.BeginScrollView(m_scrollPos); //TODO: figure out why scrollview doesn't allow scrolling
            #region History
            for (var i = m_objectHistory.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(m_objectHistory[i].Type);
                GUILayout.Label(m_objectHistory[i].Name);
                _ = EditorGUILayout.ObjectField(m_objectHistory[i].Obj, typeof(Object), true);

                GUILayout.EndHorizontal();
            }
            #endregion
            GUILayout.EndScrollView();
        }

        private static Object GetObjectFromGUID(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            return obj;
        }

        private static Object GetObjectFromFileID(long fileID) // also called local identifier
        {
            Object resultGo = null;
            //var gameObjects = FindObjectsOfTypeAll(typeof(Object));
            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>(); //TODO: this is incorrectly getting all assets, we want just assets in the current scene

            foreach (var gameobject in gameObjects)
            {
                // Test every gameobjects
                var inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                var serializedGameObject = new SerializedObject(gameobject);
                inspectorModeInfo.SetValue(serializedGameObject, InspectorMode.Debug, null);
                var localIdProp = serializedGameObject.FindProperty("m_LocalIdentfierInFile");

                if (localIdProp.longValue == fileID)
                {
                    resultGo = gameobject;
                    break;
                }

                //Test every transforms
                var serializedTransform = new SerializedObject(gameobject.transform);
                inspectorModeInfo.SetValue(serializedTransform, InspectorMode.Debug, null);
                localIdProp = serializedTransform.FindProperty("m_LocalIdentfierInFile");

                if (localIdProp.longValue == fileID)
                {
                    resultGo = gameobject.transform;
                    break;
                }

            }

            var components = Resources.FindObjectsOfTypeAll<Component>();
            foreach (var component in components)
            {
                // Test every gameobjects
                var inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                var serializedGameObject = new SerializedObject(component);
                inspectorModeInfo.SetValue(serializedGameObject, InspectorMode.Debug, null);
                var localIdProp = serializedGameObject.FindProperty("m_LocalIdentfierInFile");

                if (localIdProp.longValue == fileID)
                {
                    if (resultGo != null)
                    {
                        Debug.LogWarning("Conflict in searching FileID, overriding " + resultGo + " with " + component);
                    }
                    resultGo = component;
                }
            }
            //TODO: test each object rather than just gameobjects, transforms and monobehaviours

            return resultGo;
        }
    }
}
