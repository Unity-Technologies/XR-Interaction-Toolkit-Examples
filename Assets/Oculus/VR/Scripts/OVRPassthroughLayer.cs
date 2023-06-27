/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ColorMapType = OVRPlugin.InsightPassthroughColorMapType;

/// <summary>
/// A layer used for passthrough.
/// </summary>
public class OVRPassthroughLayer : MonoBehaviour
{
    #region Public Interface

    /// <summary>
    /// The passthrough projection surface type: reconstructed | user defined.
    /// </summary>
    public enum ProjectionSurfaceType
    {
        // Reconstructed surface type will render passthrough using automatic environment depth reconstruction
        Reconstructed,

        /// UserDefined allows you to define a surface
        UserDefined
    }

    /// <summary>
    /// The type of the surface which passthrough textures are projected on: Automatic reconstruction or user-defined geometry.
    /// This field can only be modified immediately after the component is instantiated (e.g. using `AddComponent`).
    /// Once the backing layer has been created, changes won't be reflected unless the layer is disabled and enabled again.
    /// Default is automatic reconstruction.
    /// </summary>
    public ProjectionSurfaceType projectionSurfaceType = ProjectionSurfaceType.Reconstructed;

    /// <summary>
    /// Overlay type that defines the placement of the passthrough layer to appear on top as an overlay or beneath as an underlay of the application’s main projection layer. By default, the passthrough layer appears as an overlay.
    /// </summary>
    public OVROverlay.OverlayType overlayType = OVROverlay.OverlayType.Overlay;

    /// <summary>
    /// The compositionDepth defines the order of the layers in composition. The layer with smaller compositionDepth would be composited in the front of the layer with larger compositionDepth. The default value is zero.
    /// </summary>
    public int compositionDepth = 0;

    /// <summary>
    /// Property that can hide layers when required. Should be false when present, true when hidden. By default, the value is set to false, which means the layers are present.
    /// </summary>
    public bool hidden = false;


    /// <summary>
    /// Specify whether `colorScale` and `colorOffset` should be applied to this layer. By default, the color scale and offset are not applied to the layer.
    /// </summary>
    public bool overridePerLayerColorScaleAndOffset = false;

    /// <summary>
    /// Color scale is a factor applied to the pixel color values during compositing.
    /// The four components of the vector correspond to the R, G, B, and A values, default set to `{1,1,1,1}`.
    /// </summary>
    public Vector4 colorScale = Vector4.one;

    /// <summary>
    /// Color offset is a value which gets added to the pixel color values during compositing.
    /// The four components of the vector correspond to the R, G, B, and A values, default set to `{0,0,0,0}`.
    /// </summary>
    public Vector4 colorOffset = Vector4.zero;

    /// <summary>
    /// Add a GameObject to the Insight Passthrough projection surface. This is only applicable
    /// if the projection surface type is `UserDefined`.
    /// When `updateTransform` parameter is set to `true`, OVRPassthroughLayer will update the transform
    /// of the surface mesh every frame. Otherwise only the initial transform is recorded.
    /// </summary>
    /// <param name="obj">The Gameobject you want to add to the Insight Passthrough projection surface.</param>
    /// <param name="updateTransform">Indicate if the transform should be updated every frame</param>
    public void AddSurfaceGeometry(GameObject obj, bool updateTransform = false)
    {
        if (projectionSurfaceType != ProjectionSurfaceType.UserDefined)
        {
            Debug.LogError("Passthrough layer is not configured for surface projected passthrough.");
            return;
        }

        if (surfaceGameObjects.ContainsKey(obj))
        {
            Debug.LogError("Specified GameObject has already been added as passthrough surface.");
            return;
        }

        if (obj.GetComponent<MeshFilter>() == null)
        {
            Debug.LogError("Specified GameObject does not have a mesh component.");
            return;
        }

        // Mesh and instance can't be created immediately, because the compositor layer may not have been initialized yet (layerId = 0).
        // Queue creation and attempt to do it in the update loop.
        deferredSurfaceGameObjects.Add(
            new DeferredPassthroughMeshAddition
            {
                gameObject = obj,
                updateTransform = updateTransform
            });
    }

    /// <summary>
    /// Removes a GameObject that was previously added using `AddSurfaceGeometry` from the projection surface.
    /// </summary>
    /// <param name="obj">The Gameobject to remove. </param>
    public void RemoveSurfaceGeometry(GameObject obj)
    {
        PassthroughMeshInstance passthroughMeshInstance;
        if (surfaceGameObjects.TryGetValue(obj, out passthroughMeshInstance))
        {
            if (OVRPlugin.DestroyInsightPassthroughGeometryInstance(passthroughMeshInstance.instanceHandle) &&
                OVRPlugin.DestroyInsightTriangleMesh(passthroughMeshInstance.meshHandle))
            {
                surfaceGameObjects.Remove(obj);
            }
            else
            {
                Debug.LogError("GameObject could not be removed from passthrough surface.");
            }
        }
        else
        {
            int count = deferredSurfaceGameObjects.RemoveAll(x => x.gameObject == obj);
            if (count == 0)
            {
                Debug.LogError("Specified GameObject has not been added as passthrough surface.");
            }
        }
    }

    /// <summary>
    /// Checks if the given gameobject is a surface geometry (If called with AddSurfaceGeometry).
    /// </summary>
    /// <returns> True if the gameobject is a surface geometry. </returns>
    public bool IsSurfaceGeometry(GameObject obj)
    {
        return surfaceGameObjects.ContainsKey(obj) || deferredSurfaceGameObjects.Exists(x => x.gameObject == obj);
    }

    /// <summary>
    /// Float that defines the passthrough texture opacity.
    /// </summary>
    public float textureOpacity
    {
        get { return textureOpacity_; }
        set
        {
            if (value != textureOpacity_)
            {
                textureOpacity_ = value;
                styleDirty = true;
            }
        }
    }

    /// <summary>
    /// Enable or disable the Edge rendering.
    /// Use this flag to enable or disable the edge rendering but retain the previously selected color (incl. alpha)
    /// in the UI when it is disabled.
    /// </summary>
    public bool edgeRenderingEnabled
    {
        get { return edgeRenderingEnabled_; }
        set
        {
            if (value != edgeRenderingEnabled_)
            {
                edgeRenderingEnabled_ = value;
                styleDirty = true;
            }
        }
    }

    /// <summary>
    /// Color for the edge rendering.
    /// </summary>
    public Color edgeColor
    {
        get { return edgeColor_; }
        set
        {
            if (value != edgeColor_)
            {
                edgeColor_ = value;
                styleDirty = true;
            }
        }
    }

    /// <summary>
    /// This color map method allows to recolor the grayscale camera images by specifying a color lookup table.
    /// Scripts should call the designated methods to set a color map. The fields and properties
    /// are only intended for the inspector UI.
    /// </summary>
    /// <param name="values">The color map as an array of 256 color values to map each grayscale input to a color.</param>
    public void SetColorMap(Color[] values)
    {
        if (values.Length != 256)
            throw new ArgumentException("Must provide exactly 256 colors");

        colorMapType = ColorMapType.MonoToRgba;
        colorMapEditorType = ColorMapEditorType.Custom;
        _stylesHandler.SetMonoToRgbaHandler(values);
        styleDirty = true;
    }


    /// <summary>
    /// Applies a color LUT to the passthrough layer.
    /// This is an experimental feature, please see
    /// https://developer.oculus.com/experimental/experimental-overview/ for more information
    /// and ensure that Experimental Features are enabled in OVRManager - Quest Features - Experimental.
    /// </summary>
    /// <param name="lut"></param>
    /// <param name="weight">Value between 0 and 1 which defines the blend between lut and passthrough</param>
    public void SetColorLut(OVRPassthroughColorLut lut, float weight = 1)
    {
        if (lut != null && lut.IsInitialized)
        {
            weight = ClampWeight(weight);
            colorMapType = ColorMapType.ColorLut;
            colorMapEditorType = ColorMapEditorType.Custom;
            _stylesHandler.SetColorLutHandler(lut, weight);
            styleDirty = true;
        }
        else
        {
            Debug.LogError("Trying to set an uninitialized Color LUT for Passthrough");
        }
    }

    /// <summary>
    /// Applies the interpolation between two color LUTs to the passthrough layer.
    /// This is an experimental feature, please see
    /// https://developer.oculus.com/experimental/experimental-overview/ for more information.
    /// </summary>
    /// <param name="lutSource"></param>
    /// <param name="lutTarget"></param>
    /// <param name="weight">Value between 0 and 1 which defines the blend between lutSource and lutTarget</param>
    public void SetColorLut(OVRPassthroughColorLut lutSource, OVRPassthroughColorLut lutTarget, float weight)
    {
        if (lutSource != null && lutSource.IsInitialized
                              && lutTarget != null && lutTarget.IsInitialized)
        {
            weight = ClampWeight(weight);
            colorMapType = ColorMapType.InterpolatedColorLut;
            colorMapEditorType = ColorMapEditorType.Custom;
            _stylesHandler.SetInterpolatedColorLutHandler(lutSource, lutTarget, weight);
            styleDirty = true;
        }
        else
        {
            Debug.LogError("Trying to set an uninitialized Color LUT for Passthrough");
        }
    }

    /// <summary>
    /// This method allows to generate (and apply) a color map from the set of controls which is also available in
    /// inspector.
    /// </summary>
    /// <param name="contrast">The contrast value. Range from -1 (minimum) to 1 (maximum). </param>
    /// <param name="brightness">The brightness value. Range from 0 (minimum) to 1 (maximum). </param>
    /// <param name="posterize">The posterize value. Range from 0 to 1, where 0 = no posterization (no effect), 1 = reduce to two colors. </param>
    /// <param name="gradient">The gradient will be evaluated from 0 (no intensity) to 1 (maximum intensity).
    /// This parameter only has an effect if `colorMapType` is `GrayscaleToColor`.</param>
    /// <param name="colorMapType">Type of color map which should be generated. Supported values: `Grayscale` and `GrayscaleToColor`.</param>
    public void SetColorMapControls(
        float contrast,
        float brightness = 0.0f,
        float posterize = 0.0f,
        Gradient gradient = null,
        ColorMapEditorType colorMapType = ColorMapEditorType.GrayscaleToColor)
    {
        if (!(colorMapType == ColorMapEditorType.Grayscale || colorMapType == ColorMapEditorType.GrayscaleToColor))
        {
            Debug.LogError("Unsupported color map type specified");
            return;
        }

        colorMapEditorType = colorMapType;
        colorMapEditorContrast = contrast;
        colorMapEditorBrightness = brightness;
        colorMapEditorPosterize = posterize;

        if (colorMapType == ColorMapEditorType.GrayscaleToColor)
        {
            if (gradient != null)
            {
                colorMapEditorGradient = gradient;
            }
            else if (!colorMapEditorGradient.Equals(colorMapNeutralGradient))
            {
                // Leave gradient untouched if it's already neutral to avoid unnecessary memory allocations.
                colorMapEditorGradient = CreateNeutralColorMapGradient();
            }
        }
        else if (gradient != null)
        {
            Debug.LogWarning("Gradient parameter is ignored for color map types other than GrayscaleToColor");
        }
    }

    /// <summary>
    /// This method allows to specify the color map as an array of 256 8-bit intensity values.
    /// Use this to map each grayscale input value to a grayscale output value.
    /// </summary>
    /// <param name="values">Array of 256 8-bit values.</param>
    public void SetColorMapMonochromatic(byte[] values)
    {
        if (values.Length != 256)
            throw new ArgumentException("Must provide exactly 256 values");

        colorMapType = ColorMapType.MonoToMono;
        colorMapEditorType = ColorMapEditorType.Custom;
        _stylesHandler.SetMonoToMonoHandler(values);
        styleDirty = true;
    }

    /// <summary>
    /// This method allows to configure brightness and contrast adjustment for Passthrough images.
    /// </summary>
    /// <param name="brightness">Modify the brightness of Passthrough. Valid range: [-1, 1]. A
    ///   value of 0 means that brightness is left unchanged.</param>
    /// <param name="contrast">Modify the contrast of Passthrough. Valid range: [-1, 1]. A value of 0
    ///   means that contrast is left unchanged.</param>
    /// <param name="saturation">Modify the saturation of Passthrough. Valid range: [-1, 1]. A value
    ///   of 0 means that saturation is left unchanged.</param>
    public void SetBrightnessContrastSaturation(float brightness = 0.0f, float contrast = 0.0f, float saturation = 0.0f)
    {
        colorMapType = ColorMapType.BrightnessContrastSaturation;
        colorMapEditorType = ColorMapEditorType.ColorAdjustment;

        colorMapEditorBrightness = brightness;
        colorMapEditorContrast = contrast;
        colorMapEditorSaturation = saturation;

        UpdateColorMapFromControls();
    }


    /// <summary>
    /// Disables color mapping. Use this to remove any effects.
    /// </summary>
    public void DisableColorMap()
    {
        colorMapEditorType = ColorMapEditorType.None;
    }

    #endregion


    #region Editor Interface

    /// <summary>
    /// Unity editor enumerator to provide a dropdown in the inspector.
    /// </summary>
    public enum ColorMapEditorType
    {
        // No color map is applied
        None = 0,

        // Map input color to an RGB color, optionally with brightness/constrast adjustment or posterization applied.
        GrayscaleToColor = 1,

        // Deprecated - use GrayscaleToColor instead.
        Controls = GrayscaleToColor,

        // Color map is specified using one of the class setters.
        Custom = 2,

        // Map input color to a grayscale color, optionally with brightness/constrast adjustment or posterization applied.
        Grayscale = 3,

        // Adjust brightness and contrast
        ColorAdjustment = 4,

        ColorLut = 5,
        InterpolatedColorLut = 6
    }

    [SerializeField]
    internal ColorMapEditorType colorMapEditorType_ = ColorMapEditorType.None;

    private static Dictionary<ColorMapEditorType, ColorMapType> _editorToColorMapType =
        new Dictionary<ColorMapEditorType, ColorMapType>()
        {
            { ColorMapEditorType.None, ColorMapType.None },
            { ColorMapEditorType.Grayscale, ColorMapType.MonoToMono },
            { ColorMapEditorType.GrayscaleToColor, ColorMapType.MonoToRgba },
            { ColorMapEditorType.ColorAdjustment, ColorMapType.BrightnessContrastSaturation },
            { ColorMapEditorType.ColorLut, ColorMapType.ColorLut },
            { ColorMapEditorType.InterpolatedColorLut, ColorMapType.InterpolatedColorLut }
        };

    /// <summary>
    /// Editor attribute to get or set the selection in the inspector.
    /// Using this selection will update the `colorMapType` and `colorMapData` if needed.
    /// </summary>
    public ColorMapEditorType colorMapEditorType
    {
        get { return colorMapEditorType_; }
        set
        {
            if (value != colorMapEditorType_)
            {
                colorMapEditorType_ = value;

                if (value != ColorMapEditorType.Custom)
                {
                    colorMapType = _editorToColorMapType[value];
                    _stylesHandler.SetStyleHandler(colorMapType);
                    if (value == ColorMapEditorType.None)
                    {
                        styleDirty = true;
                    }
                    else
                    {
                        UpdateColorMapFromControls(true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// This field is not intended for public scripting. Use `SetColorMapControls()` instead.
    /// </summary>
    public Gradient colorMapEditorGradient = CreateNeutralColorMapGradient();

    /// <summary>
    /// This field is not intended for public scripting. Use `SetBrightnessContrastSaturation()` or `SetColorMapControls()` instead.
    /// </summary>
    [Range(-1f, 1f)]
    public float colorMapEditorContrast;

    /// <summary>
    /// This field is not intended for public scripting. Use `SetBrightnessContrastSaturation()` or `SetColorMapControls()` instead.
    /// </summary>
    [Range(-1f, 1f)]
    public float colorMapEditorBrightness;

    /// <summary>
    /// This field is not intended for public scripting. Use `SetColorMapControls()` instead.
    /// </summary>
    [Range(0f, 1f)]
    public float colorMapEditorPosterize;

    /// <summary>
    /// This field is not intended for public scripting. Use `SetBrightnessContrastSaturation()` instead.
    /// </summary>
    [Range(-1f, 1f)]
    public float colorMapEditorSaturation;

    [SerializeField]
    internal Texture2D _colorLutSourceTexture;

    [SerializeField]
    internal Texture2D _colorLutTargetTexture;

    [SerializeField]
    [Range(0f, 1f)]
    internal float _lutWeight = 1;

    [SerializeField]
    internal bool _flipLutY = true;

    /// <summary>
    /// This method is required for internal use only.
    /// </summary>
    public void SetStyleDirty()
    {
        styleDirty = true;
    }

    private Settings _settings = new Settings(null, null, 0, 0, 0, 0, new Gradient(), 1, true);

    private struct Settings
    {
        public Texture2D colorLutTargetTexture;
        public Texture2D colorLutSourceTexture;
        public float saturation;
        public float posterize;
        public float brightness;
        public float contrast;
        public Gradient gradient;
        public float lutWeight;
        public bool flipLutY;

        public Settings(
            Texture2D colorLutTargetTexture,
            Texture2D colorLutSourceTexture,
            float saturation,
            float posterize,
            float brightness,
            float contrast,
            Gradient gradient,
            float lutWeight,
            bool flipLutY)
        {
            this.colorLutTargetTexture = colorLutTargetTexture;
            this.colorLutSourceTexture = colorLutSourceTexture;
            this.saturation = saturation;
            this.posterize = posterize;
            this.brightness = brightness;
            this.contrast = contrast;
            this.gradient = gradient;
            this.lutWeight = lutWeight;
            this.flipLutY = flipLutY;
        }
    }

    #endregion

    #region Internal Methods

    private void AddDeferredSurfaceGeometries()
    {
        for (int i = 0; i < deferredSurfaceGameObjects.Count; ++i)
        {
            var entry = deferredSurfaceGameObjects[i];
            bool entryIsPassthroughObject = false;
            if (surfaceGameObjects.ContainsKey(entry.gameObject))
            {
                entryIsPassthroughObject = true;
            }
            else
            {
                if (CreateAndAddMesh(entry.gameObject, out var meshHandle, out var instanceHandle,
                        out var localToWorld))
                {
                    surfaceGameObjects.Add(entry.gameObject, new PassthroughMeshInstance
                    {
                        meshHandle = meshHandle,
                        instanceHandle = instanceHandle,
                        updateTransform = entry.updateTransform,
                        localToWorld = localToWorld,
                    });
                    entryIsPassthroughObject = true;
                }
                else
                {
                    Debug.LogWarning(
                        "Failed to create internal resources for GameObject added to passthrough surface.");
                }
            }

            if (entryIsPassthroughObject)
            {
                deferredSurfaceGameObjects.RemoveAt(i--);
            }
        }
    }

    private Matrix4x4 GetTransformMatrixForPassthroughSurfaceObject(Matrix4x4 worldFromObj)
    {
        using var profile = new OVRProfilerScope(nameof(GetTransformMatrixForPassthroughSurfaceObject));

        if (!cameraRigInitialized)
        {
            cameraRig = OVRManager.instance.GetComponentInParent<OVRCameraRig>();
            cameraRigInitialized = true;
        }

        Matrix4x4 trackingSpaceFromWorld =
            (cameraRig != null) ? cameraRig.trackingSpace.worldToLocalMatrix : Matrix4x4.identity;

        // Use model matrix to switch from left-handed coordinate system (Unity)
        // to right-handed (Open GL/Passthrough API): reverse z-axis
        Matrix4x4 rightHandedFromLeftHanded = Matrix4x4.Scale(new Vector3(1, 1, -1));
        return rightHandedFromLeftHanded * trackingSpaceFromWorld * worldFromObj;
    }

    private bool CreateAndAddMesh(
        GameObject obj,
        out ulong meshHandle,
        out ulong instanceHandle,
        out Matrix4x4 localToWorld)
    {
        Debug.Assert(passthroughOverlay != null);
        Debug.Assert(passthroughOverlay.layerId > 0);
        meshHandle = 0;
        instanceHandle = 0;
        localToWorld = obj.transform.localToWorldMatrix;

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("Passthrough surface GameObject does not have a mesh component.");
            return false;
        }

        Mesh mesh = meshFilter.sharedMesh;

        // TODO: evaluate using GetNativeVertexBufferPtr() instead to avoid copy
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Matrix4x4 T_worldInsight_model = GetTransformMatrixForPassthroughSurfaceObject(localToWorld);

        if (!OVRPlugin.CreateInsightTriangleMesh(passthroughOverlay.layerId, vertices, triangles, out meshHandle))
        {
            Debug.LogWarning("Failed to create triangle mesh handle.");
            return false;
        }

        if (!OVRPlugin.AddInsightPassthroughSurfaceGeometry(passthroughOverlay.layerId, meshHandle,
                T_worldInsight_model, out instanceHandle))
        {
            Debug.LogWarning("Failed to add mesh to passthrough surface.");
            return false;
        }

        return true;
    }

    private void DestroySurfaceGeometries(bool addBackToDeferredQueue = false)
    {
        foreach (KeyValuePair<GameObject, PassthroughMeshInstance> el in surfaceGameObjects)
        {
            if (el.Value.meshHandle != 0)
            {
                OVRPlugin.DestroyInsightPassthroughGeometryInstance(el.Value.instanceHandle);
                OVRPlugin.DestroyInsightTriangleMesh(el.Value.meshHandle);

                // When DestroySurfaceGeometries is called from OnDisable, we want to keep track of the existing
                // surface geometries so we can add them back when the script gets enabled again. We simply reinsert
                // them into deferredSurfaceGameObjects for that purpose.
                if (addBackToDeferredQueue)
                {
                    deferredSurfaceGameObjects.Add(
                        new DeferredPassthroughMeshAddition
                        {
                            gameObject = el.Key,
                            updateTransform = el.Value.updateTransform
                        });
                }
            }
        }

        surfaceGameObjects.Clear();
    }

    private void UpdateSurfaceGeometryTransforms()
    {
        using var profile = new OVRProfilerScope(nameof(UpdateSurfaceGeometryTransforms));

        // Iterate through mesh instances and see if transforms need to be updated
        foreach (var kvp in surfaceGameObjects)
        {
            var instanceHandle = kvp.Value.instanceHandle;
            if (instanceHandle == 0) continue;

            var localToWorld = kvp.Value.updateTransform
                ? kvp.Key.transform.localToWorldMatrix
                : kvp.Value.localToWorld;

            UpdateSurfaceGeometryTransform(instanceHandle, localToWorld);
        }
    }

    private void UpdateSurfaceGeometryTransform(ulong instanceHandle, Matrix4x4 localToWorld)
    {
        var worldInsightModel = GetTransformMatrixForPassthroughSurfaceObject(localToWorld);
        using (new OVRProfilerScope(nameof(OVRPlugin.UpdateInsightPassthroughGeometryTransform)))
        {
            if (!OVRPlugin.UpdateInsightPassthroughGeometryTransform(instanceHandle, worldInsightModel))
            {
                Debug.LogWarning("Failed to update a transform of a passthrough surface");
            }
        }
    }

    // Returns a gradient from black to white.
    private static Gradient CreateNeutralColorMapGradient()
    {
        return new Gradient()
        {
            colorKeys = new GradientColorKey[2]
            {
                new GradientColorKey(new Color(0, 0, 0), 0),
                new GradientColorKey(new Color(1, 1, 1), 1)
            },
            alphaKeys = new GradientAlphaKey[2]
            {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };
    }

    private bool HasControlsBasedColorMap()
    {
        return colorMapEditorType == ColorMapEditorType.Grayscale
               || colorMapEditorType == ColorMapEditorType.ColorAdjustment
               || colorMapEditorType == ColorMapEditorType.ColorLut
               || colorMapEditorType == ColorMapEditorType.InterpolatedColorLut
               || colorMapEditorType == ColorMapEditorType.GrayscaleToColor;
    }

    private void UpdateColorMapFromControls(bool forceUpdate = false)
    {
        bool parametersChanged = _settings.brightness != colorMapEditorBrightness
                                 || _settings.contrast != colorMapEditorContrast
                                 || _settings.posterize != colorMapEditorPosterize
                                 || _settings.colorLutSourceTexture != _colorLutSourceTexture
                                 || _settings.colorLutTargetTexture != _colorLutTargetTexture
                                 || _settings.lutWeight != _lutWeight
                                 || _settings.saturation != colorMapEditorSaturation
                                 || _settings.flipLutY != _flipLutY;

        bool gradientNeedsUpdate = colorMapEditorType == ColorMapEditorType.GrayscaleToColor
                                   && !colorMapEditorGradient.Equals(_settings.gradient);

        if (!(HasControlsBasedColorMap() && parametersChanged || gradientNeedsUpdate || forceUpdate))
            return;

        _settings.gradient.CopyFrom(colorMapEditorGradient);
        _settings.brightness = colorMapEditorBrightness;
        _settings.contrast = colorMapEditorContrast;
        _settings.posterize = colorMapEditorPosterize;
        _settings.saturation = colorMapEditorSaturation;
        _settings.lutWeight = _lutWeight;
        _settings.flipLutY = _flipLutY;
        _settings.colorLutSourceTexture = _colorLutSourceTexture;
        _settings.colorLutTargetTexture = _colorLutTargetTexture;

        if (Application.isPlaying)
        {
            _stylesHandler.CurrentStyleHandler.Update(_settings);
            styleDirty = true;
        }
    }

    private void SyncToOverlay()
    {
        Debug.Assert(passthroughOverlay != null);

        passthroughOverlay.currentOverlayType = overlayType;
        passthroughOverlay.compositionDepth = compositionDepth;
        passthroughOverlay.hidden = hidden;
        passthroughOverlay.overridePerLayerColorScaleAndOffset = overridePerLayerColorScaleAndOffset;
        passthroughOverlay.colorScale = colorScale;
        passthroughOverlay.colorOffset = colorOffset;

        if (passthroughOverlay.currentOverlayShape != overlayShape)
        {
            if (passthroughOverlay.layerId > 0)
            {
                Debug.LogWarning("Change to projectionSurfaceType won't take effect until the layer " +
                                 "goes through a disable/enable cycle. ");
            }

            if (projectionSurfaceType == ProjectionSurfaceType.Reconstructed)
            {
                // Ensure there are no custom surface geometries when switching to reconstruction passthrough.
                Debug.Log("Removing user defined surface geometries");
                DestroySurfaceGeometries(false);
            }

            passthroughOverlay.currentOverlayShape = overlayShape;
        }

        // Disable the overlay when passthrough is disabled as a whole so the layer doesn't get submitted.
        // Both the desired (`isInsightPassthroughEnabled`) and the actual (IsInsightPassthroughInitialized()) PT
        // initialization state are taken into account s.t. the overlay gets disabled as soon as PT is flagged to be
        // disabled, and enabled only when PT is up and running again.
        passthroughOverlay.enabled = OVRManager.instance != null &&
                                     OVRManager.instance.isInsightPassthroughEnabled &&
                                     OVRManager.IsInsightPassthroughInitialized();
    }

    private static float ClampWeight(float weight)
    {
        if (weight < 0 || weight > 1)
        {
            Debug.LogWarning("Color lut weight should be between in [0, 1] range. Setting it to closest value.");
            weight = Mathf.Clamp01(weight);
        }

        return weight;
    }

    #endregion

    #region Internal Fields/Properties

    private OVRCameraRig cameraRig;
    private bool cameraRigInitialized = false;
    private GameObject auxGameObject;
    private OVROverlay passthroughOverlay;

    // Each GameObjects requires a MrTriangleMesh and a MrPassthroughGeometryInstance handle.
    // The structure also keeps a flag for whether transform updates should be tracked.
    private struct PassthroughMeshInstance
    {
        public ulong meshHandle;
        public ulong instanceHandle;
        public bool updateTransform;
        public Matrix4x4 localToWorld;
    }

    [Serializable]
    internal struct SerializedSurfaceGeometry
    {
        public MeshFilter meshFilter;
        public bool updateTransform;
    }

    // A structure for tracking a deferred addition of a game object to the projection surface
    private struct DeferredPassthroughMeshAddition
    {
        public GameObject gameObject;
        public bool updateTransform;
    }

    // GameObjects which are in use as Insight Passthrough projection surface.
    private Dictionary<GameObject, PassthroughMeshInstance> surfaceGameObjects =
        new Dictionary<GameObject, PassthroughMeshInstance>();

    // GameObjects which are pending addition to the Insight Passthrough projection surfaces.
    private List<DeferredPassthroughMeshAddition> deferredSurfaceGameObjects =
        new List<DeferredPassthroughMeshAddition>();

    [SerializeField, HideInInspector]
    internal List<SerializedSurfaceGeometry> serializedSurfaceGeometry =
        new List<SerializedSurfaceGeometry>();

    [SerializeField]
    internal float textureOpacity_ = 1;

    [SerializeField]
    internal bool edgeRenderingEnabled_ = false;

    [SerializeField]
    internal Color edgeColor_ = new Color(1, 1, 1, 1);

    // Internal fields which store the color map values that will be relayed to the Passthrough API in the next update.
    [SerializeField]
    private ColorMapType colorMapType = ColorMapType.None;

    // Flag which indicates whether the style values have changed since the last update in the Passthrough API.
    // It is set to `true` initially to ensure that the local default values are applied in the Passthrough API.
    private bool styleDirty = true;

    private StylesHandler _stylesHandler = new StylesHandler();

    // Keep a copy of a neutral gradient ready for comparison.
    static readonly private Gradient colorMapNeutralGradient = CreateNeutralColorMapGradient();

    // Overlay shape derived from `projectionSurfaceType`.
    private OVROverlay.OverlayShape overlayShape
    {
        get
        {
            return projectionSurfaceType == ProjectionSurfaceType.UserDefined
                ? OVROverlay.OverlayShape.SurfaceProjectedPassthrough
                : OVROverlay.OverlayShape.ReconstructionPassthrough;
        }
    }

    #endregion

    #region Unity Messages

    void Awake()
    {
        foreach (var surfaceGeometry in serializedSurfaceGeometry)
        {
            if (surfaceGeometry.meshFilter == null) continue;

            deferredSurfaceGameObjects.Add(new DeferredPassthroughMeshAddition
            {
                gameObject = surfaceGeometry.meshFilter.gameObject,
                updateTransform = surfaceGeometry.updateTransform
            });
        }
    }

    void Update()
    {
        SyncToOverlay();
    }

    void LateUpdate()
    {
        if (hidden) return;

        Debug.Assert(passthroughOverlay != null);

        // This LateUpdate() should be called after passthroughOverlay's LateUpdate() such that the layerId has
        // become available at this point. This is achieved by setting the execution order of this script to a value
        // past the default time (in .meta).

        if (passthroughOverlay.layerId <= 0)
        {
            // Layer not initialized yet
            return;
        }

        if (projectionSurfaceType == ProjectionSurfaceType.UserDefined)
        {
            // Update the poses before adding new items to avoid redundant calls.
            UpdateSurfaceGeometryTransforms();

            // Delayed additon of passthrough surface geometries.
            AddDeferredSurfaceGeometries();
        }

        // Update passthrough color map with gradient if it was changed in the inspector.
        UpdateColorMapFromControls();

        // Passthrough style updates are buffered and committed to the API atomically here.
        if (styleDirty)
        {
            if (_stylesHandler.CurrentStyleHandler.IsValid)
            {
                OVRPlugin.SetInsightPassthroughStyle(passthroughOverlay.layerId, CreateOvrPluginStyleObject());
            }

            styleDirty = false;
        }
    }

    private OVRPlugin.InsightPassthroughStyle2 CreateOvrPluginStyleObject()
    {
        OVRPlugin.InsightPassthroughStyle2 style = default;
        style.Flags = OVRPlugin.InsightPassthroughStyleFlags.HasTextureOpacityFactor |
                      OVRPlugin.InsightPassthroughStyleFlags.HasEdgeColor |
                      OVRPlugin.InsightPassthroughStyleFlags.HasTextureColorMap;

        style.TextureOpacityFactor = textureOpacity;

        style.EdgeColor = edgeRenderingEnabled
            ? edgeColor.ToColorf()
            : new OVRPlugin.Colorf { r = 0, g = 0, b = 0, a = 0 };

        style.TextureColorMapType = colorMapType;
        style.TextureColorMapData = IntPtr.Zero;
        style.TextureColorMapDataSize = 0;

        _stylesHandler.CurrentStyleHandler.ApplyStyleSettings(ref style);

        return style;
    }

    void OnEnable()
    {
        Debug.Assert(auxGameObject == null);
        Debug.Assert(passthroughOverlay == null);

        // Create auxiliary GameObject which contains the OVROverlay component for the proxy layer (and possibly other
        // auxiliary layers in the future).
        auxGameObject = new GameObject("OVRPassthroughLayer auxiliary GameObject");

        // Auxiliary GameObject must be a child of the current GameObject s.t. it survives if `DontDestroyOnLoad` is
        // called on the current GameObject.
        auxGameObject.transform.parent = this.transform;

        // Add OVROverlay component for the passthrough proxy layer.
        passthroughOverlay = auxGameObject.AddComponent<OVROverlay>();
        passthroughOverlay.currentOverlayShape = overlayShape;
        SyncToOverlay();

        // Surface geometries have been moved to the deferred additions queue in OnDisable() and will be re-added
        // in LateUpdate().

        _stylesHandler.SetStyleHandler(_editorToColorMapType[colorMapEditorType]);
        if (HasControlsBasedColorMap())
        {
            // Compute initial color map from controls
            UpdateColorMapFromControls(true);
        }

        // Flag style to be re-applied in LateUpdate()
        styleDirty = true;
    }

    void OnDisable()
    {
        if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)
        {
            DestroySurfaceGeometries(true);
        }

        if (auxGameObject != null)
        {
            Debug.Assert(passthroughOverlay != null);
            Destroy(auxGameObject);
            auxGameObject = null;
            passthroughOverlay = null;
        }
    }

    void OnDestroy()
    {
        DestroySurfaceGeometries();
    }

    #endregion

    #region Utility classes

    private interface IStyleHandler
    {
        void ApplyStyleSettings(ref OVRPlugin.InsightPassthroughStyle2 style);
        void Update(Settings settings);
        bool IsValid { get; }
        void Clear();
    }

    private class StylesHandler
    {
        private NoneStyleHandler _noneHandler;
        private ColorLutHandler _lutHandler;
        private InterpolatedColorLutHandler _interpolatedLutHandler;
        private MonoToRgbaStyleHandler _monoToRgbaHandler;
        private MonoToMonoStyleHandler _monoToMonoHandler;
        private BCSStyleHandler _bcsHandler;

        // Passthrough color map data gets pinned in the GC on allocation so it can be passed to the native side safely.
        // In remains pinned for its lifecycle to avoid pinning per frame and the resulting memory allocation and GC pressure.
        private GCHandle _colorMapDataHandle;

        // Passthrough color map data gets allocated and deallocated on demand.
        private byte[] _colorMapData = null;

        public IStyleHandler CurrentStyleHandler;

        public StylesHandler()
        {
            _noneHandler = new NoneStyleHandler();
            _lutHandler = new ColorLutHandler();
            _interpolatedLutHandler = new InterpolatedColorLutHandler();
            _monoToMonoHandler = new MonoToMonoStyleHandler(ref _colorMapDataHandle, _colorMapData);
            _monoToRgbaHandler = new MonoToRgbaStyleHandler(ref _colorMapDataHandle, _colorMapData);
            _bcsHandler = new BCSStyleHandler(ref _colorMapDataHandle, _colorMapData);
        }

        public void SetStyleHandler(ColorMapType type)
        {
            var nextStyleHandler = GetStyleHandler(type);

            if (nextStyleHandler == CurrentStyleHandler)
            {
                return;
            }

            if (CurrentStyleHandler != null)
            {
                CurrentStyleHandler.Clear();
            }

            CurrentStyleHandler = nextStyleHandler;
        }

        private IStyleHandler GetStyleHandler(ColorMapType type)
        {
            switch (type)
            {
                case ColorMapType.None:
                    return _noneHandler;
                case ColorMapType.MonoToRgba:
                    return _monoToRgbaHandler;
                case ColorMapType.MonoToMono:
                    return _monoToMonoHandler;
                case ColorMapType.BrightnessContrastSaturation:
                    return _bcsHandler;
                case ColorMapType.ColorLut:
                    return _lutHandler;
                case ColorMapType.InterpolatedColorLut:
                    return _interpolatedLutHandler;
                default:
                    throw new System.ArgumentException($"Unrecognized color map type {type}.");
            }
        }

        public void SetColorLutHandler(OVRPassthroughColorLut lut, float weight)
        {
            SetStyleHandler(ColorMapType.ColorLut);
            _lutHandler.Update(lut, weight);
        }

        internal void SetInterpolatedColorLutHandler(OVRPassthroughColorLut lutSource, OVRPassthroughColorLut lutTarget,
            float weight)
        {
            SetStyleHandler(ColorMapType.InterpolatedColorLut);
            _interpolatedLutHandler.Update(lutSource, lutTarget, weight);
        }

        internal void SetMonoToRgbaHandler(Color[] values)
        {
            SetStyleHandler(ColorMapType.MonoToRgba);
            _monoToRgbaHandler.Update(values);
        }

        internal void SetMonoToMonoHandler(byte[] values)
        {
            SetStyleHandler(ColorMapType.MonoToMono);
            _monoToMonoHandler.Update(values);
        }

    }

    private class NoneStyleHandler : IStyleHandler
    {
        public bool IsValid => true;

        public void ApplyStyleSettings(ref OVRPlugin.InsightPassthroughStyle2 style)
        {
        }

        public void Update(Settings settings)
        {
        }

        public void Clear()
        {
        }
    }

    private abstract class BaseGeneratedStyleHandler : IStyleHandler
    {
        private GCHandle _colorMapDataHandle;
        protected byte[] _colorMapData;

        protected abstract uint MapSize { get; }

        public bool IsValid => true;

        public BaseGeneratedStyleHandler(ref GCHandle colorMapDataHandler, byte[] colorMapData)
        {
            _colorMapDataHandle = colorMapDataHandler;
            _colorMapData = colorMapData;
        }

        public virtual void Update(Settings settings)
        {
        }

        public virtual void ApplyStyleSettings(ref OVRPlugin.InsightPassthroughStyle2 style)
        {
            style.TextureColorMapData = _colorMapDataHandle.AddrOfPinnedObject();
            style.TextureColorMapDataSize = MapSize;


            style.TextureColorMapData = _colorMapDataHandle.AddrOfPinnedObject();
            style.TextureColorMapDataSize = MapSize;
        }

        public void Clear()
        {
            DeallocateColorMapData();
        }

        protected virtual void AllocateColorMapData(uint size = 4096)
        {
            if (_colorMapData != null && size != _colorMapData.Length)
            {
                DeallocateColorMapData();
            }

            if (_colorMapData == null)
            {
                _colorMapData = new byte[size];


                _colorMapDataHandle = GCHandle.Alloc(_colorMapData, GCHandleType.Pinned);
            }
        }

        // Ensure that Passthrough color map data is unpinned and freed.
        protected virtual void DeallocateColorMapData()
        {
            if (_colorMapData != null)
            {

                _colorMapDataHandle.Free();
                _colorMapData = null;
            }
        }

        // Write a single color value to the Passthrough color map at the given position.
        protected void WriteColorToColorMap(int colorIndex, ref Color color)
        {
            for (int c = 0; c < 4; c++)
            {
                byte[] bytes = BitConverter.GetBytes(color[c]);
                Buffer.BlockCopy(bytes, 0, _colorMapData, colorIndex * 16 + c * 4, 4);
            }
        }

        protected void WriteFloatToColorMap(int index, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, _colorMapData, index * sizeof(float), sizeof(float));
        }

        protected static void ComputeBrightnessContrastPosterizeMap(byte[] result, float brightness, float contrast,
            float posterize)
        {
            for (int i = 0; i < 256; i++)
            {
                // Apply contrast, brightness and posterization on the grayscale value
                float value = i / 255.0f;
                // Constrast and brightness
                float contrastFactor = contrast + 1; // UI runs from -1 to 1
                value = (value - 0.5f) * contrastFactor + 0.5f + brightness;

                // Posterization
                if (posterize > 0.0f)
                {
                    // The posterization slider feels more useful if the progression is exponential. The function is emprically tuned.
                    const float posterizationBase = 50.0f;
                    float quantization = (Mathf.Pow(posterizationBase, posterize) - 1.0f) / (posterizationBase - 1.0f);
                    value = Mathf.Round(value / quantization) * quantization;
                }

                result[i] = (byte)(Mathf.Min(Mathf.Max(value, 0.0f), 1.0f) * 255.0f);
            }
        }
    }

    private class MonoToRgbaStyleHandler : BaseGeneratedStyleHandler
    {
        protected override uint MapSize => 256 * 4 * 4; /* 256 * sizeof(MrColor4f) */

        // Buffer used to store intermediate results for color map computations.
        protected byte[] _tmpColorMapData = null;

        public MonoToRgbaStyleHandler(ref GCHandle colorMapDataHandler, byte[] colorMapData)
            : base(ref colorMapDataHandler, colorMapData)
        {
        }

        public override void Update(Settings settings)
        {
            AllocateColorMapData();
            ComputeBrightnessContrastPosterizeMap(_tmpColorMapData, settings.brightness, settings.contrast,
                settings.posterize);
            for (int i = 0; i < 256; i++)
            {
                Color color = settings.gradient.Evaluate(_tmpColorMapData[i] / 255.0f);
                WriteColorToColorMap(i, ref color);
            }
        }

        public void Update(Color[] values)
        {
            AllocateColorMapData();
            for (int i = 0; i < 256; i++)
            {
                WriteColorToColorMap(i, ref values[i]);
            }
        }

        protected override void AllocateColorMapData(uint size = 4096)
        {
            base.AllocateColorMapData(size);
            _tmpColorMapData = new byte[256];
        }

        protected override void DeallocateColorMapData()
        {
            base.DeallocateColorMapData();
            _tmpColorMapData = null;
        }
    }

    private class MonoToMonoStyleHandler : BaseGeneratedStyleHandler
    {
        protected override uint MapSize => 256;

        public MonoToMonoStyleHandler(ref GCHandle colorMapDataHandler, byte[] colorMapData)
            : base(ref colorMapDataHandler, colorMapData)
        {
        }

        public override void Update(Settings settings)
        {
            AllocateColorMapData();
            ComputeBrightnessContrastPosterizeMap(_colorMapData, settings.brightness, settings.contrast,
                settings.posterize);
        }

        public void Update(byte[] values)
        {
            AllocateColorMapData();
            Buffer.BlockCopy(values, 0, _colorMapData, 0, 256);
        }
    }

    private class BCSStyleHandler : BaseGeneratedStyleHandler
    {
        protected override uint MapSize => 3 * sizeof(float);

        public BCSStyleHandler(ref GCHandle colorMapDataHandler, byte[] colorMapData)
            : base(ref colorMapDataHandler, colorMapData)
        {
        }

        public override void Update(Settings settings)
        {
            AllocateColorMapData();

            // Brightness: input is in range [-1, 1], output [0, 100]
            WriteFloatToColorMap(0, settings.brightness * 100.0f);

            // Contrast: input is in range [-1, 1], output [0, 2]
            WriteFloatToColorMap(1, settings.contrast + 1.0f);

            // Saturation: input is in range [-1, 1], output [0, 2]
            WriteFloatToColorMap(2, settings.saturation + 1.0f);
        }
    }


    private class ColorLutHandler : IStyleHandler
    {
        protected bool _currentFlipLutY;
        protected Texture2D _currentColorLutSourceTexture;
        public OVRPassthroughColorLut Lut { get; set; }
        public float Weight { get; set; }

        public bool IsValid { get; protected set; }

        public virtual void ApplyStyleSettings(ref OVRPlugin.InsightPassthroughStyle2 style)
        {
            style.LutSource = Lut._colorLutHandle;
            style.LutWeight = Weight;
        }

        public virtual void Update(Settings settings)
        {
            Update(
                GetColorLutForTexture(settings.colorLutSourceTexture, Lut, ref _currentColorLutSourceTexture,
                    settings.flipLutY),
                settings.lutWeight);
        }

        protected OVRPassthroughColorLut GetColorLutForTexture(Texture2D newTexture, OVRPassthroughColorLut lut,
            ref Texture2D lastTexture, bool flipY)
        {
            if (newTexture == null)
            {
                Debug.LogError("Trying to update style with null texture.");
                return null;
            }

            if (lastTexture != newTexture || _currentFlipLutY != flipY)
            {
                if (lut != null)
                {
                    lut.Dispose();
                }

                lastTexture = newTexture;
                _currentFlipLutY = flipY;
                return new OVRPassthroughColorLut(newTexture, _currentFlipLutY);
            }

            return lut;
        }

        internal void Update(OVRPassthroughColorLut lut, float weight)
        {
            if (lut == null)
            {
                IsValid = false;
            }
            else
            {
                IsValid = true;
                Lut = lut;
                Weight = weight;
            }
        }

        public virtual void Clear()
        {
            Lut = null;
            _currentColorLutSourceTexture = null;
        }
    }

    private class InterpolatedColorLutHandler : ColorLutHandler
    {
        private Texture2D _currentColorLutTargetTexture;
        public OVRPassthroughColorLut LutTarget { get; set; }

        public override void ApplyStyleSettings(ref OVRPlugin.InsightPassthroughStyle2 style)
        {
            base.ApplyStyleSettings(ref style);
            style.LutTarget = LutTarget._colorLutHandle;
        }

        public override void Update(Settings settings)
        {
            Update(
                GetColorLutForTexture(settings.colorLutSourceTexture, Lut, ref _currentColorLutSourceTexture,
                    settings.flipLutY),
                GetColorLutForTexture(settings.colorLutTargetTexture, LutTarget, ref _currentColorLutTargetTexture,
                    settings.flipLutY),
                settings.lutWeight);
        }

        public void Update(OVRPassthroughColorLut lutSource, OVRPassthroughColorLut lutTarget, float weight)
        {
            if (lutSource == null || lutTarget == null)
            {
                IsValid = false;
            }
            else
            {
                IsValid = true;
                Lut = lutSource;
                LutTarget = lutTarget;
                Weight = weight;
            }
        }

        public override void Clear()
        {
            base.Clear();
            LutTarget = null;
            _currentColorLutTargetTexture = null;
        }
    }

    #endregion //Utility classes
}
