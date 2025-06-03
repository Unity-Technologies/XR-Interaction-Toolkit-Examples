// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// Replaces default int ui with a dropdown of power 2 values up to and including the max value
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Pow2Attribute : PropertyAttribute
    {
        public int MaxValue { get; }

        public Pow2Attribute(int maxValue) => MaxValue = maxValue;
    }
}
