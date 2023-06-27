// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Data structure for an atomic user action (click mouse, select item in a context menu).
    /// </summary>
    [DataContract(IsReference = false)]
    internal class UserAction
    {
        /// <summary>
        /// Recorded UnityGUI event.
        /// </summary>
        [DataMember]
        public Event Event { get; set; }

        /// <summary>
        /// List of recorded selections.
        /// </summary>
        [DataMember]
        public List<string> PrepickedSelections { get; set; }

        public UserAction()
        {
            PrepickedSelections = new List<string>();
        }
    }
}
