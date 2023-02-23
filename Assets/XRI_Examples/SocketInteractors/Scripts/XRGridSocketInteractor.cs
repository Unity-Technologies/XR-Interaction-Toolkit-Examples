using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Socket Interactor for holding a group of Interactables in a 2D grid.
    /// </summary>
    /// <remarks>
    /// The grid starts at the position of the Attach Transform.
    /// During Awake, a Grid Socket instantiates one GameObject (as child of its Attach Transform) for each grid cell.
    /// The Transform component of these instantiated objects are used as the actual attach point for the Interactables.
    /// </remarks>
    public class XRGridSocketInteractor : XRSocketInteractor
    {
        [Space]
        [SerializeField]
        [Tooltip("The grid width. The grid width is along the Attach Transform's local X axis.")]
        int m_GridWidth = 2;

        /// <summary>
        /// The grid width. The grid width is along the Attach Transform's local X axis.
        /// </summary>
        public int gridWidth
        {
            get => m_GridWidth;
            set => m_GridWidth = Mathf.Max(1, value);
        }

        [SerializeField]
        [Tooltip("The grid height. The grid height is along the Attach Transform's local Y axis.")]
        int m_GridHeight = 2;

        /// <summary>
        /// The grid height. The grid height is along the Attach Transform's local Y axis.
        /// </summary>
        public int gridHeight
        {
            get => m_GridHeight;
            set => m_GridHeight = Mathf.Max(1, value);
        }

        /// <summary>
        /// (Read Only) The grid size. The maximum number of Interactables that this Interactor can hold.
        /// </summary>
        public int gridSize => m_GridWidth * m_GridHeight;

        [SerializeField]
        [Tooltip("The distance (in local space) between cells in the grid.")]
        Vector2 m_CellOffset = new Vector2(0.1f, 0.1f);

        /// <summary>
        /// The distance (in local space) between cells in the grid.
        /// </summary>
        public Vector2 cellOffset
        {
            get => m_CellOffset;
            set => m_CellOffset = value;
        }

        readonly HashSet<Transform> m_UnorderedUsedAttachedTransform = new HashSet<Transform>();
        readonly Dictionary<IXRInteractable, Transform> m_UsedAttachTransformByInteractable =
            new Dictionary<IXRInteractable, Transform>();

        Transform[,] m_Grid;

        bool hasEmptyAttachTransform => m_UnorderedUsedAttachedTransform.Count < gridSize;

        /// <summary>
        /// Creates the grid.
        /// </summary>
        void CreateGrid()
        {
            m_Grid = new Transform[m_GridHeight, m_GridWidth];

            for (var i = 0; i < m_GridHeight; i++)
            {
                for (var j = 0; j < m_GridWidth; j++)
                {
                    var attachTransformInstance = new GameObject($"[{gameObject.name}] Attach ({i},{j})").transform;
                    attachTransformInstance.SetParent(attachTransform, false);

                    var offset = new Vector3(j * m_CellOffset.x, i * m_CellOffset.y, 0f);
                    attachTransformInstance.localPosition = offset;

                    m_Grid[i, j] = attachTransformInstance;
                }
            }
        }

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            CreateGrid();

            // The same material is used on both situations
            interactableCantHoverMeshMaterial = interactableHoverMeshMaterial;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnValidate()
        {
            m_GridWidth = Mathf.Max(1, m_GridWidth);
            m_GridHeight = Mathf.Max(1, m_GridHeight);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = attachTransform != null ? attachTransform.localToWorldMatrix : transform.localToWorldMatrix;
            for (var i = 0; i < m_GridHeight; i++)
            {
                for (var j = 0; j < m_GridWidth; j++)
                {
                    var currentPosition = new Vector3(j * m_CellOffset.x, i * m_CellOffset.y, 0f);
                    Gizmos.DrawLine(currentPosition + (Vector3.left * m_CellOffset.x * 0.5f), currentPosition + (Vector3.right * m_CellOffset.y * 0.5f));
                    Gizmos.DrawLine(currentPosition + (Vector3.down * m_CellOffset.x * 0.5f), currentPosition + (Vector3.up * m_CellOffset.y * 0.5f));
                }
            }
        }

        /// <inheritdoc />
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            var closestAttachTransform = GetAttachTransform(args.interactableObject);
            m_UnorderedUsedAttachedTransform.Add(closestAttachTransform);
            m_UsedAttachTransformByInteractable.Add(args.interactableObject, closestAttachTransform);
        }

        /// <inheritdoc />
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            var closestAttachTransform = m_UsedAttachTransformByInteractable[args.interactableObject];
            m_UnorderedUsedAttachedTransform.Remove(closestAttachTransform);
            m_UsedAttachTransformByInteractable.Remove(args.interactableObject);

            base.OnSelectExiting(args);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return IsSelecting(interactable)
                   || (hasEmptyAttachTransform && !interactable.isSelected && !m_UnorderedUsedAttachedTransform.Contains(GetAttachTransform(interactable)));
        }

        /// <inheritdoc />
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable)
                   && !m_UnorderedUsedAttachedTransform.Contains(GetAttachTransform(interactable));
        }

        /// <inheritdoc />
        public override Transform GetAttachTransform(IXRInteractable interactable)
        {
            if (m_UsedAttachTransformByInteractable.TryGetValue(interactable, out var interactableAttachTransform))
                return interactableAttachTransform;

            var interactableLocalPosition = attachTransform.InverseTransformPoint(interactable.GetAttachTransform(this).position);
            var i = Mathf.RoundToInt(interactableLocalPosition.y / m_CellOffset.y);
            var j = Mathf.RoundToInt(interactableLocalPosition.x / m_CellOffset.x);
            i = Mathf.Clamp(i, 0, m_GridHeight - 1);
            j = Mathf.Clamp(j, 0, m_GridWidth - 1);
            return m_Grid[i, j];
        }
    }
}
