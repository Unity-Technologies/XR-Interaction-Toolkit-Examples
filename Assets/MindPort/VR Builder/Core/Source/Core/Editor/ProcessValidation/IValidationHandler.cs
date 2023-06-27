// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using VRBuilder.Core;

namespace VRBuilder.Editor.ProcessValidation
{
    /// <summary>
    /// ValidationHandler validates data objects, e.g. steps or conditions, of a given process and reports whether
    /// the object causes issues or is faulty, e.g. empty fields or invalid values in a behavior.
    /// </summary>
    internal interface IValidationHandler
    {
        /// <summary>
        /// <see cref="IContextResolver"/> for resolving known context types.
        /// </summary>
        IContextResolver ContextResolver { get; set; }

        /// <summary>
        /// Last report generated.
        /// </summary>
        IValidationReport LastReport { get; }

        /// <summary>
        /// Checks if validation is currently allowed.
        /// </summary>
        bool IsAllowedToValidate();

        /// <summary>
        /// Validates the given object.
        /// </summary>
        /// <param name="data">Data object, which will be validated.</param>
        /// <param name="process">Process where given <paramref name="data"/> belongs.</param>
        /// <param name="context">Context of the validation.</param>
        /// <returns>List of reports regarding invalid objects related to the <paramref name="data"/>.</returns>
        IValidationReport Validate(IData data, IProcess process, IContext context = null);
    }
}
