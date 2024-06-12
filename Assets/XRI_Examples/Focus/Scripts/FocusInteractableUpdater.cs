using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Class to demonstrate modifying interactables that are in focus.
    /// </summary>
    public class FocusInteractableUpdater : MonoBehaviour
    {
        [SerializeField, Tooltip("Left interaction group to check for focused interactables.")]
        XRInteractionGroup m_LeftInteractionGroup;

        /// <summary>
        /// Left interaction group to check for focused interactables.
        /// </summary>
        public XRInteractionGroup leftInteractionGroup
        {
            get => m_LeftInteractionGroup;
            set => m_LeftInteractionGroup = value;
        }

        [SerializeField, Tooltip("Right interaction group to check for focused interactables.")]
        XRInteractionGroup m_RightInteractionGroup;

        /// <summary>
        /// Right interaction group to check for focused interactables.
        /// </summary>
        public XRInteractionGroup rightInteractionGroup
        {
            get => m_RightInteractionGroup;
            set => m_RightInteractionGroup = value;
        }

        [SerializeField, Tooltip("List of meshes to shuffle through.")]
        List<Mesh> m_FocusMeshes = new List<Mesh>();

        /// <summary>
        /// List of meshes to shuffle through.
        /// </summary>
        public List<Mesh> focusMeshes
        {
            get => m_FocusMeshes;
            set => m_FocusMeshes = value;
        }

        readonly List<IXRFocusInteractable> m_FocusInteractables = new List<IXRFocusInteractable>();

        void Start()
        {
            foreach (var interactable in GetComponentsInChildren<IXRFocusInteractable>())
            {
                m_FocusInteractables.Add(interactable);
            }
        }

        /// <summary>
        /// Randomizes and shuffles the mesh of focused interactables in <see cref="leftInteractionGroup"/> and <see cref="rightInteractionGroup"/>.
        /// </summary>
        public void RandomizeFocusedInteractablesMesh()
        {
            if (m_FocusMeshes.Count == 0 || m_FocusInteractables.Count == 0)
                return;

            if (m_LeftInteractionGroup.focusInteractable != null && m_FocusInteractables.Contains(m_LeftInteractionGroup.focusInteractable))
            {
                var interactableTransform = m_LeftInteractionGroup.focusInteractable.transform;
                StartCoroutine(ShuffleMesh(interactableTransform.GetComponent<MeshFilter>(), interactableTransform.GetComponent<MeshCollider>()));
            }

            if (m_RightInteractionGroup.focusInteractable != null && m_FocusInteractables.Contains(m_RightInteractionGroup.focusInteractable))
            {
                var interactableTransform = m_RightInteractionGroup.focusInteractable.transform;
                StartCoroutine(ShuffleMesh(interactableTransform.GetComponent<MeshFilter>(), interactableTransform.GetComponent<MeshCollider>()));
            }
        }

        /// <summary>
        /// Updates <see cref="MeshFilter"/> and <see cref="MeshCollider"/> with random mesh from <see cref="focusMeshes"/>.
        /// </summary>
        /// <param name="meshFilter">Mesh filter to be updated.</param>
        /// <param name="meshCollider">Mesh collider to be updated.</param>
        /// <returns></returns>
        IEnumerator ShuffleMesh(MeshFilter meshFilter, MeshCollider meshCollider)
        {
            var meshIndex = Random.Range(0, m_FocusMeshes.Count);
            for (var i = 0; i < Random.Range(5, 10); ++i)
            {
                var mesh = m_FocusMeshes[meshIndex];
                meshFilter.mesh = mesh;
                meshCollider.sharedMesh = mesh;

                meshIndex++;
                meshIndex = meshIndex % m_FocusMeshes.Count;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
