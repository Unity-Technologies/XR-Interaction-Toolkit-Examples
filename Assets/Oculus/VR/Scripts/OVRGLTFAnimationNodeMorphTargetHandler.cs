/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;

public class OVRGLTFAnimationNodeMorphTargetHandler
{
    public OVRMeshData MeshData { get; private set; }
    public float[] Weights;

    private bool modified = false;

    public OVRGLTFAnimationNodeMorphTargetHandler(OVRMeshData meshData)
    {
        this.MeshData = meshData;
    }

    public void Update()
    {
        if (!modified)
        {
            return;
        }

        var meshDataBase = new OVRMeshAttributes();
        for (var i = 0; i < MeshData.morphTargets.Length; i++)
        {
            if (MeshData.morphTargets[i].vertices != null)
            {
                if (meshDataBase.vertices == null)
                {
                    meshDataBase.vertices = new Vector3[MeshData.baseAttributes.vertices.Length];
                    Array.Copy(MeshData.baseAttributes.vertices, meshDataBase.vertices,
                        MeshData.baseAttributes.vertices.Length);
                }

                var vi = i / 2;
                if (i % 2 == 0)
                {
                    var morphedData = MeshData.morphTargets[i].vertices[vi].x *
                                      Weights[i];
                    meshDataBase.vertices[vi].x += morphedData;
                }
                else
                {
                    var morphedData = MeshData.morphTargets[i].vertices[vi].y *
                                      Weights[i];
                    meshDataBase.vertices[vi].y += morphedData;
                }
            }

            if (MeshData.morphTargets[i].texcoords != null)
            {
                if (meshDataBase.texcoords == null)
                {
                    meshDataBase.texcoords = new Vector2[MeshData.baseAttributes.texcoords.Length];
                    Array.Copy(MeshData.baseAttributes.texcoords, meshDataBase.texcoords,
                        MeshData.baseAttributes.texcoords.Length);
                }


                var ti = i - 8;
                var tii = ti / 2;
                if (i % 2 == 0)
                {
                    meshDataBase.texcoords[tii].x += MeshData.morphTargets[i].texcoords[tii].x *
                                                     Weights[i];
                }
                else
                {
                    meshDataBase.texcoords[tii].y += MeshData.morphTargets[i].texcoords[tii].y *
                                                     Weights[i];
                }
            }
        }

        if (meshDataBase.vertices != null)
        {
            MeshData.mesh.vertices = meshDataBase.vertices;
            MeshData.mesh.RecalculateBounds();
        }

        if (meshDataBase.texcoords != null)
        {
            MeshData.mesh.uv = meshDataBase.texcoords;
        }

        MeshData.mesh.MarkModified();
        modified = false;
    }

    public void MarkModified()
    {
        modified = true;
    }
}
