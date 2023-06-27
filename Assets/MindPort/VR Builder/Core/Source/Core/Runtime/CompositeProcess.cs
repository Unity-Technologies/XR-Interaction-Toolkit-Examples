// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VRBuilder.Core
{
    /// <summary>
    /// A process which consists of multiple processes which execute at the same time. It ends when all its child processes end.
    /// </summary>
    public class CompositeProcess : IStageProcess
    {
        private readonly IEnumerable<IStageProcess> stageProcesses;

        /// <param name="processes">Child processes which are united into this composite process.</param>
        public CompositeProcess(params IStageProcess[] processes)
        {
            stageProcesses = processes;
        }

        /// <inheritdoc />
        public void Start()
        {
            foreach (IStageProcess childProcess in stageProcesses)
            {
                childProcess.Start();
            }
        }

        /// <inheritdoc />
        public IEnumerator Update()
        {
            IEnumerator[] updates = stageProcesses.Select(process => process.Update()).ToArray();

            bool isAnyRequiredUpdateRuns = true;

            while (isAnyRequiredUpdateRuns)
            {
                isAnyRequiredUpdateRuns = false;

                for (int i = 0; i < updates.Length; i++)
                {
                    if (updates[i] == null)
                    {
                        continue;
                    }

                    if (updates[i].MoveNext())
                    {
                        isAnyRequiredUpdateRuns = true;
                    }
                    else
                    {
                        updates[i] = null;
                    }
                }

                yield return null;
            }
        }

        /// <inheritdoc />
        public void End()
        {
            foreach (IStageProcess childProcess in stageProcesses)
            {
                childProcess.End();
            }
        }

        /// <inheritdoc />
        public void FastForward()
        {
            foreach (IStageProcess childProcess in stageProcesses)
            {
                childProcess.FastForward();
            }
        }
    }
}
