using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public interface IGroupByFilter
    {

        /// <summary>
        /// 	this name appears in the dropdown list.
        /// </summary>
        /// <returns>The name.</returns>
        string GetName();

        /// <summary>
        /// 	returns a description of the game object for this filter
        ///     eg. renderType=MeshFilter
        /// </summary>
        /// <returns>The description.</returns>
        /// <param name="fi">Fi.</param>
        string GetDescription(GameObjectFilterInfo fi);

        /// <summary>
        ///      For sorting Similar to IComparer.Compare
        /// </summary>
        int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b);
    }

    [Serializable]
    public class GameObjectFilterInfo : IComparable
    {

        public enum StandardShaderBlendMode
        {
            NotApplicable = -1,
            Opaque = 0,
            Cutout = 1,
            Fade = 2,
            Transparent = 3,
        }

        public enum URPBlendMode // called SurfaceType in URP
        {
            Opaque = 0,
            Transparent = 1
        }

        public enum HDRPBlendMode // called SurfaceType in HDRP
        {
            Opaque = 0,
            Transparent = 1
        }

        public int[] URPBlendMode_2_StandardBlendMode = new int[]
        {
            (int) StandardShaderBlendMode.Opaque, 
            (int) StandardShaderBlendMode.Transparent
        };

        public int[] HDRPBlendMode_2_StandardBlendMode = new int[]
        {
            (int) StandardShaderBlendMode.Opaque,
            (int) StandardShaderBlendMode.Transparent
        };


        public GameObject go;
        public string shaderName = "";
        public Shader[] shaders = null;  //distinct set of shaders used


        public string materialName = "";
        public Material[] materials = null; //disinct set of materials used
        public string standardShaderBlendModesName = "";
        public StandardShaderBlendMode[] standardShaderBlendModes = null;
        public bool outOfBoundsUVs = false;
        public bool submeshesOverlap = false;
        public bool alreadyInBakerList = false;
        public int numMaterials = 1;
        public int lightmapIndex = -1;
        public int numVerts = 0;
        public bool isStatic = false;
        public bool isMeshRenderer = true;
        public string warning;
        public short atlasIndex = 0;

        IGroupByFilter[] filters;


        public int CompareTo(System.Object obj)
        {
            if (obj is GameObjectFilterInfo)
            {
                GameObjectFilterInfo gobj = (GameObjectFilterInfo)obj;
                int gb;

                for (int i = 0; i < filters.Length; i++)
                {
                    if (filters[i] != null)
                    {
                        gb = filters[i].Compare(this, gobj);
                        if (gb != 0) return gb;
                    }
                }
            }
            return 0;
        }

        public GameObjectFilterInfo(GameObject g, HashSet<GameObject> objsAlreadyInBakers, IGroupByFilter[] filts)
        {
            go = g;
            Renderer r = MB_Utility.GetRenderer(g);
            //material = r.sharedMaterial;
            //if (material != null) shader = material.shader;
            HashSet<Material> matsSet = new HashSet<Material>();
            HashSet<Shader> shaderSet = new HashSet<Shader>();
            if (r is SkinnedMeshRenderer) isMeshRenderer = false;
            else isMeshRenderer = true;
            materials = r.sharedMaterials;
            //shaders = new Shader[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    matsSet.Add(materials[i]);
                    shaderSet.Add(materials[i].shader);
                }
            }
            materials = new Material[matsSet.Count];
            matsSet.CopyTo(materials);
            shaders = new Shader[shaderSet.Count];
            standardShaderBlendModes = new StandardShaderBlendMode[matsSet.Count];

            shaderSet.CopyTo(shaders);

            Array.Sort(materials, new NameComparer());
            Array.Sort(shaders, new NameComparer());

            List<string> standardShaderBlendModesNameSet = new List<string>();
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].shader.name.StartsWith("Standard") && materials[i].HasProperty("_Mode"))
                {
                    standardShaderBlendModes[i] = (StandardShaderBlendMode)materials[i].GetFloat("_Mode");
                } else if (materials[i].shader.name.StartsWith("Universal Render Pipeline") && materials[i].HasProperty("_Surface"))
                {
                    int surfaceMode = (int) materials[i].GetFloat("_Surface");
                    if (surfaceMode < 0 || surfaceMode > (int) URPBlendMode.Transparent)
                    {
                        Debug.LogError("Unsupported surface mode, were more surface modes added to the URP?");
                        surfaceMode = Mathf.Clamp(surfaceMode, (int)URPBlendMode.Opaque, (int)URPBlendMode.Transparent);
                    }

                    standardShaderBlendModes[i] = (StandardShaderBlendMode) URPBlendMode_2_StandardBlendMode[surfaceMode];
                } else if (materials[i].shader.name.StartsWith("HDRP") && materials[i].HasProperty("_SurfaceType"))
                {
                    int surfaceMode = (int)materials[i].GetFloat("_SurfaceType");
                    if (surfaceMode < 0 || surfaceMode > (int)HDRPBlendMode.Transparent)
                    {
                        Debug.LogError("Unsupported surface mode, were more surface modes added to the HDRP?");
                        surfaceMode = Mathf.Clamp(surfaceMode, (int)HDRPBlendMode.Opaque, (int)HDRPBlendMode.Transparent);
                    }

                    standardShaderBlendModes[i] = (StandardShaderBlendMode)URPBlendMode_2_StandardBlendMode[surfaceMode];
                }
                else
                {
                    standardShaderBlendModes[i] = StandardShaderBlendMode.NotApplicable;
                }
                if (!standardShaderBlendModesNameSet.Contains(standardShaderBlendModes[i].ToString())) standardShaderBlendModesNameSet.Add(standardShaderBlendModes[i].ToString());
                materialName += (materials[i] == null ? "null" : materials[i].name);
                if (i < materials.Length - 1) materialName += ",";
            }

            standardShaderBlendModesName = "";
            standardShaderBlendModesNameSet.Sort();
            foreach (string modeName in standardShaderBlendModesNameSet)
            {
                standardShaderBlendModesName += modeName + ",";
            }
            for (int i = 0; i < shaders.Length; i++)
            {
                shaderName += (shaders[i] == null ? "null" : shaders[i].name);
                if (i < shaders.Length - 1) shaderName += ",";
            }

            lightmapIndex = r.lightmapIndex;
            Mesh mesh = MB_Utility.GetMesh(g);
            numVerts = 0;
            if (mesh != null)
            {
                numVerts = mesh.vertexCount;
            }
            isStatic = go.isStatic;
            numMaterials = materials.Length;
            warning = "";
            alreadyInBakerList = objsAlreadyInBakers.Contains(g);
            outOfBoundsUVs = false;
            submeshesOverlap = false;
            filters = filts;
        }

        public string GetDescription(IGroupByFilter[] groupBy, GameObjectFilterInfo fi)
        {
            string desc = "";
            if (groupBy != null)
            {
                for (int i = 0; i < groupBy.Length; i++)
                {
                    desc += groupBy[i].GetDescription(fi) + " ";
                }
                return desc;
            }
            else
            {
                return "todo";
            }
        }

        private class NameComparer : Comparer<UnityEngine.Object>
        {
            public override int Compare(UnityEngine.Object a, UnityEngine.Object b)
            {
                return a.name.CompareTo(b.name);
            }
        }
    }
}



