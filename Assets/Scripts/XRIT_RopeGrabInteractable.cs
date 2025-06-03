#if UNITY_XR_INTERACTION_TOOLKIT && META_ROPE_UTILITIES // Assuming BurstRope is in Meta.Utilities.Ropes

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Meta.Utilities.Ropes; // For BurstRope and BindingPoint

[RequireComponent(typeof(XRGrabInteractable))]
public class XRIT_RopeGrabInteractable : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField]
    [Tooltip("The BurstRope system this interactable will control.")]
    private BurstRope m_Rope;

    [SerializeField]
    [Tooltip("Strength of the binding. A value of 0 means an unbreakable bond.")]
    private float m_BindingStrength = 0.0f; // 0 for unbreakable, higher for more 'give' if BurstRope supports it

    private XRGrabInteractable m_GrabInteractable;
    private IXRSelectInteractor m_Interactor;

    // Store the original index of our binding point in the BurstRope's list.
    // If -1, we need to add a new one.
    private int m_BindingPointListIndex = -1;
    private BindingPoint m_ManagedBindingPoint;

    void Awake()
    {
        m_GrabInteractable = GetComponent<XRGrabInteractable>();

        if (m_Rope == null)
        {
            // Try to find BurstRope on a parent GameObject if not set
            m_Rope = GetComponentInParent<BurstRope>();
            if (m_Rope == null)
            {
                Debug.LogError("XRIT_RopeGrabInteractable: BurstRope not found or not assigned!", this);
                enabled = false; // Disable script if no rope
                return;
            }
        }
    }

    void OnEnable()
    {
        m_GrabInteractable.selectEntered.AddListener(OnGrabStart);
        m_GrabInteractable.selectExited.AddListener(OnGrabEnd);
    }

    void OnDisable()
    {
        m_GrabInteractable.selectEntered.RemoveListener(OnGrabStart);
        m_GrabInteractable.selectExited.RemoveListener(OnGrabEnd);

        // Ensure binding is released if the object is disabled while grabbed
        if (m_Interactor != null && m_Rope != null)
        {
            ReleaseBinding();
        }
    }

    private void OnGrabStart(SelectEnterEventArgs args)
    {
        m_Interactor = args.interactorObject;
        if (m_Rope == null || m_Interactor == null) return;

        Vector3 grabPosition = m_Interactor.transform.position;
        int closestNodeIndex = m_Rope.ClosestIndexToPoint(grabPosition);

        // Initialize or find a BindingPoint to manage
        // For simplicity, this example tries to find an existing UNBOUND point or adds a new one.
        // A more robust system might pre-allocate binding points or have a more dynamic system.
        m_BindingPointListIndex = -1;
        for (int i = 0; i < m_Rope.Binds.Count; ++i)
        {
            if (!m_Rope.Binds[i].Bound)
            {
                m_BindingPointListIndex = i;
                break;
            }
        }

        m_ManagedBindingPoint = new BindingPoint
        {
            Index = closestNodeIndex,
            Target = m_Rope.ToRopeSpace(grabPosition),
            Bound = true,
            Strength = m_BindingStrength
        };

        if (m_BindingPointListIndex != -1)
        {
            m_Rope.Binds[m_BindingPointListIndex] = m_ManagedBindingPoint;
        }
        else
        {
            // Add new binding point if no free one was found
            m_Rope.Binds.Add(m_ManagedBindingPoint);
            m_BindingPointListIndex = m_Rope.Binds.Count - 1;
        }
         // Ensure the BurstRope processes the new binding immediately if possible.
        // This might require calling a public method on BurstRope if it normally only updates bindings at specific intervals.
        // For now, assume BurstRope will pick it up in its update loop.
    }

    private void OnGrabEnd(SelectExitEventArgs args)
    {
        if (m_Rope == null || m_Interactor == null) return;
        if (args.interactorObject != m_Interactor) return; // Ensure it's the same interactor that started the grab

        ReleaseBinding();
        m_Interactor = null;
    }

    private void ReleaseBinding()
    {
        if (m_BindingPointListIndex != -1 && m_Rope != null && m_BindingPointListIndex < m_Rope.Binds.Count)
        {
            // Instead of removing, mark as unbound to allow reuse.
            // Or, if BurstRope handles dynamic list changes well, it could be removed.
            // Marking as unbound is safer if BurstRope iterates over a fixed-size native array internally based on Binds.Count at init.
            var bp = m_Rope.Binds[m_BindingPointListIndex];
            bp.Bound = false;
            m_Rope.Binds[m_BindingPointListIndex] = bp;
        }
        m_BindingPointListIndex = -1; // Reset our managed index
    }

    void Update()
    {
        if (m_Interactor != null && m_GrabInteractable.isSelected && m_Rope != null && m_BindingPointListIndex != -1)
        {
            // Ensure the binding point index is still valid
            if (m_BindingPointListIndex < m_Rope.Binds.Count)
            {
                // Update target position
                var bp = m_Rope.Binds[m_BindingPointListIndex];
                bp.Target = m_Rope.ToRopeSpace(m_Interactor.transform.position);
                // bp.Index could also be updated here if we want the grab point to slide along the rope
                // For now, it sticks to the initial closest node.
                m_Rope.Binds[m_BindingPointListIndex] = bp;
            }
            else
            {
                // Binding point became invalid (e.g. list was cleared externally), so release
                Debug.LogWarning("XRIT_RopeGrabInteractable: Binding point became invalid. Releasing.", this);
                ReleaseBinding();
                m_Interactor = null; // Stop further updates
            }
        }
    }
}

#else // Stubs for when XR Interaction Toolkit or Meta Rope Utilities are not present
using UnityEngine;
public class XRIT_RopeGrabInteractable : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField]
    [Tooltip("The BurstRope system this interactable will control.")]
    private Component m_Rope; // Use Component as a generic placeholder for BurstRope

    void Awake()
    {
        #if !UNITY_XR_INTERACTION_TOOLKIT
        Debug.LogError("XRIT_RopeGrabInteractable: XR Interaction Toolkit is not enabled in this project. Please install the package.", this);
        #endif
        #if !META_ROPE_UTILITIES
        Debug.LogError("XRIT_RopeGrabInteractable: Meta.Utilities.Ropes (BurstRope) seems to be missing. Ensure the META_ROPE_UTILITIES scripting define symbol is set if the package is present under a different name or its asmdef doesn't auto-define it.", this);
        #endif
        enabled = false;
    }
}
#endif
