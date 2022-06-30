using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalOpus.MB.Core
{

    public partial class MB3_MeshCombinerSingle : MB3_MeshCombiner
    {
        public class MB3_MeshCombinerSimpleBones
        {
            MB3_MeshCombinerSingle combiner;
            List<MB3_MeshCombinerSingle.MB_DynamicGameObject>[] boneIdx2dgoMap = null;
            HashSet<int> boneIdxsToDelete = new HashSet<int>();
            HashSet<MB3_MeshCombinerSingle.BoneAndBindpose> bonesToAdd = new HashSet<MB3_MeshCombinerSingle.BoneAndBindpose>();
            Dictionary<BoneAndBindpose, int> boneAndBindPose2idx = new Dictionary<BoneAndBindpose, int>();

            public MB3_MeshCombinerSimpleBones(MB3_MeshCombinerSingle cm)
            {
                combiner = cm;
            }

            public HashSet<MB3_MeshCombinerSingle.BoneAndBindpose> GetBonesToAdd()
            {
                return bonesToAdd;
            }

            public int GetNumBonesToDelete()
            {
                return boneIdxsToDelete.Count;
            }

            private bool _didSetup = false;
            public void BuildBoneIdx2DGOMapIfNecessary(int[] _goToDelete)
            {
                _didSetup = false;
                if (combiner.settings.renderType == MB_RenderType.skinnedMeshRenderer)
                {
                    if (_goToDelete.Length > 0)
                    {
                        boneIdx2dgoMap = _buildBoneIdx2dgoMap();
                    }

                    for (int i = 0; i < combiner.bones.Length; i++)
                    {
                        BoneAndBindpose bn = new BoneAndBindpose(combiner.bones[i], combiner.bindPoses[i]);
                        boneAndBindPose2idx.Add(bn, i);
                        //myBone2idx.Add(combiner.bones[i], i);
                    }

                    _didSetup = true;
                }
            }

            public void FindBonesToDelete(MB_DynamicGameObject dgo)
            {
                Debug.Assert(_didSetup);
                Debug.Assert(combiner.settings.renderType == MB_RenderType.skinnedMeshRenderer);
                // We could be working with adding and deleting smr body parts from the same rig. Different smrs will share 
                // the same bones. Track if we need to delete a bone or not.
                for (int j = 0; j < dgo.indexesOfBonesUsed.Length; j++)
                {
                    int idxOfUsedBone = dgo.indexesOfBonesUsed[j];
                    List<MB_DynamicGameObject> dgosThatUseBone = boneIdx2dgoMap[idxOfUsedBone];
                    if (dgosThatUseBone.Contains(dgo))
                    {
                        dgosThatUseBone.Remove(dgo);
                        if (dgosThatUseBone.Count == 0)
                        {
                            boneIdxsToDelete.Add(idxOfUsedBone);
                        }
                    }
                }
            }

            public int GetNewBonesLength()
            {
                return combiner.bindPoses.Length + bonesToAdd.Count - boneIdxsToDelete.Count;
            }

            public bool CollectBonesToAddForDGO(MB_DynamicGameObject dgo, Renderer r, bool noExtraBonesForMeshRenderers, MeshChannelsCache meshChannelCache)
            {
                bool success = true;
                Debug.Assert(_didSetup, "Need to setup first.");
                Debug.Assert(combiner.settings.renderType == MB_RenderType.skinnedMeshRenderer);
                // We could be working with adding and deleting smr body parts from the same rig. Different smrs will share 
                // the same bones.

                //cache the bone data that we will be adding.
                Matrix4x4[] dgoBindPoses = dgo._tmpSMR_CachedBindposes = meshChannelCache.GetBindposes(r, out dgo.isSkinnedMeshWithBones);
                BoneWeight[] dgoBoneWeights = dgo._tmpSMR_CachedBoneWeights = meshChannelCache.GetBoneWeights(r, dgo.numVerts, dgo.isSkinnedMeshWithBones);
                Transform[] dgoBones = dgo._tmpSMR_CachedBones = combiner._getBones(r, dgo.isSkinnedMeshWithBones);


                for (int i = 0; i < dgoBones.Length; i++)
                {
                    if (dgoBones[i] == null)
                    {
                        Debug.LogError("Source mesh r had a 'null' bone. Bones must not be null: " + r);
                        success = false;
                    }
                }

                if (!success) return success;

                if (noExtraBonesForMeshRenderers)
                {
                    if (MB_Utility.GetRenderer(dgo.gameObject) is MeshRenderer)
                    {
                        // We are visiting a single dgo which is a MeshRenderer.
                        // It may be the child decendant of a bone in another skinned mesh that is being baked or is already in the combined mesh. We need to find that bone if it exists.
                        // We need to check our parent ancestors and search the bone lists of the other dgos being added or previously baked looking for bones that may have been added 
                        Debug.Assert(dgoBones.Length == 1 && dgoBindPoses.Length == 1);
                        //     find and cache the parent bone for this MeshRenderer (it may not be the transform.parent)
                        bool foundBoneParent = false;
                        BoneAndBindpose boneParent = new BoneAndBindpose();
                        {
                            Transform t = dgo.gameObject.transform.parent;
                            while (t != null)
                            {
                                // Look for parent peviously baked in the combined mesh.
                                foreach (BoneAndBindpose b in boneAndBindPose2idx.Keys)
                                {
                                    if (b.bone == t)
                                    {
                                        boneParent = b;
                                        foundBoneParent = true;
                                        break;
                                    }
                                }

                                // Look for parent in something we are adding.
                                foreach (BoneAndBindpose b in bonesToAdd)
                                {
                                    if (b.bone == t)
                                    {
                                        boneParent = b;
                                        foundBoneParent = true;
                                        break;
                                    }
                                }

                                if (foundBoneParent)
                                {
                                    break;
                                }
                                else
                                {
                                    t = t.parent;
                                }
                            }
                        }

                        if (foundBoneParent)
                        {
                            dgoBones[0] = boneParent.bone;
                            dgoBindPoses[0] = boneParent.bindPose;
                        }
                    }
                }

                // The mesh being added may not use all bones on the rig. Find the bones actually used.
                int[] usedBoneIdx2srcMeshBoneIdx;
                {
                    /*
                    HashSet<int> usedBones = new HashSet<int>();
                    for (int j = 0; j < dgoBoneWeights.Length; j++)
                    {
                        usedBones.Add(dgoBoneWeights[j].boneIndex0);
                        usedBones.Add(dgoBoneWeights[j].boneIndex1);
                        usedBones.Add(dgoBoneWeights[j].boneIndex2);
                        usedBones.Add(dgoBoneWeights[j].boneIndex3);
                    }

                    usedBoneIdx2srcMeshBoneIdx = new int[usedBones.Count];
                    usedBones.CopyTo(usedBoneIdx2srcMeshBoneIdx);
                    */
                }

                {
                    usedBoneIdx2srcMeshBoneIdx = new int[dgoBones.Length];
                    for (int i = 0; i < usedBoneIdx2srcMeshBoneIdx.Length; i++) usedBoneIdx2srcMeshBoneIdx[i] = i;
                }

                // For each bone see if it exists in the bones array (with the same bindpose.).
                // We might be baking several skinned meshes on the same rig. We don't want duplicate bones in the bones array.
                for (int i = 0; i < dgoBones.Length; i++)
                {
                    bool foundInBonesList = false;
                    int bidx;
                    int dgoBoneIdx = usedBoneIdx2srcMeshBoneIdx[i];
                    BoneAndBindpose bb = new BoneAndBindpose(dgoBones[dgoBoneIdx], dgoBindPoses[dgoBoneIdx]);
                    if (boneAndBindPose2idx.TryGetValue(bb, out bidx))
                    {
                        if (dgoBones[dgoBoneIdx] == combiner.bones[bidx] && 
                            !boneIdxsToDelete.Contains(bidx) &&
                            dgoBindPoses[dgoBoneIdx] == combiner.bindPoses[bidx])
                        {
                            foundInBonesList = true;
                        }
                    }

                    if (!foundInBonesList)
                    {
                        if (!bonesToAdd.Contains(bb))
                        {
                            bonesToAdd.Add(bb);
                        }
                    }
                }

                dgo._tmpSMRIndexesOfSourceBonesUsed = usedBoneIdx2srcMeshBoneIdx;
                return success;
            }

            private List<MB3_MeshCombinerSingle.MB_DynamicGameObject>[] _buildBoneIdx2dgoMap()
            {
                List<MB3_MeshCombinerSingle.MB_DynamicGameObject>[] boneIdx2dgoMap = new List<MB3_MeshCombinerSingle.MB_DynamicGameObject>[combiner.bones.Length];
                for (int i = 0; i < boneIdx2dgoMap.Length; i++) boneIdx2dgoMap[i] = new List<MB3_MeshCombinerSingle.MB_DynamicGameObject>();
                // build the map of bone indexes to objects that use them
                for (int i = 0; i < combiner.mbDynamicObjectsInCombinedMesh.Count; i++)
                {
                    MB3_MeshCombinerSingle.MB_DynamicGameObject dgo = combiner.mbDynamicObjectsInCombinedMesh[i];
                    for (int j = 0; j < dgo.indexesOfBonesUsed.Length; j++)
                    {
                        boneIdx2dgoMap[dgo.indexesOfBonesUsed[j]].Add(dgo);
                    }
                }

                return boneIdx2dgoMap;
            }

            public void CopyBonesWeAreKeepingToNewBonesArrayAndAdjustBWIndexes(Transform[] nbones, Matrix4x4[] nbindPoses, BoneWeight[] nboneWeights, int totalDeleteVerts)
            {
                // bones are copied separately because some dgos share bones
                if (boneIdxsToDelete.Count > 0)
                {
                    int[] boneIdxsToDel = new int[boneIdxsToDelete.Count];
                    boneIdxsToDelete.CopyTo(boneIdxsToDel);
                    Array.Sort(boneIdxsToDel);
                    //bones are being moved in bones array so need to do some remapping
                    int[] oldBonesIndex2newBonesIndexMap = new int[combiner.bones.Length];
                    int newIdx = 0;
                    int indexInDeleteList = 0;

                    //bones were deleted so we need to rebuild bones and bind poses
                    //and build a map of old bone indexes to new bone indexes
                    //do this by copying old to new skipping ones we are deleting
                    for (int i = 0; i < combiner.bones.Length; i++)
                    {
                        if (indexInDeleteList < boneIdxsToDel.Length &&
                            boneIdxsToDel[indexInDeleteList] == i)
                        {
                            //we are deleting this bone so skip its index
                            indexInDeleteList++;
                            oldBonesIndex2newBonesIndexMap[i] = -1;
                        }
                        else
                        {
                            oldBonesIndex2newBonesIndexMap[i] = newIdx;
                            nbones[newIdx] = combiner.bones[i];
                            nbindPoses[newIdx] = combiner.bindPoses[i];
                            newIdx++;
                        }
                    }
                    //adjust the indexes on the boneWeights
                    int numVertKeeping = combiner.boneWeights.Length - totalDeleteVerts;
                    {
                        for (int i = 0; i < numVertKeeping; i++)
                        {
                            BoneWeight bw = nboneWeights[i];
                            bw.boneIndex0 = oldBonesIndex2newBonesIndexMap[bw.boneIndex0];
                            bw.boneIndex1 = oldBonesIndex2newBonesIndexMap[bw.boneIndex1];
                            bw.boneIndex2 = oldBonesIndex2newBonesIndexMap[bw.boneIndex2];
                            bw.boneIndex3 = oldBonesIndex2newBonesIndexMap[bw.boneIndex3];
                            nboneWeights[i] = bw;
                        }
                    }

                    /*
                    unsafe
                    {
                        fixed (BoneWeight* boneWeightFirstPtr = &nboneWeights[0])
                        {
                            BoneWeight* boneWeightPtr = boneWeightFirstPtr;
                            for (int i = 0; i < numVertKeeping; i++)
                            {
                                boneWeightPtr->boneIndex0 = oldBonesIndex2newBonesIndexMap[boneWeightPtr->boneIndex0];
                                boneWeightPtr->boneIndex1 = oldBonesIndex2newBonesIndexMap[boneWeightPtr->boneIndex1];
                                boneWeightPtr->boneIndex2 = oldBonesIndex2newBonesIndexMap[boneWeightPtr->boneIndex2];
                                boneWeightPtr->boneIndex3 = oldBonesIndex2newBonesIndexMap[boneWeightPtr->boneIndex3];
                                boneWeightPtr++;
                            }
                        }
                    }
                    */

                    //adjust the bone indexes on the dgos from old to new
                    for (int i = 0; i < combiner.mbDynamicObjectsInCombinedMesh.Count; i++)
                    {
                        MB_DynamicGameObject dgo = combiner.mbDynamicObjectsInCombinedMesh[i];
                        for (int j = 0; j < dgo.indexesOfBonesUsed.Length; j++)
                        {
                            dgo.indexesOfBonesUsed[j] = oldBonesIndex2newBonesIndexMap[dgo.indexesOfBonesUsed[j]];
                        }
                    }
                }
                else
                { //no bones are moving so can simply copy bones from old to new
                    Array.Copy(combiner.bones, nbones, combiner.bones.Length);
                    Array.Copy(combiner.bindPoses, nbindPoses, combiner.bindPoses.Length);
                }
            }

            public static void AddBonesToNewBonesArrayAndAdjustBWIndexes(MB3_MeshCombinerSingle combiner, MB_DynamicGameObject dgo, Renderer r, int vertsIdx,
                                                             Transform[] nbones, BoneWeight[] nboneWeights, MeshChannelsCache meshChannelCache)
            {
                Transform[] dgoBones = dgo._tmpSMR_CachedBones;
                Matrix4x4[] dgoBindPoses = dgo._tmpSMR_CachedBindposes;
                BoneWeight[] dgoBoneWeights = dgo._tmpSMR_CachedBoneWeights;
                int[] srcIndex2combinedIndexMap = new int[dgoBones.Length];
                for (int j = 0; j < dgo._tmpSMRIndexesOfSourceBonesUsed.Length; j++)
                {
                    int dgoBoneIdx = dgo._tmpSMRIndexesOfSourceBonesUsed[j];

                    for (int k = 0; k < nbones.Length; k++)
                    {
                        if (dgoBones[dgoBoneIdx] == nbones[k])
                        {
                            if (dgoBindPoses[dgoBoneIdx] == combiner.bindPoses[k])
                            {
                                srcIndex2combinedIndexMap[dgoBoneIdx] = k;
                                break;
                            }
                        }
                    }
                }

                //remap the bone weights for this dgo
                //build a list of usedBones, can't trust dgoBones because it contains all bones in the rig
                for (int j = 0; j < dgoBoneWeights.Length; j++)
                {
                    int newVertIdx = vertsIdx + j;
                    nboneWeights[newVertIdx].boneIndex0 = srcIndex2combinedIndexMap[dgoBoneWeights[j].boneIndex0];
                    nboneWeights[newVertIdx].boneIndex1 = srcIndex2combinedIndexMap[dgoBoneWeights[j].boneIndex1];
                    nboneWeights[newVertIdx].boneIndex2 = srcIndex2combinedIndexMap[dgoBoneWeights[j].boneIndex2];
                    nboneWeights[newVertIdx].boneIndex3 = srcIndex2combinedIndexMap[dgoBoneWeights[j].boneIndex3];
                    nboneWeights[newVertIdx].weight0 = dgoBoneWeights[j].weight0;
                    nboneWeights[newVertIdx].weight1 = dgoBoneWeights[j].weight1;
                    nboneWeights[newVertIdx].weight2 = dgoBoneWeights[j].weight2;
                    nboneWeights[newVertIdx].weight3 = dgoBoneWeights[j].weight3;
                }

                // repurposing the _tmpIndexesOfSourceBonesUsed since
                //we don't need it anymore and this saves a memory allocation . remap the indexes that point to source bones to combined bones.
                for (int j = 0; j < dgo._tmpSMRIndexesOfSourceBonesUsed.Length; j++)
                {
                    dgo._tmpSMRIndexesOfSourceBonesUsed[j] = srcIndex2combinedIndexMap[dgo._tmpSMRIndexesOfSourceBonesUsed[j]];
                }
                dgo.indexesOfBonesUsed = dgo._tmpSMRIndexesOfSourceBonesUsed;
                dgo._tmpSMRIndexesOfSourceBonesUsed = null;
                dgo._tmpSMR_CachedBones = null;
                dgo._tmpSMR_CachedBindposes = null;
                dgo._tmpSMR_CachedBoneWeights = null;

                //check original bones and bindPoses
                /*
                for (int j = 0; j < dgo.indexesOfBonesUsed.Length; j++) {
                    Transform bone = bones[dgo.indexesOfBonesUsed[j]];
                    Matrix4x4 bindpose = bindPoses[dgo.indexesOfBonesUsed[j]];
                    bool found = false;
                    for (int k = 0; k < dgo._originalBones.Length; k++) {
                        if (dgo._originalBones[k] == bone && dgo._originalBindPoses[k] == bindpose) {
                            found = true;
                        }
                    }
                    if (!found) Debug.LogError("A Mismatch between original bones and bones array. " + dgo.name);
                }
                */
            }

            internal void CopyVertsNormsTansToBuffers(MB_DynamicGameObject dgo, MB_IMeshBakerSettings settings, int vertsIdx, Vector3[] nnorms, Vector4[] ntangs, Vector3[] nverts, Vector3[] normals, Vector4[] tangents, Vector3[] verts)
            {
                bool isMeshRenderer = dgo.gameObject.GetComponent<Renderer>() is MeshRenderer;
                if (settings.smrNoExtraBonesWhenCombiningMeshRenderers &&
                    isMeshRenderer &&
                    dgo._tmpSMR_CachedBones[0] != dgo.gameObject.transform // bone may not have a parent ancestor that is a bone
                    )
                {
                    // transform all the verticies, norms and tangents into the parent bone's local space (adjusted by the parent bone's bind pose).
                    // there should be only one bone and bind pose for a mesh renderer dgo. 
                    // The bone and bind pose should be the parent-bone's NOT the MeshRenderers.
                    Matrix4x4 l2parentMat = dgo._tmpSMR_CachedBindposes[0].inverse * dgo._tmpSMR_CachedBones[0].worldToLocalMatrix * dgo.gameObject.transform.localToWorldMatrix;

                    // Similar to local2world but with translation removed and we are using the inverse transpose.
                    // We use this for normals and tangents because it handles scaling correctly.
                    Matrix4x4 l2parentRotScale = l2parentMat;
                    l2parentRotScale[0, 3] = l2parentRotScale[1, 3] = l2parentRotScale[2, 3] = 0f;
                    l2parentRotScale = l2parentRotScale.inverse.transpose;

                    //can't modify the arrays we get from the cache because they will be modified multiple times if the same mesh is being added multiple times.
                    for (int j = 0; j < nverts.Length; j++)
                    {
                        int vIdx = vertsIdx + j;
                        verts[vertsIdx + j] = l2parentMat.MultiplyPoint3x4(nverts[j]);
                        if (settings.doNorm)
                        {
                            normals[vIdx] = l2parentRotScale.MultiplyPoint3x4(nnorms[j]).normalized;
                        }
                        if (settings.doTan)
                        {
                            float w = ntangs[j].w; //need to preserve the w value
                            tangents[vIdx] = l2parentRotScale.MultiplyPoint3x4(((Vector3)ntangs[j])).normalized;
                            tangents[vIdx].w = w;
                        }
                    }
                }
                else
                {
                    if (settings.doNorm) nnorms.CopyTo(normals, vertsIdx);
                    if (settings.doTan) ntangs.CopyTo(tangents, vertsIdx);
                    nverts.CopyTo(verts, vertsIdx);
                }
            }
        }

        public override void UpdateSkinnedMeshApproximateBounds()
        {
            UpdateSkinnedMeshApproximateBoundsFromBounds();
        }

        public override void UpdateSkinnedMeshApproximateBoundsFromBones()
        {
            if (outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace)
            {
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Can't UpdateSkinnedMeshApproximateBounds when output type is bakeMeshAssetsInPlace");
                return;
            }
            if (bones.Length == 0)
            {
                if (verts.Length > 0) if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("No bones in SkinnedMeshRenderer. Could not UpdateSkinnedMeshApproximateBounds.");
                return;
            }
            if (_targetRenderer == null)
            {
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Target Renderer is not set. No point in calling UpdateSkinnedMeshApproximateBounds.");
                return;
            }
            if (!_targetRenderer.GetType().Equals(typeof(SkinnedMeshRenderer)))
            {
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Target Renderer is not a SkinnedMeshRenderer. No point in calling UpdateSkinnedMeshApproximateBounds.");
                return;
            }
            UpdateSkinnedMeshApproximateBoundsFromBonesStatic(bones, (SkinnedMeshRenderer)targetRenderer);
        }

        public override void UpdateSkinnedMeshApproximateBoundsFromBounds()
        {
            if (outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace)
            {
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Can't UpdateSkinnedMeshApproximateBoundsFromBounds when output type is bakeMeshAssetsInPlace");
                return;
            }
            if (verts.Length == 0 || mbDynamicObjectsInCombinedMesh.Count == 0)
            {
                if (verts.Length > 0) if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Nothing in SkinnedMeshRenderer. CoulddoBlendShapes not UpdateSkinnedMeshApproximateBoundsFromBounds.");
                return;
            }
            if (_targetRenderer == null)
            {
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Target Renderer is not set. No point in calling UpdateSkinnedMeshApproximateBoundsFromBounds.");
                return;
            }
            if (!_targetRenderer.GetType().Equals(typeof(SkinnedMeshRenderer)))
            {
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Target Renderer is not a SkinnedMeshRenderer. No point in calling UpdateSkinnedMeshApproximateBoundsFromBounds.");
                return;
            }

            UpdateSkinnedMeshApproximateBoundsFromBoundsStatic(objectsInCombinedMesh, (SkinnedMeshRenderer)targetRenderer);
        }
    }
}
