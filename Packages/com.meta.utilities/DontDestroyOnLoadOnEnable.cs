// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class DontDestroyOnLoadOnEnable : MonoBehaviour
    {
        protected void OnEnable()
        {
            DontDestroyOnLoad(this);
        }
    }
}
