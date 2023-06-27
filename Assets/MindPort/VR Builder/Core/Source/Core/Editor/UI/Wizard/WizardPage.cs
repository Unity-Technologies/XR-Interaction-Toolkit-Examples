// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEngine;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard pages which allows you to implement your content.
    /// Care about implementing your state serializable.
    /// </summary>
    [Serializable]
    internal abstract class WizardPage
    {
        public string Name;

        public bool AllowSkip;

        public bool CanProceed = true;

        public bool ShouldRestart = false;

        public bool Mandatory = true;

        public WizardPage()
        {

        }

        public WizardPage(string name, bool allowSkip = false, bool mandatory = true)
        {
            Name = name;
            AllowSkip = allowSkip;
            Mandatory = mandatory;
        }

        public abstract void Draw(Rect window);

        public virtual void Apply()
        {

        }

        public virtual void Skip()
        {

        }

        public virtual void Back()
        {

        }

        public virtual void Closing(bool isCompleted)
        {

        }
    }
}
