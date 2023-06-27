// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.IO;

namespace VRBuilder.Editor.ImguiTester
{
    /// <summary>
    /// Exception that is thrown when file with recorded user actions is not found by the Editor IMGUI Tester.
    /// </summary>
    internal class UserActionsRecordNotFoundException : FileNotFoundException
    {
        public UserActionsRecordNotFoundException(string message, params object[] formatArgs) : base(string.Format(message, formatArgs))
        {
        }
    }
}
