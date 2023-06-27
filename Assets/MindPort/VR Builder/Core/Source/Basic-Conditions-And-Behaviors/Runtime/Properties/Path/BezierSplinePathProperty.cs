using UnityEngine;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core.Properties
{    
    /// <summary>
    /// Path property that generates a path from a <see cref="BezierSpline"/>.
    /// </summary>
    [RequireComponent(typeof(BezierSpline))]
    public class BezierSplinePathProperty : ProcessSceneObjectProperty, IPathProperty
    {
        private BezierSpline spline;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (spline == null)
            {
                spline = GetComponent<BezierSpline>();
            }
        }
       
        /// <inheritdoc/>
        public Vector3 GetPoint(float t)
        {
            return spline.GetPoint(t);
        }

        /// <inheritdoc/>
        public Vector3 GetDirection(float t)
        {
            return spline.GetDirection(t);
        }
    }
}
