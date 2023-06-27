using System;
using VRBuilder.Core.SceneObjects;
using UnityEngine;

namespace VRBuilder.Core.Properties
{
    public class ColliderWithTriggerProperty : ProcessSceneObjectProperty
    {
        public class ColliderWithTriggerEventArgs : EventArgs
        {
            public readonly GameObject CollidedObject;
            public ColliderWithTriggerEventArgs(GameObject collidedObject)
            {
                CollidedObject = collidedObject;
            }
        }

        public event EventHandler<ColliderWithTriggerEventArgs> EnteredTrigger;
        public event EventHandler<ColliderWithTriggerEventArgs> ExitedTrigger;

        protected override void OnEnable()
        {
            base.OnEnable();

            Collider[] colliders = GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                Debug.LogErrorFormat("Object '{0}' with ColliderProperty must have at least one Collider attached.", SceneObject.UniqueName);
            }
            else
            {
                if (CheckIfObjectHasTriggerCollider() == false)
                {
                    Debug.LogErrorFormat("Object '{0}' with ColliderProperty must have at least one Collider with isTrigger set to true.", SceneObject.UniqueName);
                }
            }
        }

        private bool CheckIfObjectHasTriggerCollider()
        {
            bool hasTriggerCollider = false;

            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider.enabled && collider.isTrigger)
                {
                    hasTriggerCollider = true;
                    break;
                }
            }

            return hasTriggerCollider;
        }

        public bool IsTransformInsideTrigger(Transform targetTransform)
        {
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider.enabled && collider.isTrigger)
                {      
                    // If object and collider is in same position return true as it's not possible to raycast
                    if (collider.bounds.center == targetTransform.position)
                    {
                        return true;
                    }
                    else
                    {
                        Vector3 targetTransformToColliderVector = (collider.bounds.center - targetTransform.position);

                        Ray ray = new Ray(targetTransform.position, targetTransformToColliderVector.normalized);
                        RaycastHit hitInfo;
                        float maxDistance = targetTransformToColliderVector.magnitude;

                        // If the ray doesn't hit the collider, it means the object is inside
                        if (collider.Raycast(ray, out hitInfo, maxDistance) == false)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (EnteredTrigger != null)
            {
                EnteredTrigger.Invoke(this, new ColliderWithTriggerEventArgs(other.gameObject));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (ExitedTrigger != null)
            {
                ExitedTrigger.Invoke(this, new ColliderWithTriggerEventArgs(other.gameObject));
            }
        }

        /// <summary>
        /// Instantaneously move target inside the collider and fire the event.
        /// </summary>
        /// <param name="target"></param>
        public void FastForwardEnter(ISceneObject target)
        {
            target.GameObject.transform.rotation = transform.rotation;
            target.GameObject.transform.position = transform.position;

            if (EnteredTrigger != null)
            {
                EnteredTrigger.Invoke(this, new ColliderWithTriggerEventArgs(target.GameObject));
            }
        }
    }
}
