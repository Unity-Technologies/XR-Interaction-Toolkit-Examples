// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEngine;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core
{
    /// <summary>
    /// Base factory interface for <see cref="IEntity"/> objects.
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// Creates a new <see cref="IStep"/> with given <paramref name="name"/>, <paramref name="position"/> and, if there is any valid <see cref="PostProcessEntity{T}"/>, executes corresponding post processing.
        /// </summary>
        public static IStep CreateStep(string name, Vector2 position = default, string stepType = "default")
        {
            IStep step = StepFactory.Instance.Create(name);
            step.StepMetadata.Position = position;
            step.StepMetadata.StepType = stepType;
            PostProcessEntity<IStep>(step);

            return step;
        }

        /// <summary>
        /// Creates a new <see cref="ITransition"/> and, if there is any valid <see cref="PostProcessEntity{T}"/>, executes corresponding post processing.
        /// </summary>
        public static ITransition CreateTransition()
        {
            ITransition transition = TransitionFactory.Instance.Create();
            PostProcessEntity<ITransition>(transition);

            return transition;
        }

        /// <summary>
        /// Creates a new <see cref="IChapter"/> with given <paramref name="name"/> and, if there is any valid <see cref="PostProcessEntity{T}"/>, executes corresponding post processing.
        /// </summary>
        public static IChapter CreateChapter(string name)
        {
            IChapter chapter = ChapterFactory.Instance.Create(name);
            PostProcessEntity<IChapter>(chapter);

            return chapter;
        }

        /// <summary>
        /// Creates a new <see cref="IProcess"/> with given <paramref name="name"/> and, if there is any valid <see cref="PostProcessEntity{T}"/>, executes corresponding post processing.
        /// </summary>
        public static IProcess CreateProcess(string name)
        {
            IProcess process = ProcessFactory.Instance.Create(name);
            PostProcessEntity<IProcess>(process);

            return process;
        }

        private static void PostProcessEntity<T>(IEntity entity) where T : IEntity
        {
            foreach (Type postprocessingType in ReflectionUtils.GetConcreteImplementationsOf<EntityPostProcessing<T>>())
            {
                if (ReflectionUtils.CreateInstanceOfType(postprocessingType) is EntityPostProcessing<T> postProcessing)
                {
                    postProcessing.Execute((T) entity);
                }
            }
        }
    }
}
