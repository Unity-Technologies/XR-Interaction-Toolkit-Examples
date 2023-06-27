/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Oculus.Interaction.Collections;

namespace Oculus.Interaction.Body.Input
{
    /// <summary>
    /// Maps a skeleton with joint id <see cref="TSourceJointId"/> to BodyJointId,
    /// and exposes parent/child relationships within the skeleton.
    /// </summary>
    /// <typeparam name="TSourceJointId">The enum joint type of the source skeleton</typeparam>
    public abstract class BodySkeletonMapping<TSourceJointId> : ISkeletonMapping
        where TSourceJointId : System.Enum
    {
        public IEnumerableHashSet<BodyJointId> Joints => _joints;

        public bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parentJointId)
        {
            return _jointToParent.TryGetValue(jointId, out parentJointId);
        }

        public bool TryGetSourceJointId(BodyJointId jointId, out TSourceJointId sourceJointId)
        {
            return _reverseMap.TryGetValue(jointId, out sourceJointId);
        }

        public bool TryGetBodyJointId(TSourceJointId jointId, out BodyJointId bodyJointId)
        {
            return _forwardMap.TryGetValue(jointId, out bodyJointId);
        }

        protected TSourceJointId GetSourceJointFromBodyJoint(BodyJointId jointId)
        {
            return _reverseMap[jointId];
        }

        protected BodyJointId GetBodyJointFromSourceJoint(TSourceJointId sourceJointId)
        {
            return _forwardMap[sourceJointId];
        }

        protected abstract TSourceJointId GetRoot();
        protected abstract IReadOnlyDictionary<BodyJointId, JointInfo> GetJointMapping();

        public BodySkeletonMapping()
        {
            _tree = new SkeletonTree(GetRoot(), GetJointMapping());
            _joints = new EnumerableHashSet<BodyJointId>(_tree.Nodes.Select(n => n.BodyJointId));
            _forwardMap = _tree.Nodes.ToDictionary((n) => n.SourceJointId, (n) => n.BodyJointId);
            _reverseMap = _tree.Nodes.ToDictionary((n) => n.BodyJointId, (n) => n.SourceJointId);
            _jointToParent = _tree.Nodes.ToDictionary((n) => n.BodyJointId, (n) => n.Parent.BodyJointId);
        }

        private readonly SkeletonTree _tree;
        private readonly IEnumerableHashSet<BodyJointId> _joints;
        private readonly IReadOnlyDictionary<TSourceJointId, BodyJointId> _forwardMap;
        private readonly IReadOnlyDictionary<BodyJointId, TSourceJointId> _reverseMap;
        private readonly IReadOnlyDictionary<BodyJointId, BodyJointId> _jointToParent;

        private class SkeletonTree
        {
            public readonly Node Root;
            public readonly IReadOnlyList<Node> Nodes;

            public SkeletonTree(TSourceJointId root,
                IReadOnlyDictionary<BodyJointId, JointInfo> mapping)
            {
                Dictionary<TSourceJointId, Node> nodes = new Dictionary<TSourceJointId, Node>();
                foreach (var map in mapping)
                {
                    BodyJointId jointId = map.Key;
                    JointInfo jointInfo = map.Value;
                    Assert.IsFalse(nodes.ContainsKey(jointInfo.SourceJointId),
                        "Duplicate Joint ID in Mapping");
                    nodes[jointInfo.SourceJointId] =
                        new Node(jointInfo.SourceJointId, jointId);
                }
                foreach (var jointInfo in mapping.Values)
                {
                    Node thisNode = nodes[jointInfo.SourceJointId];
                    thisNode.Parent = nodes[jointInfo.ParentJointId];
                    thisNode.Parent.Children.Add(thisNode);
                }
                Nodes = new List<Node>(nodes.Values);
                Root = nodes[root];
            }

            public class Node
            {
                public readonly TSourceJointId SourceJointId;
                public readonly BodyJointId BodyJointId;

                public Node Parent;
                public List<Node> Children = new List<Node>();

                public Node(TSourceJointId sourceJointId, BodyJointId bodyJointId)
                {
                    SourceJointId = sourceJointId;
                    BodyJointId = bodyJointId;
                }
            }
        }

        protected readonly struct JointInfo
        {
            public readonly TSourceJointId SourceJointId;
            public readonly TSourceJointId ParentJointId;

            public JointInfo(
                TSourceJointId sourceJointId,
                TSourceJointId parentJointId)
            {
                SourceJointId = sourceJointId;
                ParentJointId = parentJointId;
            }
        }
    }
}
