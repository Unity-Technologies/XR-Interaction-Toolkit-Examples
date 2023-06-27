// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Wizard
{
    internal interface IWizardNavigationEntry
    {
        bool Selected { get; set; }
        void Draw(Rect window);
    }
}
