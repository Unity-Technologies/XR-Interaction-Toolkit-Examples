// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities.Ropes;
using UnityEngine;

namespace NorthStar
{
    public class BoatRopeTransformBinder : RopeTransformBinder
    {
        public bool UseBoatSpace;

        protected override Vector3 GetPosition() =>base.GetPosition();
    }
}
