using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using DigitalOpus.MB.Core;

public class MB_TextureCombinerRenderTexture{
	public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;
	Material mat; //container for the shader that we will use to render the texture
	RenderTexture _destinationTexture;
	Camera myCamera;
	int _padding;
	bool _isNormalMap;
    bool _fixOutOfBoundsUVs;
    //bool _considerNonTextureProperties;

	//only want to render once, not every frame
	bool _doRenderAtlas = false;

	Rect[] rs;
	List<MB_TexSet> textureSets;
	int indexOfTexSetToRender;
    ShaderTextureProperty _texPropertyName;
    MB3_TextureCombinerNonTextureProperties _resultMaterialTextureBlender;
	Texture2D targTex;

    public Texture2D DoRenderAtlas(GameObject gameObject, int width, int height, int padding, Rect[] rss, List<MB_TexSet> textureSetss, int indexOfTexSetToRenders, ShaderTextureProperty texPropertyname, MB3_TextureCombinerNonTextureProperties resultMaterialTextureBlender,  bool isNormalMap, bool fixOutOfBoundsUVs, bool considerNonTextureProperties, MB3_TextureCombiner texCombiner, MB2_LogLevel LOG_LEV){
		LOG_LEVEL = LOG_LEV;
		textureSets = textureSetss;
		indexOfTexSetToRender = indexOfTexSetToRenders;
        _texPropertyName = texPropertyname;
		_padding = padding;
		_isNormalMap = isNormalMap;
        _fixOutOfBoundsUVs = fixOutOfBoundsUVs;
        //_considerNonTextureProperties = considerNonTextureProperties;
        _resultMaterialTextureBlender = resultMaterialTextureBlender;
		rs = rss;
		Shader s;
		if (_isNormalMap){
			s = Shader.Find ("MeshBaker/NormalMapShader");
		} else {
			s = Shader.Find ("MeshBaker/AlbedoShader");
		}
		if (s == null){
			Debug.LogError ("Could not find shader for RenderTexture. Try reimporting mesh baker");
			return null;
		}
		mat = new Material(s);
		_destinationTexture = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
		_destinationTexture.filterMode = FilterMode.Point;
		
		myCamera = gameObject.GetComponent<Camera>();
		myCamera.orthographic = true;
		myCamera.orthographicSize = height >> 1;
		myCamera.aspect = ((float) width) / height;
		myCamera.targetTexture = _destinationTexture;
		myCamera.clearFlags = CameraClearFlags.Color;
		
		Transform camTransform = myCamera.GetComponent<Transform>();
		camTransform.localPosition = new Vector3(width/2.0f, height/2f, 3);
		camTransform.localRotation = Quaternion.Euler(0, 180, 180);
		
		_doRenderAtlas = true;
		if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log(string.Format ("Begin Camera.Render destTex w={0} h={1} camPos={2} camSize={3} camAspect={4}", width, height, camTransform.localPosition, myCamera.orthographicSize, myCamera.aspect.ToString("f5")));
        //This triggers the OnRenderObject callback
		myCamera.Render();
		_doRenderAtlas = false;
		
		MB_Utility.Destroy(mat);
		MB_Utility.Destroy(_destinationTexture);
		
		if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log ("Finished Camera.Render ");

		Texture2D tempTex = targTex;
		targTex = null;
		if (tempTex == null) Debug.LogError(" Generated atlas was null. This can happen when using HDRP. Try using the Texture Packer 'Mesh Baker Texture Packer Fast V2' ");
		return tempTex;
	}
	
	public void OnRenderObject(){
		if (_doRenderAtlas){
			//assett rs must be same length as textureSets;
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start ();
            bool yIsFlipped = YisFlipped(LOG_LEVEL);
            for (int i = 0; i < rs.Length; i++){
				MeshBakerMaterialTexture texInfo = textureSets[i].ts[indexOfTexSetToRender];
                Texture2D tx = texInfo.GetTexture2D();
                if (LOG_LEVEL >= MB2_LogLevel.trace && tx != null) {
                    Debug.Log("Added " + tx + " to atlas w=" + tx.width + " h=" + tx.height + " offset=" + texInfo.matTilingRect.min + " scale=" + texInfo.matTilingRect.size + " rect=" + rs[i] + " padding=" + _padding);
                    //_printTexture(tx);
                }
                
                CopyScaledAndTiledToAtlas(textureSets[i], texInfo, textureSets[i].obUVoffset, textureSets[i].obUVscale, rs[i],_texPropertyName,_resultMaterialTextureBlender, yIsFlipped);
			}
			sw.Stop();
			sw.Start();
			if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log ("Total time for Graphics.DrawTexture calls " + (sw.ElapsedMilliseconds).ToString("f5"));
			if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log ("Copying RenderTexture to Texture2D. destW" + _destinationTexture.width + " destH" + _destinationTexture.height );
			//Convert the RenderTexture to a Texture2D
			/*
			Texture2D tempTexture;
			tempTexture = new Texture2D(_destinationTexture.width, _destinationTexture.height, TextureFormat.ARGB32, true);

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = _destinationTexture;
            int xblocks = Mathf.CeilToInt(((float) _destinationTexture.width) / 512);
			int yblocks = Mathf.CeilToInt(((float) _destinationTexture.height) / 512);
			if (xblocks == 0 || yblocks == 0)
			{
				if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log ("Copying all in one shot"); 
				tempTexture.ReadPixels(new Rect(0, 0, _destinationTexture.width, _destinationTexture.height), 0, 0, true);
			} else {
				
                if (yIsFlipped == false)
                {
                    for (int x = 0; x < xblocks; x++)
                    {
                        for (int y = 0; y < yblocks; y++)
                        {
                            int xx = x * 512;
                            int yy = y * 512;
                            Rect r = new Rect(xx, yy, 512, 512);
                            tempTexture.ReadPixels(r, x * 512, y * 512, true);
                        }
                    }
                }
                else
                {
				if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log ("Not OpenGL copying blocks");
                    for (int x = 0; x < xblocks; x++)
                    {
                        for (int y = 0; y < yblocks; y++)
                    {
                        int xx = x * 512;
                        int yy = _destinationTexture.height - 512 - y * 512;
                            Rect r = new Rect(xx, yy, 512, 512);
                            tempTexture.ReadPixels(r, x * 512, y * 512, true);
                        }
					}
				}
            }
            RenderTexture.active = oldRT;
            tempTexture.Apply ();
            if (LOG_LEVEL >= MB2_LogLevel.trace)
            {
                Debug.Log("TempTexture ");
                if (tempTexture.height <= 16 && tempTexture.width <= 16) _printTexture(tempTexture);
            }
			myCamera.targetTexture = null;
			RenderTexture.active = null;
			*/
			Texture2D tempTexture = new Texture2D(_destinationTexture.width, _destinationTexture.height, TextureFormat.ARGB32, true, false);
			ConvertRenderTextureToTexture2D(_destinationTexture, yIsFlipped, false, LOG_LEVEL, tempTexture);
			myCamera.targetTexture = null;

			targTex = tempTexture;	
			if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log ("Total time to copy RenderTexture to Texture2D " + (sw.ElapsedMilliseconds).ToString("f5"));
		}
	}
	
	public static void ConvertRenderTextureToTexture2D(RenderTexture _destinationTexture, bool yIsFlipped, bool doLinearColorSpace, MB2_LogLevel LOG_LEVEL, Texture2D tempTexture)
	{
		RenderTexture oldRT = RenderTexture.active;
		RenderTexture.active = _destinationTexture;
		int xblocks = Mathf.CeilToInt(((float)_destinationTexture.width) / 512);
		int yblocks = Mathf.CeilToInt(((float)_destinationTexture.height) / 512);
		if (xblocks == 0 || yblocks == 0)
		{
			if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Copying all in one shot");
			tempTexture.ReadPixels(new Rect(0, 0, _destinationTexture.width, _destinationTexture.height), 0, 0, true);
		}
		else
		{
			if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("yIsFlipped copying blocks");
			if (yIsFlipped == false)
			{
				for (int x = 0; x < xblocks; x++)
				{
					for (int y = 0; y < yblocks; y++)
					{
						int xx = x * 512;
						int yy = y * 512;
						Rect r = new Rect(xx, yy, 512, 512);
						tempTexture.ReadPixels(r, x * 512, y * 512, true);
					}
				}
			}
			else
			{
				
				for (int x = 0; x < xblocks; x++)
				{
					for (int y = 0; y < yblocks; y++)
					{
						int xx = x * 512;
						int yy = _destinationTexture.height - 512 - y * 512;
						Rect r = new Rect(xx, yy, 512, 512);
						tempTexture.ReadPixels(r, x * 512, y * 512, true);
					}
				}
			}
		}

		RenderTexture.active = oldRT;
		tempTexture.Apply();
		if (LOG_LEVEL >= MB2_LogLevel.trace && tempTexture.height <= 16 && tempTexture.width <= 16)
		{
			_printTexture(tempTexture);
		}

		// yield break;
	}


    /* 
    Unity uses a non-standard format for storing normals for some platforms. Imagine the standard format is English, Unity's is French
    When the normal-map checkbox is ticked on the asset importer the normal map is translated into french. When we build the normal atlas
    we are reading the french. When we save and click the normal map tickbox we are translating french -> french. A double transladion that
    breaks the normal map. To fix this we need to "unconvert" the normal map to english when saving the atlas as a texture so that unity importer
    can do its thing properly. 
    */
    Color32 ConvertNormalFormatFromUnity_ToStandard(Color32 c) {
        Vector3 n = Vector3.zero;
        n.x = c.a * 2f - 1f;
        n.y = c.g * 2f - 1f;
        n.z = Mathf.Sqrt(1 - n.x * n.x - n.y * n.y);
        //now repack in the regular format
        Color32 cc = new Color32();
        cc.a = 1;
        cc.r = (byte) ((n.x + 1f) * .5f);
        cc.g = (byte) ((n.y + 1f) * .5f);
        cc.b = (byte) ((n.z + 1f) * .5f);
        return cc;
    }

    public static bool YisFlipped(MB2_LogLevel LOG_LEVEL) {
        string graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion.ToLower();
        bool flipY;
        if (!MBVersion.GraphicsUVStartsAtTop())
        {
            flipY = false;
        } else {
            // "opengl es, direct3d"
            flipY = true;
        }

        if (LOG_LEVEL == MB2_LogLevel.debug) Debug.Log("Graphics device version is: " + graphicsDeviceVersion + " flipY:" + flipY);
        return flipY;
	}
	
	private void CopyScaledAndTiledToAtlas(MB_TexSet texSet, MeshBakerMaterialTexture source, Vector2 obUVoffset, Vector2 obUVscale, Rect rec, ShaderTextureProperty texturePropertyName, MB3_TextureCombinerNonTextureProperties resultMatTexBlender, bool yIsFlipped){			
		Rect r = rec;
        myCamera.backgroundColor = resultMatTexBlender.GetColorForTemporaryTexture(texSet.matsAndGOs.mats[0].mat, texturePropertyName);
        //yIsFlipped = true;
        //if (yIsFlipped)
        //{
        //}
            r.y = 1f - (r.y + r.height); // DrawTexture uses topLeft 0,0, Texture2D uses bottomLeft 0,0 
        r.x *= _destinationTexture.width;
		r.y *= _destinationTexture.height;
		r.width *= _destinationTexture.width;
		r.height *= _destinationTexture.height;
     
        Rect rPadded = r;
		rPadded.x -= _padding;
		rPadded.y -= _padding;
		rPadded.width += _padding * 2;
		rPadded.height += _padding * 2;

		Rect targPr = new Rect();
        Rect srcPrTex = texSet.ts[indexOfTexSetToRender].GetEncapsulatingSamplingRect().GetRect();
        if (!_fixOutOfBoundsUVs) {
            Debug.Assert(source.matTilingRect.GetRect() == texSet.ts[indexOfTexSetToRender].GetEncapsulatingSamplingRect().GetRect());
        }
		Texture2D tex = source.GetTexture2D();
        /*
        if (_considerNonTextureProperties && resultMatTexBlender != null)
        {
            if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(string.Format("Blending texture {0} mat {1} with non-texture properties using TextureBlender {2}", tex.name, texSet.mats[0].mat, resultMatTexBlender));

            resultMatTexBlender.OnBeforeTintTexture(texSet.mats[0].mat, texturePropertyName.name);
            //combine the tintColor with the texture
            tex = combiner._createTextureCopy(tex);
            for (int i = 0; i < tex.height; i++)
            {
                Color[] cs = tex.GetPixels(0, i, tex.width, 1);
                for (int j = 0; j < cs.Length; j++)
                {
                    cs[j] = resultMatTexBlender.OnBlendTexturePixel(texturePropertyName.name, cs[j]);
                }
                tex.SetPixels(0, i, tex.width, 1, cs);
            }
            tex.Apply();
        }
        */
        

        //main texture
        TextureWrapMode oldTexWrapMode = tex.wrapMode;
		if (srcPrTex.width == 1f && srcPrTex.height == 1f && srcPrTex.x == 0f && srcPrTex.y == 0f){
			//fixes bug where there is a dark line at the edge of the texture
			tex.wrapMode = TextureWrapMode.Clamp;
		} else {
			tex.wrapMode = TextureWrapMode.Repeat;
		}
		if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log ("DrawTexture tex=" + tex.name + " destRect=" + r + " srcRect=" + srcPrTex + " Mat=" + mat);
		//fill the padding first
		Rect srcPr = new Rect();
        
		//top margin
		srcPr.x = srcPrTex.x;
		srcPr.y = srcPrTex.y + 1 - 1f / tex.height;
		srcPr.width = srcPrTex.width;
		srcPr.height = 1f / tex.height;
		targPr.x = r.x;
		targPr.y = rPadded.y;
		targPr.width = r.width;
		targPr.height = _padding;
        RenderTexture oldRT = RenderTexture.active;
        RenderTexture.active = _destinationTexture;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);

		//bot margin
		srcPr.x = srcPrTex.x;
		srcPr.y = srcPrTex.y;
		srcPr.width = srcPrTex.width;
		srcPr.height = 1f / tex.height;
		targPr.x = r.x;
		targPr.y = r.y + r.height;
		targPr.width = r.width;
		targPr.height = _padding;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);


		//left margin
		srcPr.x = srcPrTex.x;
		srcPr.y = srcPrTex.y;
		srcPr.width = 1f / tex.width;
		srcPr.height = srcPrTex.height;
		targPr.x = rPadded.x;
		targPr.y = r.y;
		targPr.width = _padding;
		targPr.height = r.height;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);
		
		//right margin
		srcPr.x = srcPrTex.x + 1f - 1f / tex.width;
		srcPr.y = srcPrTex.y;
		srcPr.width = 1f / tex.width;
		srcPr.height = srcPrTex.height;
		targPr.x = r.x + r.width;
		targPr.y = r.y;
		targPr.width = _padding;
		targPr.height = r.height;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);


		//top left corner
		srcPr.x = srcPrTex.x;
		srcPr.y = srcPrTex.y + 1 - 1f / tex.height ;
		srcPr.width = 1f / tex.width;
		srcPr.height = 1f / tex.height;
		targPr.x = rPadded.x; 
		targPr.y = rPadded.y;
		targPr.width = _padding;
		targPr.height = _padding;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);

		//top right corner
		srcPr.x = srcPrTex.x + 1f - 1f / tex.width;
		srcPr.y = srcPrTex.y + 1 - 1f / tex.height ;
		srcPr.width = 1f / tex.width;
		srcPr.height = 1f / tex.height;
		targPr.x = r.x + r.width; 
		targPr.y = rPadded.y;
		targPr.width = _padding;
		targPr.height = _padding;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);

		//bot left corner
		srcPr.x = srcPrTex.x;
		srcPr.y = srcPrTex.y;
		srcPr.width = 1f / tex.width;
		srcPr.height = 1f / tex.height;
		targPr.x = rPadded.x; 
		targPr.y = r.y + r.height;
		targPr.width = _padding;
		targPr.height = _padding;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);
		
		//bot right corner
		srcPr.x = srcPrTex.x + 1f - 1f / tex.width;
		srcPr.y = srcPrTex.y ;
		srcPr.width = 1f / tex.width;
		srcPr.height = 1f / tex.height;
		targPr.x = r.x + r.width; 
		targPr.y = r.y + r.height;
		targPr.width = _padding;
		targPr.height = _padding;
		Graphics.DrawTexture(targPr, tex, srcPr, 0, 0, 0, 0, mat);

        //now the texture
        Graphics.DrawTexture(r, tex, srcPrTex, 0, 0, 0, 0, mat);
        RenderTexture.active = oldRT;
		tex.wrapMode = oldTexWrapMode;
	}

    static void _printTexture(Texture2D t) {
        if (t.width * t.height > 100)
        {
            Debug.Log("Not printing texture too large.");
            return;
        }
        try {
            Color32[] cols = t.GetPixels32();
            string s = "";
            for (int i = 0; i < t.height; i++) {
                for (int j = 0; j < t.width; j++) {
                    s += cols[i * t.width + j] + ", ";
                }
                s += "\n";
            }
            Debug.Log(s);
        } catch (Exception ex)
        {
            Debug.Log("Could not print texture. texture may not be readable." + ex.Message + "\n" + ex.StackTrace.ToString());
        }
    }

}

[ExecuteInEditMode]
public class MB3_AtlasPackerRenderTexture : MonoBehaviour {
	MB_TextureCombinerRenderTexture fastRenderer;
	bool _doRenderAtlas = false;

	public int width;
	public int height;
	public int padding;
	public bool isNormalMap;
    public bool fixOutOfBoundsUVs;
    public bool considerNonTextureProperties;
    public MB3_TextureCombinerNonTextureProperties resultMaterialTextureBlender;
	public Rect[] rects;
	public Texture2D tex1;
	public List<MB_TexSet> textureSets;
	public int indexOfTexSetToRender;
    public ShaderTextureProperty texPropertyName;
	public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;

	public Texture2D testTex;
	public Material testMat;

	public Texture2D OnRenderAtlas(MB3_TextureCombiner combiner){
		fastRenderer = new MB_TextureCombinerRenderTexture();
		_doRenderAtlas = true;
        Texture2D atlas = fastRenderer.DoRenderAtlas(this.gameObject,width,height,padding,rects,textureSets,indexOfTexSetToRender, texPropertyName, resultMaterialTextureBlender, isNormalMap, fixOutOfBoundsUVs, considerNonTextureProperties, combiner, LOG_LEVEL);
		_doRenderAtlas = false;
		return atlas;
	}
	
	void OnRenderObject(){
		if (_doRenderAtlas){
			fastRenderer.OnRenderObject();
			_doRenderAtlas = false;
		}
	}
}
