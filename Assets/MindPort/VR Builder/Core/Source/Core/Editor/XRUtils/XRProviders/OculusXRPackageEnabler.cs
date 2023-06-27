// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

#if UNITY_XR_MANAGEMENT && OCULUS_XR
namespace VRBuilder.Editor.XRUtils
{
    /// <summary>
    /// Enables the Oculus XR Plugin.
    /// </summary>
    internal sealed class OculusXRPackageEnabler : XRProvider
    {
        /// <inheritdoc/>
        public override string Package { get; } = "com.unity.xr.oculus";

        /// <inheritdoc/>
        public override int Priority { get; } = 2;

        protected override string XRLoaderName { get; } = "OculusLoader";
    }
}
#endif
