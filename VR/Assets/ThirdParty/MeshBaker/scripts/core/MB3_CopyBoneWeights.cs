using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DigitalOpus.MB.Core {
    public class MB3_CopyBoneWeights {
        public static void CopyBoneWeightsFromSeamMeshToOtherMeshes(float radius, Mesh seamMesh, Mesh[] targetMeshes) {
            //Todo make sure meshes are assets
			List<int> seamVerts = new List<int> ();
            if (seamMesh == null) {
                Debug.LogError(string.Format("The SeamMesh cannot be null"));
                return;
            }
			if (seamMesh.vertexCount == 0){
				Debug.LogError("The seam mesh has no vertices. Check that the Asset Importer for the seam mesh does not have 'Optimize Mesh' checked.");
				return;
			}
            Vector3[] srcVerts = seamMesh.vertices;
            BoneWeight[] srcBW = seamMesh.boneWeights;
            Vector3[] srcNormal = seamMesh.normals;
            Vector4[] srcTangent = seamMesh.tangents;
			Vector2[] srcUVs = seamMesh.uv;

			if (srcUVs.Length != srcVerts.Length) {
				Debug.LogError("The seam mesh needs uvs to identify which vertices are part of the seam. Vertices with UV > .5 are part of the seam. Vertices with UV < .5 are not part of the seam.");
				return;
			}

			for (int i = 0; i < srcUVs.Length; i++) {
				if (srcUVs[i].x > .5f && srcUVs[i].y > .5f){
					seamVerts.Add (i);
				}
			}
			Debug.Log (string.Format("The seam mesh has {0} vertices of which {1} are seam vertices.", seamMesh.vertices.Length, seamVerts.Count));
			if (seamVerts.Count == 0) {
				Debug.LogError("None of the vertices in the Seam Mesh were marked as seam vertices. To mark a vertex as a seam vertex the UV" +
				               " must be greater than (.5,.5). Vertices with UV less than (.5,.5) are excluded.");
				return;
			}

            //validate
            bool failed = false;
            for (int meshIdx = 0; meshIdx < targetMeshes.Length; meshIdx++) {
                if (targetMeshes[meshIdx] == null) {
						Debug.LogError(string.Format("Mesh {0} was null", meshIdx));
                    failed = true;
                }

                if (radius < 0f) {
                    Debug.LogError("radius must be zero or positive.");
                }
            }
            if (failed) {
                return;
            }
            for (int meshIdx = 0; meshIdx < targetMeshes.Length; meshIdx++) {
                Mesh tm = targetMeshes[meshIdx];
                Vector3[] otherVerts = tm.vertices;
                BoneWeight[] otherBWs = tm.boneWeights;
                Vector3[] otherNormals = tm.normals;
                Vector4[] otherTangents = tm.tangents;

                int numMatches = 0;
                for (int i = 0; i < otherVerts.Length; i++) {
					for (int sIdx = 0; sIdx < seamVerts.Count; sIdx++) {
						int j = seamVerts[sIdx];
                        if (Vector3.Distance(otherVerts[i], srcVerts[j]) <= radius) {
                            numMatches++;
                            otherBWs[i] = srcBW[j];
                            otherVerts[i] = srcVerts[j];
							if (otherNormals.Length == otherVerts.Length && srcNormal.Length == srcNormal.Length)
                            {
                                otherNormals[i] = srcNormal[j];
                            }
                            if (otherTangents.Length == otherVerts.Length && srcTangent.Length == srcVerts.Length)
                            {
                                otherTangents[i] = srcTangent[j];
                            }
                        }
                    }
                }
                if (numMatches > 0) {
					targetMeshes[meshIdx].vertices = otherVerts;
					targetMeshes[meshIdx].boneWeights = otherBWs;
					targetMeshes[meshIdx].normals = otherNormals;
					targetMeshes[meshIdx].tangents = otherTangents;
                }
                Debug.Log(string.Format("Copied boneweights for {1} vertices in mesh {0} that matched positions in the seam mesh.", targetMeshes[meshIdx].name, numMatches));
            }
        }
    }
}
