using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class GrabbableBezierLine : GrabbableEvents {

        public bool HighlightOnGrabbable = true;
        public bool HighlightOnRemoteGrabbable = true;

        public LineRenderer LineToDraw;

        public int SegmentCount = 100;

        public float LerpAmount = 0.5f;
        public float HeightAdjustment = 0.5f;

        Grabber lineToGrabber;

        Grabber lineRemoteGrabbing;

        void Start() {
            if(LineToDraw == null) {
                LineToDraw = transform.GetComponent<LineRenderer>();
            }

            // Start Unhighlighted
            UnhighlightItem();
        }

        private void LateUpdate() {

            // Check basic events
            if (lineToGrabber != null) {
                // Get a middle point to use for our bezier curve
                Vector3 midPoint = Vector3.Lerp(transform.position, lineToGrabber.transform.position, LerpAmount);
                midPoint.y += HeightAdjustment;

                DrawBezierCurve(transform.position, midPoint, lineToGrabber.transform.position, LineToDraw);
            }
            // Check if remote grabbing
            else if (grab != null && grab.RemoteGrabbing) {
                if (!LineToDraw.enabled) {
                    LineToDraw.enabled = true;
                }

                // Get a middle point to use for our bezier curve
                Vector3 midPoint = Vector3.Lerp(transform.position, grab.FlyingToGrabber.transform.position, LerpAmount);
                midPoint.y += HeightAdjustment;

                DrawBezierCurve(transform.position, midPoint, grab.FlyingToGrabber.transform.position, LineToDraw);
            }
            else if(LineToDraw.enabled) {
                LineToDraw.enabled = false;
            }
        }

        // Item has been grabbed by a Grabber
        public override void OnGrab(Grabber grabber) {
            UnhighlightItem();

            base.OnGrab(grabber);
        }

        // Fires if this is the closest grabbable but wasn't in the previous frame
        public override void OnBecomesClosestGrabbable(Grabber touchingGrabber) {
            if (HighlightOnGrabbable) {
                HighlightItem(touchingGrabber);
            }

            base.OnBecomesClosestGrabbable(touchingGrabber);
        }

        public override void OnNoLongerClosestGrabbable(Grabber touchingGrabber) {
            if (HighlightOnGrabbable) {
                UnhighlightItem();
            }

            base.OnNoLongerClosestGrabbable(touchingGrabber);
        }

        public override void OnBecomesClosestRemoteGrabbable(Grabber touchingGrabber) {
            if (HighlightOnRemoteGrabbable) {
                HighlightItem(touchingGrabber);
            }

            base.OnBecomesClosestRemoteGrabbable(touchingGrabber);
        }

        public override void OnNoLongerClosestRemoteGrabbable(Grabber touchingGrabber) {
            if (HighlightOnRemoteGrabbable) {
                UnhighlightItem();
            }

            base.OnNoLongerClosestRemoteGrabbable(touchingGrabber);

        }
        public void HighlightItem(Grabber touchingGrabber) {

            if(LineToDraw == null) {
                return;
            }

            // Draw our bezier curve
            if(!LineToDraw.enabled) {
                LineToDraw.enabled = true;
            }

            lineToGrabber = touchingGrabber;
        }

        public void UnhighlightItem() {
            // Disable our line
            if(LineToDraw) {
                LineToDraw.enabled = false;
            }
            
            lineToGrabber = null;
        }

        public void DrawBezierCurve(Vector3 point0, Vector3 point1, Vector3 point2, LineRenderer lineRenderer) {
            
            lineRenderer.positionCount = SegmentCount;
            lineRenderer.useWorldSpace = true;
            
            float t = 0f;
            Vector3 b = new Vector3(0, 0, 0);
            for (int i = 0; i < SegmentCount; i++) {
                b = (1 - t) * (1 - t) * point0 + 2 * (1 - t) * t * point1 + t * t * point2;
                lineRenderer.SetPosition(i, b);
                t += (1 / (float)lineRenderer.positionCount);
            }
        }
    }   
}

