/*
Copyright 2017 Google Inc. All Rights Reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using System.Collections.Generic;
using System;

// Defines types and functions to generate a JSON manifest for a Seurat
// capture.
public class JsonManifest {
  static public float[] MatrixToArray(Matrix4x4 other) {
    float[] elements = new float[16];
    for (int j = 0; j < 4; j++) {
      for (int i = 0; i < 4; i++) {
        elements[j * 4 + i] = other[j, i];
      }
    }
    return elements;
  }

  [Serializable]
  public struct Image4File {
    public string path;
    public string channel_0;
    public string channel_1;
    public string channel_2;
    public string channel_alpha;
  }

  [Serializable]
  public struct Image1File {
    public string path;
    public string channel_0;
  }

  // Indicates storage of one RGBAD image.
  [Serializable]
  public struct DepthImageFile {
    public Image4File color;
    public Image1File depth;
  }

  [Serializable]
  public struct ProjectiveCamera {
    public int image_width;
    public int image_height;
    public float[] clip_from_eye_matrix;
    public float[] world_from_eye_matrix;
    public string depth_type;
  }

  [Serializable]
  public struct View {
    public ProjectiveCamera projective_camera;
    public DepthImageFile depth_image_file;
  }

  [Serializable]
  public struct ViewGroup {
    public View[] views;
  }

  [Serializable]
  public class Capture {
    public List<ViewGroup> view_groups = new List<ViewGroup>();
  }
}
