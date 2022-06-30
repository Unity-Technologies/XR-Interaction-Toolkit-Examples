using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

#if UNITY_EDITOR
    using UnityEditor;
#endif

public class MB3_MeshBakerGrouper : MonoBehaviour, MB_IMeshBakerSettingsHolder
{
    public enum ClusterType
    {
        none,
        grid,
        pie,
        agglomerative,
    }

    public static readonly Color WHITE_TRANSP = new Color(1f,1f,1f,.1f);

    public MB3_MeshBakerGrouperCore grouper;
    public ClusterType clusterType = ClusterType.none;

    /// <summary>
    /// Baked meshes will be added as a child of this scene object.
    /// </summary>
    public Transform parentSceneObject;
    public GrouperData data = new GrouperData();

    //these are for getting a resonable bounds in which to draw gizmos.
    [HideInInspector] public Bounds sourceObjectBounds = new Bounds(Vector3.zero, Vector3.one);
    
    public string prefabOptions_outputFolder = "";
    public bool prefabOptions_autoGeneratePrefabs;
    public bool prefabOptions_mergeOutputIntoSinglePrefab;

    public MB3_MeshCombinerSettings meshBakerSettingsAsset;
    public MB3_MeshCombinerSettingsData meshBakerSettings;

    public MB_IMeshBakerSettings GetMeshBakerSettings()
    {
        if (meshBakerSettingsAsset == null)
        {
            if (meshBakerSettings == null) meshBakerSettings = new MB3_MeshCombinerSettingsData();
            return meshBakerSettings;
        }
        else
        {
            return meshBakerSettingsAsset.GetMeshBakerSettings();
        }
    }

    public void GetMeshBakerSettingsAsSerializedProperty(out string propertyName, out UnityEngine.Object targetObj)
    {
        if (meshBakerSettingsAsset == null)
        {
            targetObj = this;
            propertyName = "meshBakerSettings";
        }
        else
        {
            targetObj = meshBakerSettingsAsset;
            propertyName = "data";
        }
    }


    void OnDrawGizmosSelected()
    {
        if (grouper == null)
        {
            grouper = CreateGrouper(clusterType, data);
        }
        if (grouper.d == null)
        {
            grouper.d = data;
        }
        grouper.DrawGizmos(sourceObjectBounds);
    }

    public MB3_MeshBakerGrouperCore CreateGrouper(ClusterType t, GrouperData data)
    {
        if (t == ClusterType.grid) grouper = new MB3_MeshBakerGrouperGrid(data);
        if (t == ClusterType.pie) grouper = new MB3_MeshBakerGrouperPie(data);
        if (t == ClusterType.agglomerative)
        {
            MB3_TextureBaker tb = GetComponent<MB3_TextureBaker>();
            List<GameObject> gos;
            if (tb != null)
            {
                gos = tb.GetObjectsToCombine();
            }
            else
            {
                gos = new List<GameObject>();
            }
            grouper = new MB3_MeshBakerGrouperCluster(data, gos);
        }
        if (t == ClusterType.none) grouper = new MB3_MeshBakerGrouperNone(data);
        return grouper;
    }

    public void DeleteAllChildMeshBakers()
    {
        MB3_MeshBakerCommon[] mBakers = GetComponentsInChildren<MB3_MeshBakerCommon>();
        for (int i = 0; i < mBakers.Length; i++)
        {
            MB3_MeshBakerCommon mb = mBakers[i];
            GameObject resultGameObject = mb.meshCombiner.resultSceneObject;
            MB_Utility.Destroy(resultGameObject);
            MB_Utility.Destroy(mb.gameObject);
        }
    }
}

namespace DigitalOpus.MB.Core
{
    /// all properties go here so that settings are remembered as user switches between cluster types
    [Serializable]
    public class GrouperData
    {
        public bool clusterOnLMIndex;
        public bool clusterByLODLevel;
        public Vector3 origin;

        //Normally these properties would be in the subclasses but putting them here makes writing the inspector much easier
        //for grid
        public Vector3 cellSize;

        //for pie
        public int pieNumSegments = 4;
        public Vector3 pieAxis = Vector3.up;
        public float ringSpacing = 100f;
        public bool combineSegmentsInInnermostRing = false;

        //for clustering
        public int height = 1;
        public float maxDistBetweenClusters = 1f;
        public bool includeCellsWithOnlyOneRenderer = true;
    }

    [Serializable]
    public abstract class MB3_MeshBakerGrouperCore
    {

        public GrouperData d;
        public abstract Dictionary<string, List<Renderer>> FilterIntoGroups(List<GameObject> selection);
        public abstract void DrawGizmos(Bounds sourceObjectBounds);
        public List<MB3_MeshBakerCommon> DoClustering(MB3_TextureBaker tb, MB3_MeshBakerGrouper grouper)
        {
            List<MB3_MeshBakerCommon> outBakers = new List<MB3_MeshBakerCommon>();
            if (grouper.prefabOptions_autoGeneratePrefabs || grouper.prefabOptions_mergeOutputIntoSinglePrefab)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("Cannot generate prefabs while playing. Prefabs can only be generated in the editor and not in play mode.");
                    return outBakers;
                }
            }

            //todo warn for no objects and no Texture Bake Result
            Dictionary<string, List<Renderer>> cell2objs = FilterIntoGroups(tb.GetObjectsToCombine());

            if (d.clusterOnLMIndex)
            {
                Dictionary<string, List<Renderer>> cell2objsNew = new Dictionary<string, List<Renderer>>();
                foreach (string key in cell2objs.Keys)
                {
                    List<Renderer> gaws = cell2objs[key];
                    Dictionary<int, List<Renderer>> idx2objs = GroupByLightmapIndex(gaws);
                    foreach (int keyIdx in idx2objs.Keys)
                    {
                        string keyNew = key + "-LM-" + keyIdx;
                        cell2objsNew.Add(keyNew, idx2objs[keyIdx]);
                    }
                }
                cell2objs = cell2objsNew;
            }
            if (d.clusterByLODLevel)
            {
                //visit each cell
                //visit each renderer
                //check if that renderer is a child of an LOD group
                //      visit each LOD level check if this renderer is in that list.
                //      if not add it to LOD0 for that cell
                //      otherwise add it to LODX for that cell creating LODs as necessary
                Dictionary<string, List<Renderer>> cell2objsNew = new Dictionary<string, List<Renderer>>();
                foreach (string key in cell2objs.Keys)
                {
                    List<Renderer> gaws = cell2objs[key];
                    foreach (Renderer r in gaws)
                    {
                        if (r == null) continue;
                        bool foundInLOD = false;
                        LODGroup lodg = r.GetComponentInParent<LODGroup>();
                        if (lodg != null)
                        {
                            LOD[] lods = lodg.GetLODs();
                            for (int i = 0; i < lods.Length; i++)
                            {
                                LOD lod = lods[i];
                                if (Array.Find<Renderer>(lod.renderers, x => x == r) != null)
                                {
                                    foundInLOD = true;
                                    List<Renderer> rs;
                                    string newKey = String.Format("{0}_LOD{1}", key, i);
                                    if (!cell2objsNew.TryGetValue(newKey, out rs))
                                    {
                                        rs = new List<Renderer>();
                                        cell2objsNew.Add(newKey, rs);
                                    }
                                    if (!rs.Contains(r)) rs.Add(r);
                                }
                            }
                        }
                        if (!foundInLOD)
                        {
                            List<Renderer> rs;
                            string newKey = String.Format("{0}_LOD0", key);
                            if (!cell2objsNew.TryGetValue(newKey, out rs))
                            {
                                rs = new List<Renderer>();
                                cell2objsNew.Add(newKey, rs);
                            }
                            if (!rs.Contains(r)) rs.Add(r);
                        }
                    }
                }
                cell2objs = cell2objsNew;
            }

            int clustersWithOnlyOneRenderer = 0;
            foreach (string key in cell2objs.Keys)
            {
                List<Renderer> gaws = cell2objs[key];
                if (gaws.Count > 1 || grouper.data.includeCellsWithOnlyOneRenderer)
                {
                    outBakers.Add(AddMeshBaker(grouper, tb, key, gaws));
                }
                else
                {
                    clustersWithOnlyOneRenderer++;
                }
            }

            Debug.Log(String.Format("Found {0} cells with Renderers. Not creating bakers for {1} because there is only one mesh in the cell. Creating {2} bakers.", cell2objs.Count, clustersWithOnlyOneRenderer, cell2objs.Count - clustersWithOnlyOneRenderer));
            return outBakers;
        }

        Dictionary<int, List<Renderer>> GroupByLightmapIndex(List<Renderer> gaws)
        {
            Dictionary<int, List<Renderer>> idx2objs = new Dictionary<int, List<Renderer>>();
            for (int i = 0; i < gaws.Count; i++)
            {
                List<Renderer> objs = null;
                if (idx2objs.ContainsKey(gaws[i].lightmapIndex))
                {
                    objs = idx2objs[gaws[i].lightmapIndex];
                }
                else
                {
                    objs = new List<Renderer>();
                    idx2objs.Add(gaws[i].lightmapIndex, objs);
                }
                objs.Add(gaws[i]);
            }
            return idx2objs;
        }

        MB3_MeshBakerCommon AddMeshBaker(MB3_MeshBakerGrouper grouper, MB3_TextureBaker tb, string key, List<Renderer> gaws)
        {
            int numVerts = 0;
            for (int i = 0; i < gaws.Count; i++)
            {
                Mesh m = MB_Utility.GetMesh(gaws[i].gameObject);
                if (m != null)
                    numVerts += m.vertexCount;
            }

            GameObject nmb = new GameObject("MeshBaker-" + key);
            nmb.transform.position = Vector3.zero;
            MB3_MeshBakerCommon newMeshBaker;
            if (numVerts >= 65535)
            {
                newMeshBaker = nmb.AddComponent<MB3_MultiMeshBaker>();
                newMeshBaker.useObjsToMeshFromTexBaker = false;
            }
            else
            {
                newMeshBaker = nmb.AddComponent<MB3_MeshBaker>();
                newMeshBaker.useObjsToMeshFromTexBaker = false;
            }

            newMeshBaker.textureBakeResults = tb.textureBakeResults;
            newMeshBaker.transform.parent = tb.transform;
            newMeshBaker.meshCombiner.settingsHolder = grouper;
            for (int i = 0; i < gaws.Count; i++)
            {
                newMeshBaker.GetObjectsToCombine().Add(gaws[i].gameObject);
            }

            return newMeshBaker;
        }
    }

    [Serializable]
    public class MB3_MeshBakerGrouperNone : MB3_MeshBakerGrouperCore
    {
        public MB3_MeshBakerGrouperNone(GrouperData d)
        {
            this.d = d;
        }

        public override Dictionary<string, List<Renderer>> FilterIntoGroups(List<GameObject> selection)
        {
            Debug.Log("Filtering into groups none");

            Dictionary<string, List<Renderer>> cell2objs = new Dictionary<string, List<Renderer>>();

            List<Renderer> rs = new List<Renderer>();
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] != null)
                {
                    rs.Add(selection[i].GetComponent<Renderer>());
                }
            }

            cell2objs.Add("MeshBaker", rs);
            return cell2objs;
        }

        public override void DrawGizmos(Bounds sourceObjectBounds)
        {

        }
    }

    [Serializable]
    public class MB3_MeshBakerGrouperGrid : MB3_MeshBakerGrouperCore
    {
        public MB3_MeshBakerGrouperGrid(GrouperData d)
        {
            this.d = d;
        }

        public override Dictionary<string, List<Renderer>> FilterIntoGroups(List<GameObject> selection)
        {
            Dictionary<string, List<Renderer>> cell2objs = new Dictionary<string, List<Renderer>>();
            if (d.cellSize.x <= 0f || d.cellSize.y <= 0f || d.cellSize.z <= 0f)
            {
                Debug.LogError("cellSize x,y,z must all be greater than zero.");
                return cell2objs;
            }

            Debug.Log("Collecting renderers in each cell");
            foreach (GameObject t in selection)
            {
                if (t == null)
                {
                    continue;
                }

                GameObject go = t;
                Renderer mr = go.GetComponent<Renderer>();
                if (mr is MeshRenderer || mr is SkinnedMeshRenderer)
                {
                    //get the cell this gameObject is in
                    Vector3 gridVector = mr.bounds.center;
                    gridVector.x = Mathf.Floor((gridVector.x - d.origin.x) / d.cellSize.x) * d.cellSize.x;
                    gridVector.y = Mathf.Floor((gridVector.y - d.origin.y) / d.cellSize.y) * d.cellSize.y;
                    gridVector.z = Mathf.Floor((gridVector.z - d.origin.z) / d.cellSize.z) * d.cellSize.z;
                    List<Renderer> objs = null;
                    string gridVectorStr = gridVector.ToString();
                    if (cell2objs.ContainsKey(gridVectorStr))
                    {
                        objs = cell2objs[gridVectorStr];
                    }
                    else
                    {
                        objs = new List<Renderer>();
                        cell2objs.Add(gridVectorStr, objs);
                    }

                    if (!objs.Contains(mr))
                    {
                        //Debug.Log("Adding " + mr + " todo " + gridVectorStr);
                        objs.Add(mr);
                    }
                }
            }
            return cell2objs;
        }

        public override void DrawGizmos(Bounds sourceObjectBounds)
        {
            Vector3 cs = d.cellSize;
            if (cs.x <= .00001f || cs.y <= .00001f || cs.z <= .00001f) return;
            Gizmos.color = MB3_MeshBakerGrouper.WHITE_TRANSP;
            Vector3 p = sourceObjectBounds.center - sourceObjectBounds.extents;
            Vector3 offset = d.origin;
            offset.x = offset.x % cs.x;
            offset.y = offset.y % cs.y;
            offset.z = offset.z % cs.z;
            //snap p to closest cell center
            Vector3 start;
            p.x = Mathf.Round((p.x) / cs.x) * cs.x + offset.x;
            p.y = Mathf.Round((p.y) / cs.y) * cs.y + offset.y;
            p.z = Mathf.Round((p.z) / cs.z) * cs.z + offset.z;
            if (p.x > sourceObjectBounds.center.x - sourceObjectBounds.extents.x) p.x = p.x - cs.x;
            if (p.y > sourceObjectBounds.center.y - sourceObjectBounds.extents.y) p.y = p.y - cs.y;
            if (p.z > sourceObjectBounds.center.z - sourceObjectBounds.extents.z) p.z = p.z - cs.z;
            start = p;
            int numcells = Mathf.CeilToInt(sourceObjectBounds.size.x / cs.x + sourceObjectBounds.size.y / cs.y + sourceObjectBounds.size.z / cs.z);
            if (numcells > 200)
            {
                Gizmos.DrawWireCube(d.origin + cs / 2f, cs);
            }
            else
            {
                for (; p.x < sourceObjectBounds.center.x + sourceObjectBounds.extents.x; p.x += cs.x)
                {
                    p.y = start.y;
                    for (; p.y < sourceObjectBounds.center.y + sourceObjectBounds.extents.y; p.y += cs.y)
                    {
                        p.z = start.z;
                        for (; p.z < sourceObjectBounds.center.z + sourceObjectBounds.extents.z; p.z += cs.z)
                        {
                            Gizmos.DrawWireCube(p + cs / 2f, cs);
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public class MB3_MeshBakerGrouperPie : MB3_MeshBakerGrouperCore
    {
        public MB3_MeshBakerGrouperPie(GrouperData data)
        {
            d = data;
        }

        public override Dictionary<string, List<Renderer>> FilterIntoGroups(List<GameObject> selection)
        {
            Dictionary<string, List<Renderer>> cell2objs = new Dictionary<string, List<Renderer>>();
            if (d.pieNumSegments == 0)
            {
                Debug.LogError("pieNumSegments must be greater than zero.");
                return cell2objs;
            }

            if (d.pieAxis.magnitude <= .000001f)
            {
                Debug.LogError("Pie axis vector is too short.");
                return cell2objs;
            }

            if (d.ringSpacing <= .000001f)
            {
                Debug.LogError("Ring spacing is too small.");
                return cell2objs;
            }

            d.pieAxis.Normalize();
            Quaternion pieAxis2yIsUp = Quaternion.FromToRotation(d.pieAxis, Vector3.up);

            Debug.Log("Collecting renderers in each cell");
            foreach (GameObject t in selection)
            {
                if (t == null)
                {
                    continue;
                }

                GameObject go = t;
                Renderer mr = go.GetComponent<Renderer>();
                if (mr is MeshRenderer || mr is SkinnedMeshRenderer)
                {
                    //get the cell this gameObject is in
                    Vector3 origin2obj = mr.bounds.center - d.origin;
                    origin2obj = pieAxis2yIsUp * origin2obj;
                    Vector2 origin2Obj2D = new Vector2(origin2obj.x, origin2obj.z);
                    float radius = origin2Obj2D.magnitude;
                    origin2obj.Normalize();

                    float deg_aboutY = 0f;
                    if (Mathf.Abs(origin2obj.x) < 10e-5f && Mathf.Abs(origin2obj.z) < 10e-5f)
                    {
                        deg_aboutY = 0f;
                    }
                    else
                    {
                        deg_aboutY = Mathf.Atan2(origin2obj.x, origin2obj.z) * Mathf.Rad2Deg;
                        if (deg_aboutY < 0f) deg_aboutY = 360f + deg_aboutY;
                    }

                    //	Debug.Log ("Obj " + mr + " angle " + d_aboutY);
                    int segment = Mathf.FloorToInt(deg_aboutY / 360f * d.pieNumSegments);
                    int ring = Mathf.FloorToInt(radius / d.ringSpacing);
                    if (ring == 0 && d.combineSegmentsInInnermostRing)
                    {
                        segment = 0;
                    }

                    List<Renderer> objs = null;
                    string segStr = "seg_" + segment + "_ring_" + ring;
                    if (cell2objs.ContainsKey(segStr))
                    {
                        objs = cell2objs[segStr];
                    }
                    else
                    {
                        objs = new List<Renderer>();
                        cell2objs.Add(segStr, objs);
                    }

                    if (!objs.Contains(mr))
                    {
                        objs.Add(mr);
                    }
                }
            }

            return cell2objs;
        }

        public override void DrawGizmos(Bounds sourceObjectBounds)
        {
            
            if (d.pieAxis.magnitude < .1f) return;
            if (d.pieNumSegments < 1) return;

            Gizmos.color = MB3_MeshBakerGrouper.WHITE_TRANSP;
            float rad = sourceObjectBounds.extents.magnitude;

            int numRings = Mathf.CeilToInt(rad / d.ringSpacing);
            numRings = Mathf.Max(1, numRings);
            for (int i = 0; i < numRings; i++)
            {
                DrawCircle(d.pieAxis.normalized, d.origin, d.ringSpacing * (i + 1), 24);
            }
            
            Quaternion yIsUp2PieAxis = Quaternion.FromToRotation(Vector3.up, d.pieAxis);
            Quaternion rStep = Quaternion.AngleAxis(180f / d.pieNumSegments, Vector3.up);
            Vector3 r = Vector3.forward;
            for (int i = 0; i < d.pieNumSegments; i++)
            {
                Vector3 rr = yIsUp2PieAxis * r;
                Vector3 origin = d.origin;
                int nr = numRings;
                if (d.combineSegmentsInInnermostRing)
                {
                    origin = d.origin + rr.normalized * d.ringSpacing;
                    nr = numRings - 1;
                }

                if (nr == 0) break;

                Gizmos.DrawLine(origin, origin + nr * d.ringSpacing * rr.normalized);
                r = rStep * r;
                r = rStep * r;
            }
        }

        static int MaxIndexInVector3(Vector3 v)
        {
            int idx = 0;
            float val = v.x;
            if (v.y > val)
            {
                idx = 1;
                val = v.y;
            }
            if (v.z > val)
            {
                idx = 2;
                val = v.z;
            }
            return idx;
        }

        public static void DrawCircle(Vector3 axis, Vector3 center, float radius, int subdiv)
        {
            Quaternion q = Quaternion.AngleAxis(360 / subdiv, axis);
            int maxIdx = MaxIndexInVector3(axis);
            int otherIdx = maxIdx == 0 ? maxIdx + 1 : maxIdx - 1;
            Vector3 r = axis; //r construct a vector perpendicular to axis
            float temp = r[maxIdx];
            r[maxIdx] = r[otherIdx];
            r[otherIdx] = -temp;
            r = Vector3.ProjectOnPlane(r, axis);
            r.Normalize();
            r *= radius;
            for (int i = 0; i < subdiv + 1; i++)
            {
                Vector3 r2 = q * r;
                Gizmos.color = MB3_MeshBakerGrouper.WHITE_TRANSP;
                Gizmos.DrawLine(center + r, center + r2);
                r = r2;
            }
        }
    }


    [Serializable]
    public class MB3_MeshBakerGrouperKMeans : MB3_MeshBakerGrouperCore
    {
        public int numClusters = 4;
        public Vector3[] clusterCenters = new Vector3[0];
        public float[] clusterSizes = new float[0];

        public MB3_MeshBakerGrouperKMeans(GrouperData data)
        {
            d = data;
        }

        public override Dictionary<string, List<Renderer>> FilterIntoGroups(List<GameObject> selection)
        {
            Dictionary<string, List<Renderer>> cell2objs = new Dictionary<string, List<Renderer>>();
            List<GameObject> validObjs = new List<GameObject>();
            int numClusters = 20;
            foreach (GameObject t in selection)
            {
                if (t == null)
                {
                    continue;
                }
                GameObject go = t;
                Renderer mr = go.GetComponent<Renderer>();
                if (mr is MeshRenderer || mr is SkinnedMeshRenderer)
                {
                    //get the cell this gameObject is in
                    validObjs.Add(go);
                }
            }
            if (validObjs.Count > 0 && numClusters > 0 && numClusters < validObjs.Count)
            {
                MB3_KMeansClustering kmc = new MB3_KMeansClustering(validObjs, numClusters);
                kmc.Cluster();
                clusterCenters = new Vector3[numClusters];
                clusterSizes = new float[numClusters];
                for (int i = 0; i < numClusters; i++)
                {
                    List<Renderer> lr = kmc.GetCluster(i, out clusterCenters[i], out clusterSizes[i]);
                    if (lr.Count > 0)
                    {
                        cell2objs.Add("Cluster_" + i, lr);
                    }
                }
            }
            else
            {
                //todo error messages
            }
            return cell2objs;
        }

        public override void DrawGizmos(Bounds sceneObjectBounds)
        {
            Gizmos.color = MB3_MeshBakerGrouper.WHITE_TRANSP;
            if (clusterCenters != null && clusterSizes != null && clusterCenters.Length == clusterSizes.Length)
            {
                for (int i = 0; i < clusterSizes.Length; i++)
                {
                    Gizmos.DrawWireSphere(clusterCenters[i], clusterSizes[i]);
                }
            }
        }
    }

    [Serializable]
    public class MB3_MeshBakerGrouperCluster : MB3_MeshBakerGrouperCore
    {

        public MB3_AgglomerativeClustering cluster;
        float _lastMaxDistBetweenClusters;
        public float _ObjsExtents = 10f;
        public float _minDistBetweenClusters = .001f;
        List<MB3_AgglomerativeClustering.ClusterNode> _clustersToDraw = new List<MB3_AgglomerativeClustering.ClusterNode>();
        float[] _radii;

        public MB3_MeshBakerGrouperCluster(GrouperData data, List<GameObject> gos)
        {
            d = data;
        }

        public override Dictionary<string, List<Renderer>> FilterIntoGroups(List<GameObject> selection)
        {
            Dictionary<string, List<Renderer>> cell2objs = new Dictionary<string, List<Renderer>>();
            for (int i = 0; i < _clustersToDraw.Count; i++)
            {
                MB3_AgglomerativeClustering.ClusterNode node = _clustersToDraw[i];
                List<Renderer> rrs = new List<Renderer>();
                for (int j = 0; j < node.leafs.Length; j++)
                {
                    Renderer r = cluster.clusters[node.leafs[j]].leaf.go.GetComponent<Renderer>();
                    if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    {
                        rrs.Add(r);
                    }
                }
                cell2objs.Add("Cluster_" + i, rrs);
            }
            return cell2objs;
        }

        public void BuildClusters(List<GameObject> gos, ProgressUpdateCancelableDelegate progFunc)
        {
            if (gos.Count == 0)
            {
                Debug.LogWarning("No objects to cluster. Add some objects to the list of Objects To Combine.");
                return;
            }
            if (cluster == null) cluster = new MB3_AgglomerativeClustering();
            List<MB3_AgglomerativeClustering.item_s> its = new List<MB3_AgglomerativeClustering.item_s>();
            for (int i = 0; i < gos.Count; i++)
            {
                if (gos[i] != null && its.Find(x => x.go == gos[i]) == null)
                {
                    Renderer mr = gos[i].GetComponent<Renderer>();
                    if (mr != null && (mr is MeshRenderer || mr is SkinnedMeshRenderer))
                    {
                        MB3_AgglomerativeClustering.item_s ii = new MB3_AgglomerativeClustering.item_s();
                        ii.go = gos[i];
                        ii.coord = mr.bounds.center;
                        its.Add(ii);
                    }
                }
            }
            cluster.items = its;
            //yield return cluster.agglomerate();
            cluster.agglomerate(progFunc);
            if (!cluster.wasCanceled)
            {
                float smallest, largest;
                _BuildListOfClustersToDraw(progFunc, out smallest, out largest);
                d.maxDistBetweenClusters = Mathf.Lerp(smallest, largest, .9f);
            }
        }

        void _BuildListOfClustersToDraw(ProgressUpdateCancelableDelegate progFunc, out float smallest, out float largest)
        {
            _clustersToDraw.Clear();
            if (cluster.clusters == null)
            {
                smallest = 1f;
                largest = 10f;
                return;
            }
            if (progFunc != null) progFunc("Building Clusters To Draw A:", 0);
            List<MB3_AgglomerativeClustering.ClusterNode> removeMe = new List<MB3_AgglomerativeClustering.ClusterNode>();
            largest = 1f;
            smallest = 10e6f;
            for (int i = 0; i < cluster.clusters.Length; i++)
            {
                MB3_AgglomerativeClustering.ClusterNode node = cluster.clusters[i];
                //don't draw clusters that were merged too far apart and only want leaf nodes
                if (node.distToMergedCentroid <= d.maxDistBetweenClusters /*&& node.leaf == null*/)
                {
                    if (d.includeCellsWithOnlyOneRenderer)
                    {
                        _clustersToDraw.Add(node);
                    }
                    else if (node.leaf == null)
                    {
                        _clustersToDraw.Add(node);
                    }
                }
                if (node.distToMergedCentroid > largest)
                {
                    largest = node.distToMergedCentroid;
                }
                if (node.height > 0 && node.distToMergedCentroid < smallest)
                {
                    smallest = node.distToMergedCentroid;
                }
            }
            if (progFunc != null) progFunc("Building Clusters To Draw B:", 0);
            for (int i = 0; i < _clustersToDraw.Count; i++)
            {
                removeMe.Add(_clustersToDraw[i].cha);
                removeMe.Add(_clustersToDraw[i].chb);
            }

            for (int i = 0; i < removeMe.Count; i++)
            {
                _clustersToDraw.Remove(removeMe[i]);
            }
            _radii = new float[_clustersToDraw.Count];
            if (progFunc != null) progFunc("Building Clusters To Draw C:", 0);
            for (int i = 0; i < _radii.Length; i++)
            {
                MB3_AgglomerativeClustering.ClusterNode n = _clustersToDraw[i];
                Bounds b = new Bounds(n.centroid, Vector3.one);
                for (int j = 0; j < n.leafs.Length; j++)
                {
                    Renderer r = cluster.clusters[n.leafs[j]].leaf.go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        b.Encapsulate(r.bounds);
                    }
                }
                _radii[i] = b.extents.magnitude;
            }
            if (progFunc != null) progFunc("Building Clusters To Draw D:", 0);
            _ObjsExtents = largest + 1f;
            _minDistBetweenClusters = Mathf.Lerp(smallest, 0f, .9f);

            if (_ObjsExtents < 2f) _ObjsExtents = 2f;
        }

        public override void DrawGizmos(Bounds sceneObjectBounds)
        {
            if (cluster == null || cluster.clusters == null)
            {
                return;
            }
            if (_lastMaxDistBetweenClusters != d.maxDistBetweenClusters)
            {
                float s, l;
                _BuildListOfClustersToDraw(null, out s, out l);
                _lastMaxDistBetweenClusters = d.maxDistBetweenClusters;
            }

            Gizmos.color = MB3_MeshBakerGrouper.WHITE_TRANSP;
            for (int i = 0; i < _clustersToDraw.Count; i++)
            {
                Gizmos.color = MB3_MeshBakerGrouper.WHITE_TRANSP;
                MB3_AgglomerativeClustering.ClusterNode node = _clustersToDraw[i];
                Gizmos.DrawWireSphere(node.centroid, _radii[i]);
            }
        }
    }
}

