using System;
using Oculus.Avatar;
using UnityEngine;
using System.Runtime.InteropServices;

public class OvrAvatarAssetMesh : OvrAvatarAsset
{
    public Mesh mesh;
    private ovrAvatarSkinnedMeshPose skinnedBindPose;
    public string[] jointNames;

    public OvrAvatarAssetMesh(UInt64 _assetId, IntPtr asset, ovrAvatarAssetType meshType)
    {
        assetID = _assetId;
        mesh = new Mesh();
        mesh.name = "Procedural Geometry for asset " + _assetId;

        SetSkinnedBindPose(asset, meshType);

        long vertexCount = 0;
        IntPtr vertexBuffer = IntPtr.Zero;
        uint indexCount = 0;
        IntPtr indexBuffer = IntPtr.Zero;

        GetVertexAndIndexData(asset, meshType, out vertexCount, out vertexBuffer, out indexCount, out indexBuffer);

        AvatarLogger.Log("OvrAvatarAssetMesh: " + _assetId + " " + meshType.ToString() + " VertexCount:" + vertexCount);

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector4[] tangents = new Vector4[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];
        BoneWeight[] boneWeights = new BoneWeight[vertexCount];

        long vertexBufferStart = vertexBuffer.ToInt64();

        // We have different underlying vertex types to unpack, so switch on mesh type. 
        switch (meshType)
        {
            case ovrAvatarAssetType.Mesh:
                {
                    long vertexSize = (long)Marshal.SizeOf(typeof(ovrAvatarMeshVertex));

                    for (long i = 0; i < vertexCount; i++)
                    {
                        long offset = vertexSize * i;

                        ovrAvatarMeshVertex vertex = (ovrAvatarMeshVertex)Marshal.PtrToStructure(new IntPtr(vertexBufferStart + offset), typeof(ovrAvatarMeshVertex));
                        vertices[i] = new Vector3(vertex.x, vertex.y, -vertex.z);
                        normals[i] = new Vector3(vertex.nx, vertex.ny, -vertex.nz);
                        tangents[i] = new Vector4(vertex.tx, vertex.ty, -vertex.tz, vertex.tw);
                        uv[i] = new Vector2(vertex.u, vertex.v);
                        colors[i] = new Color(0, 0, 0, 1);

                        boneWeights[i].boneIndex0 = vertex.blendIndices[0];
                        boneWeights[i].boneIndex1 = vertex.blendIndices[1];
                        boneWeights[i].boneIndex2 = vertex.blendIndices[2];
                        boneWeights[i].boneIndex3 = vertex.blendIndices[3];
                        boneWeights[i].weight0 = vertex.blendWeights[0];
                        boneWeights[i].weight1 = vertex.blendWeights[1];
                        boneWeights[i].weight2 = vertex.blendWeights[2];
                        boneWeights[i].weight3 = vertex.blendWeights[3];
                    }
                }
                break;

            case ovrAvatarAssetType.CombinedMesh:
                {
                    long vertexSize = (long)Marshal.SizeOf(typeof(ovrAvatarMeshVertexV2));

                    for (long i = 0; i < vertexCount; i++)
                    {
                        long offset = vertexSize * i;

                        ovrAvatarMeshVertexV2 vertex = (ovrAvatarMeshVertexV2)Marshal.PtrToStructure(new IntPtr(vertexBufferStart + offset), typeof(ovrAvatarMeshVertexV2));
                        vertices[i] = new Vector3(vertex.x, vertex.y, -vertex.z);
                        normals[i] = new Vector3(vertex.nx, vertex.ny, -vertex.nz);
                        tangents[i] = new Vector4(vertex.tx, vertex.ty, -vertex.tz, vertex.tw);
                        uv[i] = new Vector2(vertex.u, vertex.v);
                        colors[i] = new Color(vertex.r, vertex.g, vertex.b, vertex.a);

                        boneWeights[i].boneIndex0 = vertex.blendIndices[0];
                        boneWeights[i].boneIndex1 = vertex.blendIndices[1];
                        boneWeights[i].boneIndex2 = vertex.blendIndices[2];
                        boneWeights[i].boneIndex3 = vertex.blendIndices[3];
                        boneWeights[i].weight0 = vertex.blendWeights[0];
                        boneWeights[i].weight1 = vertex.blendWeights[1];
                        boneWeights[i].weight2 = vertex.blendWeights[2];
                        boneWeights[i].weight3 = vertex.blendWeights[3];
                    }
                }
                break;
            default:
                throw new Exception("Bad Mesh Asset Type");
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.tangents = tangents;
        mesh.boneWeights = boneWeights;
        mesh.colors = colors;

        LoadBlendShapes(asset, vertexCount);
        LoadSubmeshes(asset, indexBuffer, indexCount);

        UInt32 jointCount = skinnedBindPose.jointCount;
        jointNames = new string[jointCount];
        for (UInt32 i = 0; i < jointCount; i++)
        {
            jointNames[i] = Marshal.PtrToStringAnsi(skinnedBindPose.jointNames[i]);
        }
    }

    private void LoadSubmeshes(IntPtr asset, IntPtr indexBufferPtr, ulong indexCount)
    {
        UInt32 subMeshCount = CAPI.ovrAvatarAsset_GetSubmeshCount(asset);

        AvatarLogger.Log("LoadSubmeshes: " + subMeshCount);

        Int16[] indices = new Int16[indexCount];
        Marshal.Copy(indexBufferPtr, indices, 0, (int)indexCount);

        mesh.subMeshCount = (int)subMeshCount;
        uint accumedOffset = 0;
        for (UInt32 index = 0; index < subMeshCount; index++)
        {
            var submeshIndexCount = CAPI.ovrAvatarAsset_GetSubmeshLastIndex(asset, index);
            var currSpan = submeshIndexCount - accumedOffset;

            Int32[] triangles = new Int32[currSpan];

            int triangleOffset = 0;
            for (ulong i = accumedOffset; i < submeshIndexCount; i += 3)
            {
                // NOTE: We are changing the order of each triangle to match unity expectations vs pipeline.
                triangles[triangleOffset + 2] = (Int32)indices[i];
                triangles[triangleOffset + 1] = (Int32)indices[i + 1];
                triangles[triangleOffset] = (Int32)indices[i + 2];

                triangleOffset += 3;
            }

            accumedOffset += currSpan;

            mesh.SetIndices(triangles, MeshTopology.Triangles, (int)index);
        }
    }
   
    private void LoadBlendShapes(IntPtr asset, long vertexCount)
    {
        UInt32 blendShapeCount = CAPI.ovrAvatarAsset_GetMeshBlendShapeCount(asset);
        IntPtr blendShapeVerts = CAPI.ovrAvatarAsset_GetMeshBlendShapeVertices(asset);

        AvatarLogger.Log("LoadBlendShapes: " + blendShapeCount);

        if (blendShapeVerts != IntPtr.Zero)
        {
            long offset = 0;
            long blendVertexSize = (long)Marshal.SizeOf(typeof(ovrAvatarBlendVertex));
            long blendVertexBufferStart = blendShapeVerts.ToInt64();

            for (UInt32 blendIndex = 0; blendIndex < blendShapeCount; blendIndex++)
            {
                Vector3[] blendVerts = new Vector3[vertexCount];
                Vector3[] blendNormals = new Vector3[vertexCount];
                Vector3[] blendTangents = new Vector3[vertexCount];

                for (long i = 0; i < vertexCount; i++)
                {
                    ovrAvatarBlendVertex vertex = (ovrAvatarBlendVertex)Marshal.PtrToStructure(new IntPtr(blendVertexBufferStart + offset), typeof(ovrAvatarBlendVertex));
                    blendVerts[i] = new Vector3(vertex.x, vertex.y, -vertex.z);
                    blendNormals[i] = new Vector3(vertex.nx, vertex.ny, -vertex.nz);
                    blendTangents[i] = new Vector4(vertex.tx, vertex.ty, -vertex.tz);

                    offset += blendVertexSize;
                }

                IntPtr namePtr = CAPI.ovrAvatarAsset_GetMeshBlendShapeName(asset, blendIndex);
                string name = Marshal.PtrToStringAnsi(namePtr);
                const float frameWeight = 100f;
                mesh.AddBlendShapeFrame(name, frameWeight, blendVerts, blendNormals, blendTangents);
            }
        }
    }

    private void SetSkinnedBindPose(IntPtr asset, ovrAvatarAssetType meshType)
    {
        switch (meshType)
        {
            case ovrAvatarAssetType.Mesh:
                skinnedBindPose = CAPI.ovrAvatarAsset_GetMeshData(asset).skinnedBindPose;
                break;
            case ovrAvatarAssetType.CombinedMesh:
                skinnedBindPose = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).skinnedBindPose;
                break;
            default:
                break;

        }
    }

    private void GetVertexAndIndexData(
        IntPtr asset,
        ovrAvatarAssetType meshType,
        out long vertexCount,
        out IntPtr vertexBuffer,
        out uint indexCount,
        out IntPtr indexBuffer)
    {
        vertexCount = 0;
        vertexBuffer = IntPtr.Zero;
        indexCount = 0;
        indexBuffer = IntPtr.Zero;

        switch (meshType)
        {
            case ovrAvatarAssetType.Mesh:
                vertexCount = CAPI.ovrAvatarAsset_GetMeshData(asset).vertexCount;
                vertexBuffer = CAPI.ovrAvatarAsset_GetMeshData(asset).vertexBuffer;
                indexCount = CAPI.ovrAvatarAsset_GetMeshData(asset).indexCount;
                indexBuffer = CAPI.ovrAvatarAsset_GetMeshData(asset).indexBuffer;
                break;
            case ovrAvatarAssetType.CombinedMesh:
                vertexCount = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).vertexCount;
                vertexBuffer = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).vertexBuffer;
                indexCount = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).indexCount;
                indexBuffer = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).indexBuffer;
                break;
            default:
                break;
        }
    }

    public SkinnedMeshRenderer CreateSkinnedMeshRendererOnObject(GameObject target)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = target.AddComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = mesh;
        mesh.name = "AvatarMesh_" + assetID;
        UInt32 jointCount = skinnedBindPose.jointCount;
        GameObject[] bones = new GameObject[jointCount];
        Transform[] boneTransforms = new Transform[jointCount];
        Matrix4x4[] bindPoses = new Matrix4x4[jointCount];
        for (UInt32 i = 0; i < jointCount; i++)
        {
            bones[i] = new GameObject();
            boneTransforms[i] = bones[i].transform;
            bones[i].name = jointNames[i];
            int parentIndex = skinnedBindPose.jointParents[i];
            if (parentIndex == -1)
            {
                bones[i].transform.parent = skinnedMeshRenderer.transform;
                skinnedMeshRenderer.rootBone = bones[i].transform;
            }
            else
            {
                bones[i].transform.parent = bones[parentIndex].transform;
            }

            // Set the position relative to the parent
            Vector3 position = skinnedBindPose.jointTransform[i].position;
            position.z = -position.z;
            bones[i].transform.localPosition = position;

            Quaternion orientation = skinnedBindPose.jointTransform[i].orientation;
            orientation.x = -orientation.x;
            orientation.y = -orientation.y;
            bones[i].transform.localRotation = orientation;

            bones[i].transform.localScale = skinnedBindPose.jointTransform[i].scale;

            bindPoses[i] = bones[i].transform.worldToLocalMatrix * skinnedMeshRenderer.transform.localToWorldMatrix;
        }
        skinnedMeshRenderer.bones = boneTransforms;
        mesh.bindposes = bindPoses;
        return skinnedMeshRenderer;
    }
}
