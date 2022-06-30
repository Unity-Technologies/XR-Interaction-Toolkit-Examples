using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{
    /// <summary>
    /// It is possible to combine multiple skinned meshes each of which may have blend shapes.
    /// This builds a map for mapping source blend shapes to combined blend shapes.
    /// The map can be serialized and saved in a prefab, this makes it possible to save combined
    /// meshes with Blend Shapes in a prefab.
    /// </summary>
    [System.Serializable]
    public class SerializableSourceBlendShape2Combined
    {
        public GameObject[] srcGameObject;

        public int[] srcBlendShapeIdx;

        public GameObject[] combinedMeshTargetGameObject;

        public int[] blendShapeIdx;

        public void SetBuffers(GameObject[] srcGameObjs, int[] srcBlendShapeIdxs,
                GameObject[] targGameObjs, int[] targBlendShapeIdx)
        {
            Debug.Assert(srcGameObjs.Length == srcBlendShapeIdxs.Length &&
                         srcGameObjs.Length == targGameObjs.Length &&
                         srcGameObjs.Length == targBlendShapeIdx.Length);
            srcGameObject = srcGameObjs;
            srcBlendShapeIdx = srcBlendShapeIdxs;
            combinedMeshTargetGameObject = targGameObjs;
            blendShapeIdx = targBlendShapeIdx;
        }

        public void DebugPrint()
        {
            if (srcGameObject == null)
            {
                Debug.LogError("Empty");
                return;
            }
            else
            {
                for (int i = 0; i < srcGameObject.Length; i++)
                {
                    Debug.LogFormat("{0} {1} {2} {3}", srcGameObject[i], srcBlendShapeIdx[i], combinedMeshTargetGameObject[i], blendShapeIdx[i]);
                }
            }

        }

        public Dictionary<MB3_MeshCombiner.MBBlendShapeKey, MB3_MeshCombiner.MBBlendShapeValue> GenerateMapFromSerializedData()
        {
            if (srcGameObject == null || srcBlendShapeIdx == null || combinedMeshTargetGameObject == null || blendShapeIdx == null ||
               srcGameObject.Length != srcBlendShapeIdx.Length ||
               srcGameObject.Length != combinedMeshTargetGameObject.Length ||
               srcGameObject.Length != blendShapeIdx.Length)
            {
                Debug.LogError("Error GenerateMapFromSerializedData. Serialized data was malformed or missing.");
                return null;
            }

            Dictionary<MB3_MeshCombiner.MBBlendShapeKey, MB3_MeshCombiner.MBBlendShapeValue> map = new Dictionary<MB3_MeshCombiner.MBBlendShapeKey, MB3_MeshCombiner.MBBlendShapeValue>();
            for (int i = 0; i < srcGameObject.Length; i++)
            {
                GameObject src = srcGameObject[i];
                GameObject targ = combinedMeshTargetGameObject[i];
                if (src == null || targ == null)
                {
                    Debug.LogError("Error GenerateMapFromSerializedData. There were null references in the serialized data to source or target game objects. This can happen " +
                           "if the SerializableSourceBlendShape2Combined was serialized in a prefab but the source and target SkinnedMeshRenderer GameObjects " +
                           " were not.");
                    return null;
                }

                map.Add(new MB3_MeshCombiner.MBBlendShapeKey(src, srcBlendShapeIdx[i]),
                        new MB3_MeshCombiner.MBBlendShapeValue()
                        {
                            combinedMeshGameObject = targ,
                            blendShapeIndex = blendShapeIdx[i]
                        }
                        );
            }

            return map;
        }
    }
}
