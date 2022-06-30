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
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering.Universal;

// Defines notification interface for various Seurat pipeline tasks.
public class CaptureStatus {
  public virtual void SendProgress(string message, float fraction_complete) {
  }
  public virtual bool TaskContinuing() {
    return true;
  }
}

// Implements image capture with a state machine, to allow incremental capture
// with an interactive GUI.
public class CaptureBuilder {

  // -- Capture Rendering data. --

  Camera color_camera_;
  Camera depth_camera_;
  GameObject depth_camera_object_;
  // Captures data from the render device and provides access to the pixels for
  // export to disk.
  Texture2D texture_;
  Texture2D texture_fp16_;
  Texture2D texture_fp32_;
  // The capture process renders to these render texture targets then reads back
  // into the various Texture2D objects.
  RenderTexture color_render_texture_;
  RenderTexture depth_render_texture_;
  // Overrides shaders defined in the scene to capture eye space depth as
  // shader output.
  Shader render_depth_shader_;
  // Provides capture settings.
  CaptureHeadbox headbox_;
  // Indicates capture precision.
  CaptureDynamicRange dynamic_range_;
  // Stores the count of camera samples taken inside the headbox; each sample
  // requires an image per face.
  int samples_per_face_;
  // Indicates root storage location for capture images and manifest.
  string capture_dir_;
  // Defines the capture sample positions distributed throughout the headbox in
  // world space.
  List<Vector3> samples_;

  // -- Incremental capture state machine members. --

  // Current frame of the capture. Always 0 for static captures, relative to
  // max_frames_ for animation capture.
  int capture_frame_;
  // Total number of frames being captured; always 1 for static captures.
  int max_frames_ = 1;
  // Current sample of the capture.
  int sample_index_;
  // Current cube face direction being captured.
  int current_side_;
  // The view group of the current sample of the current frame accumulates each
  // face rendering as the capture progresses.
  JsonManifest.ViewGroup view_group_;
  // Accumulates the view groups as capture progresses.
  JsonManifest.Capture capture_manifest_;

  // Receives status reports as the capture progresses and provides cancellation
  // signal.
  CaptureStatus status_interface_;

  // Per frame or per sample capture output path.
  string export_path_;
  float start_time_;
  float start_sample_time_;


  public bool IsCaptureComplete() {
    return capture_frame_ >= max_frames_;
  }

  public bool IsHighDynamicRange() {
    return dynamic_range_ != CaptureDynamicRange.kSDR;
  }

  RenderTextureFormat RenderTargetFormatFromDynamicRange() {
    RenderTextureFormat format;
    switch (dynamic_range_) {
      case CaptureDynamicRange.kHDR16:
        format = RenderTextureFormat.ARGBHalf;
        break;

      case CaptureDynamicRange.kHDR:
        format = RenderTextureFormat.ARGBHalf;
        break;

      default:
      case CaptureDynamicRange.kSDR:
        format = RenderTextureFormat.ARGB32;
        break;
    }
    return format;
  }

  Texture2D Texture2DFromDynamicRange() {
    Texture2D texture_for_dynamic_range;
    switch (dynamic_range_) {
      case CaptureDynamicRange.kHDR16:
        texture_for_dynamic_range = texture_fp16_;
        break;

      case CaptureDynamicRange.kHDR:
        texture_for_dynamic_range = texture_fp32_;
        break;

      default:
      case CaptureDynamicRange.kSDR:
        texture_for_dynamic_range = texture_;
        break;
    }
    return texture_for_dynamic_range;
  }

  private static string PathCombine(params string[] parts)
  {
    if (parts.Length == 0) return "";

    string result = parts[0];
    for (int i = 1; i < parts.Length; i++)
    {
      result = Path.Combine(result, parts[i]);
    }
    return result;
  }

  // Computes the radical inverse base |digit_base| of the given value |a|.
  private static float RadicalInverse(ulong a, ulong digit_base) {
    float inv_base = 1.0f / digit_base;
    ulong reversed_digits = 0;
    float inv_base_n = 1.0f;
    // Compute the reversed digits in the base entirely in integer arithmetic.
    while (a != 0) {
      ulong next = a / digit_base;
      ulong digit = a - next * digit_base;
      reversed_digits = reversed_digits * digit_base + digit;
      inv_base_n *= inv_base;
      a = next;
    }
    // Only when done are the reversed digits divided by base^n.
    return Mathf.Min(reversed_digits * inv_base_n, 1.0f);
  }

  public void BeginCapture(CaptureHeadbox headbox, string capture_dir, int max_frames, CaptureStatus status_interface) {
    start_time_ = Time.realtimeSinceStartup;

    headbox_ = headbox;
    dynamic_range_ = headbox_.dynamic_range_;
    samples_per_face_ = (int)headbox_.samples_per_face_;
    capture_dir_ = capture_dir;
    capture_frame_ = 0;
    status_interface_ = status_interface;
    max_frames_ = max_frames;
    status_interface_.SendProgress("Capturing Images...", 0.0f);
    List<Vector3> samples = new List<Vector3>();

    // Use Hammersly point set to distribute samples.
    for (int position_sample_index = 0; position_sample_index < samples_per_face_; ++position_sample_index)
    {
      Vector3 headbox_position = new Vector3(
        (float)position_sample_index / (float)(samples_per_face_ - 1),
        RadicalInverse((ulong)position_sample_index, 2),
        RadicalInverse((ulong)position_sample_index, 3));
      headbox_position.Scale(headbox.size_);
      headbox_position -= headbox.size_ * 0.5f;
      // Headbox samples are in camera space; transform to world space.
      headbox_position = headbox.transform.TransformPoint(headbox_position);
      samples.Add(headbox_position);
    }

    // Sort samples by distance from center of the headbox.
    samples.Sort(delegate (Vector3 a, Vector3 b) {
      float length_a = a.sqrMagnitude;
      float length_b = b.sqrMagnitude;
      return length_a.CompareTo(length_b);
    });
    // Replace the sample closest to the center of the headbox with a sample at
    // exactly the center. This is important because Seurat requires
    // sampling information at the center of the headbox.
    samples[0] = headbox.transform.position;

    samples_ = samples;
    // Note this uses a modified version of Unity's standard internal depth
    // capture shader. See the shader in Assets/builtin_shaders/
    // DefaultResourcesExtra/Internal-DepthNormalsTexture.shader.
    render_depth_shader_ = Shader.Find("GoogleVR/Seurat/CaptureEyeDepth");

    capture_manifest_ = new JsonManifest.Capture();

    // Setup cameras
    color_camera_ = headbox_.ColorCamera;

    depth_camera_object_ = new GameObject("Depth Camera");
    depth_camera_ = depth_camera_object_.AddComponent<Camera>();
    depth_camera_object_.AddComponent<UniversalAdditionalCameraData>();
  }

  public void EndCapture()
  {
    if (capture_manifest_ != null)
    {
      string json_data = JsonUtility.ToJson(capture_manifest_, true);
      File.WriteAllText(PathCombine(export_path_, "manifest.json"), json_data);
      capture_manifest_ = null;

      GameObject.DestroyImmediate(depth_camera_object_);
      color_camera_ = null;

      Debug.Log("Total Capture time: " + (Time.realtimeSinceStartup - start_time_ + " seconds."));

      DestroyRenderTargets();
    }
  }

  public void StartCaptureSamples()
  {
    export_path_ = capture_dir_;
    if (max_frames_ > 1)
    {
      // When capturing animation, make a directory per frame.
      export_path_ = export_path_ + "/frame_" + capture_frame_.ToString() + "/";
      Directory.CreateDirectory(export_path_);
    }

    start_sample_time_ = Time.realtimeSinceStartup;
  }

  public void RunCapture()
  {
    // Setup cameras; save, modify, and restore camera settings around each
    // captured face.
    color_camera_ = headbox_.ColorCamera;

    float original_aspect = color_camera_.aspect;
    float original_fov = color_camera_.fieldOfView;
    int original_culling_mask = color_camera_.cullingMask;

    if (color_render_texture_ == null) {
      BuildRenderTargets();
    }

    color_camera_.targetTexture = color_render_texture_;
    color_camera_.fieldOfView = 90f;
    color_camera_.aspect = 1f;
    // Propagate settings to the depth camera.
    depth_camera_.CopyFrom(color_camera_);
    depth_camera_.allowHDR = IsHighDynamicRange();
    depth_camera_.targetTexture = depth_render_texture_;
    depth_camera_.renderingPath = RenderingPath.Forward;
    depth_camera_.clearFlags = CameraClearFlags.Color;
    depth_camera_.backgroundColor = new Color(0f, 0f, 0f, 0f);

    CaptureSample();

    color_camera_.GetComponent<UniversalAdditionalCameraData>().SetRenderer(0);
    color_camera_.targetTexture = null;
    color_camera_.aspect = original_aspect;
    color_camera_.fieldOfView = original_fov;
    color_camera_.cullingMask = original_culling_mask;
  }

  private void CaptureSample()
  {
    // Transforms all cameras from world space to the eye space
    // of the reference camera.
    Matrix4x4 reference_from_world = color_camera_.worldToCameraMatrix;
    const string base_image_name = "Cube";

    string[] cube_face_names = {
      "Front",
      "Back",
      "Right",
      "Left",
      "Top",
      "Bottom",
    };

    int num_sides = cube_face_names.Length;
    if (current_side_ == 0)
    {
      StartCaptureSamples();
      view_group_ = new JsonManifest.ViewGroup();
      view_group_.views = new JsonManifest.View[6];
    }

    int side = current_side_;
    Quaternion face_rotation;

    switch (side)
    {
      case 0:
        face_rotation = Quaternion.identity;
        break;
      case 1:
        face_rotation = Quaternion.AngleAxis(180f, Vector3.up);
        break;
      case 2:
        face_rotation = Quaternion.AngleAxis(90f, Vector3.up);
        break;
      case 3:
        face_rotation = Quaternion.AngleAxis(-90f, Vector3.up);
        break;
      case 4:
        face_rotation = Quaternion.AngleAxis(-90f, Vector3.right);
        break;
      case 5:
      default:
        face_rotation = Quaternion.AngleAxis(90f, Vector3.right);
        break;
    }

    string progress_status = "Baking " + (sample_index_ + 1) + "/ " + samples_per_face_ + " Frame " + (capture_frame_ + 1) + "/" + max_frames_;
    int capture_task_index = sample_index_ * num_sides + side;
    int total_capture_tasks = samples_per_face_ * num_sides * max_frames_;
    status_interface_.SendProgress(progress_status,
      (float)capture_task_index / total_capture_tasks);
    if (!status_interface_.TaskContinuing())
    {
      return;
    }

    // Use cached samples
    JsonManifest.View view = Capture(
      base_image_name + "_" + cube_face_names[side] + "_" + sample_index_,
      face_rotation,
      samples_[sample_index_],
      reference_from_world,
      export_path_);

    // Shows the task is complete.
    status_interface_.SendProgress(progress_status,
      (float)(capture_task_index + 1) / total_capture_tasks);

    switch (side)
    {
      case 0:
        view_group_.views[0] = view;
        break;
      case 1:
        view_group_.views[1] = view;
        break;
      case 2:
        view_group_.views[3] = view;
        break;
      case 3:
        view_group_.views[2] = view;
        break;
      case 4:
        view_group_.views[5] = view;
        break;
      case 5:
      default:
        view_group_.views[4] = view;
        break;
    }

    ++current_side_;
    if (current_side_ == num_sides)
    {
      if (sample_index_ == 0) {
        // Forces recreation of render targets at the normal resolution after
        // capturing the center headbox at the typically-higher resolution.
        DestroyRenderTargets();
      }

      current_side_ = 0;
      capture_manifest_.view_groups.Add(view_group_);
      EndCaptureSample();
    }
  }

  public void EndCaptureSample()
  {
    Debug.Log("Sample Capture time: " + (Time.realtimeSinceStartup - start_sample_time_ + " seconds."));
    ++sample_index_;
    if (sample_index_ >= samples_per_face_) {
      // Go to next frame.
      sample_index_ = 0;
      ++capture_frame_;
    }
  }

  public void EndCaptureFrame()
  {
    ++capture_frame_;
  }

  public void CaptureAllHeadboxSamples()
  {
    if (!status_interface_.TaskContinuing())
    {
      return;
    }

    // Iterate the capture statemachine to acquire all samples for this frame.
    for (int position_sample_index = 0; position_sample_index < samples_per_face_;
      ++position_sample_index) {
      if (!status_interface_.TaskContinuing()) {
        break;
      }
      RunCapture();
    }
  }

  private JsonManifest.View Capture(
    string base_image_name,
    Quaternion orientation,
    Vector3 position,
    Matrix4x4 reference_from_world,
    string export_path)
  {

    // Save initial camera state
    Vector3 initial_camera_position = color_camera_.transform.position;
    Quaternion initial_camera_rotation = color_camera_.transform.rotation;

    // Setup cameras
    color_camera_.transform.position = position;
    color_camera_.transform.rotation = orientation;
    depth_camera_.transform.position = position;
    depth_camera_.transform.rotation = orientation;

    // Write out color data
    string color_image_name = base_image_name + "_Color." +
      (IsHighDynamicRange() ? "exr" : "png");
    color_camera_.GetComponent<UniversalAdditionalCameraData>().SetRenderer(0);
    color_camera_.targetTexture = color_render_texture_;
    color_camera_.Render();
    WriteImage(color_render_texture_, Texture2DFromDynamicRange(), PathCombine(export_path, color_image_name), true);

    // Write out depth data
    string depth_image_name = base_image_name + "_Depth.exr";
    depth_camera_.GetComponent<UniversalAdditionalCameraData>().SetRenderer(1);
    depth_camera_.targetTexture = depth_render_texture_;
    depth_camera_.Render();
    WriteImage(depth_render_texture_, texture_fp32_, PathCombine(export_path, depth_image_name), false);

    // Record the capture results.
    JsonManifest.View view = new JsonManifest.View();
    view.projective_camera.image_width = color_render_texture_.width;
    view.projective_camera.image_height = color_render_texture_.height;
    view.projective_camera.clip_from_eye_matrix = JsonManifest.MatrixToArray(color_camera_.projectionMatrix);
    view.projective_camera.world_from_eye_matrix = JsonManifest.MatrixToArray(reference_from_world * color_camera_.cameraToWorldMatrix);
    view.projective_camera.depth_type = "EYE_Z";
    view.depth_image_file.color.path = color_image_name;
    view.depth_image_file.color.channel_0 = "R";
    view.depth_image_file.color.channel_1 = "G";
    view.depth_image_file.color.channel_2 = "B";
    view.depth_image_file.color.channel_alpha = "CONSTANT_ONE";
    view.depth_image_file.depth.path = depth_image_name;
    view.depth_image_file.depth.channel_0 = "R";

    // Restore camera state
    color_camera_.transform.position = initial_camera_position;
    color_camera_.transform.rotation = initial_camera_rotation;

    return view;
  }

  private static void WriteImage(RenderTexture render_texture, Texture2D texture, string image_path, bool clear_alpha_to_one) {
    RenderTexture.active = render_texture;
    texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
    if (clear_alpha_to_one) {
      Color[] pixels = texture.GetPixels();
      int num_pixels = pixels.Length;
      for (int pixel = 0; pixel < num_pixels; ++pixel) {
        pixels [pixel].a = 1f;
      }
      texture.SetPixels(pixels);
    }
    texture.Apply();
    if (texture.format == TextureFormat.RGBAHalf
        || texture.format == TextureFormat.RGBAFloat
        || texture.format == TextureFormat.RFloat) {
      byte[] exr = texture.EncodeToEXR(texture.format == TextureFormat.RGBAHalf
          ? Texture2D.EXRFlags.None : (Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP));
      File.WriteAllBytes(image_path, exr);
    } else {
      byte[] png = texture.EncodeToPNG();
      File.WriteAllBytes(image_path, png);
    }
    RenderTexture.active = null;
  }

  private void BuildRenderTargets() {
    // Create scratch textures
    int resolution = (int)(sample_index_ == 0 ? headbox_.center_resolution_ : headbox_.resolution_);
    int depth_bits = 24;
    // Note this reads in linear or sRGB depending on project settings.
    color_render_texture_ = new RenderTexture(resolution, resolution, depth_bits, RenderTargetFormatFromDynamicRange());
    depth_render_texture_ = new RenderTexture(resolution, resolution, depth_bits, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
    color_render_texture_.autoGenerateMips = false;
    depth_render_texture_.autoGenerateMips = false;
    texture_ = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
    texture_fp16_ = new Texture2D(resolution, resolution, TextureFormat.RGBAHalf, false);
    texture_fp32_ = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
  }

  private void DestroyRenderTargets() {
    color_render_texture_.Release();
    color_render_texture_ = null;
    depth_render_texture_.Release();
    depth_render_texture_ = null;
    texture_ = null;
    texture_fp16_ = null;
    texture_fp32_ = null;
  }
}
#endif