using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// Contains various functionality used in the Demo Scene such as  Switching Hands and Locomotion
    /// </summary>
    public class DemoScript : MonoBehaviour {

        public Text LabelToUpdate;

        /// <summary>
        /// Used in the demo scene to shoot various objects
        /// </summary>
        public ProjectileLauncher DemoLauncher;

        /// <summary>
        /// Max number of objects to launch from DemoLauncher. Old objects will be destroyed to make room for new.
        /// </summary>
        public int MaxLaunchedObjects = 5;

        List<GameObject> launchedObjects;

        /// <summary>
        /// Used in demo scene
        /// </summary>
        public Text JoystickText;
        
        /// <summary>
        /// Used in demo scene to spawn ammo clips
        /// </summary>
        public GameObject AmmoObject;

        /// <summary>
        /// Holds all of the grabbable objects in the scene
        /// </summary>
        public Transform ItemsHolder;

        Dictionary<Grabbable, PosRot> _initalGrabbables;

        // Strictly used in demo scene
        Rigidbody cubeRigid;
        Rigidbody cubeRigid1;
        Rigidbody cubeRigid2;
        Rigidbody cubeRigid3;

        // Start is called before the first frame update
        void Start() {

            launchedObjects = new List<GameObject>();

            VRUtils.Instance.Log("Output text here by using VRUtils.Log(\"Message Here\");");
            VRUtils.Instance.Log("Click the Menu button to toggle this menu.");

            // Set up initial grabbables so we can reset them later
            if(ItemsHolder) {
                _initalGrabbables = new Dictionary<Grabbable, PosRot>();
                var allGrabs = ItemsHolder.GetComponentsInChildren<Grabbable>();
                foreach(var grab in allGrabs) {
                    _initalGrabbables.Add(grab, new PosRot() { Position = grab.transform.position, Rotation = grab.transform.rotation });
                }
            }

            // Spinning Cubes in Demo Scene
            initGravityCubes();
        }

        // Some example controls useful for testing
        void Update() {

            // Spin Cubes around
            rotateGravityCubes();
        }

        public void UpdateSliderText(float sliderValue) {
            if (LabelToUpdate != null) {
                LabelToUpdate.text = "Power : " + (int)sliderValue + "%";
            }

            // Scale Launcher based on slider value
            if(DemoLauncher) {
                DemoLauncher.SetForce(DemoLauncher.GetInitialProjectileForce() * (sliderValue / 100));
            }
        }

        public void UpdateJoystickText(float leverX, float leverY) {
            if (JoystickText != null) {
                JoystickText.text = "X : " + (int)leverX + "\nY: " + (int)leverY;
            }
        }

        public void ResetGrabbables() {
            foreach (var kvp in _initalGrabbables) {
                // Only reset high level grabbables that aren't being held
                if(kvp.Key != null && !kvp.Key.BeingHeld && kvp.Key.transform.parent == ItemsHolder) {
                    kvp.Key.transform.position = kvp.Value.Position;
                    kvp.Key.transform.rotation = kvp.Value.Rotation;

                    Rigidbody rb = kvp.Key.GetComponent<Rigidbody>();
                    if(rb) {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }

        List<Grabbable> demoClips;
        public void GrabAmmo(Grabber grabber) {

            if(demoClips == null) {
                demoClips = new List<Grabbable>();
            }

            if(demoClips.Count > 0 && demoClips[0] == null) {
                demoClips.RemoveAt(0);
            }

            if(AmmoObject != null) {

                // Make room for new clip. This ensures the demo doesn't ge bogged down
                if(demoClips.Count > 4 && demoClips[0] != null && demoClips[0].transform.parent == null) {
                    GameObject.Destroy(demoClips[0].gameObject);
                }

                GameObject ammo = Instantiate(AmmoObject, grabber.transform.position, grabber.transform.rotation) as GameObject;
                Grabbable g = ammo.GetComponent<Grabbable>();

                // Disable rings for performance
                GrabbableRingHelper grh = ammo.GetComponentInChildren<GrabbableRingHelper>();
                Destroy(grh);
                RingHelper r = ammo.GetComponentInChildren<RingHelper>();
                Destroy(r.gameObject);

                // Offset to hand
                ammo.transform.parent = grabber.transform;
                ammo.transform.localPosition = -g.GrabPositionOffset;
                ammo.transform.parent = null;

                if(g != null) {
                    demoClips.Add(g);
                }

                grabber.GrabGrabbable(g);
            }
        }

        public void ShootLauncher() {
            if(launchedObjects == null) {
                launchedObjects = new List<GameObject>();
            }

            // Went over max. Destroy oldest launch object
            if(launchedObjects.Count > MaxLaunchedObjects) {
                launchedObjects.Remove(launchedObjects[0]);
                GameObject.Destroy(launchedObjects[0]);
            }

            launchedObjects.Add(DemoLauncher.ShootProjectile(DemoLauncher.ProjectileForce));
        }

        void initGravityCubes() {
            // Makes cubes spin in example scene
            if(GameObject.Find("GravityCube 1")) {
                cubeRigid = GameObject.Find("GravityCube 1").GetComponent<Rigidbody>();
            }

            if (GameObject.Find("GravityCube 2")) {
                cubeRigid1 = GameObject.Find("GravityCube 2").GetComponent<Rigidbody>();
            }

            if (GameObject.Find("GravityCube 3")) {
                cubeRigid2 = GameObject.Find("GravityCube 3").GetComponent<Rigidbody>();
            }

            if (GameObject.Find("GravityCube 4")) {
                cubeRigid3 = GameObject.Find("GravityCube 4").GetComponent<Rigidbody>();
            }
        }

        // Cache for performance
        Vector3 rotateX = new Vector3(0.2f, 0, 0);
        Vector3 rotateY = new Vector3(0, 0.2f, 0);
        Vector3 rotateZ = new Vector3(0, 0, 0.2f);
        Vector3 rotateXYX = new Vector3(0.2f, 0.2f, 0.2f);
        void rotateGravityCubes() {
            if (cubeRigid) {
                cubeRigid.angularVelocity = rotateX;
            }

            if (cubeRigid1) {
                cubeRigid1.angularVelocity = rotateY;
            }

            if (cubeRigid2) {
                cubeRigid2.angularVelocity = rotateZ;
            }

            if (cubeRigid3) {
                cubeRigid3.angularVelocity = rotateXYX;
            }
        }
    }

    public class PosRot {
        public Vector3 Position;
        public Quaternion Rotation;
    }
}