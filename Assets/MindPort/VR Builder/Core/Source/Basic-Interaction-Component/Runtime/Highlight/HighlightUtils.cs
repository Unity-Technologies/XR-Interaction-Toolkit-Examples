using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRBuilder.BasicInteraction
{
    internal static class HighlightUtils
    {
        /// <summary>
        /// Creates a basic highlight material with given color, does support alpha values.
        /// </summary>
        public static Material CreateDefaultHighlightMaterial(Color highlightColor)
        {
            Shader shader = GetDefaultHighlightShader();
            Material material = new Material(shader);
            material.color = highlightColor;

            // In case the color has some level of transparency,
            // we set the Material's Rendering Mode to Transparent. 
            if (Mathf.Approximately(highlightColor.a, 1f) == false)
            {
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            return material;
        }
        
        /// <summary>
        /// Creates the default highlight shader which will be used.
        /// </summary>
        public static Shader GetDefaultHighlightShader()
        {
            string shaderName = GraphicsSettings.currentRenderPipeline ? "Universal Render Pipeline/Lit" : "Standard";
            Shader defaultShader = Shader.Find(shaderName);

            if (defaultShader == null)
            {
                throw new NullReferenceException($"Failed to create a default material," + 
                                                 $" shader \"{shaderName}\" was not found. Make sure the shader is included into the game build.");
            }

            return defaultShader;
        }
        
        /// <summary>
        /// Crawls the given GameObject child tree and extracts all eligible renderer. 
        /// </summary>
        public static Renderer[] FindAllIncludedRenderer(GameObject target)
        {
            return target.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Concat<Renderer>(target.GetComponentsInChildren<MeshRenderer>()
                .Where(renderer => renderer.gameObject.activeInHierarchy 
                                   && renderer.enabled
                                   && renderer.gameObject.GetComponent<IExcludeFromHighlightMesh>() == null))
                .ToArray();
        }
        
        /// <summary>
        /// Combines the mesh from all given renderer.
        /// </summary>
        /// <exception cref="InvalidOperationException">Will be thrown when there is any static renderer included.</exception>
        /// <exception cref="NullReferenceException">Will be thrown when there is no mesh at all.</exception>
        public static Mesh GeneratePreviewMesh(GameObject position, Renderer[] renderers)
        {
            Transform transform = position.transform;
            List<CombineInstance> meshes = new List<CombineInstance>();

            Vector3 cachedPosition = transform.position;
            Quaternion cachedRotation = transform.rotation;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            try
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.isPartOfStaticBatch)
                    {
                        throw new InvalidOperationException($"{position.name} is marked as 'Batching Static', no preview mesh to be highlighted could be generated at runtime.\n" +
                            "In order to fix this issue, please either remove the static flag of this GameObject or simply " +
                            "select it in edit mode so a preview mesh could be generated and cached.");
                    }
                    
                    Type renderType = renderer.GetType();

                    if (renderType == typeof(MeshRenderer))
                    {
                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                        
                        if (meshFilter.sharedMesh == null)
                        {
                            continue;
                        }
                        
                        meshes.AddRange(CollectMeshInformationFromMeshFilter(meshFilter));
                    }
                    else if (renderType == typeof(SkinnedMeshRenderer))
                    {
                        SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;

                        if (skinnedMeshRenderer.sharedMesh == null)
                        {
                            continue;
                        }

                        meshes.AddRange(CollectMeshInformationFromSkinnedMeshRenderer(skinnedMeshRenderer));
                    }
                }
            }
            finally
            {
                transform.SetPositionAndRotation(cachedPosition, cachedRotation);
            }
            
            if (meshes.Any())
            {
                Mesh previewMesh = new Mesh();
                previewMesh.CombineMeshes(meshes.ToArray());
                
                return previewMesh;
            }
            throw new NullReferenceException($"{position.name} has no valid meshes to be highlighted.");
        }
        
        private static IEnumerable<CombineInstance> CollectMeshInformationFromMeshFilter(MeshFilter meshFilter)
        {
            List<CombineInstance> combinedInstances = new List<CombineInstance>();

            for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                CombineInstance combineInstance = new CombineInstance
                {
                    subMeshIndex = i,
                    mesh = meshFilter.sharedMesh,
                    transform = meshFilter.transform.localToWorldMatrix
                };

                combinedInstances.Add(combineInstance);
            }

            return combinedInstances;
        }
        
        private static IEnumerable<CombineInstance> CollectMeshInformationFromSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            List<CombineInstance> combinedInstances = new List<CombineInstance>();

            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.subMeshCount; i++)
            {
                CombineInstance combineInstance = new CombineInstance
                {
                    subMeshIndex = i,
                    mesh = skinnedMeshRenderer.sharedMesh,
                    transform = skinnedMeshRenderer.transform.localToWorldMatrix
                };

                combinedInstances.Add(combineInstance);
            }

            return combinedInstances;
        }
    }
}