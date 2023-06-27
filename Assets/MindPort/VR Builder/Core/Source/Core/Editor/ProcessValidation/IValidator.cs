// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;

namespace VRBuilder.Editor.ProcessValidation
{
    /// <summary>
    /// Validator provides validation for a specific Type.
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Type which is validated by this validator.
        /// </summary>
        Type ValidatedType { get; }

        /// <summary>
        /// Type of Context which is this is this validators scope.
        /// </summary>
        Type ValidatedContext { get; }

        /// <summary>
        /// Will return true when the object can be validated by this validator.
        /// </summary>
        /// <param name="validatableObject">Object to validate.</param>
        /// <returns>True if object can be validated.</returns>
        bool CanValidate(object validatableObject);

        /// <summary>
        /// Validates the given object.
        /// </summary>
        /// <param name="validatableObject">Object, which will be validated.</param>
        /// <param name="context">Context of the validation.</param>
        /// <returns>List of reports regarding invalid objects related to the <paramref name="validatableObject"/>.</returns>
        List<EditorReportEntry> Validate(object validatableObject, IContext context);
    }
}
