using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Combine meshes at runtime to save draw calls
    /// </summary>
    public class StaticBatch : MonoBehaviour {

        public Material CombineMaterial;

        void Start() {

            if(CombineMaterial == null) {
                Debug.Log("No material specified for mesh combine. Forget to assign it in the inspector?");
                return;
            }

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            // Make sure renderer is available
            MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
            if (mr == null) {
                mr = gameObject.AddComponent<MeshRenderer>();
                var firstMesh = transform.GetComponentInChildren<MeshRenderer>();

                mr.sharedMaterial = firstMesh.material;
                mr.sharedMaterial = CombineMaterial;
            }

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            List<MeshFilter> meshfilter = new List<MeshFilter>();
            for (int i = 1; i < meshFilters.Length; i++) {
                meshfilter.Add(meshFilters[i]);
            }
                
            CombineInstance[] combine = new CombineInstance[meshfilter.Count];
            int np = 0;
            while (np < meshfilter.Count) {
                combine[np].mesh = meshfilter[np].sharedMesh;
                combine[np].transform = meshfilter[np].transform.localToWorldMatrix;
                meshfilter[np].gameObject.SetActive(false);
                np++;
            }

            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            if(mf == null) {
                mf = gameObject.AddComponent<MeshFilter>();
            }

            mf.mesh = new Mesh();
            mf.mesh.CombineMeshes(combine, true, true);

            transform.gameObject.SetActive(true);
            
            transform.position = startPosition;
            transform.rotation = startRotation;
        }
    }
}

