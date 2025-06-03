// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Provides more information about an interaction if neccecary
    /// </summary>
    public class ExtraInteractionData : MonoBehaviour
    {
        public LineSegment LineSegment;
        public bool FreeRotation = false;
    }
}
