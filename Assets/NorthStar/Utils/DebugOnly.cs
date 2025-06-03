// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Disable / self-destruct if this isn't a debug build!
    /// </summary>
    public class DebugOnly : MonoBehaviour
    {
        [SerializeField] private bool m_destroyGameObject;

        private void Awake()
        {
#if !DEBUG
            if (Application.isPlaying)
            {
                if (m_destroyGameObject)
                    DestroyImmediate(gameObject);
                else
                    gameObject.SetActive(false);
            }
#endif
        }
    }
}