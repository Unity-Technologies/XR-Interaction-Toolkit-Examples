using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;


/*
 TODO
    vertical as well as horizontal
    if images different heights should scale all of them to be the max

Tests For All Texture Packers
    can handle images larger than maxdim
    
*/
 
namespace DigitalOpus.MB.Core{
    // uses this algorithm http://blackpawn.com/texts/lightmaps/
    public class MB2_TexturePackerHorizontalVert : MB2_TexturePacker {
		
        public enum TexturePackingOrientation
        {
            horizontal,
            vertical
        }

        public TexturePackingOrientation packingOrientation;

        public bool stretchImagesToEdges = true;

        public override AtlasPackingResult[] GetRects(List<Vector2> imgWidthHeights, int maxDimensionX, int maxDimensionY, int padding)
        {
            List<AtlasPadding> paddings = new List<AtlasPadding>();
            for (int i = 0; i < imgWidthHeights.Count; i++)
            {
                AtlasPadding p = new AtlasPadding();
                if (packingOrientation == TexturePackingOrientation.horizontal)
                {
                    p.leftRight = 0;
                    p.topBottom = 8;
                } else
                {
                    p.leftRight = 8;
                    p.topBottom = 0;
                }
                paddings.Add(p);
            }
            return GetRects(imgWidthHeights, paddings, maxDimensionX, maxDimensionY, false);
        }

        public override AtlasPackingResult[] GetRects(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionX, int maxDimensionY, bool doMultiAtlas){
            Debug.Assert(imgWidthHeights.Count == paddings.Count);
            int maxPaddingX = 0;
            int maxPaddingY = 0;
            for (int i = 0; i < paddings.Count; i++)
            {
                maxPaddingX = Mathf.Max(maxPaddingX, paddings[i].leftRight);
                maxPaddingY = Mathf.Max(maxPaddingY, paddings[i].topBottom);
            }
            if (doMultiAtlas)
            {
                if (packingOrientation == TexturePackingOrientation.vertical)
                {
                    return _GetRectsMultiAtlasVertical(imgWidthHeights, paddings, maxDimensionX, maxDimensionY, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2);
                } else
                {
                    return _GetRectsMultiAtlasHorizontal(imgWidthHeights, paddings, maxDimensionX, maxDimensionY, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2, 2 + maxPaddingX * 2, 2 + maxPaddingY * 2);
                }
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

        AtlasPackingResult _GetRectsSingleAtlas(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionX, int maxDimensionY, int minImageSizeX, int minImageSizeY, int masterImageSizeX, int masterImageSizeY, int recursionDepth)
        {
            AtlasPackingResult res = new AtlasPackingResult(paddings.ToArray());

            List<Rect> rects = new List<Rect>();
            int extent = 0;
            int maxh = 0;
            int maxw = 0;
            List<Image> images = new List<Image>();
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Packing rects for: " + imgWidthHeights.Count);
            for (int i = 0; i < imgWidthHeights.Count; i++)
            {
                Image im = new Image(i, (int) imgWidthHeights[i].x, (int) imgWidthHeights[i].y, paddings[i], minImageSizeX, minImageSizeY);

                // if images are stacked horizontally then there is no padding at the top or bottom
                if (packingOrientation == TexturePackingOrientation.vertical)
                {
                    im.h -= paddings[i].topBottom * 2;
                    im.x = extent;
                    im.y = 0;
                    rects.Add(new Rect(im.w, im.h, extent, 0));
                    extent += im.w;
                    maxh = Mathf.Max(maxh, im.h);
                } else
                {
                    im.w -= paddings[i].leftRight * 2;
                    im.y = extent;
                    im.x = 0;
                    rects.Add(new Rect(im.w, im.h, 0, extent));
                    extent += im.h;
                    maxw = Mathf.Max(maxw, im.w);
                }
                images.Add(im);
            }
            //scale atlas to fit maxDimension
            Vector2 rootWH;
            if (packingOrientation == TexturePackingOrientation.vertical) { rootWH = new Vector2(extent, maxh); }
            else { rootWH = new Vector2(maxw,extent); }
            int outW = (int) rootWH.x;
            int outH = (int) rootWH.y;
            if (packingOrientation == TexturePackingOrientation.vertical) {
                if (atlasMustBePowerOfTwo)
                {
                    outW = Mathf.Min(CeilToNearestPowerOfTwo(outW), maxDimensionX);
                }
                else
                {
                    outW = Mathf.Min(outW, maxDimensionX);
                }
            } else
            {
                if (atlasMustBePowerOfTwo)
                {
                    outH = Mathf.Min(CeilToNearestPowerOfTwo(outH), maxDimensionY);
                }
                else
                {
                    outH = Mathf.Min(outH, maxDimensionY);
                }
            }

            float padX, padY;
            int newMinSizeX, newMinSizeY;
            if (!ScaleAtlasToFitMaxDim(rootWH, images, maxDimensionX, maxDimensionY, paddings[0], minImageSizeX, minImageSizeY, masterImageSizeX, masterImageSizeY,
                ref outW, ref outH, out padX, out padY, out newMinSizeX, out newMinSizeY))
            {

                res = new AtlasPackingResult(paddings.ToArray());
                res.rects = new Rect[images.Count];
                res.srcImgIdxs = new int[images.Count];
                res.atlasX = outW;
                res.atlasY = outH;
                for (int i = 0; i < images.Count; i++)
                {
                    Image im = images[i];
                    Rect r;
                    if (packingOrientation == TexturePackingOrientation.vertical)
                    {
                        r = res.rects[i] = new Rect((float)im.x / (float)outW + padX,
                                                             (float)im.y / (float)outH,
                                                             (float)im.w / (float)outW - padX * 2f,
                                                             stretchImagesToEdges ? 1f : (float)im.h / (float)outH); // all images are stretched to fill the height
                    } else
                    {
                        r = res.rects[i] = new Rect((float)im.x / (float)outW,
                                                             (float)im.y / (float)outH + padY,
                                                             (stretchImagesToEdges ? 1f : ((float)im.w / (float)outW)),
                                                             (float)im.h / (float)outH - padY * 2f); // all images are stretched to fill the height
                    }
                    res.srcImgIdxs[i] = im.imgId;
                    if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Image: " + i + " imgID=" + im.imgId + " x=" + r.x * outW +
                               " y=" + r.y * outH + " w=" + r.width * outW +
                               " h=" + r.height * outH + " padding=" + paddings[i] + " outW=" + outW + " outH=" + outH);
                }
                res.CalcUsedWidthAndHeight();
                return res;
            }
            Debug.Log("Packing failed returning null atlas result");
            return null;
        }

        AtlasPackingResult[] _GetRectsMultiAtlasVertical(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionPassedX, int maxDimensionPassedY, int minImageSizeX, int minImageSizeY, int masterImageSizeX, int masterImageSizeY)
        {
            List<AtlasPackingResult> rs = new List<AtlasPackingResult>();
            int extent = 0;
            int maxh = 0;
            int maxw = 0;
            
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Packing rects for: " + imgWidthHeights.Count);
            
            List<Image> allImages = new List<Image>();
            for (int i = 0; i < imgWidthHeights.Count; i++)
            {
                Image im = new Image(i, (int)imgWidthHeights[i].x, (int)imgWidthHeights[i].y, paddings[i], minImageSizeX, minImageSizeY);
                im.h -= paddings[i].topBottom * 2;
                allImages.Add(im);
            }
            allImages.Sort(new ImageWidthComparer());
            List<Image> images = new List<Image>();
            List<Rect> rects = new List<Rect>();
            int spaceRemaining = maxDimensionPassedX;
            while (allImages.Count > 0 || images.Count > 0)
            {
                Image im = PopLargestThatFits(allImages, spaceRemaining, maxDimensionPassedX, images.Count == 0);
                if (im == null)
                {
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Atlas filled creating a new atlas ");
                    AtlasPackingResult apr = new AtlasPackingResult(paddings.ToArray());
                    apr.atlasX = maxw;
                    apr.atlasY = maxh;
                    Rect[] rss = new Rect[images.Count];
                    int[] srcImgIdx = new int[images.Count];
                    for (int j = 0; j < images.Count; j++)
                    {
                        Rect r = new Rect(images[j].x, images[j].y, 
                                        images[j].w, 
                                        stretchImagesToEdges ? maxh : images[j].h);
                        rss[j] = r;
                        srcImgIdx[j] = images[j].imgId;
                    }
                    apr.rects = rss;
                    apr.srcImgIdxs = srcImgIdx;
                    apr.CalcUsedWidthAndHeight();
                    images.Clear();
                    rects.Clear();
                    extent = 0;
                    maxh = 0;
                    rs.Add(apr);
                    spaceRemaining = maxDimensionPassedX;
                } else
                {
                    im.x = extent;
                    im.y = 0;
                    images.Add(im);
                    rects.Add(new Rect(extent, 0, im.w, im.h));
                    extent += im.w;
                    maxh = Mathf.Max(maxh, im.h);
                    maxw = extent;
                    spaceRemaining = maxDimensionPassedX - extent;
                }
            }

            for (int i = 0; i < rs.Count; i++)
            {
                int outW = rs[i].atlasX;
                int outH = Mathf.Min(rs[i].atlasY, maxDimensionPassedY);
                if (atlasMustBePowerOfTwo)
                {
                    outW = Mathf.Min(CeilToNearestPowerOfTwo(outW), maxDimensionPassedX);
                }
                else
                {
                    outW = Mathf.Min(outW, maxDimensionPassedX);
                }
                rs[i].atlasX = outW;
                //-------------------------------
                //scale atlas to fit maxDimension
                float padX, padY;
                int newMinSizeX, newMinSizeY;
                ScaleAtlasToFitMaxDim(new Vector2(rs[i].atlasX, rs[i].atlasY), images, maxDimensionPassedX, maxDimensionPassedY, paddings[0], minImageSizeX, minImageSizeY, masterImageSizeX, masterImageSizeY,
                                     ref outW, ref outH, out padX, out padY, out newMinSizeX, out newMinSizeY);
            }

            

            //normalize atlases so that that rects are 0 to 1
            for (int i = 0; i < rs.Count; i++) {
                ConvertToRectsWithoutPaddingAndNormalize01(rs[i], paddings[i]);
                rs[i].CalcUsedWidthAndHeight();
            }
            //-----------------------------
            return rs.ToArray();
        }

        AtlasPackingResult[] _GetRectsMultiAtlasHorizontal(List<Vector2> imgWidthHeights, List<AtlasPadding> paddings, int maxDimensionPassedX, int maxDimensionPassedY, int minImageSizeX, int minImageSizeY, int masterImageSizeX, int masterImageSizeY)
        {
            List<AtlasPackingResult> rs = new List<AtlasPackingResult>();
            int extent = 0;
            int maxh = 0;
            int maxw = 0;

            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Packing rects for: " + imgWidthHeights.Count);

            List<Image> allImages = new List<Image>();
            for (int i = 0; i < imgWidthHeights.Count; i++)
            {
                Image im = new Image(i, (int)imgWidthHeights[i].x, (int)imgWidthHeights[i].y, paddings[i], minImageSizeX, minImageSizeY);
                im.w -= paddings[i].leftRight * 2;
                allImages.Add(im);
            }
            allImages.Sort(new ImageHeightComparer());
            List<Image> images = new List<Image>();
            List<Rect> rects = new List<Rect>();
            int spaceRemaining = maxDimensionPassedY;
            while (allImages.Count > 0 || images.Count > 0)
            {
                Image im = PopLargestThatFits(allImages, spaceRemaining, maxDimensionPassedY, images.Count == 0);
                if (im == null)
                {
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Atlas filled creating a new atlas ");
                    AtlasPackingResult apr = new AtlasPackingResult(paddings.ToArray());
                    apr.atlasX = maxw;
                    apr.atlasY = maxh;
                    Rect[] rss = new Rect[images.Count];
                    int[] srcImgIdx = new int[images.Count];
                    for (int j = 0; j < images.Count; j++)
                    {
                        Rect r = new Rect(images[j].x, images[j].y,
                                stretchImagesToEdges ? maxw : images[j].w,
                                images[j].h);
                        rss[j] = r;
                        srcImgIdx[j] = images[j].imgId;
                    }
                    apr.rects = rss;
                    apr.srcImgIdxs = srcImgIdx;

                    images.Clear();
                    rects.Clear();
                    extent = 0;
                    maxh = 0;
                    rs.Add(apr);
                    spaceRemaining = maxDimensionPassedY;
                }
                else
                {
                    im.x = 0;
                    im.y = extent;
                    images.Add(im);
                    rects.Add(new Rect(0, extent, im.w, im.h));
                    extent += im.h;
                    maxw = Mathf.Max(maxw, im.w);
                    maxh = extent;
                    spaceRemaining = maxDimensionPassedY - extent;
                }
            }

            for (int i = 0; i < rs.Count; i++)
            {
                int outH = rs[i].atlasY;
                int outW = Mathf.Min(rs[i].atlasX, maxDimensionPassedX);
                if (atlasMustBePowerOfTwo)
                {
                    outH = Mathf.Min(CeilToNearestPowerOfTwo(outH), maxDimensionPassedY);
                }
                else
                {
                    outH = Mathf.Min(outH, maxDimensionPassedY);
                }
                rs[i].atlasY = outH;
                //-------------------------------
                //scale atlas to fit maxDimension
                float padX, padY;
                int newMinSizeX, newMinSizeY;
                ScaleAtlasToFitMaxDim(new Vector2(rs[i].atlasX, rs[i].atlasY), images, maxDimensionPassedX, maxDimensionPassedY, paddings[0], minImageSizeX, minImageSizeY, masterImageSizeX, masterImageSizeY,
                                     ref outW, ref outH, out padX, out padY, out newMinSizeX, out newMinSizeY);
            }



            //normalize atlases so that that rects are 0 to 1
            for (int i = 0; i < rs.Count; i++)
            {
                ConvertToRectsWithoutPaddingAndNormalize01(rs[i], paddings[i]);
                rs[i].CalcUsedWidthAndHeight();
            }
            //-----------------------------
            return rs.ToArray();
        }

        Image PopLargestThatFits(List<Image> images, int spaceRemaining, int maxDim, bool emptyAtlas)
        {
            //pop single images larger than maxdim into their own atlas
            int imageDim;
            if (images.Count == 0)
            {
                return null;
            } 

            if (packingOrientation == TexturePackingOrientation.vertical)
            {
                imageDim = images[0].w;
            } else
            {
                imageDim = images[0].h;
            }
            if (images.Count > 0 && imageDim >= maxDim)
            {
                if (emptyAtlas)
                {
                    Image im = images[0];
                    images.RemoveAt(0);
                    return im;
                } else
                {
                    return null;
                }
            }

            // now look for images that will fit
            int i = 0;
            while (i < images.Count && imageDim >= spaceRemaining)
            {
                i++;
            }
            if (i < images.Count)
            {
                Image im = images[i];
                images.RemoveAt(i);
                return im;
            } else
            {
                return null;
            }
        }
    }

}