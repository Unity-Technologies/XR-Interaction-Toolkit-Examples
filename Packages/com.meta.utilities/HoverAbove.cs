// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

//A simple script to allow an object to hover over another object. Useful to have a child follow a specific position of the parent ignoring relative rotation.
namespace Meta.Utilities
{
    public class HoverAbove : MonoBehaviour
    {
        [SerializeField] private Transform m_hoverPoint;
        [Tooltip("The distance to hover this object above the hover point")]
        [SerializeField] private float m_hoverDistance = 1f;

        // Update is called once per frame
        private void Update()
        {
            if (m_hoverPoint)
            {
                transform.position = m_hoverPoint.position + new Vector3(0, m_hoverDistance, 0);
            }
        }
    }
}
