using UnityEngine;
using System.Collections.Generic;

namespace DigitalOpus.MB.Core
{
    public interface IAssignToMeshCustomizer
    {
        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV0(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV2(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV3(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV4(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV5(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV6(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV7(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_UV8(int channel, MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Vector2[] uvs, float[] sliceIndexes);

        /// <summary>
        /// For customizing data just before it is assigned to a mesh. If using Texture Arrays
        /// this can be used to inject the slice index into a coordinate in the mesh.
        /// </summary>
        void meshAssign_colors(MB_IMeshBakerSettings settings, MB2_TextureBakeResults textureBakeResults, Mesh mesh, Color[] colors, float[] sliceIndexes);
    }
}
