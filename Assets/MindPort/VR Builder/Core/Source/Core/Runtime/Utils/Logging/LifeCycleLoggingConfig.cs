// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Runtime.Utils;

namespace VRBuilder.Core.Utils.Logging
{
    /// <summary>
    /// ScriptableObject which allows you to configure what of the process life cycle should be logged.
    /// </summary>
    public class LifeCycleLoggingConfig : SettingsObject<LifeCycleLoggingConfig>
    {
        /// <summary>
        /// True, if behaviors are allowed to be logged.
        /// </summary>
        public bool LogBehaviors = false;

        /// <summary>
        /// True, if conditions are allowed to be logged.
        /// </summary>
        public bool LogConditions = false;

        /// <summary>
        /// True, if chapters are allowed to be logged.
        /// </summary>
        public bool LogChapters = true;

        /// <summary>
        /// True, if steps are allowed to be logged.
        /// </summary>
        public bool LogSteps = true;

        /// <summary>
        /// True, if transitions are allowed to be logged.
        /// </summary>
        public bool LogTransitions = false;

        /// <summary>
        /// True, if data property changes are allowed to be logged.
        /// </summary>
        public bool LogDataPropertyChanges = false;
    }
}
