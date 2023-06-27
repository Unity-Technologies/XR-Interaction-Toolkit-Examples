using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;
using VRBuilder.Editor.UndoRedo;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// Settings section to manage tags that can be attached to scene objects.
    /// </summary>
    public class SceneObjectTagsSettingsSection : IProjectSettingsSection
    {
        private string newLabel = "";
        private Dictionary<SceneObjectTags.Tag, bool> foldoutStatus = new Dictionary<SceneObjectTags.Tag, bool>();
        private static readonly EditorIcon deleteIcon = new EditorIcon("icon_delete");

        /// <inheritdoc/>
        public string Title => "Tags in Project";

        /// <inheritdoc/>
        public Type TargetPageProvider => typeof(SceneObjectTagsSettingsProvider);

        /// <inheritdoc/>
        public int Priority => 64;

        /// <inheritdoc/>
        public void OnGUI(string searchContext)
        {
            SceneObjectTags config = SceneObjectTags.Instance;

            // Create new label
            GUILayout.BeginHorizontal();
            newLabel = EditorGUILayout.TextField(newLabel);

            EditorGUI.BeginDisabledGroup(config.CanCreateTag(newLabel) == false);
            if (GUILayout.Button("Create Tag", GUILayout.ExpandWidth(false)))
            {
                Guid guid = Guid.NewGuid();

                RevertableChangesHandler.Do(new ProcessCommand(
                    () => {
                        config.CreateTag(newLabel, guid);
                        EditorUtility.SetDirty(config);
                    },
                    () => {
                        config.RemoveTag(guid);
                        EditorUtility.SetDirty(config);
                    }
                    ));

                GUI.FocusControl("");
                newLabel = "";                
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // List all tags
            foreach (SceneObjectTags.Tag tag in config.Tags)
            {
                if(foldoutStatus.ContainsKey(tag) == false)
                {
                    foldoutStatus.Add(tag, false);
                }

                IEnumerable<ISceneObject> objectsWithTag = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(tag.Guid);
                
                GUILayout.BeginHorizontal();

                // Foldout
                EditorGUI.BeginDisabledGroup(objectsWithTag.Count() == 0);
                
                foldoutStatus[tag] = EditorGUILayout.Foldout(foldoutStatus[tag], "");
                EditorGUI.EndDisabledGroup();


                string label = tag.Label;

                // Label field
                string newLabel = EditorGUILayout.TextField(label);

                if(string.IsNullOrEmpty(newLabel) == false && newLabel != label)
                {
                    RevertableChangesHandler.Do(new ProcessCommand(
                        () => {
                            config.RenameTag(tag, newLabel);
                            EditorUtility.SetDirty(config);
                        },
                        () => {
                            config.RenameTag(tag, label);
                            EditorUtility.SetDirty(config);
                        }
                        ));

                    EditorUtility.SetDirty(config);
                }

                // Delete button
                if (GUILayout.Button(deleteIcon.Texture, GUILayout.Height(EditorDrawingHelper.SingleLineHeight)))
                {
                    RevertableChangesHandler.Do(new ProcessCommand(
                        () => {
                            config.RemoveTag(tag.Guid);
                            EditorUtility.SetDirty(config);
                        },
                        () => {
                            config.CreateTag(tag.Label, tag.Guid);
                            EditorUtility.SetDirty(config);
                        }
                        ));

                    EditorUtility.SetDirty(config);
                    break;
                }

                // Objects in scene
                GUILayout.Label($"{objectsWithTag.Count()} objects in scene");

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(EditorDrawingHelper.VerticalSpacing);

                if (foldoutStatus[tag])
                {
                    foreach (ISceneObject sceneObject in objectsWithTag)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(EditorDrawingHelper.IndentationWidth);
                        if (GUILayout.Button("Show", GUILayout.ExpandWidth(false)))
                        {
                            EditorGUIUtility.PingObject(sceneObject.GameObject);
                        }

                        GUILayout.Label($"{sceneObject.GameObject.name} - uid: {sceneObject.UniqueName}");

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }
    }
}
