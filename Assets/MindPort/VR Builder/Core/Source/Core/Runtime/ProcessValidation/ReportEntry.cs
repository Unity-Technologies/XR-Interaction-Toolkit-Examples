// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Validation
{
    /// <summary>
    /// Base report entry with all information available on editor Builder core.
    /// </summary>
    public class ReportEntry
    {
        /// <summary>
        /// Priority level for this <see cref="ValidationReportEntry"/>.
        /// </summary>
        public readonly ValidationErrorLevel ErrorLevel;

        /// <summary>
        /// ErrorCode to easily identifying the error.
        /// </summary>
        public readonly int Code;

        /// <summary>
        /// Detailed description of the issue.
        /// </summary>
        public readonly string Message;

        public ReportEntry(int code, string message, ValidationErrorLevel errorLevel)
        {
            Code = code;
            Message = message;
            ErrorLevel = errorLevel;
        }

        protected ReportEntry(ReportEntry entry)
        {
            Code = entry.Code;
            Message = entry.Message;
            ErrorLevel = entry.ErrorLevel;
        }
    }
}
