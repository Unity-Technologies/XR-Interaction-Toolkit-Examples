using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// A simple decal with random scale and rotation
    /// </summary>
    public class BulletHole : MonoBehaviour {
        public Transform BulletHoleDecal;

        public float MaxScale = 1f;
        public float MinScale = 0.75f;

        public bool RandomYRotation = true;

        public float DestroyTime = 10f;

        // Start is called before the first frame update
        void Start() {
            transform.localScale = Vector3.one * Random.Range(0.75f, 1.5f);

            if (BulletHoleDecal != null && RandomYRotation) {
                Vector3 currentRotation = BulletHoleDecal.transform.localEulerAngles;
                BulletHoleDecal.transform.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, Random.Range(0, 90f));
            }

            // Make sure audio follows timestep pitch
            AudioSource audio = GetComponent<AudioSource>();
            audio.pitch = Time.timeScale;

            Invoke("DestroySelf", DestroyTime);
        }

        public void TryAttachTo(Collider col) {
            if (transformIsEqualScale(col.transform)) {
                BulletHoleDecal.parent = col.transform;
                GameObject.Destroy(BulletHoleDecal.gameObject, DestroyTime);
            }
            // No need to parent if static collider
            else if (col.gameObject.isStatic) {
                GameObject.Destroy(BulletHoleDecal.gameObject, DestroyTime);
            }
            // Malformed collider (non-equal proportions)
            // Just destroy the decal quickly
            else {
                // BulletHoleDecal.parent = col.transform;
                GameObject.Destroy(BulletHoleDecal.gameObject, 0.1f);
            }
        }

        // Are all scales equal? Ex : 1, 1, 1
        bool transformIsEqualScale(Transform theTransform) {
            return theTransform.localScale.x == theTransform.localScale.y && theTransform.localScale.x == theTransform.localScale.z;
        }

        void DestroySelf() {
            transform.parent = null;
            GameObject.Destroy(this.gameObject);
        }
    }
}

