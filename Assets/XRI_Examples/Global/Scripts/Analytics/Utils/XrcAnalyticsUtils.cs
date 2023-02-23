using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Contains utility methods to easily send XRContent analytics data.
    /// </summary>
    class XrcAnalyticsUtils
    {
        internal static void Register(Door door, StationParameter lockedParameter, StationParameter unlockedParameter)
        {
            if (door == null)
                return;

            door.onLock.AddListener(() => Send(lockedParameter));
            door.onUnlock.AddListener(() => Send(unlockedParameter));
        }

        internal static void Register(XRPushButton pushButton, StationParameter parameter)
        {
            if (pushButton == null)
                return;

            pushButton.onPress.AddListener(() => XrcAnalytics.interactionEvent.Send(parameter));
        }

        internal static void Register(XRGripButton gripButton, StationParameter parameter)
        {
            if (gripButton == null)
                return;

            gripButton.onPress.AddListener(() => XrcAnalytics.interactionEvent.Send(parameter));
        }

        internal static void Register(XRSocketInteractor socket, StationParameter connectParameter, StationParameter disconnectParameter = null)
        {
            if (socket == null)
                return;

            socket.selectEntered.AddListener(_ => Send(connectParameter));
            if (disconnectParameter != null)
                socket.selectExited.AddListener(args => OnSocketDisconnected(args, disconnectParameter));
        }

        internal static void Register(IEnumerable<OnTrigger> onTriggers, StationParameter onEnterParameter)
        {
            foreach (var onTrigger in onTriggers)
                Register(onTrigger, onEnterParameter);
        }

        internal static void Register(OnTrigger onTrigger, StationParameter onEnterParameter)
        {
            if (onTrigger == null)
                return;

            onTrigger.onEnter.AddListener(otherGameObject => Send(onEnterParameter));
        }

        internal static void Register(IEnumerable<XRBaseInteractable> interactables, StationParameter grabParameter, StationParameter activateParameter = null)
        {
            foreach (var interactable in interactables)
                Register(interactable, grabParameter, activateParameter);
        }

        internal static void Register(XRBaseInteractable interactable, StationParameter grabParameter, StationParameter activateParameter = null)
        {
            if (interactable == null)
                return;

            interactable.selectEntered.AddListener(args => OnGrabInteractable(args, grabParameter));
            if (activateParameter != null)
                interactable.activated.AddListener(_ => Send(activateParameter));
        }

        static void OnSocketDisconnected(SelectExitEventArgs args, StationParameter parameter)
        {
            if (!args.isCanceled)
                XrcAnalytics.interactionEvent.Send(parameter);
        }

        static void OnGrabInteractable(SelectEnterEventArgs args, StationParameter parameter)
        {
            if (!(args.interactorObject is XRBaseControllerInteractor))
                return;

            XrcAnalytics.interactionEvent.Send(parameter);
        }

        static void Send(StationParameter parameter)
        {
            XrcAnalytics.interactionEvent.Send(parameter);
        }
    }
}
