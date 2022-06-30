using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    public class MB_ReplacePrefabsInScene
    {
        [System.Serializable]
        public class Error
        {
            public GameObject errorObj;
            public String error;
        }

        private class Prop2Reference
        {
            public string propertyName;
            public bool overriddenInSrcInstance;
            public UnityEngine.Object obj;
            public string parentArrayName;
            public int parentArraySize;
        }

        private class Component2MeshAndMaterials
        {
            public Component component;
            public List<Prop2Reference> props = new List<Prop2Reference>();
        }

        private List<Error> errors;

        public bool replaceEnforceStructure = true;

        void AddError(GameObject obj, string message)
        {
            Error err = new Error()
            {
                errorObj = obj,
                error = message,
            };

            errors.Add(err);
            Debug.LogError(message);
        }

        public int ReplacePrefabInstancesInScene(GameObject mySrcPrefab, GameObject myTargPrefab, List<Error> objsWithErrors)
        {

            errors = objsWithErrors;
            errors.Clear();

            if (mySrcPrefab == null)
            {
                AddError(mySrcPrefab, "Source Prefab was null");
                return 0;
            }

            if (myTargPrefab == null)
            {
                AddError(myTargPrefab, "Target Prefab was null");
                return 0;
            }

            if (MB_Utility.IsSceneInstance(mySrcPrefab))
            {
                AddError(mySrcPrefab, "The source prefab was a scene instance. It must be a project asset.");
                return 0;
            }

            if (MB_Utility.IsSceneInstance(myTargPrefab))
            {
                AddError(mySrcPrefab, "The target prefab was a scene instance. It must be a project asset.");
                return 0;
            }

            // First validate all the prefabs, check that they are "up-to-date"
            if (replaceEnforceStructure)
            {
                bool structureIsSame = ValidateStructureAndCollectInternalReferences(mySrcPrefab, myTargPrefab, null, null);
                if (!structureIsSame)
                {
                    AddError(mySrcPrefab, "Prefab Structure is not the same for prefabs:" + mySrcPrefab + " and " + myTargPrefab);
                    return 0;
                }
            }

            // Second pass collect all prefab instances in the scene, validate if they are replacable
            List<GameObject> instancesInScene = FindAllPrefabInstances(mySrcPrefab);
            if (replaceEnforceStructure)
            {
                for (int i = 0; i < instancesInScene.Count; i++)
                {
                    if (!ValidateStructureAndCollectInternalReferences(instancesInScene[i], myTargPrefab, null, null))
                    {
                        AddError(instancesInScene[i], "A scene instance for prefab " + mySrcPrefab + " has a modified structure that is too different from targetPrefab:" + instancesInScene[i]);
                    }
                }
            }

            if (errors.Count > 0) return 0;

            // Third pass replace all the prefabs in the scene.
            int numReplaced = 0;
            for (int i = 0; i < instancesInScene.Count; i++)
            {
                GameObject srcInstance = instancesInScene[i];
                GameObject targInstance = (GameObject)PrefabUtility.InstantiatePrefab(myTargPrefab);
                Undo.RegisterCreatedObjectUndo(targInstance, "Replace Prefabs");
                targInstance.transform.parent = srcInstance.transform.parent;
                targInstance.name = srcInstance.name;
                if (ReplaceSinglePrefabInstance(srcInstance, targInstance))
                {
                    Undo.DestroyObjectImmediate(srcInstance);
                    MB_Utility.Destroy(srcInstance);
                    numReplaced++;
                }
                else
                {
                    Undo.DestroyObjectImmediate(targInstance);
                    MB_Utility.Destroy(targInstance);
                }
            }

            Debug.Log("Replaced " + numReplaced + " instances in the scene for prefab:" + mySrcPrefab);
            return numReplaced;
        }

        private static List<GameObject> FindAllPrefabInstances(UnityEngine.Object myPrefab)
        {
            List<GameObject> result = new List<GameObject>();
            GameObject[] allObjects = (GameObject[]) GameObject.FindObjectsOfType(typeof(GameObject));
            foreach (GameObject go in allObjects)
            {
                MB_PrefabType objPrefabType = MBVersionEditor.PrefabUtility_GetPrefabType(go);
                if (objPrefabType == MB_PrefabType.scenePefabInstance)
                {
                    UnityEngine.Object GO_prefab = MBVersionEditor.PrefabUtility_GetCorrespondingObjectFromSource(go);
                    if (myPrefab == GO_prefab)
                    {
                        result.Add(go);
                    }
                }
            }

            return result;
        }

        private bool ReplaceSinglePrefabInstance(GameObject src, GameObject targ)
        {
            Debug.Assert(MB_Utility.IsSceneInstance(src));
            Debug.Assert(MB_Utility.IsSceneInstance(targ));

            // Build a source 2 target map of all internal references
            if (replaceEnforceStructure)
            {
                Dictionary<UnityEngine.Object, UnityEngine.Object> src2targetObjMap = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

                // Collect all references to project assets. 
                Dictionary<Component, Component2MeshAndMaterials> component2MeshAndMats = new Dictionary<Component, Component2MeshAndMaterials>();
                bool identicalStructure = ValidateStructureAndCollectInternalReferences(src, targ, src2targetObjMap, component2MeshAndMats);
                if (!identicalStructure)
                {
                    AddError(src, "Prefabs did not have identical structure " + targ);
                    return false;
                }

                return VisitObj(src, src, targ, src2targetObjMap, component2MeshAndMats);
            } else
            {
                targ.layer = src.layer;
                targ.tag = src.tag;
                GameObjectUtility.SetStaticEditorFlags(targ, GameObjectUtility.GetStaticEditorFlags(src));
                targ.transform.localPosition = src.transform.localPosition;
                targ.transform.localRotation = src.transform.localRotation;
                targ.transform.localScale = src.transform.localScale;
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcObj"></param>
        /// <param name="targObj"></param>
        /// <param name="src2targetObjMap">Can be null.</param>
        /// <param name="component2MeshAndMats">Can be null</param>
        private bool ValidateStructureAndCollectInternalReferences(GameObject srcObj, GameObject targObj,
            Dictionary<UnityEngine.Object, UnityEngine.Object> src2targetObjMap,
            Dictionary<Component, Component2MeshAndMaterials> component2MeshAndMats)
        {
            if (src2targetObjMap != null) src2targetObjMap.Add(srcObj, targObj);
            Component[] srcComponents = srcObj.GetComponents<Component>();
            Component[] targComponents = targObj.GetComponents<Component>();
            if (srcComponents.Length != targComponents.Length) return false;
            for (int i = 0; i < srcComponents.Length; i++)
            {
                if (srcComponents[i].GetType() != targComponents[i].GetType())
                {
                    Debug.Log("Components are different " + srcObj + " " + srcComponents[i] + "   " + targObj + " " + targComponents[i]);
                    return false;
                }

                if (src2targetObjMap != null && component2MeshAndMats != null)
                {
                    src2targetObjMap.Add(srcComponents[i], targComponents[i]);
                    CollectAssetReferncesForComponent(srcComponents[i], component2MeshAndMats);
                    CollectAssetReferncesForComponent(targComponents[i], component2MeshAndMats, true);
                }
            }

            if (srcObj.transform.childCount != targObj.transform.childCount) return false;
            for (int i = 0; i < srcObj.transform.childCount; i++)
            {
                Transform srcChild = srcObj.transform.GetChild(i);
                Transform targChild = targObj.transform.GetChild(i);
                bool childIsValid = ValidateStructureAndCollectInternalReferences(srcChild.gameObject, targChild.gameObject,
                                        src2targetObjMap, component2MeshAndMats);
                if (!childIsValid) return false;
            }

            return true;
        }

        private static void CollectAssetReferncesForComponent(Component comp, Dictionary<Component, Component2MeshAndMaterials> propRefereces, bool log = false)
        {
            SerializedObject srcSO = new SerializedObject(comp);
            SerializedProperty prop = srcSO.GetIterator();
            List<Prop2Reference> propRefs = new List<Prop2Reference>();
            SerializedProperty arrayPropParent = null;
            if (prop.NextVisible(true))
            {
                do
                {
                    if (log && prop.isArray) Debug.Log("Found Array prop: " + prop.propertyPath + " size: " + prop.arraySize);
                    if (prop.isArray)
                    {
                        arrayPropParent = srcSO.FindProperty(prop.propertyPath);
                    }

                    // If is an internal reference
                    // Or is a reference to a mesh or material that is different.
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (!IsSceneInstanceAsset(prop.objectReferenceValue))
                        {
                            if (log) Debug.Log("Visiting coponent " + comp + " propName: " + prop.name + " propPath: " + prop.propertyPath + " isArray: " + prop.isArray);
                            // Get some info about the array if this is an element of an array.
                            string parentArrayPath;
                            int parentArraySize;
                            if (arrayPropParent != null &&
                                prop.propertyPath.StartsWith(arrayPropParent.propertyPath + ".Array.data["))
                            {
                                parentArrayPath = arrayPropParent.propertyPath;
                                parentArraySize = arrayPropParent.arraySize;
                            } else
                            {
                                parentArrayPath = "";
                                parentArraySize = -1;
                            }

                            // mesh to materials
                            Prop2Reference srcComp2Mats = new Prop2Reference()
                            {
                                propertyName = prop.propertyPath,
                                overriddenInSrcInstance = prop.prefabOverride,
                                obj = prop.objectReferenceValue,
                                parentArrayName = parentArrayPath,
                                parentArraySize = parentArraySize,
                            };

                            propRefs.Add(srcComp2Mats);
                        }
                    }
                }

                while (prop.NextVisible(true));
            }

            if (propRefs.Count > 0)
            {
                Component2MeshAndMaterials srcComp2Mats = new Component2MeshAndMaterials()
                {
                    component = comp,
                    props = propRefs,
                };

                propRefereces.Add(srcComp2Mats.component, srcComp2Mats);
            }
        }

        public static bool IsSceneInstanceAsset(UnityEngine.Object obj)
        {
            if (obj == null) return true;
            string pth = AssetDatabase.GetAssetPath(obj);
            if (pth == null || pth.Equals("")) return true;
            return false;
        }

        private bool CopyGameObjectDifferences(GameObject srcGO, GameObject targGO,
            Dictionary<UnityEngine.Object, UnityEngine.Object> src2targetObjMap,
            Dictionary<Component, Component2MeshAndMaterials> component2MeshAndMats)
        {
            Component[] srcComps = srcGO.GetComponents<Component>();
            Component[] targComps = targGO.GetComponents<Component>();
            if (srcComps.Length != targComps.Length)
            {
                AddError(srcGO, "Source GameObject had a different number of  components than target.");
                return false;
            }

            for (int i = 0; i < srcComps.Length; i++)
            {
                if (srcComps[i].GetType() == targComps[i].GetType())
                {
                    List<Prop2Reference> targetInernalRefs = new List<Prop2Reference>();
                    SerializedObject serializedObject = new SerializedObject(targComps[i]);

                    // Go through target and find all references, check if these are refs to internal gameObjects/components of the prefab
                    // snapshot all internal refs.
                    {
                        SerializedProperty arrayPropParent = null;
                        SerializedProperty prop = serializedObject.GetIterator();
                        if (prop.NextVisible(true))
                        {
                            do
                            {
                                // Get some info about the array if this is an element of an array.
                                string parentArrayPath;
                                int parentArraySize;
                                if (arrayPropParent != null &&
                                    prop.propertyPath.StartsWith(arrayPropParent.propertyPath + ".Array.data["))
                                {
                                    parentArrayPath = arrayPropParent.propertyPath;
                                    parentArraySize = arrayPropParent.arraySize;
                                }
                                else
                                {
                                    parentArrayPath = "";
                                    parentArraySize = -1;
                                }

                                // If is an internal reference
                                // Or is a reference to a mesh or material that is different.
                                if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                                    src2targetObjMap.ContainsValue(prop.objectReferenceValue))
                                {
                                    Prop2Reference p2r = new Prop2Reference()
                                    {
                                        propertyName = prop.propertyPath,
                                        obj = prop.objectReferenceValue,
                                        parentArrayName = parentArrayPath,
                                        parentArraySize = parentArraySize,
                                    };

                                    targetInernalRefs.Add(p2r);
                                }
                            }

                            while (prop.NextVisible(true));
                        }
                    }

                    EditorUtility.CopySerializedIfDifferent(srcComps[i], targComps[i]);
                    serializedObject.Update();

                    // Restore internal references.
                    for (int refIdx = 0; refIdx < targetInernalRefs.Count; refIdx++)
                    {
                        Prop2Reference p2r = targetInernalRefs[refIdx];
                        SerializedProperty sp = serializedObject.FindProperty(p2r.propertyName);
                        sp.objectReferenceValue = targetInernalRefs[refIdx].obj;
                        // The copy may have resized arrays. Restore the array size from the prefab.
                        if (p2r.parentArraySize != -1)
                        {
                            sp = serializedObject.FindProperty(p2r.parentArrayName);
                            sp.arraySize = p2r.parentArraySize;
                        }
                    }

                    // Restore references to project assets
                    // Don't restore renderer assets because these are materials and go with the meshes.
                    {
                        if (component2MeshAndMats.ContainsKey(targComps[i]))
                        {
                            Component2MeshAndMaterials meshAndMats = component2MeshAndMats[targComps[i]];
                            SerializedObject targSO = new SerializedObject(meshAndMats.component);
                            for (int prpIdx = 0; prpIdx < meshAndMats.props.Count; prpIdx++)
                            {
                                Prop2Reference p2r = meshAndMats.props[prpIdx];
                                SerializedProperty prop = targSO.FindProperty(p2r.propertyName);
                                Debug.Log("Restoring asset refs for  component " + targComps[i] + " nm " + prop.name + " parentArray " + p2r.parentArrayName + " arraySize" + p2r.parentArraySize);
                                prop.objectReferenceValue = meshAndMats.props[prpIdx].obj;
                                // The copy may have resized arrays. Restore the array size from the prefab.
                                if (p2r.parentArraySize != -1)
                                {
                                    prop = targSO.FindProperty(p2r.parentArrayName);
                                    prop.arraySize = p2r.parentArraySize;
                                }
                            }

                            targSO.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }

                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    AddError(srcGO, "Components did not match");
                    return false;
                }
            }

            return true;
        }

        private bool VisitObj(GameObject srcRoot, GameObject srcChildObj, GameObject targRoot,
            Dictionary<UnityEngine.Object, UnityEngine.Object> src2targetObjMap,
            Dictionary<Component, Component2MeshAndMaterials> component2MeshAndMats)
        {
            Debug.Assert(replaceEnforceStructure, "Should only be called if enforcing structure.");
            // Ensure that it has the same hierarchy and every game object hase the same components
            Transform targChildTr = FindCorrespondingTransform(srcRoot.transform, srcChildObj.transform, targRoot.transform);

            if (targChildTr != null)
            {
                targChildTr.gameObject.layer = srcChildObj.gameObject.layer;
                targChildTr.gameObject.tag = srcChildObj.gameObject.tag;
                GameObjectUtility.SetStaticEditorFlags(targChildTr.gameObject, GameObjectUtility.GetStaticEditorFlags(srcChildObj.gameObject));
                if (!CopyGameObjectDifferences(srcChildObj, targChildTr.gameObject, src2targetObjMap, component2MeshAndMats))
                {
                    return false;
                }
            }
            else
            {
                AddError(srcRoot, "PrefabInstance " + srcRoot + " had an child " + srcChildObj + " that was not found in the target prefab.");
                return false;
            }

            foreach (Transform tr in srcChildObj.transform)
            {
                if (!VisitObj(srcRoot, tr.gameObject, targRoot, src2targetObjMap, component2MeshAndMats))
                {
                    return false;
                }
            }

            return true;
        }

        private static Transform FindCorrespondingTransform(Transform srcRoot, Transform srcChild,
                                             Transform targRoot)
        {
            if (srcRoot == srcChild) return targRoot;

            //build the path to the root in the source prefab
            List<Transform> path_root2child = new List<Transform>();
            Transform t = srcChild;
            do
            {
                path_root2child.Insert(0, t);
                t = t.parent;
            } while (t != null && t != t.root && t != srcRoot);
            if (t == null)
            {
                Debug.LogError("scrChild was not child of srcRoot " + srcRoot + " " + srcChild);
                return null;
            }
            path_root2child.Insert(0, srcRoot);

            //try to find a matching path in the target prefab
            t = targRoot;
            for (int i = 1; i < path_root2child.Count; i++)
            {
                Transform tSrc = path_root2child[i - 1];
                //try to find child in same position with same name
                int srcIdx = TIndexOf(tSrc, path_root2child[i]);
                if (srcIdx < t.childCount && path_root2child[i].name.Equals(t.GetChild(srcIdx).name))
                {
                    t = t.GetChild(srcIdx);
                    continue;
                }
                //try to find child with same name
                for (int j = 0; j < t.childCount; j++)
                {
                    if (t.GetChild(j).name.Equals(path_root2child[i].name))
                    {
                        t = t.GetChild(j);
                        continue;
                    }
                }
                t = null;
                break;
            }


            return t;
        }

        private static int TIndexOf(Transform p, Transform c)
        {
            for (int i = 0; i < p.childCount; i++)
            {
                if (c == p.GetChild(i))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}