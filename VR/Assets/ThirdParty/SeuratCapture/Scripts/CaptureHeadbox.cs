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
#if UNITY_EDITOR
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System;
using System.IO;

public enum CubeFaceResolution
{
  k512 = 512,
  k1024 = 1024,
  k1536 = 1536,
  k2048 = 2048,
  k4096 = 4096,
  k8192 = 8192
}

public enum PositionSampleCount {
  k2 = 2,
  k4 = 4,
  k8 = 8,
  k16 = 16,
  k32 = 32,
  k64 = 64,
  k128 = 128,
  k256 = 256,
}

public enum CaptureDynamicRange {
  // Standard (or low) dynamic range, e.g. sRGB.
  kSDR = 0,
  // High dynamic range with medium precision floating point data; requires half float render targets.
  kHDR16 = 1,
  // High dynamic range with full float precision render targets.
  kHDR = 2,
}

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CaptureHeadbox : MonoBehaviour {
  // -- Capture Settings --

  [Tooltip("The dimensions of the headbox.")]
  public Vector3 size_ = Vector3.one;
  [Tooltip("The number of samples per face of the headbox.")]
  public PositionSampleCount samples_per_face_ = PositionSampleCount.k32;
  [Tooltip("The resolution of the center image, taken at the camera position at the center of the headbox. This should be 4x higher than the resolution of the remaining samples, for antialiasing.")]
  public CubeFaceResolution center_resolution_ = CubeFaceResolution.k4096;
  [Tooltip("The resolution of all samples other than the center.")]
  public CubeFaceResolution resolution_ = CubeFaceResolution.k1024;

  [Tooltip("Capture in standard (SDR) or high dynamic range (HDR). HDR requires floating-point render targets, the Camera Component have allow HDR enabled, and enables EXR output.")]
  public CaptureDynamicRange dynamic_range_ = CaptureDynamicRange.kSDR;

  // -- Processing Settings --

  [Tooltip("Root destination folder for capture data; empty instructs the capture to use an automatically-generated, unique folder in the project temp folder.")]
  public string output_folder_ = "";

  // Indicates location of most-recent capture artifacts.
  public string last_output_dir_;

  private Camera color_camera_;
  private CaptureBuilder capture_;

  public Camera ColorCamera {
    get {
      if (color_camera_ == null) {
        color_camera_ = GetComponent<Camera>();
      }
      return color_camera_;
    }
  }

  void Update() {
    if (IsCapturing()) {
      RunCapture();
    }

    if (Input.GetKeyDown(KeyCode.BackQuote)) {
      ToggleCaptureMode();
    }
  }

  bool IsCapturing() {
    return capture_ != null;
  }

  void RunCapture() {
    Debug.Log("Capturing headbox samples...", this);
    capture_.CaptureAllHeadboxSamples();
    if (capture_.IsCaptureComplete()) {
      StopCapture();
    }
  }

  void ToggleCaptureMode() {
    if (IsCapturing()) {
      StopCapture();
    } else {
      StartCapture();
    }
  }

  void StartCapture() {
    Debug.Log("Capture start - temporarily setting fixed framerate.", this);
    capture_ = new CaptureBuilder();

    string capture_output_folder = output_folder_;
    if (capture_output_folder.Length <= 0) {
      capture_output_folder = FileUtil.GetUniqueTempPathInProject();
    }
    Directory.CreateDirectory(capture_output_folder);
    capture_.BeginCapture(this, capture_output_folder, 1, new CaptureStatus());

    // See Time.CaptureFramerate example, e.g. here:
    // https://docs.unity3d.com/ScriptReference/Time-captureFramerate.html
    Time.captureFramerate = 60;
  }

  void StopCapture() {
    Debug.Log("Capture stop", this);
    if (capture_ != null) {
      capture_.EndCapture();
    }
    capture_ = null;
    Time.captureFramerate = 0;
  }

  void OnDrawGizmos()
  {
    // The headbox is defined in camera coordinates.
    Gizmos.matrix = transform.localToWorldMatrix;
    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(Vector3.zero, size_);
  }
}
#endif