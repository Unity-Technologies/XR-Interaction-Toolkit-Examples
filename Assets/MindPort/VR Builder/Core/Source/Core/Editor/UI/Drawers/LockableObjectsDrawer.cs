// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Settings;
using System.Collections.Generic;

namespace VRBuilder.Editor.UI.Drawers
{
    [DefaultProcessDrawer(typeof(LockableObjectsCollection))]
    internal class LockableObjectsDrawer : DataOwnerDrawer
    {
        private LockableObjectsCollection lockableCollection;
        private SceneObjectTagBase selectedTag = new SceneObjectTag<ISceneObject>();
        private Dictionary<Guid, bool> foldoutStatus = new Dictionary<Guid, bool>();

        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            lockableCollection = (LockableObjectsCollection) currentValue;

            Rect currentPosition = new Rect(rect.x, rect.y, rect.width, EditorDrawingHelper.HeaderLineHeight);
            currentPosition.y += 10;

            GUI.Label(currentPosition,"Automatically unlocked objects in this step");

            for (int i = 0; i < lockableCollection.SceneObjects.Count; i++)
            {
                ISceneObject objectInScene = lockableCollection.SceneObjects[i];
                currentPosition = DrawSceneObject(currentPosition, objectInScene);
                currentPosition.y += EditorDrawingHelper.SingleLineHeight;
            }

            currentPosition.y += EditorDrawingHelper.SingleLineHeight;
            EditorGUI.LabelField(currentPosition, "To add new ProcessSceneObject, drag it in here:");
            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;

            ProcessSceneObject newSceneObject = (ProcessSceneObject) EditorGUI.ObjectField(currentPosition, null, typeof(ProcessSceneObject), true);
            if (newSceneObject != null)
            {
                lockableCollection.AddSceneObject(newSceneObject);
            }

            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;

            currentPosition = DrawerLocator.GetDrawerForValue(selectedTag, typeof(SceneObjectTagBase)).Draw(currentPosition, selectedTag, (value) => { selectedTag = value as SceneObjectTagBase; }, "Select tag to unlock:"); ;
            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;

            EditorGUI.BeginDisabledGroup(selectedTag.IsEmpty() || lockableCollection.TagsToUnlock.Contains(selectedTag.Guid));
            if (GUI.Button(currentPosition, "Add tag to unlock list"))
            {
                lockableCollection.AddTag(selectedTag.Guid);

                if (foldoutStatus.ContainsKey(selectedTag.Guid) == false)
                {
                    foldoutStatus.Add(selectedTag.Guid, true);
                }
            }

            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;
            EditorGUI.EndDisabledGroup();

            EditorGUI.LabelField(currentPosition, "Select the properties to attempt to unlock for each tag:");
            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;

            foreach (Guid guid in new List<Guid>(lockableCollection.TagsToUnlock))
            {
                GUILayout.BeginArea(currentPosition);
                GUILayout.BeginHorizontal();

                if(foldoutStatus.ContainsKey(guid) == false)
                {
                    foldoutStatus.Add(guid, false);
                }

                foldoutStatus[guid] = EditorGUILayout.Foldout(foldoutStatus[guid], SceneObjectTags.Instance.GetLabel(guid));                

                if(GUILayout.Button("x", GUILayout.ExpandWidth(false)))
                {
                    lockableCollection.RemoveTag(guid);
                    break;
                }

                currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;
                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                if (foldoutStatus[guid])
                {
                    foreach (Type type in PropertyReflectionHelper.ExtractFittingPropertyType<LockableProperty>(typeof(LockableProperty))) 
                    {
                        Rect objectPosition = currentPosition;
                        objectPosition.x += EditorDrawingHelper.IndentationWidth * 2f;
                        objectPosition.width -= EditorDrawingHelper.IndentationWidth * 2f;

                        bool isFlagged = lockableCollection.IsPropertyEnabledForTag(guid, type);

                        if(EditorGUI.Toggle(currentPosition, isFlagged) != isFlagged)
                        {
                            if(isFlagged)
                            {
                                lockableCollection.RemovePropertyFromTag(guid, type);
                                break;
                            }
                            else
                            {
                                lockableCollection.AddPropertyToTag(guid, type);
                                break;
                            }
                        }

                        EditorGUI.LabelField(objectPosition, type.Name);

                        currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;
                    }
                }
            }

            // EditorDrawingHelper.HeaderLineHeight - 24f is just the magic number to make it properly fit...
            return new Rect(rect.x, rect.y, rect.width, currentPosition.y - EditorDrawingHelper.HeaderLineHeight - 24f);
        }

        private Rect DrawSceneObject(Rect currentPosition, ISceneObject sceneObject)
        {
            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;

            Rect objectFieldPosition = currentPosition;
            objectFieldPosition.width -= 24;
            GUI.enabled = false;
            EditorGUI.ObjectField(objectFieldPosition, (ProcessSceneObject) sceneObject, typeof(ProcessSceneObject), true);
            // If scene object is used by a property, dont allow removing it.
            GUI.enabled = lockableCollection.IsUsedInAutoUnlock(sceneObject) == false;
            objectFieldPosition.x = currentPosition.width - 24 + 6f;
            objectFieldPosition.width = 20;
            if (GUI.Button(objectFieldPosition,"x", new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }))
            {
                lockableCollection.RemoveSceneObject(sceneObject);
            }
            GUI.enabled = true;

            try
            {
                foreach (LockableProperty property in sceneObject.Properties.Where(property => property is LockableProperty))
                {
                    currentPosition = DrawProperty(currentPosition, property);
                }
            }
            catch (MissingReferenceException)
            {
                // Swallow this exception, will be thrown in frames between exiting playmode and having setup the object reference library.
            }

            return currentPosition;
        }

        private Rect DrawProperty(Rect currentPosition, LockableProperty property)
        {
            currentPosition.y += EditorDrawingHelper.SingleLineHeight + EditorDrawingHelper.VerticalSpacing;
            Rect objectPosition = currentPosition;
            objectPosition.x += EditorDrawingHelper.IndentationWidth * 2f;
            objectPosition.width -= EditorDrawingHelper.IndentationWidth * 2f;

            GUI.enabled = lockableCollection.IsInAutoUnlockList(property) == false;
            bool isFlagged = GUI.enabled == false || lockableCollection.IsInManualUnlockList(property);
            if (EditorGUI.Toggle(currentPosition, isFlagged) != isFlagged)
            {
                // Inverted due to not updated with the toggle
                if (isFlagged)
                {
                    lockableCollection.Remove(property);
                }
                else
                {
                    lockableCollection.Add(property);
                }
            }
            GUI.enabled = true;
            EditorGUI.LabelField(objectPosition, property.GetType().Name);
            return currentPosition;
        }
    }
}
