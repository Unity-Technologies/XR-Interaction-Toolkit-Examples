using System.Collections;
using UnityEngine;

namespace BNG {
    public class Marker : GrabbableEvents {

        public Material DrawMaterial;
        public Color DrawColor = Color.red;
        public float LineWidth = 0.02f;

        public Transform RaycastStart;
        public LayerMask DrawingLayers;

        public float RaycastLength = 0.01f;

        /// <summary>
        /// Minimum distance required from points to place drawing down
        /// </summary>
        public float MinDrawDistance = 0.02f;
        public float ReuseTolerance = 0.001f;

        bool IsNewDraw = false;
        Vector3 lastDrawPoint;
        LineRenderer LineRenderer;

        // Use this to store our Marker's LineRenderers
        Transform root;
        Transform lastTransform;
        Coroutine drawRoutine = null;
        float lastLineWidth = 0;
        int renderLifeTime = 0;


        public override void OnGrab(Grabber grabber) {
            if (drawRoutine == null) {
                drawRoutine = StartCoroutine(WriteRoutine());
            }

            base.OnGrab(grabber);
        }

        public override void OnRelease() {
            if (drawRoutine != null) {
                StopCoroutine(drawRoutine);
                drawRoutine = null;
            }
            base.OnRelease();
        }

        IEnumerator WriteRoutine() {
            while (true) {
                if (Physics.Raycast(RaycastStart.position, RaycastStart.up, out RaycastHit hit, RaycastLength, DrawingLayers, QueryTriggerInteraction.Ignore)) {
                    float tipDistance = Vector3.Distance(hit.point, RaycastStart.transform.position);
                    float tipDercentage = tipDistance / RaycastLength;
                    Vector3 drawStart = hit.point + (-RaycastStart.up * 0.0005f);
                    Quaternion drawRotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
                    float lineWidth = LineWidth * (1 - tipDercentage);
                    InitDraw(drawStart, drawRotation, lineWidth, DrawColor);
                }
                else {
                    IsNewDraw = true;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        void InitDraw(Vector3 position, Quaternion rotation, float lineWidth, Color lineColor) {
            if (IsNewDraw) {
                lastDrawPoint = position;
                DrawPoint(lastDrawPoint, position, lineWidth, lineColor, rotation);
                IsNewDraw = false;
            }
            else {
                float dist = Vector3.Distance(lastDrawPoint, position);
                if (dist > MinDrawDistance) {
                    lastDrawPoint = DrawPoint(lastDrawPoint, position, lineWidth, DrawColor, rotation);
                }
            }
        }

        Vector3 DrawPoint(Vector3 lastDrawPoint, Vector3 endPosition, float lineWidth, Color lineColor, Quaternion rotation) {
            var dif = Mathf.Abs(lastLineWidth - lineWidth);
            lastLineWidth = lineWidth;
            if (dif > ReuseTolerance || renderLifeTime >= 98) {
                LineRenderer = null;
                renderLifeTime = 0;
            }
            else {
                renderLifeTime += 1;
            }
            if (IsNewDraw || LineRenderer == null) {
                lastTransform = new GameObject().transform;
                lastTransform.name = "DrawLine";
                if (root == null) {
                    root = new GameObject().transform;
                    root.name = "MarkerLineHolder";
                }
                lastTransform.parent = root;
                lastTransform.position = endPosition;
                lastTransform.rotation = rotation;
                LineRenderer = lastTransform.gameObject.AddComponent<LineRenderer>();

                LineRenderer.startColor = lineColor;
                LineRenderer.endColor = lineColor;
                LineRenderer.startWidth = lineWidth;
                LineRenderer.endWidth = lineWidth;
                var curve = new AnimationCurve();
                curve.AddKey(0, lineWidth);
                //curve.AddKey(1, lineWidth);
                LineRenderer.widthCurve = curve;
                if (DrawMaterial) {
                    LineRenderer.material = DrawMaterial;
                }
                LineRenderer.numCapVertices = 5;
                LineRenderer.alignment = LineAlignment.TransformZ;
                LineRenderer.useWorldSpace = true;
                LineRenderer.SetPosition(0, lastDrawPoint);
                LineRenderer.SetPosition(1, endPosition);
            }
            else {
                if (LineRenderer != null) {
                    LineRenderer.widthMultiplier = 1;
                    LineRenderer.positionCount += 1;
                    var curve = LineRenderer.widthCurve;
                    curve.AddKey((LineRenderer.positionCount - 1) / 100, lineWidth);
                    LineRenderer.widthCurve = curve;
                    LineRenderer.SetPosition(LineRenderer.positionCount - 1, endPosition);
                }
            }
            return endPosition;
        }

        void OnDrawGizmosSelected() {
            // Show Grip Point
            Gizmos.color = Color.green;
            Gizmos.DrawLine(RaycastStart.position, RaycastStart.position + RaycastStart.up * RaycastLength);
        }
    }
}

