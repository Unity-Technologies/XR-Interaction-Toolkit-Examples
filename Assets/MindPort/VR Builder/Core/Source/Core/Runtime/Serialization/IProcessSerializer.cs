// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Serialization
{
    /// <summary>
    /// A serializer which can de/serialize processes and steps to a certain format.
    /// </summary>
    public interface IProcessSerializer
    {
        /// <summary>
        /// Display name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// File format used for this serializer. For example, 'json'.
        /// </summary>
        string FileFormat { get; }

        /// <summary>
        /// Serializes a given process into a byte array.
        /// </summary>
        byte[] ProcessToByteArray(IProcess target);

        /// <summary>
        /// Deserializes a given process to a usable object.
        /// </summary>
        IProcess ProcessFromByteArray(byte[] data);

        /// <summary>
        /// Serializes a given step into a byte array. The implementation should trim target steps of the step.
        /// </summary>
        byte[] StepToByteArray(IStep step);

        /// <summary>
        /// Deserializes a given step to a usable object.
        /// </summary>
        IStep StepFromByteArray(byte[] data);
    }
}
