// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Meta.Utilities
{
    public class SetMaterialPropertiesOnEnable : MonoBehaviour
    {
        [Serializable]
        public struct PropertyValue<T>
        {
            public string Name;
            public T Value;
        }

        [SerializeField] private Renderer m_renderer;
        [SerializeField] private int m_materialIndex = 0;

        [Header("Properties")]
        [SerializeField] private PropertyValue<Color>[] m_colors = { };
        [SerializeField] private PropertyValue<int>[] m_integers = { };
        [SerializeField] private PropertyValue<float>[] m_floats = { };
        [SerializeField] private PropertyValue<Vector4>[] m_vectors = { };
        [SerializeField] private PropertyValue<Texture>[] m_textures = { };

        protected void OnEnable()
        {
            var block = new MaterialPropertyBlock();
            foreach (var value in m_colors)
                block.SetColor(value.Name, value.Value);
            foreach (var value in m_integers)
                block.SetInteger(value.Name, value.Value);
            foreach (var value in m_floats)
                block.SetFloat(value.Name, value.Value);
            foreach (var value in m_vectors)
                block.SetVector(value.Name, value.Value);
            foreach (var value in m_textures)
                block.SetTexture(value.Name, value.Value);
            m_renderer.SetPropertyBlock(block, m_materialIndex);
        }

        protected void OnValidate()
        {
            OnEnable();
        }
    }
}
