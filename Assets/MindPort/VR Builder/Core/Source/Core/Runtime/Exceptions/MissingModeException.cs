// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Exceptions
{
    public class MissingModeException : ProcessException
    {
        public MissingModeException()
        {
        }

        public MissingModeException(string message) : base(message)
        {
        }

        public MissingModeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
