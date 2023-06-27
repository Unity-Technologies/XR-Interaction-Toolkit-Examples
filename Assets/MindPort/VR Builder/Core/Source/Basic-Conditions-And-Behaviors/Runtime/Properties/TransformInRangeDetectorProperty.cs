using System;
using UnityEngine;

namespace VRBuilder.Core.Properties
{
    public class TransformInRangeDetectorProperty : ProcessSceneObjectProperty
    {
        private bool isTransformInRange = false;
        private Transform trackedTransform;

        public float DetectionRange { get; set; }

        public class RangeEventArgs : EventArgs
        {
            public readonly Transform TrackedTransform;
            public RangeEventArgs(Transform trackedTransform)
            {
                TrackedTransform = trackedTransform;
            }
        }

        public event EventHandler<RangeEventArgs> EnteredRange;
        public event EventHandler<RangeEventArgs> ExitedRange;

        private void Update()
        {
            Refresh();
        }

        /// <summary>
        /// Check if there are transforms in range and fire the appropriate events.
        /// </summary>
        public void Refresh()
        {
            if (trackedTransform == null)
            {
                return;
            }

            bool isInsideArea = IsTargetInsideRange();

            if (isInsideArea && isTransformInRange == false)
            {
                EmitEnteredArea();
                isTransformInRange = true;
            }
            else if (isInsideArea == false && isTransformInRange)
            {
                EmitExitedArea();
                isTransformInRange = false;
            }
        }

        public virtual bool IsTargetInsideRange()
        {
            return Vector3.Distance(transform.position, trackedTransform.position) < DetectionRange;
        }

        public void SetTrackedTransform(Transform transformToBeTracked)
        {
            trackedTransform = transformToBeTracked;
        }

        public void DestroySelf()
        {
            Destroy(this);
        }

        protected void EmitEnteredArea()
        {
            if (EnteredRange != null)
            {
                EnteredRange.Invoke(this, new RangeEventArgs(trackedTransform));
            }
        }

        protected void EmitExitedArea()
        {
            if (ExitedRange != null)
            {
                ExitedRange.Invoke(this, new RangeEventArgs(trackedTransform));
            }
        }
    }
}
