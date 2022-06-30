using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BNG {

    public class VRUISystem : BaseInputModule {

        [Header("XR Controller Options : ")]
        [Tooltip("This setting determines if LeftPointerTransform or RightPointerTransform will be used as a forward vector for World Space UI events")]
        public ControllerHand SelectedHand = ControllerHand.Right;

        [Tooltip("A transform on the left controller to use when raycasting for world space UI events")]
        public Transform LeftPointerTransform;

        [Tooltip("A transform on the right controller to use when raycasting for world space UI events")]
        public Transform RightPointerTransform;

        [Tooltip("Controller Binding to use for input down, up, etc.")]
        public List<ControllerBinding> ControllerInput = new List<ControllerBinding>() { ControllerBinding.RightTrigger };

        [Tooltip("Unity Input Action used to simulate a click or touch event")]
        public InputActionReference UIInputAction;

        [Tooltip("If true a PhysicsRaycaster component will be added to the UI camera, allowing physical objects to use IPointer events such as OnPointClick, OnPointEnter, etc.")]
        public bool AddPhysicsRaycaster = false;

        public LayerMask PhysicsRaycasterEventMask;

        [Tooltip("If true the Right Thumbstick will send scroll events to the UI")]
        public bool RightThumbstickScroll = true;

        [Header("Shown for Debug : ")]
        public GameObject PressingObject;
        public GameObject DraggingObject;
        public GameObject ReleasingObject;

        public PointerEventData EventData { get; private set; }

        Camera cameraCaster;
        
        private GameObject _initialPressObject;
        private bool _lastInputDown;
        bool inputDown;

        private static VRUISystem _instance;
        public static VRUISystem Instance {
            get {
                if (_instance == null) {
                    _instance = GameObject.FindObjectOfType<VRUISystem>();

                    if (_instance == null) {
                        // Check for existing event system
                        EventSystem eventSystem = EventSystem.current;
                        if(eventSystem == null) {
                            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>(); ;
                        }                        

                        _instance = eventSystem.gameObject.AddComponent<VRUISystem>();
                    }
                }

                return _instance;
            }
        }

        protected override void Awake() {

            UpdateControllerHand(SelectedHand);

            EventData = new PointerEventData(eventSystem);
            EventData.position = new Vector2(cameraCaster.pixelWidth / 2, cameraCaster.pixelHeight / 2);

            AssignCameraToAllCanvases(cameraCaster);
        }       

        void init() {
            if(cameraCaster == null) {

                // Create the camera required for the caster.
                // We can reduce the fov and disable the camera component for performance
                var go = new GameObject("CameraCaster");
                cameraCaster = go.AddComponent<Camera>();
                cameraCaster.stereoTargetEye = StereoTargetEyeMask.None;
                cameraCaster.fieldOfView = 5f;
                cameraCaster.nearClipPlane = 0.01f;
                cameraCaster.clearFlags = CameraClearFlags.Nothing;
                cameraCaster.enabled = false;

                // Add PhysicsRaycaster so other objects can subscribe to IPointer events
                if(AddPhysicsRaycaster) {
                    var pr = go.AddComponent<PhysicsRaycaster>();
                    pr.eventMask = PhysicsRaycasterEventMask;
                }
            }
        }

        public override void Process() {

            // Input isn't ready if this Camera Caster's gameObject isn't active
            if (EventData == null || !CameraCasterReady()) {
                return;
            }

            EventData.position = new Vector2(cameraCaster.pixelWidth / 2, cameraCaster.pixelHeight / 2);

            eventSystem.RaycastAll(EventData, m_RaycastResultCache);

            EventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();

            // Handle Hover
            HandlePointerExitAndEnter(EventData, EventData.pointerCurrentRaycast.gameObject);

            // Handle Drag
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);

            // Handle scroll
            if(RightThumbstickScroll) {
                EventData.scrollDelta = InputBridge.Instance.RightThumbstickAxis;
                if (!Mathf.Approximately(EventData.scrollDelta.sqrMagnitude, 0)) {
                    ExecuteEvents.Execute(ExecuteEvents.GetEventHandler<IScrollHandler>(EventData.pointerCurrentRaycast.gameObject), EventData, ExecuteEvents.scrollHandler);
                }
            }
            
            // Press Events
            inputDown = InputReady();

            // On Trigger Down > TriggerDownValue this frame but not last
            if (inputDown && _lastInputDown == false) {
                PressDown();
            }
            // On Held Down
            else if(inputDown) {
                Press();
            }
            // On Release
            else {
                Release();
            }

            _lastInputDown = inputDown;
        }

        public virtual bool InputReady() {

            // Input isn't ready if this Camera Caster's gameObject isn't active
            if(!CameraCasterReady()) {
                return false;
            }

            // Check Unity Action
            if (UIInputAction != null && UIInputAction.action.ReadValue<float>() == 1f) {
                return true;
            }

            // Check for bound controller button
            for (int x = 0; x < ControllerInput.Count; x++) {
                if (InputBridge.Instance.GetControllerBindingValue(ControllerInput[x])) {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Returns true if we have a camera caster enabled and ready to send out data
        /// Returns false if the camera caster is null or it's gameobject is disabled
        /// </summary>
        /// <returns></returns>
        public virtual bool CameraCasterReady() {
            if (cameraCaster != null && !cameraCaster.gameObject.activeInHierarchy) {
                return false;
            }

            return true;
        }

        public virtual void PressDown() {
            EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

            // Deselect if selection changed
            if(_initialPressObject != null) {
                // ExecuteEvents.Execute(_initialPressObject, EventData, ExecuteEvents.deselectHandler);
                _initialPressObject = null;
            }

            _initialPressObject = ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerPressRaycast.gameObject);

            // Set Press Objects and Events
            SetPressingObject(_initialPressObject);
            ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerDownHandler);

            // Set Drag Objects and Events
            SetDraggingObject(ExecuteEvents.GetEventHandler<IDragHandler>(EventData.pointerPressRaycast.gameObject));
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.beginDragHandler);
        }

        public virtual void Press() {
            EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

            // Set Press Objects and Events
            SetPressingObject(ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerPressRaycast.gameObject));
            ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerDownHandler);

            // Set Drag Objects and Events
            SetDraggingObject(ExecuteEvents.GetEventHandler<IDragHandler>(EventData.pointerPressRaycast.gameObject));
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.beginDragHandler);
        }

        public virtual void Release() {

            SetReleasingObject(ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerCurrentRaycast.gameObject));

            // Considered a click event if released after an initial click
            if (EventData.pointerPress == ReleasingObject) {
                ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerClickHandler);
            }

            ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.endDragHandler);

            // Send deselect to
            // ExecuteEvents.Execute(ReleasingObject, EventData, ExecuteEvents.deselectHandler);

            ClearAll();
        }        

        public virtual void ClearAll() {
            SetPressingObject(null);
            SetDraggingObject(null);

            EventData.pointerCurrentRaycast.Clear();
        }

        public virtual void SetPressingObject(GameObject pressing) {
            EventData.pointerPress = pressing;
            PressingObject = pressing;
        }

        public virtual void SetDraggingObject(GameObject dragging) {
            EventData.pointerDrag = dragging;
            DraggingObject = dragging;
        }

        public virtual void SetReleasingObject(GameObject releasing) {
            ReleasingObject = releasing;
        }

        public virtual void AssignCameraToAllCanvases(Camera cam) {
            Canvas[] allCanvas = FindObjectsOfType<Canvas>();
            for (int x = 0; x < allCanvas.Length; x++) {
                AddCanvasToCamera(allCanvas[x], cam);
            }
        }

        public virtual void AddCanvas(Canvas canvas) {
            AddCanvasToCamera(canvas, cameraCaster);
        }

        public virtual void AddCanvasToCamera(Canvas canvas, Camera cam) {
            canvas.worldCamera = cam;
        }

        public virtual void UpdateControllerHand(ControllerHand hand) {
            
            // Make sure variables exist
            init();

            // Setup the Transform
            if (hand == ControllerHand.Left && LeftPointerTransform != null) {
                cameraCaster.transform.parent = LeftPointerTransform;
                cameraCaster.transform.localPosition = Vector3.zero;
                cameraCaster.transform.localEulerAngles = Vector3.zero;
            }
            else if (hand == ControllerHand.Right && RightPointerTransform != null) {
                cameraCaster.transform.parent = RightPointerTransform;
                cameraCaster.transform.localPosition = Vector3.zero;
                cameraCaster.transform.localEulerAngles = Vector3.zero;
            }
        }
    }
}
