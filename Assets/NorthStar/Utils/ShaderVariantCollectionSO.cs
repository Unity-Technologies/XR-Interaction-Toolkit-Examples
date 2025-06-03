// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Meta.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace NorthStar
{
    /// <summary>
    /// Stores shader variant information that is normally write-only in a standard ShaderVariantCollection. Used for warming up PSO's during loading to prevent hitching during gameplay
    /// </summary>
    [CreateAssetMenu(menuName = "Data/ShaderVariantCollection")]
    public class ShaderVariantCollectionSO : ScriptableObject
    {
        [Serializable]
        public class ShaderData
        {
            public string Name;
            public string Pass;
            public PassType PassType;
            public string[] Keywords;

            [NonSerialized] private Material m_material;
            [NonSerialized] private int m_passIndex = -1;

            private void RequireMaterialAndPass()
            {
                if (m_material == null)
                {
                    m_material = new(Shader.Find(Name)) { shaderKeywords = Keywords, };
                    m_passIndex = m_material.FindPass(Pass);
                }
            }

            public Material Material
            {
                get
                {
                    RequireMaterialAndPass();
                    return m_material;
                }
            }

            public int PassIndex
            {
                get
                {
                    RequireMaterialAndPass();
                    return m_passIndex;
                }
            }
        }

        public List<ShaderData> Shaders = new();

        [Serializable]
        public class VertexAttributeSet
        {
            [Serializable]
            public struct VertexAttributeSO
            {
                public VertexAttribute Attribute;
                public VertexAttributeFormat Format;
                public int Dimension;
                public int Stream;
                public VertexAttributeSO(VertexAttributeDescriptor attr)
                {
                    Attribute = attr.attribute;
                    Format = attr.format;
                    Dimension = attr.dimension;
                    Stream = attr.stream;
                }
                public static implicit operator VertexAttributeDescriptor(VertexAttributeSO attr)
                    => new(attr.Attribute, attr.Format, attr.Dimension, attr.Stream);
            }
            public IndexFormat IndexFormat;
            public VertexAttributeSO[] Attributes;

            private Mesh m_dummyMesh;
            public Mesh GetDummyMesh()
            {
                if (m_dummyMesh == null) m_dummyMesh = new() { name = "Dummy Mesh" };
                var attrs = new VertexAttributeDescriptor[Attributes.Length];
                for (var i = 0; i < attrs.Length; i++)
                    attrs[i] = Attributes[i];
                m_dummyMesh.SetVertexBufferParams(3, attrs);
                m_dummyMesh.SetIndexBufferParams(3, IndexFormat);
                m_dummyMesh.subMeshCount = 1;
                m_dummyMesh.SetVertices(new Vector3[] { default, default, default });
                m_dummyMesh.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
                m_dummyMesh.bounds = new(default, new(99999f, 99999f, 99999f));
                return m_dummyMesh;
            }
        }

        [Serializable]
        public class ShaderAttributesSet
        {
            public Shader Shader;
            public VertexAttributeSet[] AttributeSets;
        }

        public ShaderAttributesSet[] ShaderAttributes;
        private Dictionary<Shader, VertexAttributeSet[]> m_attributesByShader;
        public VertexAttributeSet[] GetAttributeSetsForShader(Shader shader)
        {
            if (m_attributesByShader == null)
            {
                m_attributesByShader = new();
                foreach (var kv in ShaderAttributes)
                {
                    m_attributesByShader[kv.Shader] = kv.AttributeSets;
                }
            }
            return m_attributesByShader.TryGetValue(shader, out var sets) ? sets : null;
        }

        // Add STEREO_MULTIVIEW_ON where supported. Add motion vector. Remove duplicates.
        [ContextMenu("Preprocess")]
        public void FilterList()
        {
            var newShaders = new List<ShaderData>();
            foreach (var variant in Shaders)
            {
                var expKw = variant.Keywords.IndexOf("FOG_EXP") ?? -1;
                if (expKw >= 0) variant.Keywords[expKw] = "FOG_EXP2";
                var material = variant.Material;
                var shader = material.shader;
                if (!variant.Keywords.Contains("STEREO_MULTIVIEW_ON"))
                {
                    if (shader.keywordSpace.keywordNames.Contains("STEREO_MULTIVIEW_ON"))
                    {
                        variant.Keywords = variant.Keywords.Append("STEREO_MULTIVIEW_ON").ToArray();
                        Debug.Log($"Adding STEREO_MULTIVIEW_ON to {variant.Name}");
                    }
                }
                if (variant.Pass != "MotionVectors")
                {
                    if (material.renderQueue <= 2500 && material.FindPass("MotionVectors") >= 0)
                    {
                        newShaders.Add(new ShaderData()
                        {
                            Name = variant.Name,
                            Pass = "MotionVectors",
                            PassType = PassType.MotionVectors,
                            Keywords = (string[])variant.Keywords.Clone(),
                        });
                    }
                }
                else
                {
                    if (material.renderQueue > 2500)
                    {
                        Debug.LogError($"Invalid renderqueue {material.renderQueue} for motion vector pass {variant.Name}");
                    }
                }
            }

            var oldCount = Shaders.Count;
            Shaders = Shaders.Concat(newShaders).ToList();
            Shaders = Shaders
                .Distinct(new ShaderComparer())
                .ToList();
            if (Shaders.Count != oldCount)
            {
                Debug.Log($"Shader count changed from {oldCount} to {Shaders.Count}");
            }
        }

        private class ShaderComparer : IEqualityComparer<ShaderData>
        {
            public bool Equals(ShaderData x, ShaderData y)
            {
                if (x.Name != y.Name) return false;
                if (x.Pass != y.Pass) return false;
                var sortedX = x.Keywords.OrderBy(k => k);
                var sortedY = y.Keywords.OrderBy(k => k);
                return sortedX.SequenceEqual(sortedY);
            }
            public int GetHashCode(ShaderData obj)
            {
                var hashcode = HashCode.Combine(obj.Name, obj.Pass);
                foreach (var keyword in obj.Keywords.OrderBy(k => k))
                {
                    hashcode = HashCode.Combine(hashcode, keyword);
                }
                return hashcode;
            }
        }

        private struct MeshDescriptor : IEquatable<MeshDescriptor>
        {
            public IndexFormat IndexFormat;
            public VertexAttributeDescriptor[] VertexAttributes;
            public bool Equals(MeshDescriptor other)
            {
                return IndexFormat == other.IndexFormat && VertexAttributes.SequenceEqual(other.VertexAttributes);
            }
            public override int GetHashCode()
            {
                var hashCode = IndexFormat.GetHashCode();
                foreach (var attr in VertexAttributes)
                    hashCode = HashCode.Combine(hashCode, attr);
                return hashCode;
            }
            public override string ToString() => $"{IndexFormat} +{VertexAttributes.Length}";
        }

        // Open all game scenes (drag them into the Hierarchy) before running this
        // it will collect any loaded MeshFilters and use them to determine which
        // vertex layouts are required for each shader
        [ContextMenu("Catalogue Mesh Attributes")]
        public void CatalogueMeshAttributes()
        {
            // Find all currently loaded MeshFilters
            var allMeshFilters = FindObjectsOfType<MeshFilter>(true);
            var allVertDescriptors = new Dictionary<MeshDescriptor, (int Count, HashSet<Mesh> Meshes, HashSet<Shader> Shaders, HashSet<string> Names)>();
            foreach (var meshFilter in allMeshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;
                var renderer = meshFilter.GetComponent<Renderer>();
                if (renderer == null) continue;

                var attributes = new VertexAttributeDescriptor[mesh.vertexAttributeCount];
                for (var i = 0; i < mesh.vertexAttributeCount; i++)
                    attributes[i] = mesh.GetVertexAttribute(i);

                var indexFormat = mesh.indexFormat;
                var descriptor = new MeshDescriptor() { IndexFormat = indexFormat, VertexAttributes = attributes };
                if (!allVertDescriptors.TryGetValue(descriptor, out var sets))
                    sets = new(0, new(), new(), new());

                sets.Count++;
                _ = sets.Meshes.Add(mesh);
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null) _ = sets.Shaders.Add(material.shader);
                }
                _ = sets.Names.Add(meshFilter.name);

                allVertDescriptors[descriptor] = sets;
            }

            // Collect all vertex layouts used with each shader
            var shaderSets = new Dictionary<Shader, List<VertexAttributeSet>>();
            foreach (var desc in allVertDescriptors)
            {
                var attrSet = new VertexAttributeSet()
                {
                    IndexFormat = desc.Key.IndexFormat,
                    Attributes = new VertexAttributeSet.VertexAttributeSO[desc.Key.VertexAttributes.Length],
                };

                for (var i = 0; i < desc.Key.VertexAttributes.Length; i++)
                    attrSet.Attributes[i] = new(desc.Key.VertexAttributes[i]);

                foreach (var shader in desc.Value.Shaders)
                {
                    if (!shaderSets.TryGetValue(shader, out var attrSets)) shaderSets[shader] = attrSets = new();
                    attrSets.Add(attrSet);
                }
            }

            // Shaders that are not in the Shader Collection wont be preloaded
            // remove their reference from here
            var shadersSeen = Shaders.Select(s => s.Material.shader).Distinct().ToHashSet();

            var invalidShaders = shaderSets
                .Where(kv => !shadersSeen.Contains(kv.Key))
                .Select(kv => kv.Key.name)
                .Aggregate((k1, k2) => $"{k1}, {k2}");
            if (!string.IsNullOrEmpty(invalidShaders)) Debug.LogError($"Unreferenced shaders: {invalidShaders}\nThese were found in the world but are not present in the Shader Collection");

            // Store this in ourselves so that the preloader can access it
            ShaderAttributes = shaderSets
                .Where(kv => shadersSeen.Contains(kv.Key))
                .Select(kv => new ShaderAttributesSet() { Shader = kv.Key, AttributeSets = kv.Value.ToArray() })
                .ToArray();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif

            // Breakpoint and inspect this for more information on which
            // vertex layouts are used by which meshes/shaders/objects
            var sortedSet = allVertDescriptors
                .OrderByDescending(kv => kv.Value.Count)
                .Select(kv => (kv.Value.Count,
                    Descriptor: kv.Key,
                    Meshes: kv.Value.Meshes.ToArray(),
                    Shaders: kv.Value.Shaders.ToArray(),
                    GONames: kv.Value.Names.ToArray()
                ))
                .ToArray();

            // Dump a summary to the console (just names)
            StringBuilder debugString = new();
            foreach (var item in sortedSet)
            {
                debugString.AppendLine($"x{item.Count}: {item.Descriptor} :: {item.Meshes.Length} meshes, {item.Shaders.Length} shaders");
                void PrintList<T>(T[] items, Func<T, string> getName)
                {
                    debugString.Append("  ");
                    foreach (var item in items.Take(10)) debugString.Append(getName(item) + ", ");
                    if (items.Length > 10) debugString.Append("...");
                    debugString.AppendLine();
                }
                PrintList(item.Meshes, static (mesh) => mesh.name);
                PrintList(item.Shaders, static (shader) => shader.name);
                PrintList(item.GONames, static (name) => name);
            }
            Debug.Log($"Found {allVertDescriptors.Count}:\n {debugString}");
        }
    }
}
