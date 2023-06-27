/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Dictation;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.WitAi.Utilities
{
    [Serializable]
    public struct DictationServiceReference
    {
        [SerializeField] internal DictationService dictationService;

        public DictationService DictationService
        {
            get
            {
                if (!dictationService)
                {
                    DictationService[] services = Resources.FindObjectsOfTypeAll<DictationService>();
                    if (services != null)
                    {
                        // Set as first instance that isn't a prefab
                        dictationService = Array.Find(services, (o) => o.gameObject.scene.rootCount != 0);
                    }
                }

                return dictationService;
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DictationServiceReference))]
    public class DictationServiceReferenceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var refProp = property.FindPropertyRelative("DictationService");
            var reference = refProp.objectReferenceValue as DictationService;
            var dictationServices = GameObject.FindObjectsOfType<DictationService>();
            var dictationServiceNames = new string[dictationServices.Length + 1];
            int index = 0;
            dictationServiceNames[0] = "Autodetect";
            if (dictationServices.Length == 1)
            {
                dictationServiceNames[0] = $"{dictationServiceNames[0]} - {dictationServices[0].name}";
            }
            for (int i = 0; i < dictationServices.Length; i++)
            {
                dictationServiceNames[i + 1] = dictationServices[i].name;
                if (dictationServices[i] == reference)
                {
                    index = i + 1;
                }
            }
            EditorGUI.BeginProperty(position, label, property);
            var updatedIndex = EditorGUI.Popup(position, index, dictationServiceNames);
            if (index != updatedIndex)
            {
                if (updatedIndex > 0)
                {
                    refProp.objectReferenceValue = dictationServices[updatedIndex - 1];
                }
                else
                {
                    refProp.objectReferenceValue = null;
                }

                property.serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.EndProperty();
        }
    }
#endif
}
