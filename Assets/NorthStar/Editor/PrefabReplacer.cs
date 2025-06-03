// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Replaces all selected objects with the specified prefab, and sets the position/rotation/scale of the new object to the replacement
    /// </summary>
    public class ReplaceWithPrefab : ScriptableWizard
    {
        [SerializeField] private GameObject m_prefab = null;

        [MenuItem("Tools/NorthStar/Replace with Prefab")]
        public static void Open()
        {
            _ = DisplayWizard<ReplaceWithPrefab>("Prefab Replacer", "Replace and Close", "Replace");
        }

        private void OnWizardCreate()
        {
            Replace();
        }

        private void OnWizardOtherButton()
        {
            Replace();
        }

        private void Replace()
        {
            foreach (var gameObject in Selection.gameObjects)
            {
                var transform = gameObject.transform;
                transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);

                var clone = PrefabUtility.InstantiatePrefab(m_prefab, transform.parent) as GameObject;
                clone.transform.SetLocalPositionAndRotation(localPosition, localRotation);
                clone.transform.localScale = transform.localScale;
                Undo.RegisterCreatedObjectUndo(clone, "Replace Prefab");

                Undo.DestroyObjectImmediate(gameObject);
                GameObjectUtility.EnsureUniqueNameForSibling(clone);
            }
        }
    }
}