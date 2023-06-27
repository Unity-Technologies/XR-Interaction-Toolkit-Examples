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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System.Linq;
using UnityEngine.Serialization;

/// <summary>
/// Supports Virtual Keyboard integration by providing the implementation to necessary common patterns
/// </summary>
[DisallowMultipleComponent]
public class OVRVirtualKeyboard : MonoBehaviour, OVRManager.EventListener
{
    public enum KeyboardPosition
    {
        Far = 0,
        Direct = 1
    }

    public class InteractorRootTransformOverride
    {
        private struct InteractorRootOverrideData
        {
            public Transform root;
            public OVRPose originalPose;
            public OVRPose targetPose;
        }

        private Queue<InteractorRootOverrideData> applyQueue = new Queue<InteractorRootOverrideData>();
        private Queue<InteractorRootOverrideData> revertQueue = new Queue<InteractorRootOverrideData>();

        public void Enqueue(Transform interactorRootTransform, OVRPlugin.Posef interactorRootPose)
        {
            if (interactorRootTransform == null)
            {
                throw new Exception("Transform is undefined");
            }

            applyQueue.Enqueue(new InteractorRootOverrideData()
            {
                root = interactorRootTransform,
                originalPose = interactorRootTransform.ToOVRPose(),
                targetPose = interactorRootPose.ToOVRPose()
            });
        }

        public void LateApply(MonoBehaviour coroutineRunner)
        {
            while (applyQueue.Count > 0)
            {
                var queueItem = applyQueue.Dequeue();
                var restoreToPose = queueItem.root.ToOVRPose();
                if (!ApplyOverride(queueItem))
                {
                    continue;
                }

                queueItem.originalPose = queueItem.root.ToOVRPose();
                queueItem.targetPose = restoreToPose;
                revertQueue.Enqueue(queueItem);
            }

            if (revertQueue.Count > 0 && coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(RevertInteractorOverrides());
            }
        }

        public void Reset()
        {
            while (revertQueue.Count > 0)
            {
                ApplyOverride(revertQueue.Dequeue());
            }
        }

        private IEnumerator RevertInteractorOverrides()
        {
            yield return new WaitForEndOfFrame();
            Reset();
        }

        private static bool ApplyOverride(InteractorRootOverrideData interactorOverride)
        {
            if (interactorOverride.root.position != interactorOverride.originalPose.position ||
                interactorOverride.root.rotation != interactorOverride.originalPose.orientation)
            {
                return false;
            }

            interactorOverride.root.position = interactorOverride.targetPose.position;
            interactorOverride.root.rotation = interactorOverride.targetPose.orientation;
            return true;
        }
    }

    public enum InputSource
    {
        ControllerLeft,
        ControllerRight,
        HandLeft,
        HandRight
    }

    private interface IInputSource
    {
        void Update();
    }

    private class ControllerInputSource : IInputSource
    {
        private static readonly float TriggerPressedThreshold = 0.9f;
        private readonly Transform _transform;
        private readonly InputSource _inputSource;
        private readonly OVRInput.Controller _controllerType;
        private readonly OVRInput.RawAxis1D _triggerAxis;
        private readonly OVRVirtualKeyboard _keyboard;

        public ControllerInputSource(OVRVirtualKeyboard keyboard, InputSource inputSource,
            OVRInput.Controller controllerType, Transform transform)
        {
            _keyboard = keyboard;
            _inputSource = inputSource;
            _controllerType = controllerType;
            _triggerAxis = _controllerType == OVRInput.Controller.LTouch
                ? OVRInput.RawAxis1D.LIndexTrigger
                : OVRInput.RawAxis1D.RIndexTrigger;
            _transform = transform;
        }

        public void Update()
        {
            if (!OVRInput.GetControllerPositionValid(_controllerType) || !_transform)
            {
                return;
            }

            if (_keyboard.controllerRayInteraction)
            {
                _keyboard.SendVirtualKeyboardRayInput(
                    _transform, _inputSource,
                    OVRInput.Get(_triggerAxis) > TriggerPressedThreshold);
            }

            if (_keyboard.controllerDirectInteraction)
            {
                _keyboard.SendVirtualKeyboardDirectInput(_transform.position, _inputSource,
                    OVRInput.Get(_triggerAxis) > TriggerPressedThreshold);
            }
        }
    }

    private class HandInputSource : IInputSource
    {
        private readonly OVRHand _hand;
        private readonly InputSource _inputSource;
        private readonly OVRVirtualKeyboard _keyboard;
        private readonly OVRSkeleton _skeleton;

        public HandInputSource(OVRVirtualKeyboard keyboard, InputSource inputSource, OVRHand hand)
        {
            _keyboard = keyboard;
            _hand = hand;
            _skeleton = _hand.GetComponent<OVRSkeleton>();
            _inputSource = inputSource;
        }

        public void Update()
        {
            if (!_hand)
            {
                return;
            }

            if (_keyboard.handRayInteraction && _hand.IsPointerPoseValid)
            {
                _keyboard.SendVirtualKeyboardRayInput(
                    _hand.PointerPose,
                    _inputSource, _hand.GetFingerIsPinching(OVRHand.HandFinger.Index));
            }

            if (_keyboard.handDirectInteraction && _skeleton && _skeleton.IsDataValid)
            {
                var indexTip = _skeleton.Bones.First(b => b.Id == OVRSkeleton.BoneId.Hand_IndexTip);
                var interactorRoot = _skeleton.Bones.First(b => b.Id == OVRSkeleton.BoneId.Hand_WristRoot);
                _keyboard.SendVirtualKeyboardDirectInput(
                    indexTip.Transform.position,
                    _inputSource, _hand.GetFingerIsPinching(OVRHand.HandFinger.Index), interactorRoot.Transform);
            }
        }
    }

    private static OVRVirtualKeyboard singleton_;

    /// <summary>
    /// Occurs when text has been committed
    /// @params (string text)
    /// </summary>
    public event Action<string> CommitText;

    /// <summary>
    /// Occurs when a backspace is pressed
    /// </summary>
    public event Action Backspace;

    /// <summary>
    /// Occurs when a return key is pressed
    /// </summary>
    public event Action Enter;

    /// <summary>
    /// Occurs when keyboard is shown
    /// </summary>
    public event Action KeyboardShown;

    /// <summary>
    /// Occurs when keyboard is hidden
    /// </summary>
    public event Action KeyboardHidden;

    public Collider Collider { get; private set; }

    /// <summary>
    /// Unity UI field to automatically commit text into. (optional)
    /// </summary>
    [SerializeField]
    [FormerlySerializedAs("TextCommitField")]
    private InputField textCommitField;

    [Header("Controller Input")]
    public Transform leftControllerInputTransform;

    public Transform rightControllerInputTransform;
    public bool controllerDirectInteraction = true;
    public bool controllerRayInteraction = true;
    public LayerMask controllerRaycastLayerMask = 1 << 0;

    [Header("Hand Input")]
    public OVRHand handLeft;

    public OVRHand handRight;
    public bool handDirectInteraction = true;
    public bool handRayInteraction = true;
    public LayerMask handRaycastLayerMask = 1 << 0;

    [Header("Graphics")]
    public Shader keyboardModelShader;

    public Shader keyboardModelAlphaBlendShader;

    [NonSerialized]
    public bool InputEnabled = true;

    private bool isKeyboardCreated_ = false;

    private UInt64 keyboardSpace_;

    private Dictionary<ulong, List<Material>> virtualKeyboardTextures_ = new Dictionary<ulong, List<Material>>();
    private OVRGLTFScene virtualKeyboardScene_;
    private UInt64 virtualKeyboardModelKey_;
    private bool modelInitialized_ = false;
    private bool modelAvailable_ = false;
    private bool keyboardVisible_ = false;
    private InteractorRootTransformOverride _interactorRootTransformOverride = new InteractorRootTransformOverride();
    private List<IInputSource> _inputSources;

    // Used to ignore internal invokes of OnValueChanged without unbinding/rebinding
    private bool ignoreTextCommmitFieldOnValueChanged_;
    private InputField runtimeInputField_;

    // ensures runtime updates to the TextCommitField keep text context in sync
    public InputField TextCommitField
    {
        get => runtimeInputField_;
        set
        {
            if (runtimeInputField_ == value)
            {
                return;
            }

            if (runtimeInputField_ != null)
            {
                runtimeInputField_.onValueChanged.RemoveListener(OnTextCommitFieldChange);
            }

            runtimeInputField_ = value;
            if (runtimeInputField_ != null)
            {
                runtimeInputField_.onValueChanged.AddListener(OnTextCommitFieldChange);
                ChangeTextContextInternal(runtimeInputField_.text);
            }
        }
    }

    // Unity event functions
    void Awake()
    {
        if (keyboardModelShader == null)
        {
            keyboardModelShader = Shader.Find("Unlit/Color");
        }

        if (keyboardModelAlphaBlendShader == null)
        {
            keyboardModelAlphaBlendShader = Shader.Find("Unlit/Transparent");
        }

        if (singleton_ != null)
        {
            GameObject.Destroy(this);
            throw new Exception("OVRVirtualKeyboard only supports a single instance");
        }

        singleton_ = this;
        OVRManager.instance.RegisterEventListener(this);

        // Initialize serialized text commit field
        TextCommitField = textCommitField;

        // Register for events
        CommitText += OnCommitText;
        Backspace += OnBackspace;
        Enter += OnEnter;
        KeyboardShown += OnKeyboardShown;
        KeyboardHidden += OnKeyboardHidden;
    }

    void OnDestroy()
    {
        CommitText -= OnCommitText;
        Backspace -= OnBackspace;
        Enter -= OnEnter;
        KeyboardShown -= OnKeyboardShown;
        KeyboardHidden -= OnKeyboardHidden;

        TextCommitField = null;

        if (singleton_ == this)
        {
            if (OVRManager.instance != null)
            {
                OVRManager.instance.DeregisterEventListener(this);
            }

            singleton_ = null;
        }

        DestroyKeyboard();
    }

    void OnEnable()
    {
        ShowKeyboard();
    }

    void OnDisable()
    {
        HideKeyboard();
    }

    // public functions
    public void UseSuggestedLocation(KeyboardPosition position)
    {
        OVRPlugin.VirtualKeyboardLocationInfo locationInfo = new OVRPlugin.VirtualKeyboardLocationInfo();
        switch (position)
        {
            case KeyboardPosition.Direct:
                locationInfo.locationType = OVRPlugin.VirtualKeyboardLocationType.Direct;
                break;
            case KeyboardPosition.Far:
                locationInfo.locationType = OVRPlugin.VirtualKeyboardLocationType.Far;
                break;
            default:
                Debug.LogError("Unknown KeyboardInputMode: " + position);
                break;
        }

        var result = OVRPlugin.SuggestVirtualKeyboardLocation(locationInfo);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("SuggestVirtualKeyboardLocation failed: " + result);
        }
    }

    public void SendVirtualKeyboardRayInput(Transform inputTransform,
        InputSource source, bool isPressed, bool useRaycastMask = true)
    {
        var inputSource = source switch
        {
            InputSource.ControllerLeft => OVRPlugin.VirtualKeyboardInputSource.ControllerRayLeft,
            InputSource.ControllerRight => OVRPlugin.VirtualKeyboardInputSource.ControllerRayRight,
            InputSource.HandLeft => OVRPlugin.VirtualKeyboardInputSource.HandRayLeft,
            InputSource.HandRight => OVRPlugin.VirtualKeyboardInputSource.HandRayRight,
            _ => throw new Exception("Unknown input source: " + source)
        };

        var ray = new Ray(inputTransform.position, inputTransform.forward);
        LayerMask raycastMask = (source == InputSource.ControllerLeft || source == InputSource.ControllerRight)
            ? controllerRaycastLayerMask
            : handRaycastLayerMask;
        if (useRaycastMask && Physics.Raycast(ray, out var hitInfo, 100,
                raycastMask, QueryTriggerInteraction.Ignore) && hitInfo.collider != this.Collider)
        {
            return;
        }

        SendVirtualKeyboardInput(inputSource, inputTransform.ToOVRPose(), isPressed);
    }

    public void SendVirtualKeyboardDirectInput(Vector3 position,
        InputSource source, bool isPressed, Transform interactorRootTransform = null)
    {
        var inputSource = source switch
        {
            InputSource.ControllerLeft => OVRPlugin.VirtualKeyboardInputSource.ControllerDirectLeft,
            InputSource.ControllerRight => OVRPlugin.VirtualKeyboardInputSource.ControllerDirectRight,
            InputSource.HandLeft => OVRPlugin.VirtualKeyboardInputSource.HandDirectIndexTipLeft,
            InputSource.HandRight => OVRPlugin.VirtualKeyboardInputSource.HandDirectIndexTipRight,
            _ => throw new Exception("Unknown input source: " + source)
        };
        SendVirtualKeyboardInput(inputSource, new OVRPose()
        {
            position = position
        }, isPressed, interactorRootTransform);
    }

    public void OnEvent(OVRPlugin.EventDataBuffer eventDataBuffer)
    {
        switch (eventDataBuffer.EventType)
        {
            case OVRPlugin.EventType.VirtualKeyboardCommitText:
            {
                CommitText?.Invoke(
                    Encoding.UTF8.GetString(eventDataBuffer.EventData)
                        .Replace("\0", "")
                );
                break;
            }
            case OVRPlugin.EventType.VirtualKeyboardBackspace:
            {
                Backspace?.Invoke();
                break;
            }
            case OVRPlugin.EventType.VirtualKeyboardEnter:
            {
                Enter?.Invoke();
                break;
            }
            case OVRPlugin.EventType.VirtualKeyboardShown:
            {
                KeyboardShown?.Invoke();
                break;
            }
            case OVRPlugin.EventType.VirtualKeyboardHidden:
            {
                KeyboardHidden?.Invoke();
                break;
            }
        }
    }

    public void ChangeTextContext(string textContext)
    {
        if (TextCommitField != null && TextCommitField.text != textContext)
        {
            Debug.LogWarning("TextCommitField text out of sync with Keyboard text context");
        }

        ChangeTextContextInternal(textContext);
    }

    // Private methods
    private bool LoadRuntimeVirtualKeyboardMesh()
    {
        modelAvailable_ = false;
        Debug.Log("LoadRuntimeVirtualKeyboardMesh");
        string[] modelPaths = OVRPlugin.GetRenderModelPaths();

        var keyboardPath = modelPaths?.FirstOrDefault(p => p.Equals("/model_fb/virtual_keyboard")
                                                           || p.Equals("/model_meta/keyboard/virtual"));

        if (String.IsNullOrEmpty(keyboardPath))
        {
            Debug.LogError("Failed to find keyboard model.  Check Render Model support.");
            return false;
        }

        OVRPlugin.RenderModelProperties modelProps = new OVRPlugin.RenderModelProperties();
        if (OVRPlugin.GetRenderModelProperties(keyboardPath, ref modelProps))
        {
            if (modelProps.ModelKey != OVRPlugin.RENDER_MODEL_NULL_KEY)
            {
                virtualKeyboardModelKey_ = modelProps.ModelKey;
                byte[] data = OVRPlugin.LoadRenderModel(modelProps.ModelKey);
                if (data != null)
                {
                    OVRGLTFLoader gltfLoader = new OVRGLTFLoader(data);
                    gltfLoader.textureUriHandler = (string rawUri, Material mat) =>
                    {
                        var uri = new Uri(rawUri);
                        // metaVirtualKeyboard://texture/{id}?w={width}&h={height}&ft=RGBA32
                        if (uri.Scheme != "metaVirtualKeyboard" && uri.Host != "texture")
                        {
                            return null;
                        }

                        var textureId = ulong.Parse(uri.LocalPath.Substring(1));
                        if (virtualKeyboardTextures_.ContainsKey(textureId) == false)
                        {
                            virtualKeyboardTextures_[textureId] = new List<Material>();
                        }

                        virtualKeyboardTextures_[textureId].Add(mat);
                        return null; // defer texture data loading
                    };
                    gltfLoader.SetModelShader(keyboardModelShader);
                    gltfLoader.SetModelAlphaBlendShader(keyboardModelAlphaBlendShader);
                    virtualKeyboardScene_ = gltfLoader.LoadGLB(supportAnimation: true, loadMips: true);
                    virtualKeyboardScene_.root.gameObject.name = "OVRVirtualKeyboardModel";
                    modelAvailable_ = virtualKeyboardScene_.root != null;
                    if (modelAvailable_)
                    {
                        PopulateCollision();
                    }
                }
            }
        }

        return modelAvailable_;
    }

    private void PopulateCollision()
    {
        if (!modelAvailable_)
        {
            throw new Exception("Keyboard Model Unavailable");
        }

        var childrenMeshes = virtualKeyboardScene_.root.GetComponentsInChildren<MeshFilter>();
        var collisionMesh = childrenMeshes.Where(mesh => mesh.gameObject.name == "collision").FirstOrDefault();
        if (collisionMesh != null)
        {
            var meshCollider = collisionMesh.gameObject.AddComponent<MeshCollider>();
            Collider = meshCollider;
        }
    }

    private void ShowKeyboard()
    {
        if (!isKeyboardCreated_)
        {
            var createInfo = new OVRPlugin.VirtualKeyboardCreateInfo();

            var result = OVRPlugin.CreateVirtualKeyboard(createInfo);
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Create failed: '" + result + "'. Check for Virtual Keyboard Support.");
                return;
            }

            // Once created the keyboard should be positioned
            // instead of using a default location, initially use with the unity keyboard root transform
            var locationInfo = ComputeLocation(transform);

            var createSpaceInfo = new OVRPlugin.VirtualKeyboardSpaceCreateInfo();
            createSpaceInfo.pose = OVRPlugin.Posef.identity;
            result = OVRPlugin.CreateVirtualKeyboardSpace(createSpaceInfo, out keyboardSpace_);
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Create failed to create keyboard space: " + result);
                return;
            }

            result = OVRPlugin.SuggestVirtualKeyboardLocation(locationInfo);
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Create failed to position keyboard: " + result);
                return;
            }

            // Initialize the keyboard model
            if (modelInitialized_ != true)
            {
                modelInitialized_ = true;
                if (!LoadRuntimeVirtualKeyboardMesh())
                {
                    DestroyKeyboard();
                    return;
                }

                UpdateVisibleState();
            }

            // Should call this whenever the keyboard is created or when the text focus changes
            if (TextCommitField != null)
            {
                ChangeTextContextInternal(TextCommitField.text);
            }
        }

        try
        {
            SetKeyboardVisibility(true);
            UpdateKeyboardLocation();
            isKeyboardCreated_ = true;
        }
        catch
        {
            DestroyKeyboard();
            throw;
        }
    }

    private void SetKeyboardVisibility(bool visible)
    {
        if (!modelInitialized_)
        {
            // Set active was called before the model was even attempted to be loaded
            return;
        }

        if (!modelAvailable_)
        {
            Debug.LogError("Failed to set visibility. Keyboard model unavailable.");
            return;
        }

        var visibility = new OVRPlugin.VirtualKeyboardModelVisibility();
        visibility.Visible = visible;
        var res = OVRPlugin.SetVirtualKeyboardModelVisibility(ref visibility);
        if (res != OVRPlugin.Result.Success)
        {
            Debug.LogError("SetVirtualKeyboardModelVisibility failed: " + res);
        }
    }

    private void HideKeyboard()
    {
        if (!modelAvailable_)
        {
            // If model has not been loaded, completely uninitialize
            DestroyKeyboard();
            return;
        }

        SetKeyboardVisibility(false);
    }

    private void DestroyKeyboard()
    {
        if (isKeyboardCreated_)
        {
            if (modelAvailable_)
            {
                GameObject.Destroy(virtualKeyboardScene_.root);
                modelAvailable_ = false;
                modelInitialized_ = false;
            }

            var result = OVRPlugin.DestroyVirtualKeyboard();
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Destroy failed");
                return;
            }

            Debug.Log("Destroy success");
        }

        isKeyboardCreated_ = false;
    }

    private float MaxElement(Vector3 vec)
    {
        return Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
    }

    private OVRPlugin.VirtualKeyboardLocationInfo ComputeLocation(Transform transform)
    {
        OVRPlugin.VirtualKeyboardLocationInfo location = new OVRPlugin.VirtualKeyboardLocationInfo();

        location.locationType = OVRPlugin.VirtualKeyboardLocationType.Custom;
        // Plane in Unity has its normal facing towards camera by default, in runtime it's facing away,
        // so to compensate, flip z for both position and rotation, for both plane and pointer pose.
        location.pose.Position = transform.position.ToFlippedZVector3f();
        location.pose.Orientation = transform.rotation.ToFlippedZQuatf();
        location.scale = MaxElement(transform.localScale);
        return location;
    }

    private void UpdateKeyboardLocation()
    {
        var locationInfo = ComputeLocation(transform);
        var result = OVRPlugin.SuggestVirtualKeyboardLocation(locationInfo);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("Failed to update keyboard location: " + result);
        }
    }

    void Update()
    {
        if (!isKeyboardCreated_)
        {
            return;
        }

        UpdateInputs();
        SyncKeyboardLocation();
        UpdateAnimationState();
    }

    private void LateUpdate()
    {
        _interactorRootTransformOverride.LateApply(this);
    }

    private void SendVirtualKeyboardInput(OVRPlugin.VirtualKeyboardInputSource inputSource, OVRPose pose,
        bool isPressed, Transform interactorRootTransform = null)
    {
        var inputInfo = new OVRPlugin.VirtualKeyboardInputInfo();
        inputInfo.inputSource = inputSource;
        inputInfo.inputPose = pose.ToPosef();
        inputInfo.inputState = (isPressed) ? OVRPlugin.VirtualKeyboardInputStateFlags.IsPressed : 0;
        var hasInteractorRootTransform = interactorRootTransform != null;
        var interactorRootPose = (!hasInteractorRootTransform)
            ? pose.ToPosef()
            : interactorRootTransform.ToOVRPose().ToPosef();
        var result = OVRPlugin.SendVirtualKeyboardInput(inputInfo, ref interactorRootPose);
        if (result != OVRPlugin.Result.Success)
        {
#if DEVELOPMENT_BUILD
            Debug.LogError("Failed to send input source " + inputSource);
#endif
            return;
        }

        if (interactorRootTransform != null)
        {
            _interactorRootTransformOverride.Enqueue(interactorRootTransform, interactorRootPose);
        }
    }

    private void UpdateInputs()
    {
        if (!InputEnabled || !modelAvailable_)
        {
            return;
        }

        _inputSources ??= new List<IInputSource>()
        {
            new ControllerInputSource(this, InputSource.ControllerLeft, OVRInput.Controller.LTouch,
                leftControllerInputTransform),
            new ControllerInputSource(this, InputSource.ControllerRight, OVRInput.Controller.RTouch,
                rightControllerInputTransform),
            new HandInputSource(this, InputSource.HandLeft, handLeft),
            new HandInputSource(this, InputSource.HandRight, handRight)
        };

        foreach (var inputSource in _inputSources)
        {
            inputSource.Update();
        }
    }

    private void SyncKeyboardLocation()
    {
        // If unity transform has updated, sync with runtime
        if (transform.hasChanged)
        {
            // ensure scale uniformity
            var scale = MaxElement(transform.localScale);
            var maxScale = Vector3.one * scale;
            transform.localScale = maxScale;
            UpdateKeyboardLocation();
        }

        // query the runtime for the true position
        if (!OVRPlugin.TryLocateSpace(keyboardSpace_, OVRPlugin.GetTrackingOriginType(), out var keyboardPose))
        {
            Debug.LogError("Failed to locate the virtual keyboard space.");
            return;
        }

        var result = OVRPlugin.GetVirtualKeyboardScale(out var keyboardScale);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("Failed to get virtual keyboard scale.");
            return;
        }

        transform.SetPositionAndRotation(keyboardPose.Position.FromFlippedZVector3f(),
            keyboardPose.Orientation.FromFlippedZQuatf());
        transform.localScale = new Vector3(keyboardScale, keyboardScale, keyboardScale);

        if (modelAvailable_)
        {
            virtualKeyboardScene_.root.transform.position = keyboardPose.Position.FromFlippedZVector3f();
            // Rotate to face user
            virtualKeyboardScene_.root.transform.rotation =
                keyboardPose.Orientation.FromFlippedZQuatf() * Quaternion.Euler(0, 180f, 0);
            virtualKeyboardScene_.root.transform.localScale = transform.localScale;
        }

        transform.hasChanged = false;
    }

    private void UpdateAnimationState()
    {
        if (!modelAvailable_)
        {
            return;
        }

        OVRPlugin.GetVirtualKeyboardDirtyTextures(out var dirtyTextures);
        foreach (var textureId in dirtyTextures.TextureIds)
        {
            if (!virtualKeyboardTextures_.TryGetValue(textureId, out var textureMaterials))
            {
                continue;
            }

            var textureData = new OVRPlugin.VirtualKeyboardTextureData();
            OVRPlugin.GetVirtualKeyboardTextureData(textureId, ref textureData);
            if (textureData.BufferCountOutput > 0)
            {
                try
                {
                    textureData.Buffer = Marshal.AllocHGlobal((int)textureData.BufferCountOutput);
                    textureData.BufferCapacityInput = textureData.BufferCountOutput;
                    OVRPlugin.GetVirtualKeyboardTextureData(textureId, ref textureData);

                    var texBytes = new byte[textureData.BufferCountOutput];
                    Marshal.Copy(textureData.Buffer, texBytes, 0, (int)textureData.BufferCountOutput);

                    var tex = new Texture2D((int)textureData.TextureWidth, (int)textureData.TextureHeight,
                        TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Trilinear;
                    tex.SetPixelData(texBytes, 0);
                    tex.Apply(true /*updateMipmaps*/, true /*makeNoLongerReadable*/);
                    foreach (var material in textureMaterials)
                    {
                        material.mainTexture = tex;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(textureData.Buffer);
                }
            }
        }

        var result = OVRPlugin.GetVirtualKeyboardModelAnimationStates(out var animationStates);
        if (result == OVRPlugin.Result.Success)
        {
            for (var i = 0; i < animationStates.States.Length; i++)
            {
                if (!virtualKeyboardScene_.animationNodeLookup.ContainsKey(animationStates.States[i].AnimationIndex))
                {
                    Debug.LogWarning($"Unknown Animation State Index {animationStates.States[i].AnimationIndex}");
                    continue;
                }

                var animationNodes =
                    virtualKeyboardScene_.animationNodeLookup[animationStates.States[i].AnimationIndex];
                foreach (var animationNode in animationNodes)
                {
                    animationNode.UpdatePose(animationStates.States[i].Fraction, false);
                }
            }

            if (animationStates.States.Length > 0)
            {
                foreach (var morphTargets in virtualKeyboardScene_.morphTargetHandlers)
                {
                    morphTargets.Update();
                }
            }
        }
    }

    private void OnCommitText(string text)
    {
        if (TextCommitField == null)
        {
            return;
        }
        if (TextCommitField.isFocused && TextCommitField.caretPosition != TextCommitField.text.Length)
        {
            Debug.LogWarning("Virtual Keyboard expects an end of text caretPosition");
        }

        TextCommitField.SetTextWithoutNotify(TextCommitField.text + text);
        // Text Context currently expects an end of text caretPosition
        if (TextCommitField.isFocused && TextCommitField.caretPosition != TextCommitField.text.Length)
        {
            TextCommitField.caretPosition = TextCommitField.text.Length;
        }

        // only process change events when text changes externally
        ignoreTextCommmitFieldOnValueChanged_ = true;
        try
        {
            TextCommitField.onValueChanged.Invoke(TextCommitField.text);
        }
        finally
        {
            // Resume processing text change events
            ignoreTextCommmitFieldOnValueChanged_ = false;
        }
    }

    private void OnTextCommitFieldChange(string textContext)
    {
        if (ignoreTextCommmitFieldOnValueChanged_)
        {
            return;
        }

        ChangeTextContextInternal(textContext);
    }

    private void ChangeTextContextInternal(string textContext)
    {
        if (!isKeyboardCreated_)
        {
            return;
        }

        var result = OVRPlugin.ChangeVirtualKeyboardTextContext(textContext);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("Failed to set keyboard text context");
        }
    }

    private void OnBackspace()
    {
        if (TextCommitField == null || TextCommitField.text == String.Empty)
        {
            return;
        }
        if (TextCommitField.isFocused && TextCommitField.caretPosition != TextCommitField.text.Length)
        {
            Debug.LogWarning("Virtual Keyboard expects an end of text caretPosition");
        }

        string text = TextCommitField.text;
        TextCommitField.SetTextWithoutNotify(text.Substring(0, text.Length - 1));
        // Text Context currently expects an end of text caretPosition
        if (TextCommitField.isFocused && TextCommitField.caretPosition != TextCommitField.text.Length)
        {
            TextCommitField.caretPosition = TextCommitField.text.Length;
        }

        // only process change events when text changes externally
        ignoreTextCommmitFieldOnValueChanged_ = true;
        try
        {
            TextCommitField.onValueChanged.Invoke(TextCommitField.text);
        }
        finally
        {
            // Resume processing text change events
            ignoreTextCommmitFieldOnValueChanged_ = false;
        }
    }

    private void OnEnter()
    {
        if (TextCommitField == null)
        {
            return;
        }
        if (TextCommitField.multiLine)
        {
            OnCommitText("\n");
        }
        else
        {
            TextCommitField.onEndEdit?.Invoke(TextCommitField.text);
        }
    }

    private void OnKeyboardShown()
    {
        if (!keyboardVisible_)
        {
            keyboardVisible_ = true;
            UpdateVisibleState();
        }
    }

    private void OnKeyboardHidden()
    {
        if (keyboardVisible_)
        {
            keyboardVisible_ = false;
            UpdateVisibleState();
        }
    }

    private void UpdateVisibleState()
    {
        gameObject.SetActive(keyboardVisible_);
        if (modelAvailable_)
        {
            virtualKeyboardScene_.root.gameObject.SetActive(keyboardVisible_);
        }
    }
}
