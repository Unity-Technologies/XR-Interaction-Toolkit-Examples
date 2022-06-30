using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{
    /// <summary>
    /// This MeshAssignCustomizer alters the UV data as it is being assigned to the mesh.
    /// It appends the Texture Array slice index in the UV.z channel.
    /// 
    /// Shaders must be modified to read the slice index from the UV.z channel to use this.
    /// </summary>
    [CreateAssetMenu(fileName = "MeshAssignCustomizerPutSliceIdxInUV0_z", menuName = "Mesh Baker/Assign To Mesh Customizer/Put Slice Index In UV0.z", order = 1)]
    public class CustomizerPutSliceIndexInUV0_z : MB_DefaultMeshAssignCustomizer
    {
        public override void meshAssign_UV0(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
            if (textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.atlas)
            {
                mesh.uv = uvs;
            }
            else
            {
                {
                    if (uvs.Length == sliceIndexes.Length)
                    {
                        List<Vector3> nuvs = new List<Vector3>();
                        for (int i = 0; i < uvs.Length; i++)
                        {
                            nuvs.Add(new Vector3(uvs[i].x, uvs[i].y, sliceIndexes[i]));
                        }

                        mesh.SetUVs(0, nuvs);
                    }
                    else
                    {
                        Debug.LogError("UV slice buffer was not the same size as the uv buffer");
                    }
                }
            }
        }
    }
}
