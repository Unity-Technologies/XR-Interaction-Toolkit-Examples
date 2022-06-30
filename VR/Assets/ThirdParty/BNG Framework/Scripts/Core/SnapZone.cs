using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class SnapZone : MonoBehaviour {

        [Header("Starting / Held Item")]
        [Tooltip("The currently held item. Set this in the editor to equip on Start().")]
        public Grabbable HeldItem;

        [Tooltip("TSet this in the editor to equip on Start().")]
        public Grabbable StartingItem;

        [Header("Options")]
        /// <summary>
        /// If false, Item will Move back to inventory space if player drops it.
        /// </summary>
        [Tooltip("If false, Item will Move back to inventory space if player drops it.")]
        public bool CanDropItem = true;

        /// <summary>
        /// If false the snap zone cannot have it's content replaced.
        /// </summary>
        [Tooltip("If false the snap zone cannot have it's content replaced.")]
        public bool CanSwapItem = true;

        /// <summary>
        /// If false the item inside the snap zone may not be removed
        /// </summary>
        [Tooltip("If false the snap zone cannot have it's content replaced.")]
        public bool CanRemoveItem = true;

        /// <summary>
        /// Multiply Item Scale times this when in snap zone.
        /// </summary>
        [Tooltip("Multiply Item Scale times this when in snap zone.")]
        public float ScaleItem = 1f;
        private float _scaleTo;

        public bool DisableColliders = true;
        List<Collider> disabledColliders = new List<Collider>();

        [Tooltip("If true the item inside the SnapZone will be duplicated, instead of removed, from the SnapZone.")]
        public bool DuplicateItemOnGrab = false;

        /// <summary>
        /// Only snap if Grabbable was dropped maximum of X seconds ago
        /// </summary>
        [Tooltip("Only snap if Grabbable was dropped maximum of X seconds ago")]
        public float MaxDropTime = 0.1f;

        /// <summary>
        /// Last Time.time this item was snapped into
        /// </summary>
        [HideInInspector]
        public float LastSnapTime;

        [Header("Filtering")]
        /// <summary>
        /// If not empty, can only snap objects if transform name contains one of these strings
        /// </summary>
        [Tooltip("If not empty, can only snap objects if transform name contains one of these strings")]
        public List<string> OnlyAllowNames;

        /// <summary>
        /// Do not allow snapping if transform contains one of these names
        /// </summary>
        [Tooltip("Do not allow snapping if transform contains one of these names")]
        public List<string> ExcludeTransformNames;

        [Header("Audio")]
        public AudioClip SoundOnSnap;
        public AudioClip SoundOnUnsnap;


        [Header("Events")]
        /// <summary>
        /// Optional Unity Event  to be called when something is snapped to this SnapZone. Passes in the Grabbable that was attached.
        /// </summary>
        public GrabbableEvent OnSnapEvent;

        /// <summary>
        /// Optional Unity Event to be called when something has been detached from this SnapZone. Passes in the Grabbable is being detattached.
        /// </summary>
        public GrabbableEvent OnDetachEvent;

        GrabbablesInTrigger gZone;

        Rigidbody heldItemRigid;
        bool heldItemWasKinematic;
        Grabbable trackedItem; // If we can't drop the item, track it separately

        // Closest Grabbable in our trigger
        [HideInInspector]
        public Grabbable ClosestGrabbable;

        SnapZoneOffset offset;

        void Start() {
            gZone = GetComponent<GrabbablesInTrigger>();
            _scaleTo = ScaleItem;

            // Auto Equip item by moving it into place and grabbing it
            if(StartingItem != null) {
                StartingItem.transform.position = transform.position;
                GrabGrabbable(StartingItem);
            }
            // Can also use HeldItem (retains backwards compatibility)
            else if (HeldItem != null) {
                HeldItem.transform.position = transform.position;
                GrabGrabbable(HeldItem);
            }
        }

        void Update() {

            ClosestGrabbable = getClosestGrabbable();

            // Can we grab something
            if (HeldItem == null && ClosestGrabbable != null) {
                float secondsSinceDrop = Time.time - ClosestGrabbable.LastDropTime;
                if (secondsSinceDrop < MaxDropTime) {
                    GrabGrabbable(ClosestGrabbable);
                }
            }

            // Keep snapped to us or drop
            if (HeldItem != null) {

                // Something picked this up or changed transform parent
                if (HeldItem.BeingHeld || HeldItem.transform.parent != transform) {
                    ReleaseAll();
                }
                else {
                    // Scale Item while inside zone.                                            
                    HeldItem.transform.localScale = Vector3.Lerp(HeldItem.transform.localScale, HeldItem.OriginalScale * _scaleTo, Time.deltaTime * 30f);
                    
                    // Make sure this can't be grabbed from the snap zone
                    if(HeldItem.enabled || (disabledColliders != null && disabledColliders.Count > 0 && disabledColliders[0] != null && disabledColliders[0].enabled)) {
                        disableGrabbable(HeldItem);
                    }
                }
            }

            // Can't drop item. Lerp to position if not being held
            if (!CanDropItem && trackedItem != null && HeldItem == null) {
                if (!trackedItem.BeingHeld) {
                    GrabGrabbable(trackedItem);
                }
            }
        }

        Grabbable getClosestGrabbable() {

            Grabbable closest = null;
            float lastDistance = 9999f;

            if (gZone == null || gZone.NearbyGrabbables == null) {
                return null;
            }

            foreach (var g in gZone.NearbyGrabbables) {

                // Collider may have been disabled
                if(g.Key == null) {
                    continue;
                }

                float dist = Vector3.Distance(transform.position, g.Value.transform.position);
                if(dist < lastDistance) {

                    //  Not allowing secondary grabbables such as slides
                    if(g.Value.OtherGrabbableMustBeGrabbed != null) {
                        continue;
                    }

                    // Don't allow SnapZones in SnapZones
                    if(g.Value.GetComponent<SnapZone>() != null) {
                        continue;
                    }

                    // Don't allow InvalidSnapObjects to snap
                    if (g.Value.CanBeSnappedToSnapZone == false) {
                        continue;
                    }

                    // Must contain transform name
                    if (OnlyAllowNames != null && OnlyAllowNames.Count > 0) {
                        string transformName = g.Value.transform.name;
                        bool matchFound = false;
                        for (int x = 0; x < OnlyAllowNames.Count; x++) {
                            string name = OnlyAllowNames[x];
                            if (transformName.Contains(name)) {
                                matchFound = true;                                
                            }
                        }

                        // Not a valid match
                        if(!matchFound) {
                            continue;
                        }
                    }

                    // Check for name exclusion
                    if (ExcludeTransformNames != null) {
                        string transformName = g.Value.transform.name;
                        bool matchFound = false;
                        for (int x = 0; x < ExcludeTransformNames.Count; x++) {
                            // Not a valid match
                            if (transformName.Contains(ExcludeTransformNames[x])) {
                                matchFound = true;
                            }
                        }
                        // Exclude this
                        if (matchFound) {
                            continue;
                        }
                    }

                    // Only valid to snap if being held or recently dropped
                    if (g.Value.BeingHeld || (Time.time - g.Value.LastDropTime < MaxDropTime)) {
                        closest = g.Value;
                        lastDistance = dist;
                    }
                }
            }

            return closest;
        }

        public void GrabGrabbable(Grabbable grab) {

            // Grab is already in Snap Zone
            if(grab.transform.parent != null && grab.transform.parent.GetComponent<SnapZone>() != null) {
                return;
            }

            if(HeldItem != null) {
                ReleaseAll();
            }

            HeldItem = grab;
            heldItemRigid = HeldItem.GetComponent<Rigidbody>();

            // Mark as kinematic so it doesn't fall down
            if(heldItemRigid) {
                heldItemWasKinematic = heldItemRigid.isKinematic;
                heldItemRigid.isKinematic = true;
            }
            else {
                heldItemWasKinematic = false;
            }

            // Set the parent of the object 
            grab.transform.parent = transform;

            // Set scale factor            
            // Use SnapZoneScale if specified
            if (grab.GetComponent<SnapZoneScale>()) {
                _scaleTo = grab.GetComponent<SnapZoneScale>().Scale;
            }
            else {
                _scaleTo = ScaleItem;
            }

            // Is there an offset to apply?
            SnapZoneOffset off = grab.GetComponent<SnapZoneOffset>();
            if(off) {
                offset = off;
            }
            else {
                offset = grab.gameObject.AddComponent<SnapZoneOffset>();
                offset.LocalPositionOffset = Vector3.zero;
                offset.LocalRotationOffset = Vector3.zero;
            }

            // Lock into place
            if (offset) {
                HeldItem.transform.localPosition = offset.LocalPositionOffset;
                HeldItem.transform.localEulerAngles = offset.LocalRotationOffset;
            }
            else {
                HeldItem.transform.localPosition = Vector3.zero;
                HeldItem.transform.localEulerAngles = Vector3.zero;
            }

            // Disable the grabbable. This is picked up through a Grab Action
            disableGrabbable(grab);

            // Call Grabbable Event from SnapZone
            if (OnSnapEvent != null) {
                OnSnapEvent.Invoke(grab);
            }

            // Fire Off Events on Grabbable
            GrabbableEvents[] ge = grab.GetComponents<GrabbableEvents>();
            if (ge != null) {
                for (int x = 0; x < ge.Length; x++) {
                    ge[x].OnSnapZoneEnter();
                }
            }

            if (SoundOnSnap) {
                // Only play the sound if not just starting the scene
                if(Time.timeSinceLevelLoad > 0.1f) {
                    VRUtils.Instance.PlaySpatialClipAt(SoundOnSnap, transform.position, 0.75f);
                }
            }

            LastSnapTime = Time.time;
        }

        void disableGrabbable(Grabbable grab) {

            if (DisableColliders) {
                disabledColliders = grab.GetComponentsInChildren<Collider>(false).ToList();
                for (int x = 0; x < disabledColliders.Count; x++) {
                    disabledColliders[x].enabled = false;
                }
            }

            // Disable the grabbable. This is picked up through a Grab Action
            grab.enabled = false;
        }

        /// <summary>
        /// This is typically called by the GrabAction on the SnapZone
        /// </summary>
        /// <param name="grabber"></param>
        public void GrabEquipped(Grabber grabber) {

            if (grabber != null) {
                if(HeldItem) {

                    // Not allowed to be removed
                    if(!CanBeRemoved()) {
                        return;
                    }

                    var g = HeldItem;
                    if(DuplicateItemOnGrab) {

                        ReleaseAll();

                        // Position next to grabber if somewhat far away
                        if (Vector3.Distance(g.transform.position, grabber.transform.position) > 0.2f) {
                            g.transform.position = grabber.transform.position;
                        }

                        // Instantiate the object before it is grabbed
                        GameObject go = Instantiate(g.gameObject, transform.position, Quaternion.identity) as GameObject;
                        Grabbable grab = go.GetComponent<Grabbable>();

                        // Ok to attach it to snap zone now
                        this.GrabGrabbable(grab);

                        // Finish Grabbing the desired object
                        grabber.GrabGrabbable(g);
                    }
                    else {
                        ReleaseAll();

                        // Position next to grabber if somewhat far away
                        if (Vector3.Distance(g.transform.position, grabber.transform.position) > 0.2f) {
                            g.transform.position = grabber.transform.position;
                        }

                        // Do grab
                        grabber.GrabGrabbable(g);
                    }
                }
            }
        }

        public virtual bool CanBeRemoved() {
            // Not allowed to be removed
            if (!CanRemoveItem) {
                return false;
            }

            // Not a valid grab if we just snapped this item in an it's a toggle type
            if (HeldItem.Grabtype == HoldType.Toggle && (Time.time - LastSnapTime < 0.1f)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Release  everything snapped to us
        /// </summary>
        public void ReleaseAll() {

            // No need to keep checking
            if (HeldItem == null) {
                return;
            }

            // Still need to keep track of item if we can't fully drop it
            if (!CanDropItem && HeldItem != null) {
                trackedItem = HeldItem;
            }

            HeldItem.ResetScale();

            if (DisableColliders && disabledColliders != null) {
                foreach (var c in disabledColliders) {
                    if(c) {
                        c.enabled = true;
                    }
                }
            }
            disabledColliders = null;

            // Reset Kinematic status
            if(heldItemRigid) {
                heldItemRigid.isKinematic = heldItemWasKinematic;
            }

            HeldItem.enabled = true;
            HeldItem.transform.parent = null;

            // Play Unsnap sound
            if(HeldItem != null) {
                if (SoundOnUnsnap) {
                    if (Time.timeSinceLevelLoad > 0.1f) {
                        VRUtils.Instance.PlaySpatialClipAt(SoundOnUnsnap, transform.position, 0.75f);
                    }
                }

                // Call event
                if (OnDetachEvent != null) {
                    OnDetachEvent.Invoke(HeldItem);
                }

                // Fire Off Grabbable Events
                GrabbableEvents[] ge = HeldItem.GetComponents<GrabbableEvents>();
                if (ge != null) {
                    for (int x = 0; x < ge.Length; x++) {
                        ge[x].OnSnapZoneExit();
                    }
                }
            }

            HeldItem = null;
        }
    }
}
