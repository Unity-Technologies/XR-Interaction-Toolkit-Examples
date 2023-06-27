// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.Serialization.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VRBuilder.Core.Serialization
{
    /// <summary>
    /// Improved version of the NewtonsoftJsonProcessSerializer, which now allows to serialize very long chapters.
    /// </summary>
    public class ImprovedNewtonsoftJsonProcessSerializer : NewtonsoftJsonProcessSerializer
    {
        /// <inheritdoc/>
        public override string Name { get; } = "Improved Newtonsoft Json Importer";

        protected override int Version { get; } = 2;

        /// <inheritdoc/>
        public override IProcess ProcessFromByteArray(byte[] data)
        {
            string stringData = new UTF8Encoding().GetString(data);
            JObject dataObject = JsonConvert.DeserializeObject<JObject>(stringData, ProcessSerializerSettings);

            // Check if process was serialized with version 1
            int version = dataObject.GetValue("$serializerVersion").ToObject<int>();
            if (version == 1)
            {
                return base.ProcessFromByteArray(data);
            }

            ProcessWrapper wrapper = Deserialize<ProcessWrapper>(data, ProcessSerializerSettings);
            return wrapper.GetProcess();
        }

        /// <inheritdoc/>
        public override byte[] ProcessToByteArray(IProcess process)
        {
            ProcessWrapper wrapper = new ProcessWrapper(process);
            JObject jObject = JObject.FromObject(wrapper, JsonSerializer.Create(ProcessSerializerSettings));
            jObject.Add("$serializerVersion", Version);
            // This line is required to undo the changes applied to the process.
            wrapper.GetProcess();

            return new UTF8Encoding().GetBytes(jObject.ToString());
        }

        [Serializable]
        private class ProcessWrapper
        {
            [DataMember]
            public List<IStep> Steps = new List<IStep>();

            [DataMember]
            public IProcess Process;

            public ProcessWrapper()
            {

            }

            public ProcessWrapper(IProcess process)
            {
                foreach (IChapter chapter in process.Data.Chapters)
                {
                    Steps.AddRange(chapter.Data.Steps);
                }

                foreach (IStep step in Steps)
                {
                    foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
                    {
                        if (transition.Data.TargetStep != null)
                        {
                            transition.Data.TargetStep = new StepRef() { PositionIndex = Steps.IndexOf(transition.Data.TargetStep) };
                        }
                    }
                }
                Process = process;
            }

            public IProcess GetProcess()
            {
                foreach (IStep step in Steps)
                {
                    foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
                    {
                        if (transition.Data.TargetStep == null)
                        {
                            continue;
                        }

                        StepRef stepRef = (StepRef) transition.Data.TargetStep;
                        transition.Data.TargetStep = stepRef.PositionIndex >= 0 ? Steps[stepRef.PositionIndex] : null;
                    }
                }

                return Process;
            }

            [Serializable]
            public class StepRef : IStep
            {
                [DataMember]
                public int PositionIndex = -1;

                IData IDataOwner.Data { get; } = null;

                IStepData IDataOwner<IStepData>.Data { get; } = null;

                public ILifeCycle LifeCycle { get; } = null;

                public IStageProcess GetActivatingProcess()
                {
                    throw new NotImplementedException();
                }

                public IStageProcess GetActiveProcess()
                {
                    throw new NotImplementedException();
                }

                public IStageProcess GetDeactivatingProcess()
                {
                    throw new NotImplementedException();
                }

                public void Configure(IMode mode)
                {
                    throw new NotImplementedException();
                }

                public void Update()
                {
                    throw new NotImplementedException();
                }

                public IStep Clone()
                {
                    throw new NotImplementedException();
                }

                public StepMetadata StepMetadata { get; set; }
                public IEntity Parent { get; set; }
            }
        }
    }
}
