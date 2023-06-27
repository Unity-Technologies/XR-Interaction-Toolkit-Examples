using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using UnityEngine.Scripting;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// This behavior changes the parent of a game object in the scene hierarchy. It can accept a null parent, in which case the object will be unparented.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/set-parent")]
    public class SetParentBehavior : Behavior<SetParentBehavior.EntityData>
    {
        [DisplayName("Set Parent")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            // Process object to reparent.
            [DataMember]
            public SceneObjectReference Target { get; set; }

            // New parent game object.
            [DataMember]
            public SceneObjectReference Parent { get; set; }

            [DataMember]
            [DisplayName("Snap to parent transform")]
            public bool SnapToParentTransform { get; set; }

            public Metadata Metadata { get; set; }

            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string target = Target.IsEmpty() ? "[NULL]" : Target.Value.GameObject.name;
                    string parent = Parent.IsEmpty() ? "[NULL]" : Parent.Value.GameObject.name;

                    return Parent.IsEmpty() ? $"Unparent {target}" : $"Make {target} child of {parent}";
                }
            }
        }

        [JsonConstructor, Preserve]
        public SetParentBehavior() : this("", "")
        {
        }

        public SetParentBehavior(ISceneObject target, ISceneObject parent, bool snapToParentTransform = false) : this(ProcessReferenceUtils.GetNameFrom(target), ProcessReferenceUtils.GetNameFrom(parent), snapToParentTransform)
        {
        }

        public SetParentBehavior(string target, string parent, bool snapToParentTransform = false)
        {
            Data.Target = new SceneObjectReference(target);
            Data.Parent = new SceneObjectReference(parent);
            Data.SnapToParentTransform = snapToParentTransform;
        }

        private class ActivatingProcess : StageProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
                yield return null;
            }

            /// <inheritdoc />
            public override void End()
            {
                if (Data.Parent.Value == null)
                {
                    Data.Target.Value.GameObject.transform.SetParent(null);
                }
                else
                {
                    if (HasScaleIssues())
                    {
                        Debug.LogWarning($"'{Data.Target.Value.GameObject.name}' is being parented to a hierarchy that has changes in rotation and scale. This may result in a distorted object after parenting.");
                    }

                    if (Data.SnapToParentTransform)
                    {
                        Data.Target.Value.GameObject.transform.SetPositionAndRotation(Data.Parent.Value.GameObject.transform.position, Data.Parent.Value.GameObject.transform.rotation);
                    }

                    Data.Target.Value.GameObject.transform.SetParent(Data.Parent.Value.GameObject.transform, true);
                }
            }

            /// <inheritdoc />
            public override void FastForward()
            {
            }

            private bool HasScaleIssues()
            {
                Transform currentTransform = Data.Target.Value.GameObject.transform;
                Transform parentTransform = Data.Parent.Value.GameObject.transform;

                bool changesScale = currentTransform.localScale != Vector3.one;
                bool changesRotation = currentTransform.rotation != parentTransform.rotation && Data.SnapToParentTransform == false; 

                while (parentTransform != null)
                {
                    changesScale |= parentTransform.localScale != Vector3.one;

                    if (parentTransform.parent != null)
                    {
                        changesRotation |= parentTransform.rotation != parentTransform.parent.rotation;
                    }

                    parentTransform = parentTransform.parent;
                }

                return changesScale && changesRotation;
            }
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }
    }
}
