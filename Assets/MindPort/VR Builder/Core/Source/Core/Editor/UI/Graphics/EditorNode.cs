// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Base class for Entry and Exit nodes.
    /// </summary>
    internal abstract class EditorNode : GraphicalElement
    {
        /// <summary>
        /// List of entry joints, to which incoming Transitions can connect. Since workflow is linear for now, there is only one joint per editor node.
        /// </summary>
        public List<EntryJoint> EntryJoints { get; private set; }

        /// <summary>
        /// List of exit joints, to which outcoming Transitions can connect. Since workflow is linear for now, there is only one joint per editor node.
        /// </summary>
        public List<ExitJoint> ExitJoints { get; private set; }

        /// <inheritdoc />
        public override int Layer
        {
            get
            {
                return 0;
            }
        }

        /// <inheritdoc />
        protected EditorNode(EditorGraphics owner, bool isReceivingEvents) : base(owner, isReceivingEvents)
        {
            EntryJoints = new List<EntryJoint>();
            ExitJoints = new List<ExitJoint>();
        }
    }
}
