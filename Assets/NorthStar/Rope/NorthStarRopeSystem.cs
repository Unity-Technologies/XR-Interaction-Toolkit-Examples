// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities.Ropes;

namespace NorthStar
{
    public class NorthStarRopeSystem : RopeSystem
    {
        protected BodyPositions m_bodyPositions;

        protected override void SetupHandRefs()
        {
            m_bodyPositions = FindObjectOfType<BodyPositions>();

            if (m_bodyPositions is not null && m_grabAnchors.Length == 2)
            {
                m_grabAnchors[0].Hand = m_bodyPositions.SyntheticHands[0];
                m_grabAnchors[1].Hand = m_bodyPositions.SyntheticHands[1];
            }
        }
    }
}
