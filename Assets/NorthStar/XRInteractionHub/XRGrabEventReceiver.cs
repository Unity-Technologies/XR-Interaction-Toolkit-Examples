using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.XR.CoreUtils;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Pooling;

#if BURST_PRESENT
using Unity.Burst;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Interactables
{
#if BURST_PRESENT
    [BurstCompile]
#endif
    public class XRGrabEventReceiver: XRBaseInteractable, IFarAttachProvider
    {
        public InteractableFarAttachMode farAttachMode { get; set; }
    }
}