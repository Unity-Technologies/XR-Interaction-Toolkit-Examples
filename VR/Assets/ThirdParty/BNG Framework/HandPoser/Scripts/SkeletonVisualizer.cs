using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {

    [ExecuteInEditMode]
    public class SkeletonVisualizer : MonoBehaviour {

        public bool ShowGizmos = true;

        [Range(0.0f, 1f)]
        public float JointRadius = 0.00875f;

        [Range(0.0f, 5f)]
        public float BoneThickness = 3f;

        public Color GizmoColor = new Color(255f, 255f, 255f, 0.5f);

        public bool ShowTransformNames = false;

        bool isQuiting;
        void OnApplicationQuit() {
            isQuiting = true;
        }

        void OnDestroy() {
            if (isQuiting) {
                return;
            }

#if UNITY_EDITOR
            // Remove any components we had added
            if (Application.isEditor && !Application.isPlaying) {
                ResetEditorHandles();

                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {

            // Show Gizmos indicating bones / joints
            if (ShowGizmos) {
                Transform[] children = GetComponentsInChildren<Transform>();

                int childCount = children.Length;
                for (int x = 0; x < childCount; x++) {

                    Transform child = children[x];

                    // Don't count the root object
                    if(child == transform ) {
                        continue;
                    }

                    bool isFingerTip = IsTipOfBone(child);
                    float opacity = isFingerTip ? 0.5f : 0.1f;
                    EditorHandle handle = child.gameObject.GetComponent<EditorHandle>();

                    // Create the editor handle if not available
                    if (handle == null) {
                        handle = child.gameObject.AddComponent<EditorHandle>();

                        // Hide in Inspector
                        child.gameObject.hideFlags = HideFlags.None;
                        handle.hideFlags = HideFlags.HideInInspector;
                    }

                    handle.Radius = JointRadius / 1000f;
                    handle.BaseColor = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, opacity);
                    handle.ShowTransformName = ShowTransformNames;

                    // Connect with Lines
                    if (child.parent != null) {
                        // Line way of drawing
                        // Gizmos.DrawLine(child.position, child.parent.position);

                        // Smoothed line way of drawing
                        // Only show if the line goes far enough
                        if(Vector3.Distance(child.position, child.parent.position) > 0.001f) {
                            UnityEditor.Handles.DrawBezier(child.position, child.parent.position, child.position, child.parent.position, GizmoColor, null, BoneThickness);
                        }
                    }
                }
            }
            else {
                ResetEditorHandles();
            }
        }

        void OnDrawGizmosSelected() {
            // Update every frame even while in editor
            if (!Application.isPlaying) {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
        }
#endif


        /// <summary>
        /// I.e. a finger tip, tip of toe, etc.
        /// </summary>
        /// <param name="fingerJoint"></param>
        /// <returns></returns>
        public virtual bool IsTipOfBone(Transform fingerJoint) {

            if (fingerJoint.childCount == 0 && fingerJoint != transform && fingerJoint.parent != transform) {
                return true;
            }

            return false;
        }

        public void ResetEditorHandles() {
            EditorHandle[] handles = GetComponentsInChildren<EditorHandle>();
            for (int x = 0; x < handles.Length; x++) {
                if (handles[x] != null && handles[x].gameObject != null) {
                    GameObject.DestroyImmediate((handles[x]));
                }
            }
        }
    }
}

