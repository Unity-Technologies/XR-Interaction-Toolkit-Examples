# XR Interaction Toolkit Examples

Example projects that use [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest) to demonstrate its functionality with sample assets and behaviors.

The intention of this repository is to provide a means for getting started with the features in the XR Interaction Toolkit package.

## Technical details

### Versioning

The behaviors included in this example may change in a way that does not maintain backwards compatibility.

### Requirements

These projects are compatible with the following versions of the Unity Editor:

- 2020.3 and higher

### Other packages

These example projects rely on additional Unity packages:
- [AR Foundation](https://docs.unity3d.com/Manual/com.unity.xr.arfoundation.html)
- [Input System](https://docs.unity3d.com/Manual/com.unity.inputsystem.html)
- [XR Plugin Management](https://docs.unity3d.com/Manual/com.unity.xr.management.html)

### Sharing feedback

The [XR Interaction Toolkit and Input forum](https://forum.unity.com/forums/xr-interaction-toolkit-and-input.519/) is the best place to open discussions and ask questions. Please use the [public roadmap](https://portal.productboard.com/brs5gbymuktquzeomnargn2u) to submit feature requests. If you encounter a bug, please use the Unity Bug Reporter in the Unity Editor, accessible via **Help &gt; Report a Bug**. Include “XR Interaction Toolkit” in the title to help our team triage things appropriately!

### Contributions and pull requests

We are not accepting pull requests at this time.

## Getting started

1. Clone or download this repository to a workspace on your drive
    1. Click the **⤓ Code** button on this page to get the URL to clone with Git or click **Download ZIP** to get a copy of this repository that you can extract
1. Open a project in Unity
    1. Download, install, and run [Unity Hub](https://unity3d.com/get-unity/download)
    1. In the **Installs** tab, select **Locate** or **Add** to find or install Unity 2020.3 LTS or later
    1. In the **Projects** tab, click **Add**
    1. Browse to the AR or VR folder within your downloaded copy of this repository and click **Select Folder**
    1. Click the project which should now be added to the list to open the project
1. For the VR project, open the scene at `Assets\Scenes\WorldInteractionDemo` by double clicking it in the Project window. For the AR project, open the scene at `Assets\Scenes\ARInteraction`.
1. Configure the VR project for XR devices
    1. From Unity’s main menu, go to **Edit &gt; Project Settings &gt; XR Plug-in Management**, and select the platform(s) you plan to deploy to
1. To run the VR sample on a headset, go to **File &gt; Build Settings** and build the app. If you have a PC VR headset, you can also preview the app by connecting your device and clicking **Play (►)**

## Project overview

This repository contains two different example Unity projects, one with a focus on a Virtual Reality ([VR](#vr)) experience, and another with a focus on an Augmented Reality ([AR](#ar)) experience for mobile.

### VR

This example project contains a couple scenes located in `Assets\Scenes\`.

|**Scene**|**Description**|
|---|---|
|`Scenes\WorldInteractionDemo`|<p>This scene contains several demo stations that demonstrate various features of the XR Interaction Toolkit. This scene also uses an XR Origin to showcase various ways of navigating the virtual environment via locomotion, including snap-turning, continuous turning, continuous movement, and teleportation.</p><p>This scene also makes use of several behaviors included with this Unity project and the Starter Assets sample, including a Controller Manager which manages a logical controller state to selectively enable or disable certain input actions and behaviors.</p><p>Your left hand can grab objects remotely using a Ray Interactor, and your right hand can grab objects directly using a Direct Interactor. Pushing up on the thumbstick will switch to a different Ray Interactor used for teleportation, and releasing the thumbstick will teleport when aiming at a valid location. Pushing left, right, or down on the thumbstick will turn left, right, or around, respectively.</p><p>Use the trigger to interact with the UI in the world while pointing at it with the left controller. The station in the scene labeled Locomotion Configuration can be used to change locomotion control schemes and configuration preferences. For example, using the controller to enable Continuous Move will make it so the XR Origin will move based on the left controller thumbstick instead of being used to turn and teleport.</p>|
|`Scenes\DeprecatedWorldInteractionDemo`|<p>A similar environment to `WorldInteractionDemo`, but with an alternate XR Origin setup that uses the Device-based variants of behaviors that do not use the Input System.</p><p>It is recommended that you use the Action-based variant of behaviors used in `WorldInteractionDemo` instead of the Device-based variant to take advantage of the benefits that the Input System package provides. Some features, such as the XR Device Simulator, are only supported when using Actions. This scene is included mainly as a reference for users who began using the package before version 0.10.0.</p>|

#### Controller Manager

This project contains two variants of a Controller Manager behavior to manage multiple Interactors depending on different conditions of the player.

The `ActionBasedControllerManager` script makes use of Input System Actions to both read input to detect logical controller state changes, and to enable or disable shared Actions. This is used in the `WorldInteractionDemo` scene to swap between two mutually exclusive Interactor behaviors for each controller. For example, for the right-hand Controller Manager, when detecting input to activate a teleportation mode, the behavior as configured in the scene will swap between the Direct Interactor used for grabbing objects, and the Ray Interactor used for aiming a teleportation arc.

The `ControllerManager` script is similar to the `ActionBasedControllerManager` script, but uses the API to read features values of an `InputDevice` directly rather than from Input System Actions. It also differs in that it only manages transitions between two states: a normal selection state, and a teleportation state. This is used in the `DeprecatedWorldInteractionDemo` scene.

#### Locomotion

The `LocomotionSchemeManager` script in the example project is used in the `WorldInteractionDemo` scene. It is used as a central manager to configure locomotion control schemes and configuration preferences.

The `LocomotionConfigurationMenu` script is used to present locomotion control schemes and configuration preferences, and respond to player input in the UI to set them. It is attached to a Canvas in the `WorldInteractionDemo` scene as a way for the player to adjust settings related to locomotion.

The Starter Assets sample included with the XR Interaction Toolkit package is installed in this example project, which contains an Input Action Asset with different locomotion control schemes. When used with the Locomotion Scheme Manager as configured in the scene, this allows the player to swap between different input styles for locomotion, such as changing between snap-turning left and right, and smooth turning.

#### Android/Oculus

A modified Android manifest file is used to enable the Oculus system keyboard when an input field receives focus, an example of which is in the `WorldInteractionDemo` scene. This was done by editing the generating `AndroidManifest.xml` file created by first opening **Edit** &gt; **Project Settings** &gt; **Player**, clicking the Android settings tab, and then under the Build header clicking to enable **Custom Main Manifest**. The following line was added to the `manifest` element:
```xml
<uses-feature android:name="oculus.software.overlay_keyboard" android:required="false"/>
```

### AR

This example project contains a scene located in `Assets\Scenes\`. The AR example project currently supports only mobile AR platforms. See the [ARKit documentation](https://docs.unity3d.com/Manual/com.unity.xr.arkit.html) or the [ARCore documentation](https://docs.unity3d.com/Manual/com.unity.xr.arcore.html) for more details on how to set up the project to deploy to either platform.

For testing single touch interactions in the editor with a mouse you must first enable **Simulate Touch Input from Mouse or Pen** in the Input Debug window under Options **Window** &gt; **Analysis** &gt; **Input Debugger**.

|**Scene**|**Description**|
|---|---|
|`Scenes\ARInteraction`|<p>This scene shows how to set up a Gesture Interactor and a Placement Interactable for a mobile AR experience.</p><p>Place a cube onto a detected plane, and use gestures to move, rotate, and scale the placed object. This demonstrates the functionality of `ARTranslationInteractable`, `ARRotationInteractable`, and `ARScaleInteractable` respectively. The scene also demonstrates how to use `ARSelectionInteractable` to create selection visual effects such as the cube being highlighted when selected.</p>|
|`Scenes\ARInteraction_MultipleObjects`|<p>This scene is similar to `ARInteraction` but it includes a sample script called `SwitchPlacementPrefab` which demonstrates how to place multiple different types of `GameObjects`.</p><p>Known Limitations: Taps on the dropdown UI when over a tracked plane will also place an object.</p>|
