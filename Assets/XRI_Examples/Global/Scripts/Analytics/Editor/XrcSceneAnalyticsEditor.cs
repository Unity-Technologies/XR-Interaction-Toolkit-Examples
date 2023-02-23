using UnityEngine;
using UnityEngine.XR.Content.Interaction.Analytics;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Editor for <see cref="XrcSceneAnalytics"/> that makes it easy to spot missing analytics objects by validating
    /// object reference properties in all components in the same GameObject.
    /// </summary>
    [CustomEditor(typeof(XrcSceneAnalytics))]
    class XrcSceneAnalyticsEditor : Editor
    {
        const string k_WarningMessage = "Missing analytics object reference in property \'{0}.{1}\'";

        static void ValidateObjectReferences(UnityObject target)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    Debug.LogWarningFormat(target, k_WarningMessage, target.GetType().Name, property.propertyPath);
            }
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Validate References"))
            {
                var sceneAnalytics = target as XrcSceneAnalytics;
                if (sceneAnalytics == null)
                    return;

                var monoBehaviours = sceneAnalytics.GetComponentsInChildren<MonoBehaviour>();
                foreach (var monoBehaviour in monoBehaviours)
                    ValidateObjectReferences(monoBehaviour);
            }
        }
    }
}
