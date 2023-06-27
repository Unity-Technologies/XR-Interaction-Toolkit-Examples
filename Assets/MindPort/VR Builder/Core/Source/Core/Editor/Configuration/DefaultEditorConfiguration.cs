// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.ObjectModel;
using System.Linq;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Serialization;
using VRBuilder.Core.Serialization.NewtonsoftJson;
using VRBuilder.Editor.ProcessValidation;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.Configuration
{
    /// <summary>
    /// Default editor configuration definition which is used if no other was implemented.
    /// </summary>
    public class DefaultEditorConfiguration : IEditorConfiguration
    {
        private AllowedMenuItemsSettings allowedMenuItemsSettings;

        /// <inheritdoc />
        public virtual string ProcessStreamingAssetsSubdirectory
        {
            get { return "Processes"; }
        }

        /// <inheritdoc />
        public virtual string AllowedMenuItemsSettingsAssetPath
        {
            get { return "Assets/MindPort/VR Builder/Editor/Config/AllowedMenuItems.json"; }
        }

        /// <inheritdoc />
        public virtual IProcessSerializer Serializer
        {
            get { return new NewtonsoftJsonProcessSerializerV3(); }
        }

        /// <inheritdoc />
        public virtual ReadOnlyCollection<MenuOption<IBehavior>> BehaviorsMenuContent
        {
            get { return AllowedMenuItemsSettings.GetBehaviorMenuOptions().Cast<MenuOption<IBehavior>>().ToList().AsReadOnly(); }
        }

        /// <inheritdoc />
        public virtual ReadOnlyCollection<MenuOption<ICondition>> ConditionsMenuContent
        {
            get { return AllowedMenuItemsSettings.GetConditionMenuOptions().Cast<MenuOption<ICondition>>().ToList().AsReadOnly(); }
        }

        /// <inheritdoc />
        public virtual AllowedMenuItemsSettings AllowedMenuItemsSettings
        {
            get
            {
                if (allowedMenuItemsSettings == null)
                {
                    allowedMenuItemsSettings = AllowedMenuItemsSettings.Load();
                }

                return allowedMenuItemsSettings;
            }
            set { allowedMenuItemsSettings = value; }
        }

        internal virtual IValidationHandler Validation { get; }

        protected DefaultEditorConfiguration()
        {
#if CREATOR_PRO
            Validation = new DefaultValidationHandler();
#else
            Validation = new DisabledValidationHandler();
#endif
        }
    }
}
