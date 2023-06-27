// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;
using VRBuilder.Editor.ProcessValidation;

/// <summary>
/// Does not validate, used to disabled the validation system.
/// </summary>
internal class DisabledValidationHandler : IValidationHandler
{
    public IContextResolver ContextResolver { get; set; } = null;

    public IValidationReport LastReport { get; } = null;

    public bool IsAllowedToValidate()
    {
        return false;
    }

    public IValidationReport Validate(IData data, IProcess process, IContext context = null)
    {
        return null;
    }
}
