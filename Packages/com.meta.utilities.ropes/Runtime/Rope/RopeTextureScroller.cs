// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using UnityEngine;

namespace Meta.Utilities.Ropes
{
    /// <summary>
    /// Scrolls the UV offset for materials based on an event driven value (used for faking rope movement in the boat's rigging)
    /// </summary>
    public class RopeTextureScroller : MonoBehaviour
    {
        private static readonly int s_offset = Shader.PropertyToID("_Offset");

        [Serializable]
        public class MeshRendererInfo
        {
            public MeshRenderer Renderer;
            public int MaterialIndex;
            public Vector2 ScrollSpeed;
            [NonSerialized] public Material[] MaterialInstances;
        }

        [SerializeField] private MeshRendererInfo[] m_meshRendererInfos;

        public float Value
        {
            get => m_value;
            set
            {
                m_value = value;
                foreach (var info in m_meshRendererInfos)
                {
                    info.MaterialInstances[info.MaterialIndex].SetVector(s_offset, info.ScrollSpeed * m_value);
                }
            }
        }

        private float m_value;

        private void Start()
        {
            foreach (var info in m_meshRendererInfos)
            {
                info.MaterialInstances = info.Renderer.materials;
            }

            Value = Value;
        }
    }
}
