// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Configuration.Modes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRBuilder.Core
{
    /// <summary>
    /// Runs a <see cref="IProcess"/>, expects to be run only once.
    /// </summary>
    public static class ProcessRunner
    {
        public class ProcessEvents
        {
            /// <summary>
            /// Will be called before the process is setup internally.
            /// </summary>
            public EventHandler<ProcessEventArgs> ProcessSetup;

            /// <summary>
            /// Will be called on process start.
            /// </summary>
            public EventHandler<ProcessEventArgs> ProcessStarted;

            /// <summary>
            /// Will be called each time a chapter activates.
            /// </summary>
            public EventHandler<ProcessEventArgs> ChapterStarted;

            /// <summary>
            /// Will be called each time a step activates.
            /// </summary>
            public EventHandler<ProcessEventArgs> StepStarted;

            /// <summary>
            /// Will be called when the process finishes.
            /// </summary>
            public EventHandler<ProcessEventArgs> ProcessFinished;

            /// <summary>
            /// Will be called when manual fast forward is triggered.
            /// </summary>
            public EventHandler<FastForwardProcessEventArgs> FastForwardStep;
        }

        private class ProcessRunnerInstance : MonoBehaviour
        {
            /// <summary>
            /// Reference to the currently used <see cref="IProcess"/>.
            /// </summary>
            public IProcess process = null;

            private void HandleModeChanged(object sender, ModeChangedEventArgs args)
            {
                if (process != null)
                {
                    process.Configure(args.Mode);
                    RuntimeConfigurator.Configuration.StepLockHandling.Configure(RuntimeConfigurator.Configuration.Modes.CurrentMode);
                }
            }

            private void HandleProcessStageChanged(object sender, ActivationStateChangedEventArgs e)
            {
                if (e.Stage == Stage.Inactive)
                {
                    RuntimeConfigurator.ModeChanged -= HandleModeChanged;
                    Destroy(gameObject);
                }
            }

            private void Update()
            {
                if (process == null)
                {
                    return;
                }

                if (process.LifeCycle.Stage == Stage.Inactive)
                {
                    return;
                }

                process.Update();

                if (process.Data.Current?.LifeCycle.Stage == Stage.Activating)
                {
                    Events.ChapterStarted?.Invoke(this, new ProcessEventArgs(process));
                }

                if (process.Data.Current?.Data.Current?.LifeCycle.Stage == Stage.Activating)
                {
                    Events.StepStarted?.Invoke(this, new ProcessEventArgs(process));
                }

                if (process.LifeCycle.Stage == Stage.Active)
                {
                    process.LifeCycle.Deactivate();
                    RuntimeConfigurator.Configuration.StepLockHandling.OnProcessFinished(process);
                    Events.ProcessFinished?.Invoke(this, new ProcessEventArgs(process));
                }
            }

            /// <summary>
            /// Starts the <see cref="IProcess"/>.
            /// </summary>
            public void Execute()
            {
                Events.ProcessSetup?.Invoke(this, new ProcessEventArgs(process));

                RuntimeConfigurator.ModeChanged += HandleModeChanged;

                process.LifeCycle.StageChanged += HandleProcessStageChanged;
                process.Configure(RuntimeConfigurator.Configuration.Modes.CurrentMode);

                RuntimeConfigurator.Configuration.StepLockHandling.Configure(RuntimeConfigurator.Configuration.Modes.CurrentMode);
                RuntimeConfigurator.Configuration.StepLockHandling.OnProcessStarted(process);
                process.LifeCycle.Activate();

                Events.ProcessStarted?.Invoke(this, new ProcessEventArgs(process));
            }
        }

        private static ProcessRunnerInstance instance;

        private static ProcessEvents events;

        /// <summary>
        /// Returns all process events for the current scene.
        /// </summary>
        public static ProcessEvents Events
        {
            get
            {
                if (events == null)
                {
                    events = new ProcessEvents();
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                }
                return events;
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            events = null;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// Currently running <see cref="IProcess"/>
        /// </summary>
        public static IProcess Current
        {
            get
            {
                return instance == null ? null : instance.process;
            }
        }

        /// <summary>
        /// Returns true if the current <see cref="IProcess"/> is running.
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                return Current != null && Current.LifeCycle.Stage != Stage.Inactive;
            }
        }

        /// <summary>
        /// Initializes the process runner by creating all required component in scene.
        /// </summary>
        /// <param name="process">The process which should be run.</param>
        public static void Initialize(IProcess process)
        {
            instance = instance == null ? new GameObject("PROCESS_RUNNER").AddComponent<ProcessRunnerInstance>() : instance;
            instance.process = process;
        }

        /// <summary>
        /// Skips the given amount of chapters.
        /// </summary>
        /// <param name="numberOfChapters">Number of chapters.</param>
        public static void SkipChapters(int numberOfChapters)
        {
            IList<IChapter> chapters = Current.Data.Chapters;

            foreach (IChapter currentChapter in chapters.Skip(chapters.IndexOf(Current.Data.Current)).Take(numberOfChapters))
            {
                currentChapter.LifeCycle.MarkToFastForward();
            }
        }

        /// <summary>
        /// Skips the current chapters.
        /// </summary>
        public static void SkipCurrentChapter()
        {
            if (IsRunning == false)
            {
                return;
            }
            
            IChapter currentChapter = Current.Data.Current;
            if (currentChapter.LifeCycle.Stage == Stage.Inactive)
            {
                currentChapter.LifeCycle.Activate();
            }
            
            currentChapter.LifeCycle.MarkToFastForward();
            currentChapter.LifeCycle.Deactivate();
        }

        /// <summary>
        /// Sets the specified chapter as the next chapter in the process.
        /// </summary>        
        public static void SetNextChapter(IChapter chapter)
        {            
            Current.Data.OverrideNext = chapter;
        }

        /// <summary>
        /// Skips the current step and uses given transition.
        /// </summary>
        /// <param name="transition">Transition which should be used.</param>
        public static void SkipStep(ITransition transition)
        {
            if (IsRunning == false)
            {
                return;
            }

            Current.Data.Current.Data.Current.LifeCycle.MarkToFastForward();
            transition.Autocomplete();

            Events.FastForwardStep?.Invoke(instance, new FastForwardProcessEventArgs(transition, Current));
        }

        /// <summary>
        /// Starts the <see cref="IProcess"/>.
        /// </summary>
        public static void Run()
        {
            if (IsRunning)
            {
                return;
            }

            instance.Execute();
        }
    }
}
