using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.BasicInteraction.RigSetup;
using VRBuilder.Core.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VRBuilder.Editor.BasicInteraction.RigSetup
{
    
    [CustomEditor(typeof(InteractionRigSetup))]
    internal class InteractionRigSetupDrawer : UnityEditor.Editor
    {
        private readonly float lineHeight = EditorGUIUtility.singleLineHeight;
        
        private ReorderableList list;

        private List<InteractionRigProvider> foundProvider = new List<InteractionRigProvider>();
        
        private GUIContent warningIcon;
        
        private void OnEnable()
        {
            InteractionRigSetup rigSetup = (InteractionRigSetup) target;
            
            if (Application.isPlaying == false)
            {
                foundProvider = rigSetup.UpdateRigList();
            }

            list = new ReorderableList(serializedObject,
                serializedObject.FindProperty("PossibleInteractionRigs"),
                true, true, false, false);

            list.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Available Interaction Rigs"); };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (foundProvider == null || foundProvider.Count == 0 || foundProvider.Count <= index)
                {
                    foundProvider = rigSetup.UpdateRigList();
                }

                InteractionRigProvider provider = FindProvider(rigSetup.PossibleInteractionRigs[index].Name);
                
                Rect labelRect = new Rect(rect.x, rect.y, rect.width - 2 * lineHeight - 4, lineHeight);
                if (provider != null)
                {
                    bool canBeUsed = provider.CanBeUsed();
                    GUI.enabled = canBeUsed;
                    EditorGUI.LabelField(labelRect, provider.Name);
                    GUI.enabled = true;

                    Rect toggleRect = new Rect(rect.x + rect.width - 2 * lineHeight, rect.y, lineHeight, lineHeight);
                    rigSetup.PossibleInteractionRigs[index].Enabled = EditorGUI.Toggle(toggleRect, rigSetup.PossibleInteractionRigs[index].Enabled);
                    
                    if (canBeUsed == false)
                    {
                        Rect warningRect = new Rect(rect.x + rect.width - lineHeight, rect.y, lineHeight, lineHeight);
                        GUIContent labelContent = new GUIContent("", warningIcon.image, provider.GetSetupTooltip());
                        EditorGUI.LabelField(warningRect, labelContent);
                    }
                    
                }
                else
                {
                    EditorGUI.LabelField(labelRect, "#Error");
                    foundProvider = rigSetup.UpdateRigList();
                    Repaint();
                }
            };

            list.onReorderCallback = reorderableList =>
            {
                foundProvider = rigSetup.UpdateRigList();
            }; 

            list.drawFooterCallback = rect => { };
        }

        private InteractionRigProvider FindProvider(string name)
        {
            return foundProvider.FirstOrDefault(provider => provider.Name == name);
        }

        public override void OnInspectorGUI()
        {
            if (warningIcon == null)
            {
                warningIcon = EditorGUIUtility.IconContent("Warning@2x");
            }
            
            GUILayout.Box(
                "Enable/Disable available interaction Rigs, you are also able to prioritize them by changing the position in the array. Top most has the highest priority. The interaction Rig will be spawned at the USER GameObject.");
            serializedObject.Update();
            list.DoLayoutList();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DummyUser"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
