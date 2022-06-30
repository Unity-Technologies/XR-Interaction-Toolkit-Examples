//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace DigitalOpus.MB.Core{
public class MB_Utility{

    public static bool DO_INTEGRITY_CHECKS = false;

	public struct MeshAnalysisResult{
		public Rect uvRect;
		public bool hasOutOfBoundsUVs;
		public bool hasOverlappingSubmeshVerts;
		public bool hasOverlappingSubmeshTris;
        public bool hasUVs;
        public float submeshArea;
	}

	public static Texture2D createTextureCopy(Texture2D source){
		Texture2D newTex = new Texture2D(source.width,source.height,TextureFormat.ARGB32,true);
		newTex.SetPixels(source.GetPixels());
		return newTex;
	}
	
	public static bool ArrayBIsSubsetOfA(System.Object[] a, System.Object[] b){
		for (int i = 0; i < b.Length; i++){
			bool foundBinA = false;
			for (int j = 0; j < a.Length; j++){
				if (a[j] == b[i]){
						foundBinA = true;
					break;
				}
			}
			if (foundBinA == false) return false;
		}
		return true;
	}

	public static Material[] GetGOMaterials(GameObject go){
		if (go == null) return new Material[0];
		Material[] sharedMaterials = null;
		Mesh mesh = null;
		MeshRenderer mr = go.GetComponent<MeshRenderer>();
		if (mr != null){
			sharedMaterials = mr.sharedMaterials;
			MeshFilter mf = go.GetComponent<MeshFilter>();
			if (mf == null){
				throw new Exception("Object " + go + " has a MeshRenderer but no MeshFilter.");
			}
			mesh = mf.sharedMesh;
		}
		
		SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
		if (smr != null){
			sharedMaterials = smr.sharedMaterials;
			mesh = smr.sharedMesh;
		}
		
		if (sharedMaterials == null){
			Debug.LogError("Object " + go.name + " does not have a MeshRenderer or a SkinnedMeshRenderer component");
			return new Material[0];	
		} else if (mesh == null){
			Debug.LogError("Object " + go.name + " has a MeshRenderer or SkinnedMeshRenderer but no mesh.");
			return new Material[0];				
		} else {
			if (mesh.subMeshCount < sharedMaterials.Length){
				Debug.LogWarning("Object " + go + " has only " + mesh.subMeshCount + " submeshes and has " + sharedMaterials.Length + " materials. Extra materials do nothing.");	
				Material[] newSharedMaterials = new Material[mesh.subMeshCount];
				Array.Copy(sharedMaterials,newSharedMaterials,newSharedMaterials.Length);
				sharedMaterials = newSharedMaterials;
			}
			return sharedMaterials;
		}
	}
	
	public static Mesh GetMesh(GameObject go){
		if (go == null) return null;
		MeshFilter mf = go.GetComponent<MeshFilter>();
		if (mf != null){
			return mf.sharedMesh;
		}
		
		SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
		if (smr != null){
			return smr.sharedMesh;
		}
		
		return null;
	}
	
        public static void SetMesh(GameObject go, Mesh m)
        {
            if (go == null) return;
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = m;
            }
            else
            {
                SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.sharedMesh = m;
                }
            }
        }

	public static Renderer GetRenderer(GameObject go){
		if (go == null) return null;
		MeshRenderer mr = go.GetComponent<MeshRenderer>();
		if (mr != null) return mr; 
		
		
		SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
		if (smr != null) return smr;
		return null;		
	}
	
	public static void DisableRendererInSource(GameObject go){
		if (go == null) return;
		MeshRenderer mf = go.GetComponent<MeshRenderer>();
		if (mf != null){
			mf.enabled = false;
			return;
		}
		
		SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
		if (smr != null){
			smr.enabled = false;
			return;
		}			
	}
	
	public static bool hasOutOfBoundsUVs(Mesh m, ref Rect uvBounds){
		MeshAnalysisResult mar = new MeshAnalysisResult();
		bool outVal = hasOutOfBoundsUVs(m, ref mar);
        uvBounds = mar.uvRect;
        return outVal;
	}

    public static bool hasOutOfBoundsUVs(Mesh m, ref MeshAnalysisResult putResultHere, int submeshIndex = -1, int uvChannel = 0)
    {
        if (m == null)
        {
            putResultHere.hasOutOfBoundsUVs = false;
            return putResultHere.hasOutOfBoundsUVs;
        }
        Vector2[] uvs;
        if (uvChannel == 0) {
            uvs = m.uv;
        } else if (uvChannel == 1)
        {
            uvs = m.uv2;
        } else if (uvChannel == 2)
        {
            uvs = m.uv3;
		} else {

			uvs = m.uv4;
        }
        return hasOutOfBoundsUVs(uvs, m, ref putResultHere, submeshIndex);
    }

    public static bool hasOutOfBoundsUVs(Vector2[] uvs, Mesh m, ref MeshAnalysisResult putResultHere, int submeshIndex = -1)
    {
        putResultHere.hasUVs = true;
        if (uvs.Length == 0)
        {
            putResultHere.hasUVs = false;
            putResultHere.hasOutOfBoundsUVs = false;
            putResultHere.uvRect = new Rect();
            return putResultHere.hasOutOfBoundsUVs;
        }
        float minx, miny, maxx, maxy;
        if (submeshIndex >= m.subMeshCount)
        {
            putResultHere.hasOutOfBoundsUVs = false;
            putResultHere.uvRect = new Rect();
            return putResultHere.hasOutOfBoundsUVs;
        }
        else if (submeshIndex >= 0)
        {
            //checking specific submesh
            int[] tris = m.GetTriangles(submeshIndex);
            if (tris.Length == 0)
            {
                putResultHere.hasOutOfBoundsUVs = false;
                putResultHere.uvRect = new Rect();
                return putResultHere.hasOutOfBoundsUVs;
            }
            minx = maxx = uvs[tris[0]].x;
            miny = maxy = uvs[tris[0]].y;
            for (int idx = 0; idx < tris.Length; idx++)
            {
                int i = tris[idx];
                if (uvs[i].x < minx) minx = uvs[i].x;
                if (uvs[i].x > maxx) maxx = uvs[i].x;
                if (uvs[i].y < miny) miny = uvs[i].y;
                if (uvs[i].y > maxy) maxy = uvs[i].y;
            }
        }
        else {
            //checking all UVs
            minx = maxx = uvs[0].x;
            miny = maxy = uvs[0].y;
            for (int i = 0; i < uvs.Length; i++)
            {
                if (uvs[i].x < minx) minx = uvs[i].x;
                if (uvs[i].x > maxx) maxx = uvs[i].x;
                if (uvs[i].y < miny) miny = uvs[i].y;
                if (uvs[i].y > maxy) maxy = uvs[i].y;
            }
        }
        Rect uvBounds = new Rect();
        uvBounds.x = minx;
        uvBounds.y = miny;
        uvBounds.width = maxx - minx;
        uvBounds.height = maxy - miny;
        if (maxx > 1f || minx < 0f || maxy > 1f || miny < 0f)
        {
            putResultHere.hasOutOfBoundsUVs = true;
        }
        else
        {
            putResultHere.hasOutOfBoundsUVs = false;
        }
        putResultHere.uvRect = uvBounds;
        return putResultHere.hasOutOfBoundsUVs;
    }

    public static void setSolidColor(Texture2D t, Color c)
    {
        Color[] cs = t.GetPixels();
        for (int i = 0; i < cs.Length; i++)
        {
            cs[i] = c;
        }
        t.SetPixels(cs);
        t.Apply();
    }
	
	public static Texture2D resampleTexture(Texture2D source, int newWidth, int newHeight){
		TextureFormat f = source.format;
		if (f == TextureFormat.ARGB32 ||
			f == TextureFormat.RGBA32 ||
			f == TextureFormat.BGRA32 ||
			f == TextureFormat.RGB24  ||
			f == TextureFormat.Alpha8 ||
			f == TextureFormat.DXT1)
		{
			Texture2D newTex = new Texture2D(newWidth,newHeight,TextureFormat.ARGB32,true);
			float w = newWidth;
			float h = newHeight;
			for (int i = 0; i < newWidth; i++){
				for (int j = 0; j < newHeight; j++){
					float u = i/w;
					float v = j/h;
					newTex.SetPixel(i,j,source.GetPixelBilinear(u,v));
				}
			}
			newTex.Apply(); 		
			return newTex;
		} else {
			Debug.LogError("Can only resize textures in formats ARGB32, RGBA32, BGRA32, RGB24, Alpha8 or DXT. texture:" + source + " was in format: " + source.format);	
			return null;
		}
	}

	class MB_Triangle{
		int submeshIdx;
		int[] vs = new int[3];
		
		public bool isSame(object obj){
			MB_Triangle tobj = (MB_Triangle) obj;
			if (vs[0] == tobj.vs[0] &&
				vs[1] == tobj.vs[1] &&
				vs[2] == tobj.vs[2] &&
				submeshIdx != tobj.submeshIdx){
				return true;	
			}
			return false;
		}
		
		public bool sharesVerts(MB_Triangle obj){
			if (vs[0] == obj.vs[0] ||
				vs[0] == obj.vs[1] ||
				vs[0] == obj.vs[2]){
				if (submeshIdx != obj.submeshIdx) return true;	
			}
			if (vs[1] == obj.vs[0] ||
				vs[1] == obj.vs[1] ||
				vs[1] == obj.vs[2]){
				if (submeshIdx != obj.submeshIdx) return true;
			}	
			if (vs[2] == obj.vs[0] ||
				vs[2] == obj.vs[1] ||
				vs[2] == obj.vs[2]){
				if (submeshIdx != obj.submeshIdx) return true;	
			}
			return false;			
		}
		
		public void Initialize(int[] ts, int idx, int sIdx){
			vs[0] = ts[idx];
			vs[1] = ts[idx + 1];
			vs[2] = ts[idx + 2];
			submeshIdx = sIdx;
			Array.Sort(vs);
		}
	}
	
	public static bool AreAllSharedMaterialsDistinct(Material[] sharedMaterials){
		for (int i = 0; i < sharedMaterials.Length; i++){
			for (int j = i + 1; j < sharedMaterials.Length; j++){
				if (sharedMaterials[i] == sharedMaterials[j]){
					return false;
				}
			}
		}
		return true;
	}
	
	public static int doSubmeshesShareVertsOrTris(Mesh m, ref MeshAnalysisResult mar){
		MB_Triangle consider = new MB_Triangle();
		MB_Triangle other = new MB_Triangle();
		//cache all triangles
		int[][] tris = new int[m.subMeshCount][];
		for (int i = 0; i < m.subMeshCount; i++){
			tris[i] = m.GetTriangles(i);
		}
		bool sharesVerts = false;
		bool sharesTris = false;
		for (int i = 0; i < m.subMeshCount; i++){
			int[] smA = tris[i];
			for (int j = i+1; j < m.subMeshCount; j++){
				int[] smB = tris[j];
				for (int k = 0; k < smA.Length; k+=3){
					consider.Initialize(smA,k,i);
					for (int l = 0; l < smB.Length; l+=3){
						other.Initialize(smB,l,j);
						if (consider.isSame(other)){
							sharesTris = true;
							break;
						}
						if (consider.sharesVerts(other)){
							sharesVerts = true;
							break;
						}					
					}
				}
			}
		}
		if (sharesTris){
				mar.hasOverlappingSubmeshVerts = true;
				mar.hasOverlappingSubmeshTris = true;
				return 2;
		} else if (sharesVerts){
				mar.hasOverlappingSubmeshVerts = true;
				mar.hasOverlappingSubmeshTris = false;
				return 1;
		} else {
				mar.hasOverlappingSubmeshTris = false;
				mar.hasOverlappingSubmeshVerts = false;
				return 0;
		}
	}	
	
	public static bool GetBounds(GameObject go, out Bounds b){
		if (go == null){
			Debug.LogError("go paramater was null");
			b = new Bounds(Vector3.zero,Vector3.zero);
			return false;				
		}
		Renderer r = GetRenderer(go);
		if (r == null){
			Debug.LogError("GetBounds must be called on an object with a Renderer");
			b = new Bounds(Vector3.zero,Vector3.zero);
			return false;
		}
		if (r is MeshRenderer){
			b = r.bounds;
			return true;
		} else if (r is SkinnedMeshRenderer){
			b = r.bounds;
			return true;
		}
		Debug.LogError("GetBounds must be called on an object with a MeshRender or a SkinnedMeshRenderer.");
		b = new Bounds(Vector3.zero,Vector3.zero);
		return false;		
	}
			
	public static void Destroy(UnityEngine.Object o){
		if (Application.isPlaying){
			MonoBehaviour.Destroy(o);
		} else {
//			string p = AssetDatabase.GetAssetPath(o);
//			if (p != null && p.Equals("")) // don't try to destroy assets
				MonoBehaviour.DestroyImmediate(o,false);
		}
	}

        public static string ConvertAssetsRelativePathToFullSystemPath(string pth)
        {
            string aPth = Application.dataPath.Replace("Assets", "");
            return aPth + pth;
        }

		public static bool IsSceneInstance(GameObject go)
		{
			// go.scene.name 
			//       - is the name of the scene if in a scene
			//       - is the name of the prefab if in prefab edit scene
			//       - is null if is a prefab assigned from the project folder
			return go.scene.name != null;
		}
	}
}
