using UnityEngine;
using System.Collections.Generic;

namespace DigitalOpus.MB.Core
{
    public class MB_DefaultMeshAssignCustomizer : ScriptableObject, IAssignToMeshCustomizer
    {
        public virtual void meshAssign_UV0(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
            mesh.uv = uvs;
        }

        public virtual void meshAssign_UV2(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
            mesh.uv2 = uvs;
        }

        public virtual void meshAssign_UV3(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
            mesh.uv3 = uvs;
        }

        public virtual void meshAssign_UV4(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
            mesh.uv4 = uvs;
        }


        public virtual void meshAssign_UV5(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
#if UNITY_2018_2_OR_NEWER
            mesh.uv5 = uvs;
#endif
        }

        public virtual void meshAssign_UV6(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
#if UNITY_2018_2_OR_NEWER
            mesh.uv6 = uvs;
#endif
        }

        public virtual void meshAssign_UV7(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
#if UNITY_2018_2_OR_NEWER
            mesh.uv7 = uvs;
#endif
        }

        public virtual void meshAssign_UV8(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes)
        {
#if UNITY_2018_2_OR_NEWER
            mesh.uv8 = uvs;
#endif
        }

        public virtual void meshAssign_colors(MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Color[] colors, float[] sliceIndexes)
        {
            mesh.colors = colors;
        }

        public static void DefaultDelegateAssignMeshColors(MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults,
                    Mesh mesh, Color[] colors, float[] sliceIndexes)
        {
            mesh.colors = colors;
        }
    }
}
