using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace DigitalOpus.MB.Core{
    // uses this algorithm http://blackpawn.com/texts/lightmaps/

    [System.Serializable]
    public struct AtlasPadding
    {
        public int topBottom;
        public int leftRight;

        public AtlasPadding(int p)
        {
            topBottom = p;
            leftRight = p;
        }

        public AtlasPadding(int px, int py)
        {
            topBottom = py;
            leftRight = px;
        }
    }

    [System.Serializable]
    public class AtlasPackingResult
    {
        public int atlasX;
        public int atlasY;
        public int usedW;
        public int usedH;
        public Rect[] rects;
        public AtlasPadding[] padding;
        public int[] srcImgIdxs;
        public object data;

        public AtlasPackingResult(AtlasPadding[] pds)
        {
            padding = pds;
        }

        public void CalcUsedWidthAndHeight()
        {
            Debug.Assert(rects != null);
            float maxW = 0;
            float maxH = 0;
            float paddingX = 0;
            float paddingY = 0;
            for (int i = 0; i < rects.Length; i++)
            {
                paddingX += padding[i].leftRight * 2f;
                paddingY += padding[i].topBottom * 2f;
                maxW = Mathf.Max(maxW, rects[i].x + rects[i].width);
                maxH = Mathf.Max(maxH, rects[i].y + rects[i].height);
            }
            usedW = Mathf.CeilToInt(maxW * atlasX + paddingX);
            usedH = Mathf.CeilToInt(maxH * atlasY + paddingY);
            if (usedW > atlasX) usedW = atlasX;
            if (usedH > atlasY) usedH = atlasY;
        }

        public override string ToString()
        {
            return string.Format("numRects: {0}, atlasX: {1} atlasY: {2} usedW: {3} usedH: {4}", rects.Length, atlasX, atlasY, usedW, usedH);
        }
    }

    public abstract class MB2_TexturePacker
    {

        public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;

        internal const int MAX_RECURSION_DEPTH = 10;

        internal enum NodeType
        {
            Container,
            maxDim,
            regular
        }

        internal class PixRect
        {
            public int x;
            public int y;
            public int w;
            public int h;

            public PixRect() { }
            public PixRect(int xx, int yy, int ww, int hh)
            {
                x = xx;
                y = yy;
                w = ww;
                h = hh;
            }

            public override string ToString()
            {
                return String.Format("x={0},y={1},w={2},h={3}", x, y, w, h);
            }
        }

        internal class Image
        {
            public int imgId;
            public int w;
            public int h;
            public int x;
            public int y;

            public Image(int id, int tw, int th, AtlasPadding padding, int minImageSizeX, int minImageSizeY)
            {
                imgId = id;
                w = Mathf.Max(tw + padding.leftRight * 2, minImageSizeX);
                h = Mathf.Max(th + padding.topBottom * 2, minImageSizeY);
            }

            public Image(Image im)
            {
                imgId = im.imgId;
                w = im.w;
                h = im.h;
                x = im.x;
                y = im.y;
            }
        }

        internal class ImgIDComparer : IComparer<Image>
        {
            public int Compare(Image x, Image y)
            {
                if (x.imgId > y.imgId)
                    return 1;
                if (x.imgId == y.imgId)
                    return 0;
                return -1;
            }
        }

        internal class ImageHeightComparer : IComparer<Image>
        {
            public int Compare(Image x, Image y)
            {
                if (x.h > y.h)
                    return -1;
                if (x.h == y.h)
                    return 0;
                return 1;
            }
        }

        internal class ImageWidthComparer : IComparer<Image>
        {
            public int Compare(Image x, Image y)
            {
                if (x.w > y.w)
                    return -1;
                if (x.w == y.w)
                    return 0;
                return 1;
            }
        }

        internal class ImageAreaComparer : IComparer<Image>
        {
            public int Compare(Image x, Image y)
            {
                int ax = x.w * x.h;
                int ay = y.w * y.h;
                if (ax > ay)
                    return -1;
                if (ax == ay)
                    return 0;
                return 1;
            }
        }

        public bool atlasMustBePowerOfTwo = true;

        public static int RoundToNearestPositivePowerOfTwo(int x)
        {
            int p = (int)Mathf.Pow(2, Mathf.RoundToInt(Mathf.Log(x) / Mathf.Log(2)));
            if (p == 0 || p == 1) p = 2;
            return p;
        }

        public static int CeilToNearestPowerOfTwo(int x)
        {
            int p = (int)Mathf.Pow(2, Mathf.Ceil(Mathf.Log(x) / Mathf.Log(2)));
            if (p == 0 || p == 1) p = 2;
            return p;
        }

        public abstract AtlasPackingResult[] GetRects(List<Vector2> imgWidthHeights, int maxDimensionX, int maxDimensionY, int padding);

        public abstract AtlasPackingResult[] GetRects(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionX, int maxDimensionY, bool doMultiAtlas);

        /*
        Packed rects may exceed atlas size and require scaling
        When scaling want pixel perfect fit in atlas. Corners of rects should exactly align with pixel grid
        Padding should be subtracted from pixel perfect rect to create pixel perfect square 
        TODO this doesn't handle each rectangle having different padding
        */
        internal bool ScaleAtlasToFitMaxDim(Vector2 rootWH, List<Image> images, int maxDimensionX, int maxDimensionY, AtlasPadding padding, int minImageSizeX, int minImageSizeY, int masterImageSizeX, int masterImageSizeY, 
            ref int outW, ref int outH, out float padX, out float padY, out int newMinSizeX, out int newMinSizeY)
        {
            newMinSizeX = minImageSizeX;
            newMinSizeY = minImageSizeY;
            bool redoPacking = false;

            // the atlas may be packed larger than the maxDimension. If so then the atlas needs to be scaled down to fit
            padX = (float)padding.leftRight / (float)outW; //padding needs to be pixel perfect in size
            if (rootWH.x > maxDimensionX)
            {
                padX = (float)padding.leftRight / (float)maxDimensionX;
                float scaleFactor = (float)maxDimensionX / (float)rootWH.x;
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Packing exceeded atlas width shrinking to " + scaleFactor);
                for (int i = 0; i < images.Count; i++)
                {
                    Image im = images[i];
                    if (im.w * scaleFactor < masterImageSizeX)
                    { //check if small images will be rounded too small. If so need to redo packing forcing a larger min size
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Small images are being scaled to zero. Will need to redo packing with larger minTexSizeX.");
                        redoPacking = true;
                        newMinSizeX = Mathf.CeilToInt(minImageSizeX / scaleFactor);
                    }
                    int right = (int)((im.x + im.w) * scaleFactor);
                    im.x = (int)(scaleFactor * im.x);
                    im.w = right - im.x;
                }
                outW = maxDimensionX;
            }

            padY = (float)padding.topBottom / (float)outH;
            if (rootWH.y > maxDimensionY)
            {
                //float minSizeY = ((float)minImageSizeY + 1) / maxDimension;
                padY = (float)padding.topBottom / (float)maxDimensionY;
                float scaleFactor = (float)maxDimensionY / (float)rootWH.y;
                if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Packing exceeded atlas height shrinking to " + scaleFactor);
                for (int i = 0; i < images.Count; i++)
                {
                    Image im = images[i];
                    if (im.h * scaleFactor < masterImageSizeY)
                    { //check if small images will be rounded too small. If so need to redo packing forcing a larger min size
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Small images are being scaled to zero. Will need to redo packing with larger minTexSizeY.");
                        redoPacking = true;
                        newMinSizeY = Mathf.CeilToInt(minImageSizeY / scaleFactor);
                    }
                    int bottom = (int)((im.y + im.h) * scaleFactor);
                    im.y = (int)(scaleFactor * im.y);
                    im.h = bottom - im.y;
                }
                outH = maxDimensionY;
            }
            return redoPacking;
        }

        //normalize atlases so that that rects are 0 to 1
        public void ConvertToRectsWithoutPaddingAndNormalize01(AtlasPackingResult rr, AtlasPadding padding)
        {
            for (int i = 0; i < rr.rects.Length; i++)
            {
                rr.rects[i].x = (rr.rects[i].x + padding.leftRight) / rr.atlasX;
                rr.rects[i].y = (rr.rects[i].y + padding.topBottom) / rr.atlasY;
                rr.rects[i].width = (rr.rects[i].width - padding.leftRight * 2) / rr.atlasX;
                rr.rects[i].height = (rr.rects[i].height - padding.topBottom * 2) / rr.atlasY;
            }
        }
    }

    public class MB2_TexturePackerRegular : MB2_TexturePacker {		
		class ProbeResult{
			public int w;
			public int h;
            public int outW;
            public int outH;
			public Node root;
			public bool largerOrEqualToMaxDim;
			public float efficiency;
			public float squareness;

            //these are for the multiAtlasPacker
            public float totalAtlasArea;
            public int numAtlases;
			
			public void Set(int ww, int hh, int outw, int outh, Node r, bool fits, float e, float sq){
				w = ww;
				h = hh;
                outW = outw;
                outH = outh;
				root = r;
				largerOrEqualToMaxDim = fits;
				efficiency = e;
				squareness = sq;
			}
			
			public float GetScore(bool doPowerOfTwoScore){
				float fitsScore = largerOrEqualToMaxDim ? 1f : 0f;
				if (doPowerOfTwoScore){
					return fitsScore * 2f + efficiency;
				} else {
					return squareness + 2 * efficiency + fitsScore;
				}
			}

            public void PrintTree()
            {
                printTree(root, "  ");
            }
		}
		
		internal class Node {
            internal NodeType isFullAtlas; //is this node a full atlas used for scaling to fit  
            internal Node[] child = new Node[2];
            internal PixRect r;
            internal Image img;
            ProbeResult bestRoot;
            internal Node(NodeType rootType)
            {
                isFullAtlas = rootType;
            }

            private bool isLeaf(){
				if (child[0] == null || child[1] == null){
					return true;
				}
				return false;
			}
			
			internal Node Insert(Image im, bool handed){
				int a,b;
				if (handed){
				  a = 0;
				  b = 1;
				} else {
				  a = 1;
				  b = 0;
				}
				if (!isLeaf()){
					//try insert into first child
					Node newNode = child[a].Insert(im,handed);
					if (newNode != null)
						return newNode;
					//no room insert into second
					return child[b].Insert(im,handed);
				} else {
			        //(if there's already a img here, return)
			        if (img != null) 
						return null;
			
			        //(if space too small, return)
			        if (r.w < im.w || r.h < im.h)
			            return null;
					
			        //(if space just right, accept)
			        if (r.w == im.w && r.h == im.h){
						img = im;
			            return this;
					}
			        
			        //(otherwise, gotta split this node and create some kids)
			        child[a] = new Node(NodeType.regular);
			        child[b] = new Node(NodeType.regular);
			        
			        //(decide which way to split)
			        int dw = r.w - im.w;
			        int dh = r.h - im.h;
			        
			        if (dw > dh){
			            child[a].r = new PixRect(r.x, r.y, im.w, r.h);
			            child[b].r = new PixRect(r.x + im.w, r.y, r.w - im.w, r.h);
					} else {
			            child[a].r = new PixRect(r.x, r.y, r.w, im.h);
			            child[b].r = new PixRect(r.x, r.y+ im.h, r.w, r.h - im.h);
					}
			        return child[a].Insert(im,handed);				
				}
			}
		}

        ProbeResult bestRoot;
        public int atlasY;

        static void printTree(Node r, string spc){
            Debug.Log(spc + "Nd img=" + (r.img != null) + " r=" + r.r);
			if (r.child[0] != null)
				printTree(r.child[0], spc + "      ");
			if (r.child[1] != null)
				printTree(r.child[1], spc + "      ");		
		}
		
		static void flattenTree(Node r, List<Image> putHere){
			if (r.img != null){
				r.img.x = r.r.x;
				r.img.y = r.r.y;				
				putHere.Add(r.img);
			}
			if (r.child[0] != null)
				flattenTree(r.child[0], putHere);
			if (r.child[1] != null)
				flattenTree(r.child[1], putHere);		
		}
		
		static void drawGizmosNode(Node r){
			Vector3 extents = new Vector3(r.r.w, r.r.h, 0);
			Vector3 pos = new Vector3(r.r.x + extents.x/2, -r.r.y - extents.y/2, 0f);
            Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(pos,extents);
			if (r.img != null){
				Gizmos.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                extents = new Vector3(r.img.w, r.img.h, 0);
                pos = new Vector3(r.r.x + extents.x / 2, -r.r.y - extents.y / 2, 0f);
                Gizmos.DrawCube(pos,extents);
			}
			if (r.child[0] != null){
				Gizmos.color = Color.red;
				drawGizmosNode(r.child[0]);
			}
			if (r.child[1] != null){
				Gizmos.color = Color.green;
				drawGizmosNode(r.child[1]);
			}
		}
	    	
		static Texture2D createFilledTex(Color c, int w, int h){
			Texture2D t = new Texture2D(w,h);
			for (int i = 0; i < w; i++){
				for (int j = 0; j < h; j++){
					t.SetPixel(i,j,c);
				}
			}
			t.Apply();
			return t;
		}
		
		public void DrawGizmos(){
            if (bestRoot != null)
            {
                drawGizmosNode(bestRoot.root);
                Gizmos.color = Color.yellow;
                Vector3 extents = new Vector3(bestRoot.outW, -bestRoot.outH, 0);
                Vector3 pos = new Vector3(extents.x / 2, extents.y / 2, 0f);
                Gizmos.DrawWireCube(pos, extents);
            }
		}

		bool ProbeSingleAtlas(Image[] imgsToAdd, int idealAtlasW, int idealAtlasH, float imgArea, int maxAtlasDimX, int maxAtlasDimY, ProbeResult pr){
			Node root = new Node(NodeType.maxDim);
			root.r = new PixRect(0,0,idealAtlasW,idealAtlasH);
            //Debug.Assert(maxAtlasDim >= 1);
			for (int i = 0; i < imgsToAdd.Length; i++){
				Node n = root.Insert(imgsToAdd[i],false);
				if (n == null){
					return false;
				} else if (i == imgsToAdd.Length -1){
					int usedW = 0; 
					int usedH = 0; 
					GetExtent(root,ref usedW, ref usedH);
					float efficiency,squareness;
					bool fitsInMaxDim;
                    int atlasW = usedW;
                    int atlasH = usedH;
					if (atlasMustBePowerOfTwo){
						atlasW = Mathf.Min (CeilToNearestPowerOfTwo(usedW), maxAtlasDimX);
						atlasH = Mathf.Min (CeilToNearestPowerOfTwo(usedH), maxAtlasDimY);
						if (atlasH < atlasW / 2) atlasH = atlasW / 2;
						if (atlasW < atlasH / 2) atlasW = atlasH / 2;
						fitsInMaxDim = usedW <= maxAtlasDimX && usedH <= maxAtlasDimY;
						float scaleW = Mathf.Max (1f,((float)usedW)/ maxAtlasDimX);
						float scaleH = Mathf.Max (1f,((float)usedH)/ maxAtlasDimY);
						float atlasArea = atlasW * scaleW * atlasH * scaleH; //area if we scaled it up to something large enough to contain images
						efficiency = 1f - (atlasArea - imgArea) / atlasArea;
						squareness = 1f; //don't care about squareness in power of two case
					} else {
						efficiency = 1f - (usedW * usedH - imgArea) / (usedW * usedH);
						if (usedW < usedH) squareness = (float) usedW / (float) usedH;
						else squareness = (float) usedH / (float) usedW;
						fitsInMaxDim = usedW <= maxAtlasDimX && usedH <= maxAtlasDimY;
					}
					pr.Set(usedW,usedH,atlasW,atlasH,root,fitsInMaxDim,efficiency,squareness);
					if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Probe success efficiency w=" + usedW + " h=" + usedH + " e=" + efficiency + " sq=" + squareness + " fits=" + fitsInMaxDim);
					return true;
				}
			}	
			Debug.LogError("Should never get here.");
			return false;
		}

        bool ProbeMultiAtlas(Image[] imgsToAdd, int idealAtlasW, int idealAtlasH, float imgArea, int maxAtlasDimX, int maxAtlasDimY, ProbeResult pr)
        {
            int numAtlases = 0;
            Node root = new Node(NodeType.maxDim);
            root.r = new PixRect(0, 0, idealAtlasW, idealAtlasH);
            for (int i = 0; i < imgsToAdd.Length; i++)
            {
                Node n = root.Insert(imgsToAdd[i], false);
                if (n == null)
                {
                    if (imgsToAdd[i].x > idealAtlasW && imgsToAdd[i].y > idealAtlasH)
                    {
                        return false;
                    } else
                    {
                        // create a new root node wider than previous atlas
                        Node newRoot = new Node(NodeType.Container);
                        newRoot.r = new PixRect(0, 0, root.r.w + idealAtlasW, idealAtlasH);
                        // create a new right child
                        Node newRight = new Node(NodeType.maxDim);
                        newRight.r = new PixRect(root.r.w, 0, idealAtlasW, idealAtlasH);
                        newRoot.child[1] = newRight;
                        // insert root as a new left child
                        newRoot.child[0] = root;
                        root = newRoot;
                        n = root.Insert(imgsToAdd[i], false);
                        numAtlases++;
                    }
                }
            }
            pr.numAtlases = numAtlases;
            pr.root = root;
            //todo atlas may not be maxDim * maxDim. Do some checking to see what actual needed sizes are and update pr.totalArea
            pr.totalAtlasArea = numAtlases * maxAtlasDimX * maxAtlasDimY;
            if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Probe success efficiency numAtlases=" + numAtlases + " totalArea=" + pr.totalAtlasArea);
            return true;

        }

        internal void GetExtent(Node r, ref int x, ref int y)
        {
            if (r.img != null)
            {
                if (r.r.x + r.img.w > x)
                {
                    x = r.r.x + r.img.w;
                }
                if (r.r.y + r.img.h > y) y = r.r.y + r.img.h;
            }
            if (r.child[0] != null)
                GetExtent(r.child[0], ref x, ref y);
            if (r.child[1] != null)
                GetExtent(r.child[1], ref x, ref y);
        }

        int StepWidthHeight(int oldVal, int step, int maxDim){
			if (atlasMustBePowerOfTwo && oldVal < maxDim){
				return oldVal * 2;
			} else {
				int newVal = oldVal + step;
				if (newVal > maxDim && oldVal < maxDim) newVal = maxDim;
				return newVal;
			}
		}

        public override AtlasPackingResult[] GetRects(List<Vector2> imgWidthHeights, int maxDimensionX, int maxDimensionY, int atPadding)
        {
            List<AtlasPadding> padding = new List<AtlasPadding>();
            for (int i = 0; i < imgWidthHeights.Count; i++)
            {
                AtlasPadding p = new AtlasPadding();
                p.leftRight = p.topBottom = atPadding;
                padding.Add(p);
            }
            return GetRects(imgWidthHeights, padding, maxDimensionX, maxDimensionY, false);
        }

        public override AtlasPackingResult[] GetRects(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionX, int maxDimensionY, bool doMultiAtlas){
            Debug.Assert(imgWidthHeights.Count == paddings.Count, imgWidthHeights.Count + " " + paddings.Count);
            int maxPaddingX = 0;
            int maxPaddingY = 0;
            for (int i = 0; i < paddings.Count; i++)
            {
                maxPaddingX = Mathf.Max(maxPaddingX, paddings[i].leftRight);
                maxPaddingY = Mathf.Max(maxPaddingY, paddings[i].topBottom);
            }
            if (doMultiAtlas)
            {
                return _GetRectsMultiAtlas(imgWidthHeights, paddings, maxDimensionX, maxDimensionY, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2);
            }
            else
            {
                AtlasPackingResult apr = _GetRectsSingleAtlas(imgWidthHeights, paddings, maxDimensionX, maxDimensionY, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2, 0);
                if (apr == null)
                {
                    return null;
                } else
                {
                    return new AtlasPackingResult[] { apr };
                }
            }
		}

        //------------------ Algorithm for fitting everything into one atlas and scaling down
        // 
        // for images being added calc area, maxW, maxH. A perfectly packed atlas will match area exactly. atlas must be at least maxH and maxW in size.
        // Sort images from big to small using either height, width or area comparer
        // Explore space to find a resonably efficient packing. Grow the atlas gradually until a fit is found
        // Scale atlas to fit
        //
        AtlasPackingResult _GetRectsSingleAtlas(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionX, int maxDimensionY, int minImageSizeX, int minImageSizeY, int masterImageSizeX, int masterImageSizeY, int recursionDepth){
			if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log (String.Format("_GetRects numImages={0}, maxDimension={1}, minImageSizeX={2}, minImageSizeY={3}, masterImageSizeX={4}, masterImageSizeY={5}, recursionDepth={6}",
			                                                                 imgWidthHeights.Count, maxDimensionX, minImageSizeX, minImageSizeY, masterImageSizeX, masterImageSizeY, recursionDepth));
            if (recursionDepth > MAX_RECURSION_DEPTH) {
                if (LOG_LEVEL >= MB2_LogLevel.error) Debug.LogError("Maximum recursion depth reached. The baked atlas is likely not very good. " +
                    " This happens when the packed atlases exceeds the maximum" +
                    " atlas size in one or both dimensions so that the atlas needs to be downscaled AND there are some very thin or very small images (only-a-few-pixels)." +
                    " these very thin images can 'vanish' completely when the atlas is downscaled.\n\n" +
                    " Try one or more of the following: using multiple atlases, increase the maximum atlas size, don't use 'force-power-of-two', remove the source materials that are are using very small/thin textures.");
				//return null;
			}
			float area = 0;
			int maxW = 0;
			int maxH = 0;
			Image[] imgsToAdd = new Image[imgWidthHeights.Count];
			for (int i = 0; i < imgsToAdd.Length; i++){
                int iw = (int)imgWidthHeights[i].x;
                int ih = (int)imgWidthHeights[i].y;
                Image im = imgsToAdd[i] = new Image(i, iw, ih, paddings[i], minImageSizeX, minImageSizeY);
				area += im.w * im.h;
				maxW = Mathf.Max(maxW, im.w);
				maxH = Mathf.Max(maxH, im.h);
			}
			
			if ((float)maxH/(float)maxW > 2){
				if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Using height Comparer");
				Array.Sort(imgsToAdd,new ImageHeightComparer());
			}
			else if ((float)maxH/(float)maxW < .5){
				if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Using width Comparer");
				Array.Sort(imgsToAdd,new ImageWidthComparer());
			}
			else{
				if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Using area Comparer");
				Array.Sort(imgsToAdd,new ImageAreaComparer());
			}

			//explore the space to find a resonably efficient packing 
			int sqrtArea = (int) Mathf.Sqrt(area);
			int idealAtlasW; 
			int idealAtlasH;

            if (atlasMustBePowerOfTwo)
            {
                idealAtlasW = idealAtlasH = RoundToNearestPositivePowerOfTwo(sqrtArea);
                if (maxW > idealAtlasW)
                {
                    idealAtlasW = CeilToNearestPowerOfTwo(idealAtlasW);
                }
                if (maxH > idealAtlasH)
                {
                    idealAtlasH = CeilToNearestPowerOfTwo(idealAtlasH);
                }
            }
            else
            {
                idealAtlasW = sqrtArea;
                idealAtlasH = sqrtArea;
                if (maxW > sqrtArea)
                {
                    idealAtlasW = maxW;
                    idealAtlasH = Mathf.Max(Mathf.CeilToInt(area / maxW), maxH);
                }
                if (maxH > sqrtArea)
                {
                    idealAtlasW = Mathf.Max(Mathf.CeilToInt(area / maxH), maxW);
                    idealAtlasH = maxH;
                }
            }

			if (idealAtlasW == 0) idealAtlasW = 4;
			if (idealAtlasH == 0) idealAtlasH = 4;
			int stepW = (int)(idealAtlasW * .15f);
			int stepH = (int)(idealAtlasH * .15f);
			if (stepW == 0) stepW = 1;
			if (stepH == 0) stepH = 1;
			int numWIterations=2;
			int steppedWidth = idealAtlasW;
			int steppedHeight = idealAtlasH;

            while (numWIterations >= 1 && steppedHeight < sqrtArea * 1000)
            {
                bool successW = false;
                numWIterations = 0;
                steppedWidth = idealAtlasW;
                while (!successW && steppedWidth < sqrtArea * 1000)
                {
                    ProbeResult pr = new ProbeResult();
                    if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Probing h=" + steppedHeight + " w=" + steppedWidth);
                    if (ProbeSingleAtlas (imgsToAdd, steppedWidth, steppedHeight, area, maxDimensionX, maxDimensionY, pr))
                    {
                        successW = true;
                        if (bestRoot == null) bestRoot = pr;
                        else if (pr.GetScore(atlasMustBePowerOfTwo) > bestRoot.GetScore(atlasMustBePowerOfTwo)) bestRoot = pr;
                    }
                    else
                    {
                        numWIterations++;
                        steppedWidth = StepWidthHeight(steppedWidth, stepW, maxDimensionX);
                        if (LOG_LEVEL >= MB2_LogLevel.trace) MB2_Log.LogDebug("increasing Width h=" + steppedHeight + " w=" + steppedWidth);
                    }
                }
                steppedHeight = StepWidthHeight(steppedHeight, stepH, maxDimensionY);
                if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("increasing Height h=" + steppedHeight + " w=" + steppedWidth);
            }
            if (bestRoot == null)
            {
                return null;
            }

            int outW = 0;
			int outH = 0;
			if (atlasMustBePowerOfTwo){
				outW = Mathf.Min (CeilToNearestPowerOfTwo(bestRoot.w), maxDimensionX);
				outH = Mathf.Min (CeilToNearestPowerOfTwo(bestRoot.h), maxDimensionY);
				if (outH < outW / 2) outH = outW / 2; //smaller dim can't be less than half larger
				if (outW < outH / 2) outW = outH / 2;
			} else
            {
                outW = Mathf.Min(bestRoot.w, maxDimensionX);
                outH = Mathf.Min(bestRoot.h, maxDimensionY);
            }

            bestRoot.outW = outW;
            bestRoot.outH = outH;
			if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Best fit found: atlasW=" + outW + " atlasH" + outH + " w=" + bestRoot.w + " h=" + bestRoot.h + " efficiency=" + bestRoot.efficiency + " squareness=" + bestRoot.squareness + " fits in max dimension=" + bestRoot.largerOrEqualToMaxDim);

			//Debug.Assert(images.Count != imgsToAdd.Length, "Result images not the same lentgh as source"));

            //the atlas can be larger than the max dimension scale it if this is the case
			//int newMinSizeX = minImageSizeX;
			//int	newMinSizeY = minImageSizeY;
         

            List<Image> images = new List<Image>();
            flattenTree(bestRoot.root, images);
            images.Sort(new ImgIDComparer());
            // the atlas may be packed larger than the maxDimension. If so then the atlas needs to be scaled down to fit
            Vector2 rootWH = new Vector2(bestRoot.w, bestRoot.h);
            float padX, padY; 
            int newMinSizeX, newMinSizeY;
            if (!ScaleAtlasToFitMaxDim(rootWH, images, maxDimensionX, maxDimensionY, paddings[0], minImageSizeX, minImageSizeY, masterImageSizeX, masterImageSizeY,
                ref outW, ref outH, out padX, out padY, out newMinSizeX, out newMinSizeY) ||
                recursionDepth > MAX_RECURSION_DEPTH)
            {
                AtlasPackingResult res = new AtlasPackingResult(paddings.ToArray());
                res.rects = new Rect[images.Count];
                res.srcImgIdxs = new int[images.Count];
                res.atlasX = outW;
                res.atlasY = outH;
                res.usedW = -1;
                res.usedH = -1;
                for (int i = 0; i < images.Count; i++)
                {
                    Image im = images[i];
                    Rect r = res.rects[i] = new Rect((float)im.x / (float)outW + padX,
                                                     (float)im.y / (float)outH + padY,
                                                     (float)im.w / (float)outW - padX * 2f,
                                                     (float)im.h / (float)outH - padY * 2f);
                    res.srcImgIdxs[i] = im.imgId;
                    if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Image: " + i + " imgID=" + im.imgId + " x=" + r.x * outW +
                               " y=" + r.y * outH + " w=" + r.width * outW +
                               " h=" + r.height * outH + " padding=" + (paddings[i].leftRight * 2) + "x" + (paddings[i].topBottom*2));
                }
                res.CalcUsedWidthAndHeight();
                return res;


            }
            else
            {
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("==================== REDOING PACKING ================");
                //root = null;
                return _GetRectsSingleAtlas(imgWidthHeights, paddings, maxDimensionX, maxDimensionY, newMinSizeX, newMinSizeY, masterImageSizeX, masterImageSizeY, recursionDepth + 1);
            }


            //if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug(String.Format("Done GetRects atlasW={0} atlasH={1}", bestRoot.w, bestRoot.h));		
			
			//return res;			
		}


        //----------------- Algorithm for fitting everything into multiple Atlases
        //
        // for images being added calc area, maxW, maxH. A perfectly packed atlas will match area exactly. atlas must be at least maxH and maxW in size.
        // Sort images from big to small using either height, width or area comparer
        // 
        // If an image is bigger than maxDim, then shrink it to max size on the largest dimension
        // distribute images using the new algorithm, should never have to expand the atlas instead create new atlases as needed
        // should not need to scale atlases
        //
        AtlasPackingResult[] _GetRectsMultiAtlas(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionPassedX, int maxDimensionPassedY, int minImageSizeX, int minImageSizeY, int masterImageSizeX, int masterImageSizeY)
        {
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log(String.Format("_GetRects numImages={0}, maxDimensionX={1}, maxDimensionY={2} minImageSizeX={3}, minImageSizeY={4}, masterImageSizeX={5}, masterImageSizeY={6}",
                                                                             imgWidthHeights.Count, maxDimensionPassedX, maxDimensionPassedY, minImageSizeX, minImageSizeY, masterImageSizeX, masterImageSizeY));
            float area = 0;
            int maxW = 0;
            int maxH = 0;
            Image[] imgsToAdd = new Image[imgWidthHeights.Count];
            int maxDimensionX = maxDimensionPassedX;
            int maxDimensionY = maxDimensionPassedY;
            if (atlasMustBePowerOfTwo)
            {
                maxDimensionX = RoundToNearestPositivePowerOfTwo(maxDimensionX);
                maxDimensionY = RoundToNearestPositivePowerOfTwo(maxDimensionY);
            }
            for (int i = 0; i < imgsToAdd.Length; i++)
            {
                int iw = (int)imgWidthHeights[i].x;
                int ih = (int)imgWidthHeights[i].y;

                //shrink the image so that it fits in maxDimenion if it is larger than maxDimension if atlas exceeds maxDim x maxDim then new alas will be created
                iw = Mathf.Min(iw, maxDimensionX - paddings[i].leftRight * 2);
                ih = Mathf.Min(ih, maxDimensionY - paddings[i].topBottom * 2);

                Image im = imgsToAdd[i] = new Image(i, iw, ih, paddings[i], minImageSizeX, minImageSizeY);
                area += im.w * im.h;
                maxW = Mathf.Max(maxW, im.w);
                maxH = Mathf.Max(maxH, im.h);
            }

            //explore the space to find a resonably efficient packing
            //int sqrtArea = (int)Mathf.Sqrt(area);
            int idealAtlasW;
            int idealAtlasH;

            if (atlasMustBePowerOfTwo)
            {
                idealAtlasH = RoundToNearestPositivePowerOfTwo(maxDimensionY);
                idealAtlasW = RoundToNearestPositivePowerOfTwo(maxDimensionX);
            }
            else
            {
                idealAtlasH = maxDimensionY;
                idealAtlasW = maxDimensionX;
            }

            if (idealAtlasW == 0) idealAtlasW = 4;
            if (idealAtlasH == 0) idealAtlasH = 4;

            ProbeResult pr = new ProbeResult();
            Array.Sort(imgsToAdd, new ImageHeightComparer());
            if (ProbeMultiAtlas(imgsToAdd, idealAtlasW, idealAtlasH, area, maxDimensionX, maxDimensionY, pr))
            {
                bestRoot = pr;
            }
            Array.Sort(imgsToAdd, new ImageWidthComparer());
            if (ProbeMultiAtlas(imgsToAdd, idealAtlasW, idealAtlasH, area, maxDimensionX, maxDimensionY, pr))
            {
                if (pr.totalAtlasArea < bestRoot.totalAtlasArea)
                {
                    bestRoot = pr;
                }
            }
            Array.Sort(imgsToAdd, new ImageAreaComparer());
            if (ProbeMultiAtlas(imgsToAdd, idealAtlasW, idealAtlasH, area, maxDimensionX, maxDimensionY, pr))
            {
                if (pr.totalAtlasArea < bestRoot.totalAtlasArea)
                {
                    bestRoot = pr;
                }
            }

            if (bestRoot == null)
            {
                return null;
            }
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Best fit found: w=" + bestRoot.w + " h=" + bestRoot.h + " efficiency=" + bestRoot.efficiency + " squareness=" + bestRoot.squareness + " fits in max dimension=" + bestRoot.largerOrEqualToMaxDim);

            //the atlas can be larger than the max dimension scale it if this is the case
            //int newMinSizeX = minImageSizeX;
            //int newMinSizeY = minImageSizeY;
            List<AtlasPackingResult> rs = new List<AtlasPackingResult>();

            // find all Nodes that are an individual atlas
            List<Node> atlasNodes = new List<Node>();
            Stack<Node> stack = new Stack<Node>();
            Node node = bestRoot.root;

            while (node != null)
            {
                stack.Push(node);
                node = node.child[0];
            }

            // traverse the tree collecting atlasNodes
            while (stack.Count > 0)
            {
                node = stack.Pop();
                if (node.isFullAtlas == NodeType.maxDim) atlasNodes.Add(node);
                if (node.child[1] != null)
                {
                    node = node.child[1];
                    while (node != null)
                    {
                        stack.Push(node);
                        node = node.child[0];
                    }
                }
            }

            //pack atlases so they all fit
            for (int i = 0; i < atlasNodes.Count; i++)
            {
                List<Image> images = new List<Image>();
                flattenTree(atlasNodes[i], images);
                Rect[] rss = new Rect[images.Count];
                int[] srcImgIdx = new int[images.Count];
                for (int j = 0; j < images.Count; j++)
                {
                    rss[j] = (new Rect(images[j].x - atlasNodes[i].r.x, images[j].y, images[j].w, images[j].h));
                    srcImgIdx[j] = images[j].imgId;
                }
                AtlasPackingResult res = new AtlasPackingResult(paddings.ToArray());
                GetExtent(atlasNodes[i], ref res.usedW, ref res.usedH);
                res.usedW -= atlasNodes[i].r.x; 
                int outW = atlasNodes[i].r.w;
                int outH = atlasNodes[i].r.h;
                if (atlasMustBePowerOfTwo)
                {
                    outW = Mathf.Min(CeilToNearestPowerOfTwo(res.usedW), atlasNodes[i].r.w);
                    outH = Mathf.Min(CeilToNearestPowerOfTwo(res.usedH), atlasNodes[i].r.h);
                    if (outH < outW / 2) outH = outW / 2; //smaller dim can't be less than half larger
                    if (outW < outH / 2) outW = outH / 2;
                } else
                {
                    outW = res.usedW;
                    outH = res.usedH;
                }

                res.atlasY = outH;
                res.atlasX = outW;
                
                res.rects = rss;
                res.srcImgIdxs = srcImgIdx;
                res.CalcUsedWidthAndHeight();
                rs.Add(res);
                ConvertToRectsWithoutPaddingAndNormalize01(res, paddings[i]);
                if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug(String.Format("Done GetRects "));
            }

            return rs.ToArray();
        }
	}


}