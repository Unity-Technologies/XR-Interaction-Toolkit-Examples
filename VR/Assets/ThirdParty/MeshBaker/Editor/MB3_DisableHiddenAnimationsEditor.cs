using UnityEngine;
using System.Collections;
using UnityEditor;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    [CustomEditor(typeof(MB3_DisableHiddenAnimations))]
    public class MB3_DisableHiddenAnimationsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "HOW TO USE \n\nPlace this component on the same game object that has the combined SkinMeshRenderer " +
                                     "\n\n Drag game objects with Animator and/or Animation components that were baked into the combined SkinnedMeshRenderer into the lists below", MessageType.Info);
            DrawDefaultInspector();
        }
    }
}
