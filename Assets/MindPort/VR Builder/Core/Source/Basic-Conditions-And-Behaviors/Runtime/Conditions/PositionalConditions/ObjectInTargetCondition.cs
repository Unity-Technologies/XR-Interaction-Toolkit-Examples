using System.Collections;
using UnityEngine;

namespace VRBuilder.Core.Conditions
{
    /// <summary>
    /// An active process for "object in target" conditions.
    /// </summary>
    public abstract class ObjectInTargetActiveProcess<TData> : StageProcess<TData> where TData : class, IObjectInTargetData
    {
        protected ObjectInTargetActiveProcess(TData data) : base(data)
        {
        }

        private bool isInside;
        private float timeStarted;

        /// <inheritdoc />
        public override void Start()
        {
            Data.IsCompleted = false;
            isInside = IsInside();

            if (isInside)
            {
                timeStarted = Time.time;
            }
        }

        /// <summary>
        /// Returns true if the object is inside target.
        /// </summary>
        protected abstract bool IsInside();

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            while (true)
            {
                if (isInside != IsInside())
                {
                    isInside = !isInside;

                    if (isInside)
                    {
                        timeStarted = Time.time;
                    }
                }

                if (isInside && Time.time - timeStarted >= Data.RequiredTimeInside)
                {
                    Data.IsCompleted = true;
                    break;
                }

                yield return null;
            }
        }

        /// <inheritdoc />
        public override void End()
        {
        }

        /// <inheritdoc />
        public override void FastForward()
        {
        }
    }
}
