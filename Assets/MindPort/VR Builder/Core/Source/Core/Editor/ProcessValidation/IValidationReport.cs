// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Reflection;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;

namespace VRBuilder.Editor.ProcessValidation
{
    /// <summary>
    /// Report about the last validation done by the validation system.
    /// </summary>
    internal interface IValidationReport
    {
        /// <summary>
        /// List of <see cref="EditorReportEntry"/> generated from the <see cref="IValidationScope"/>'s validation.
        /// </summary>
        List<EditorReportEntry> Entries { get; }

        /// <summary>
        /// Time spent on generation of this report in milliseconds.
        /// </summary>
        long GenerationTime { get; }

        /// <summary>
        /// Returns all <see cref="EditorReportEntry"/> found for given step.
        /// </summary>
        List<EditorReportEntry> GetEntriesFor(IContext context);

        /// <summary>
        /// Returns <see cref="EditorReportEntry"/> for given steps Behaviors.
        /// </summary>
        List<EditorReportEntry> GetBehaviorEntriesFor(IStepData step);

        /// <summary>
        /// Returns <see cref="EditorReportEntry"/> for given steps Conditions.
        /// </summary>
        List<EditorReportEntry> GetConditionEntriesFor(IStepData step);

        /// <summary>
        /// Returns <see cref="EditorReportEntry"/> for given context and step.
        /// </summary>
        List<EditorReportEntry> GetContextEntriesFor<T>(IStepData step) where T : IContext;

        /// <summary>
        /// Returns all <see cref="EditorReportEntry"/> found for given step.
        /// </summary>
        List<EditorReportEntry> GetEntriesFor(IData data, MemberInfo info);

        /// <summary>
        /// Get Entries for <see cref="IBehaviorData"/>.
        /// </summary>
        List<EditorReportEntry> GetEntriesFor(IBehaviorData data);

        /// <summary>
        /// Get Entries for <see cref="IConditionData"/>
        /// </summary>
        List<EditorReportEntry> GetEntriesFor(IConditionData data);
    }
}
