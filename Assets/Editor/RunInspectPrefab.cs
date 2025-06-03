#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// InspectPrefab class is in the global namespace as it was not defined with one.
// No specific 'using' statement for InspectPrefab's namespace is needed.

public static class RunInspectPrefab
{
    [MenuItem("Tools/Run RopeSystem Prefab Inspection")]
    public static void ExecuteInspection()
    {
        Debug.Log("Attempting to run InspectPrefab.InspectRopeSystemPrefab via RunInspectPrefab.ExecuteInspection...");
        try
        {
            // Call the static method from InspectPrefab class.
            // InspectPrefab is in the global namespace.
            InspectPrefab.InspectRopeSystemPrefab();
            Debug.Log("InspectPrefab.InspectRopeSystemPrefab() was called successfully by RunInspectPrefab.ExecuteInspection.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to execute InspectPrefab.InspectRopeSystemPrefab via RunInspectPrefab.ExecuteInspection: {e.ToString()}");
        }
    }
}
#endif
