// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// State of <see cref="EditorImguiTest{T}"/>
    /// </summary>
    internal enum TestState
    {
        Normal,
        Pending,
        Failed,
        Passed
    }
}
