using System;

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Base class for interaction parameters.
    /// </summary>
    [Serializable]
    abstract class InteractionEventParameter
    {
        /// <summary>
        /// The parameter unique name.
        /// </summary>
        [SerializeField]
        protected string Name;

        internal InteractionEventParameter()
        {
            Name = GetType().Name;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    // Scene Started Parameter
    [Serializable]
    sealed class SceneStarted : InteractionEventParameter
    { }

    /// <summary>
    /// Base class for station parameters. A station parameter has a <c>Station</c> and <c>Substaion</c> name.
    /// </summary>
    [Serializable]
    abstract class StationParameter : InteractionEventParameter
    {
        protected static class GrabInteractableStation
        {
            internal const string k_StationName = "GrabInteractable";

            internal const string k_SimpleObjectSubstationName = "SimpleObject";
            internal const string k_WateringCanSubstationName = "WateringCan";
            internal const string k_PiggyBankSubstationName = "PiggyBank";
            internal const string k_RibbonStickSubstationName = "RibbonSitck";
        }

        protected static class ActiveInteractableStation
        {
            internal const string k_StationName = "ActiveInteractable";

            internal const string k_ActiveSimpleObjectSubstationName = "ActiveSimpleObject";
            internal const string k_CandleSubstationName = "Candle";
            internal const string k_LauncherSubstationName = "Launcher";
            internal const string k_MegaphoneSubstationName = "Megaphone";
        }

        protected static class SocketInteractorStation
        {
            internal const string k_StationName = "SocketInteractors";

            internal const string k_SimpleSocketSubstationName = "SocketSimpleObject";
            internal const string k_PerlerMachineSubstationName = "PerlerMachine";
        }

        protected static class Interaction3DUIStation
        {
            internal const string k_StationName = "3DUIInteraction";

            internal const string k_3DUISimpleControlsSubstationName = "3DUISimpleControls";
            internal const string k_ClawMachineSubstationName = "ClawMachine";
        }

        protected static class PhysicsInteractableStation
        {
            internal const string k_StationName = "PhysicsInteraction";

            internal const string k_PhysicsSimpleControlsSubstationName = "PhysicsSimpleControls";
            internal const string k_CabinetExampleSubstationName = "CabinetExample";
            internal const string k_DoorsExampleSubstationName = "DoorsExample";
        }

        [SerializeField]
        protected string Station;

        [SerializeField]
        protected string Substation;
    }

    // Grab Simple Object Parameters
    [Serializable]
    abstract class SimpleObjectSubstation : StationParameter
    {
        internal SimpleObjectSubstation()
        {
            Station = GrabInteractableStation.k_StationName;
            Substation = GrabInteractableStation.k_SimpleObjectSubstationName;
        }
    }

    [Serializable]
    sealed class GrabSimpleObjectInstant : SimpleObjectSubstation
    { }

    [Serializable]
    sealed class GrabSimpleObjectKinematic : SimpleObjectSubstation
    { }

    [Serializable]
    sealed class GrabSimpleObjectVelocity : SimpleObjectSubstation
    { }

    // Watering Can Parameters
    [Serializable]
    abstract class WateringCanSubstation : StationParameter
    {
        internal WateringCanSubstation()
        {
            Station = GrabInteractableStation.k_StationName;
            Substation = GrabInteractableStation.k_WateringCanSubstationName;
        }
    }

    [Serializable]
    sealed class GrabWateringCan : WateringCanSubstation
    { }

    [Serializable]
    sealed class WateringPlant : WateringCanSubstation
    { }

    // Piggy Bank Parameters
    [Serializable]
    abstract class PiggyBankSubstation : StationParameter
    {
        internal PiggyBankSubstation()
        {
            Station = GrabInteractableStation.k_StationName;
            Substation = GrabInteractableStation.k_PiggyBankSubstationName;
        }
    }

    [Serializable]
    sealed class GrabMallet : PiggyBankSubstation
    { }

    [Serializable]
    sealed class BreakPiggyBank : PiggyBankSubstation
    { }

    // Grab Ribbon Stick Parameters
    [Serializable]
    sealed class GrabRibbonStick : StationParameter
    {
        internal GrabRibbonStick()
        {
            Station = GrabInteractableStation.k_StationName;
            Substation = GrabInteractableStation.k_RibbonStickSubstationName;
        }
    }

    // Active Simple Object Parameters
    [Serializable]
    abstract class ActiveSimpleObjectSubstation : StationParameter
    {
        internal ActiveSimpleObjectSubstation()
        {
            Station = ActiveInteractableStation.k_StationName;
            Substation = ActiveInteractableStation.k_ActiveSimpleObjectSubstationName;
        }
    }

    [Serializable]
    sealed class GrabActiveSimpleObject : ActiveSimpleObjectSubstation
    { }

    [Serializable]
    sealed class SimpleObjectActivated : ActiveSimpleObjectSubstation
    { }

    // Candle Parameters
    [Serializable]
    abstract class CandleSubstation : StationParameter
    {
        internal CandleSubstation()
        {
            Station = ActiveInteractableStation.k_StationName;
            Substation = ActiveInteractableStation.k_CandleSubstationName;
        }
    }

    [Serializable]
    sealed class GrabLighter : CandleSubstation
    { }

    [Serializable]
    sealed class LighterActivated : CandleSubstation
    { }

    [Serializable]
    sealed class GrabCandle : CandleSubstation
    { }

    [Serializable]
    sealed class LightCandle : CandleSubstation
    { }

    // Launcher Parameters
    [Serializable]
    abstract class LauncherSubstation : StationParameter
    {
        internal LauncherSubstation()
        {
            Station = ActiveInteractableStation.k_StationName;
            Substation = ActiveInteractableStation.k_LauncherSubstationName;
        }
    }

    [Serializable]
    sealed class GrabLauncher : LauncherSubstation
    { }

    [Serializable]
    sealed class LauncherActivated : LauncherSubstation
    { }

    [Serializable]
    sealed class LauncherEasyTargetHit : LauncherSubstation
    { }

    [Serializable]
    sealed class LauncherMediumTargetHit : LauncherSubstation
    { }

    [Serializable]
    sealed class LauncherHardTargetHit : LauncherSubstation
    { }

    // Megaphone Parameters
    [Serializable]
    abstract class MegaphoneSubstation : StationParameter
    {
        internal MegaphoneSubstation()
        {
            Station = ActiveInteractableStation.k_StationName;
            Substation = ActiveInteractableStation.k_MegaphoneSubstationName;
        }
    }

    [Serializable]
    sealed class GrabMegaphone : MegaphoneSubstation
    { }

    [Serializable]
    sealed class MegaphoneActivated : MegaphoneSubstation
    { }

    // Socket Simple Object
    [Serializable]
    abstract class SimpleSocketSubstation : StationParameter
    {
        internal SimpleSocketSubstation()
        {
            Station = SocketInteractorStation.k_StationName;
            Substation = SocketInteractorStation.k_SimpleSocketSubstationName;
        }
    }

    [Serializable]
    sealed class ConnectSocketSimpleObject : SimpleSocketSubstation
    { }

    [Serializable]
    sealed class DisconnectSocketSimpleObject : SimpleSocketSubstation
    { }

    // Perler Machine
    [Serializable]
    abstract class PerlerMachineSubstation : StationParameter
    {
        internal PerlerMachineSubstation()
        {
            Station = SocketInteractorStation.k_StationName;
            Substation = SocketInteractorStation.k_PerlerMachineSubstationName;
        }
    }

    [Serializable]
    sealed class ConnectPerlerMachineBattery : PerlerMachineSubstation
    { }

    [Serializable]
    sealed class GrabPerlerBead : PerlerMachineSubstation
    { }

    [Serializable]
    sealed class ConnectPerlerBead : PerlerMachineSubstation
    { }

    // 3DUI Simple Controls
    [Serializable]
    abstract class SimpleControlsParameter : StationParameter
    {
        internal SimpleControlsParameter()
        {
            Station = Interaction3DUIStation.k_StationName;
            Substation = Interaction3DUIStation.k_3DUISimpleControlsSubstationName;
        }
    }

    [Serializable]
    sealed class LeverInteraction : SimpleControlsParameter
    { }

    [Serializable]
    sealed class JoystickInteraction : SimpleControlsParameter
    { }

    [Serializable]
    sealed class DialInteraction : SimpleControlsParameter
    { }

    [Serializable]
    sealed class WheelInteraction : SimpleControlsParameter
    { }

    [Serializable]
    sealed class SliderInteraction : SimpleControlsParameter
    { }

    [Serializable]
    sealed class GripButtonPressed : SimpleControlsParameter
    { }

    [Serializable]
    sealed class PushButtonPressed : SimpleControlsParameter
    { }

    // Claw Machine Parameters
    [Serializable]
    abstract class ClawMachineParameter : StationParameter
    {
        internal ClawMachineParameter()
        {
            Station = Interaction3DUIStation.k_StationName;
            Substation = Interaction3DUIStation.k_ClawMachineSubstationName;
        }
    }

    [Serializable]
    sealed class ClawMachineJoystickInteraction : ClawMachineParameter
    { }

    [Serializable]
    sealed class ClawMachinePushButtonPressed : ClawMachineParameter
    { }

    [Serializable]
    sealed class ConnectClawMachineToPrize : ClawMachineParameter
    { }

    [Serializable]
    sealed class GrabClawMachinePrize : ClawMachineParameter
    { }

    // Physics Simple Controls
    [Serializable]
    abstract class PhysicsSimpleControlsParameter : StationParameter
    {
        internal PhysicsSimpleControlsParameter()
        {
            Station = PhysicsInteractableStation.k_StationName;
            Substation = PhysicsInteractableStation.k_PhysicsSimpleControlsSubstationName;
        }
    }

    [Serializable]
    sealed class GrabSpringJoint : PhysicsSimpleControlsParameter
    { }

    [Serializable]
    sealed class GrabHingeJoint : PhysicsSimpleControlsParameter
    { }

    // Cabinet Example
    [Serializable]
    abstract class CabinetExampleParameter : StationParameter
    {
        internal CabinetExampleParameter()
        {
            Station = PhysicsInteractableStation.k_StationName;
            Substation = PhysicsInteractableStation.k_CabinetExampleSubstationName;
        }
    }

    [Serializable]
    sealed class GrabCabinet1 : CabinetExampleParameter
    { }

    [Serializable]
    sealed class GrabCabinet2 : CabinetExampleParameter
    { }

    // Doors Example
    [Serializable]
    abstract class DoorsExampleParameter : StationParameter
    {
        internal DoorsExampleParameter()
        {
            Station = PhysicsInteractableStation.k_StationName;
            Substation = PhysicsInteractableStation.k_DoorsExampleSubstationName;
        }
    }

    [Serializable]
    sealed class PushFlipDoor : DoorsExampleParameter
    { }

    [Serializable]
    sealed class GrabDoorKey : DoorsExampleParameter
    { }

    [Serializable]
    sealed class DoorUnlocked : DoorsExampleParameter
    { }

    [Serializable]
    sealed class DoorLocked : DoorsExampleParameter
    { }

    [Serializable]
    sealed class GrabDoorHandle : DoorsExampleParameter
    { }

    /// <summary>
    /// Editor event used to send <c>XRContent</c> interaction analytics data.
    /// Only accepts <c>InteractionEventParameter</c> objects as parameter.
    /// </summary>
    class InteractionEvent : EditorEvent
    {
        const string k_EventName = "xrcInteraction";

        internal InteractionEvent(int maxPerHour = k_DefaultMaxEventsPerHour, int maxElementCount = k_DefaultMaxElementCount)
            : base(k_EventName, maxPerHour, maxElementCount)
        { }

        internal bool Send(InteractionEventParameter parameter)
        {
            return parameter != null && base.Send(parameter);
        }
    }
}
