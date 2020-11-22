# XR Interaction Toolkit Examples

Example projects that use [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest) to demonstrate its functionality with sample assets and behaviors.

The intention of this repository is to provide a means for getting started with the features in the XR Interaction Toolkit package.

## Technical details

### Preview package

The XR Interaction Toolkit is available as a preview package, so it is still in the process of becoming stable enough to release. The features and documentation in this package might change before it is ready for release.

Additionally, the behaviors included in this example may change in a way that does not maintain backwards compatibility.

### Requirements

These projects are compatible with the following versions of the Unity Editor:

- 2019.4 and later

### Other packages

These example projects rely on additional Unity packages that are 2019.4 verified:
- [AR Foundation 2.1](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@2.1/manual/index.html)
- [Input System 1.0](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
- [XR Plugin Management 3.2](https://docs.unity3d.com/Packages/com.unity.xr.management@3.2/manual/index.html)

### Sharing feedback

The [XR Interaction Toolkit and Input forum](https://forum.unity.com/forums/xr-interaction-toolkit-and-input.519/) is the best place to open discussions and ask questions. Please use the [public roadmap](https://portal.productboard.com/brs5gbymuktquzeomnargn2u) to submit feature requests. If you encounter a bug, please use the Unity Bug Reporter in the Unity Editor, accessible via **Help &gt; Report a Bug**. Include “XR Interaction Toolkit” in the title to help our team triage things appropriately!

### Contributions and pull requests

We are not accepting pull requests at this time.

## Getting started

1. Clone or download this repository to a workspace on your drive
    1. Click the **⤓ Code** button on this page to get the URL to clone with Git or click **Download ZIP** to get a copy of this repository that you can extract
1. Open a project in Unity
    1. Download, install, and run [Unity Hub](https://unity3d.com/get-unity/download)
    1. In the **Installs** tab, select **Locate** or **Add** to find or install Unity 2019.4 LTS or later
    1. In the **Projects** tab, click **Add**
    1. Browse to the AR or VR folder within your downloaded copy of this repository and click **Select Folder**
    1. Click the project which should now be added to the list to open the project
1. Open the Scene at `Assets\Scenes\WorldInteractionDemo` by double clicking it in the Project window
1. Configure the project for XR
    1. From Unity’s main menu, go to **Edit &gt; Project Settings &gt; XR Plug-in Management**, and select the platform(s) you plan to deploy to
1. To run the sample on a headset, go to **File &gt; Build Settings** and build the app. If you have a PC VR headset, you can also preview the app by connecting your device and clicking **Play (►)**

If using VR, make sure the Game view has focus (required for XR input currently) by clicking it with your mouse. A **Lock Input to Game View** option is available in the [Input Debugger](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Debugging.html#input-debugger) window (**Window &gt; Analysis &gt; Input Debugger**). Enabling this option forces input to continue processing even when the Game view does not have focus.

As an alternative you can use Unity's package manager in order to import the examples in your project (for now it is available only for the VR examples), following the steps below:

* Navigate to the `Packages` directory of your project.
* Adjust the [project manifest file][Project-Manifest] `manifest.json` in a text editor.
  * Add `"com.unity.xr.interaction.examples": "https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples.git?path=/VR/Assets"` to `dependencies`.
* Switch back to the Unity software and wait for it to finish importing the added package.

## Project overview

This repository contains two different example Unity projects, one with a focus on a Virtual Reality ([VR](#vr)) experience, and another with a focus on an Augmented Reality ([AR](#ar)) experience for mobile.

### VR

This example project contains several Scenes located in `Assets\Scenes` and `Assets\VR Examples\Scenes`.

|**Scene**|**Description**|
|---|---|
|`Scenes\WorldInteractionDemo`|<p>This Scene contains several demo stations that demonstrate various features of the XR Interaction Toolkit. This scene also uses an XR Rig to showcase various ways of navigating the virtual environment via locomotion, including snap-turning, continuous turning, continuous movement, and teleportation.</p><p>This Scene also makes use of several custom behaviors included with this Unity project, including a Controller Manager which manages a logical controller state to selectively enable or disable certain input actions and behaviors.</p><p>Your left hand can grab objects remotely using a Ray Interactor, and your right hand can grab objects directly using a Direct Interactor. Pushing up on the thumbstick will switch to a different Ray Interactor used for teleportation, and releasing the thumbstick will teleport when aiming at a valid location. Pushing left, right, or down on the thumbstick will turn left, right, or around, respectively.</p><p>Use the trigger to interact with the UI in the world while pointing at it with the left controller. The station in the Scene labeled Locomotion Configuration can be used to change locomotion control schemes and configuration preferences. For example, using the controller to enable Continuous Move will make it so the rig will move based on the left controller thumbstick instead of being used to turn and teleport.</p>|
|`Scenes\DeprecatedWorldInteractionDemo`|<p>A similar environment to WorldInteractionDemo, but with an alternate XR Rig setup that uses the Device-based variants of behaviors that do not use the Input System.</p><p>It is recommended that you use the Action-based variant of behaviors used in WorldInteractionDemo instead of the Device-based variant to take advantage of the benefits that the Input System package provides. Some features, such as the XR Device Simulator, are only supported when using Actions. This Scene is included mainly as a reference for users who began using the package before version 0.10.0.</p>|
|`VR Examples\Scenes\Initial Tracking`|<p>This is a basic scene that shows how to set up device tracking. Look around in your VR headset and identify the tracked hand controllers.</p>|
|`VR Examples\Scenes\Grab Interaction`|<p>This scene shows how to set up a grab interactable that can be grabbed by either hand controller.</p><p>Your left hand can grab the cube remotely using a Ray Interactor, and your right hand can grab it directly using a Direct Interactor.</p>|
|`VR Examples\Scenes\Socket Interaction`|<p>This scene shows how to set up a socket to hold an interactable.</p><p>Grab the cube then try throwing it to the socket. You will see the cube snap to the socket.</p>|
|`VR Examples\Scenes\Teleportation`|<p>This scene shows examples of configuring Teleportation Areas, Anchors, and options on visuals and orientation.</p><p>Use the Trigger (Select) button on your controller to teleport when pointing to a platform.</p>|
|`VR Examples\Scenes\UI Interaction`|<p>This scene shows how to set up a world canvas UI.</p><p>Use the trigger button to interact with the UI elements.</p>|

#### Controller Manager

This project contains two variants of a Controller Manager behavior to manage multiple Interactors depending on different conditions of the player.

The `ActionBasedControllerManager` script makes use of Input System Actions to both read input to detect logical controller state changes, and to enable or disable shared Actions. This is used in the `WorldInteractionDemo` Scene to swap between two mutually exclusive Interactor behaviors for each controller. For example, for the right-hand Controller Manager, when detecting input to activate a teleportation mode, the behavior as configured in the Scene will swap between the Direct Interactor used for grabbing objects, and the Ray Interactor used for aiming a teleportation arc.

The `ControllerManager` script is similar to the `ActionBasedControllerManager` script, but uses the API to read features values of an `InputDevice` directly rather than from Input System Actions. It also differs in that it only manages transitions between two states: a normal selection state, and a teleportation state. This is used in the `DeprecatedWorldInteractionDemo` Scene.

#### Locomotion

The `LocomotionSchemeManager` script in the example project is used in the `WorldInteractionDemo` Scene. It is used as a central manager to configure locomotion control schemes and configuration preferences.

The `LocomotionConfigurationMenu` script is used to present locomotion control schemes and configuration preferences, and respond to player input in the UI to set them. It is attached to a Canvas in the `WorldInteractionDemo` Scene as a way for the player to adjust settings related to locomotion.

The Default Input Actions sample included with the XR Interaction Toolkit package is installed in this example project, which contains an Input Actions Asset with different locomotion control schemes. When used with the Locomotion Scheme Manager as configured in the Scene, this allows the player to swap between different input styles for locomotion, such as changing between snap-turning left and right, and smooth turning.

### AR

This example project contains a Scene located in `Assets\Scenes`. The AR example project currently supports only mobile AR platforms. See the [ARKit documentation](https://docs.unity3d.com/Packages/com.unity.xr.arkit@2.1/manual/index.html) or the [ARCore documentation](https://docs.unity3d.com/Packages/com.unity.xr.arcore@2.1/manual/index.html) for more details on how to set up the project to deploy to either platform.

|**Scene**|**Description**|
|---|---|
|`Scenes\ARInteraction`|<p>This scene shows how to set up a Gesture Interactor and a Placement Interactable for a mobile AR experience.</p><p>Place a cube onto a detected plane, and use gestures to move, scale and rotate the placed object. This demonstrates the functionality of `ARTranslationInteractable`, `ARRotationInteractable`, and `ARScaleInteractable` respectively. The scene also demonstrates how to use `ARScaleInteractable` to create selection visual effects such as the cube being highlighted when selected.</p>|
