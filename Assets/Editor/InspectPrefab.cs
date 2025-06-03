#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text;

public static class InspectPrefab
{
    private const string PrefabPath = "Assets/RopeSystem.prefab";

    [MenuItem("Tools/Inspect RopeSystem.prefab")]
    public static void InspectRopeSystemPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at path: {PrefabPath}");
            return;
        }

        GameObject instance = null;
        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                // Fallback if InstantiatePrefab returns null (e.g. for non-prefab assets, though LoadAssetAtPath<GameObject> should prevent this)
                // Or if the prefab is an abstract game object without a visual representation that can be instantiated.
                // However, for this specific problem, we expect a concrete GameObject.
                Debug.LogError($"Failed to instantiate prefab: {PrefabPath}. PrefabUtility.InstantiatePrefab returned null.");
                return;
            }

            instance.name = prefab.name; // Ensure the instance has the same name as the prefab for clarity in logs

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Inspecting Prefab: {instance.name}");
            InspectTransformRecursive(instance.transform, sb, "");
            Debug.Log(sb.ToString());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during prefab instantiation or inspection: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
        }
    }

    private static void InspectTransformRecursive(Transform currentTransform, StringBuilder sb, string indent)
    {
        sb.Append(indent);
        sb.Append("- ");
        sb.Append(currentTransform.gameObject.name);
        sb.Append(" (Components: ");

        Component[] components = currentTransform.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                sb.Append("Missing Component");
            }
            else
            {
                sb.Append(components[i].GetType().Name);
            }
            if (i < components.Length - 1)
            {
                sb.Append(", ");
            }
        }
        sb.AppendLine(")");

        foreach (Transform child in currentTransform)
        {
            InspectTransformRecursive(child, sb, indent + "  ");
        }
    }
}
#endif
