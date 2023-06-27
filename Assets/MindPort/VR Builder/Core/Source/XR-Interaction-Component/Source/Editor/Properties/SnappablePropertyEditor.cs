using System;
using System.IO;
using System.Collections.Generic;
using VRBuilder.XRInteraction;
using VRBuilder.Core.SceneObjects;
using VRBuilder.XRInteraction.Properties;
using VRBuilder.BasicInteraction.Validation;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.XRInteraction
{
    /// <summary>
    /// Custom inspector for <see cref="SnappableProperty"/>, adding a button to create <see cref="VRBuilder.XRInteraction.SnapZone"/>s automatically.
    /// </summary>
    [CustomEditor(typeof(SnappableProperty)), CanEditMultipleObjects]
    internal class SnappablePropertyEditor : UnityEditor.Editor
    {
        private const string PrefabPath = "Assets/MindPort/VR Builder/Resources/SnapZones/Prefabs";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Snap Zone for this object"))
            {
                foreach (UnityEngine.Object targetObject in serializedObject.targetObjects)
                {
                    if (targetObject is SnappableProperty snappable)
                    {
                        SnapZone snapZone = CreateSnapZone(snappable);
                        SetupSingleObjectValidation(snapZone, snappable.GetComponent<ProcessSceneObject>());
                    }
                }
            }

            if (GUILayout.Button("Create Snap Zone for objects with the same tags"))
            {
                foreach (UnityEngine.Object targetObject in serializedObject.targetObjects)
                {
                    if (targetObject is SnappableProperty snappable)
                    {
                        SnapZone snapZone = CreateSnapZone(snappable);
                        SetupTagValidation(snapZone, snappable.GetComponent<ProcessSceneObject>());
                    }
                }
            }
        }
        
        private SnapZone CreateSnapZone(SnappableProperty snappable)
        {
            // Retrieves a SnapZoneSettings and creates a clone for the snappable object
            SnapZoneSettings settings = SnapZoneSettings.Settings;
            GameObject snapZoneBlueprint = DuplicateObject(snappable.gameObject, settings.HighlightMaterial);
            
            // Saves it as highlight prefab.
            GameObject snapZonePrefab = SaveSnapZonePrefab(snapZoneBlueprint);

            // Creates a new object for the SnapZone.
            GameObject snapObject = new GameObject($"{CleanName(snappable.name)}_SnapZone");
            Undo.RegisterCreatedObjectUndo(snapObject, $"Create {snapObject.name}");
            
            // Positions the Snap Zone at the same position, rotation and scale as the snappable object.
            snapObject.transform.SetParent(snappable.transform);
            snapObject.transform.SetPositionAndRotation(snappable.transform.position, snappable.transform.rotation);
            snapObject.transform.localScale = Vector3.one;
            snapObject.transform.SetParent(null);
            
            // Adds a Snap Zone component to our new object.
            SnapZone snapZone = snapObject.AddComponent<SnapZoneProperty>().SnapZone;
            snapZone.ShownHighlightObject = snapZonePrefab;

            settings.ApplySettingsToSnapZone(snapZone);

            GameObject snapPoint = new GameObject("SnapPoint");
            snapPoint.transform.SetParent(snapZone.transform);
            snapPoint.transform.localPosition = Vector3.zero;
            snapPoint.transform.localScale = Vector3.one;
            snapPoint.transform.localRotation = Quaternion.identity;
            snapPoint.AddComponent<SnapZonePreviewDrawer>();

            SerializedObject snapZoneSerialization = new SerializedObject(snapZone);
            SerializedProperty property = snapZoneSerialization.FindProperty("m_AttachTransform");
            property.objectReferenceValue = snapPoint.transform;
            snapZoneSerialization.ApplyModifiedPropertiesWithoutUndo();

            // Calculates the volume of the Snap Zone out of the snappable object.
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Renderer renderer in snapZoneBlueprint.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            // Adds a BoxCollider and sets it up.
            BoxCollider boxCollider = snapObject.AddComponent<BoxCollider>();
            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;
            boxCollider.isTrigger = true;

            // Disposes the cloned object.
            DestroyImmediate(snapZoneBlueprint);

            Selection.activeGameObject = snapZone.gameObject;

            return snapZone;
        }

        private void SetupSingleObjectValidation(SnapZone snapZone, ProcessSceneObject processSceneObject)
        {
            IsProcessSceneObjectValidation validation = snapZone.gameObject.AddComponent<IsProcessSceneObjectValidation>();
            validation.AddProcessSceneObject(processSceneObject);
        }

        private void SetupTagValidation(SnapZone snapZone, ProcessSceneObject processSceneObject)
        {
            IsObjectWithTagValidation validation = snapZone.gameObject.AddComponent<IsObjectWithTagValidation>();
            foreach(Guid tag in processSceneObject.Tags)
            {
                validation.AddTag(tag);
            }
        }
        
        private GameObject DuplicateObject(GameObject originalObject, Material sharedMaterial, Transform parent = null)
        {
            GameObject cloneObject = new GameObject($"{CleanName(originalObject.name)}_Highlight.prefab");
            
            if (parent != null)
            {
                cloneObject.transform.SetParent(parent);
            }
            
            ProcessRenderer(originalObject, cloneObject, sharedMaterial);
            
            EditorUtility.CopySerialized(originalObject.transform, cloneObject.transform);

            foreach (Transform child in originalObject.transform)
            {
                DuplicateObject(child.gameObject, sharedMaterial, cloneObject.transform);
            }

            return cloneObject;
        }

        private void ProcessRenderer(GameObject originalObject, GameObject cloneObject, Material sharedMaterial)
        {
            Renderer renderer = originalObject.GetComponent<Renderer>();
            
            Type renderType = renderer.GetType();

            if (renderType == typeof(SkinnedMeshRenderer))
            {
                SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                    
                MeshRenderer meshRenderer = cloneObject.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = cloneObject.AddComponent<MeshFilter>();
                List<Material> sharedMaterials = new List<Material>();
                    
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.subMeshCount; i++)
                {
                    sharedMaterials.Add(sharedMaterial);
                }

                meshRenderer.sharedMaterials = sharedMaterials.ToArray();
                meshFilter.sharedMesh = skinnedMeshRenderer.sharedMesh;
            }
            
            if (renderType == typeof(MeshRenderer))
            {
                MeshRenderer originalMeshRenderer = renderer as MeshRenderer;
                MeshFilter originalMeshFilter = originalObject.GetComponent<MeshFilter>();
                
                MeshRenderer meshRenderer = cloneObject.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = cloneObject.AddComponent<MeshFilter>();
                List<Material> sharedMaterials = new List<Material>();
                    
                for (int i = 0; i < originalMeshFilter.sharedMesh.subMeshCount; i++)
                {
                    sharedMaterials.Add(sharedMaterial);
                }
                
                meshRenderer.sharedMaterials = sharedMaterials.ToArray();
                meshFilter.sharedMesh = originalMeshFilter.sharedMesh;

                Mesh mesh = cloneObject.GetComponent<MeshFilter>().sharedMesh;
                if (mesh.isReadable == false)
                {
                    Debug.LogWarning($"The mesh <i>{mesh.name}</i> on <i>{cloneObject.name}</i> is not set readable. In builds, the mesh will not be visible in the snap zone highlight. Please enable <b>Read/Write</b> in the mesh import settings.");
                }
            }
        }

        private GameObject SaveSnapZonePrefab(GameObject snapZoneBlueprint)
        {
            if (Directory.Exists(PrefabPath) == false)
            {
                Directory.CreateDirectory(PrefabPath);
            }
            
            snapZoneBlueprint.transform.localScale = Vector3.one;
            snapZoneBlueprint.transform.position = Vector3.zero;
            snapZoneBlueprint.transform.rotation = Quaternion.identity;

            string prefabPath = $"{PrefabPath}/{snapZoneBlueprint.name}";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(snapZoneBlueprint, prefabPath);

            if (prefab != null)
            {
                Debug.LogWarningFormat("A new highlight prefab was saved at {0}", prefabPath);
            }
            
            return prefab;
        }

        private string CleanName(string originalName)
        {
            // Unity replaces invalid characters with '_' when creating new prefabs in the editor.
            // We try to simulate that behavior.
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                originalName = originalName.Replace(invalidCharacter, '_');
            }

            // Non windows systems consider '\' as a valid file name. 
            return originalName.Replace('\\', '_');
        }
    }
}