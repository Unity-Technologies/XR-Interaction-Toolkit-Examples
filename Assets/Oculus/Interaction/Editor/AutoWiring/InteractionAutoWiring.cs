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

using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using UnityEditor;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class InteractionAutoWiring
    {
        static InteractionAutoWiring()
        {
            AutoWiring.Register(
                typeof(HandRef),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(ControllerRef),
                new[]
                {
                    new ComponentWiringStrategyConfig("_controller", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );

            AutoWiring.Register(
                typeof(FingerFeatureStateProvider),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                }
            );

            AutoWiring.Register(
                typeof(TransformFeatureStateProvider),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_trackingToWorldTransformer", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_hmd", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors,
                            FieldWiringStrategies.WireFieldToSceneComponent
                        })
                }
            );

            AutoWiring.Register(
                typeof(JointDeltaProvider),
                new[] {
                    new ComponentWiringStrategyConfig("_hand", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                }
            );

            #region HandGrab

            AutoWiring.Register(
                typeof(HandGrab.HandGrabInteractable),
                new[] {
                    new ComponentWiringStrategyConfig("_rigidbody", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_pointableElement", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        }),
                    new ComponentWiringStrategyConfig("_physicsGrabbable", new FieldWiringStrategy[]
                        {
                            FieldWiringStrategies.WireFieldToAncestors
                        })
                }
            );
            #endregion
        }
    }
}
