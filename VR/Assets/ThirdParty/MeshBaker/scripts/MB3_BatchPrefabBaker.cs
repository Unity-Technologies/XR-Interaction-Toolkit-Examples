using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

public class MB3_BatchPrefabBaker : MonoBehaviour {
    public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;

    [System.Serializable]
    public class MB3_PrefabBakerRow{
        public GameObject sourcePrefab;
        public GameObject resultPrefab;
    }

    public MB3_PrefabBakerRow[] prefabRows = new MB3_PrefabBakerRow[0];

    public string outputPrefabFolder = "";

    [ContextMenu("Create Instances For Prefab Rows")]
    public void CreateSourceAndResultPrefabInstances()
    {
#if UNITY_EDITOR
        // instantiate the prefabs
        List<GameObject> srcPrefabs = new List<GameObject>();
        List<GameObject> resultPrefabs = new List<GameObject>();
        for (int i = 0; i < prefabRows.Length; i++)
        {
            if (prefabRows[i].sourcePrefab != null && prefabRows[i].resultPrefab != null)
            {
                GameObject src = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabRows[i].sourcePrefab);
                GameObject result = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabRows[i].resultPrefab);
                srcPrefabs.Add(src);
                resultPrefabs.Add(result);
            }
        }

        Vector3 offsetX = new Vector3(2, 0, 0);

        // layout the prefabs
        GameObject srcRoot = new GameObject("SourcePrefabInstances");
        GameObject resultRoot = new GameObject("ResultPrefabInstance");

        Vector3 srcPos = Vector3.zero - offsetX;
        Vector3 resultPos = Vector3.zero + offsetX;
        for (int i = 0; i < srcPrefabs.Count; i++)
        {
            Renderer[] rs = srcPrefabs[i].GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(Vector3.zero, Vector3.one);
            if (rs.Length > 0)
            {
                b = rs[0].bounds;
                for (int bndsIdx = 1; bndsIdx < rs.Length; bndsIdx++)
                {
                    b.Encapsulate(rs[bndsIdx].bounds);
                }
            }

            srcPrefabs[i].transform.parent = srcRoot.transform;
            resultPrefabs[i].transform.parent = resultRoot.transform;
            srcPrefabs[i].transform.localPosition = srcPos + new Vector3(-b.extents.x, 0, b.extents.z + b.extents.z * .3f);
            resultPrefabs[i].transform.localPosition = resultPos + new Vector3(b.extents.x, 0, b.extents.z + b.extents.z * .3f);
            srcPos += new Vector3(0,0,b.size.z + 1f);
            resultPos += new Vector3(0, 0, b.size.z + 1f);
        }
#else
        Debug.LogError("Cannot be used outside the editor");
#endif
    }

}
