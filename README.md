# XR Interaction Toolkit Examples - Version 3.0.7

## Introduction

This project provides examples that use Unity's [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html) (XRI) to demonstrate its functionality with example assets and behaviors.
The intention of this project is to provide a means for getting started with the features in the XR Interaction Toolkit package.

> Note: If you are looking for the original XRI Examples project, that has been archived into two separate branches [Classic 1.0](https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples/tree/1.0/classic) and [Classic 2.2](https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples/tree/classic/2.2). Both of these branches still have both the `AR` and `VR` projects available.

## Getting started

### Requirements
The current version of the XRI Examples is compatible with the following versions of the Unity Editor:

* 2021.3 and later

### Downloading the project

1. Clone or download this repository to a workspace on your drive
    1. Click the **⤓ Code** button on this page to get the URL to clone with Git or click **Download ZIP** to get a copy of this repository that you can extract
1. Open a project in Unity
    1. Download, install, and run [Unity Hub](https://unity3d.com/get-unity/download)
    1. In the **Installs** tab, select **Locate** or **Add** to find or install Unity 2021.3 LTS or later. Include the **Windows Build Support (IL2CPP)** module if building for PC, and the **Android Build Support** if building for Android (for example, Meta Quest).
    1. In the **Projects** tab, click **Add**
    1. Browse to folder where you downloaded a copy of this repository and click **Select Folder**
    1. Verify the project has been added as **XR-Interaction-Toolkit-Examples**, and click on it to open the project

## General setup

The main example scene is located at `Assets/XRI_Examples/Scenes/XRI_Examples_Main`. This example scene is laid out as a ring with different stations along it. The first examples you will encounter are the simplest use-cases of XRI features. Behind each example is a doorway leading to advanced uses of each feature.

Use the simple examples when you need objects you can copy-and-paste, while leveraging the advanced examples when needing to achieve complex outcomes.

The **XR Origin** is located within the **Complete Set Up** prefab. This prefab contains everything needed for a fully functional user interaction with XRI. This includes the components needed for general input, interaction, and UI interaction.

Scripts, assets, and prefabs related to each feature or use case are located in the associated folder in `Assets/XRI_Examples`.

The following stations are available in the XRI Examples:

* [Station 1: Locomotion Setup](Documentation/LocomotionSetup.md) - Overview of the built-in locomotion options and how to configure them.
* [Station 2: Grab Interactables](Documentation/GrabInteractables.md) - Basic object manipulation.
* [Station 3: Activate Interactables](Documentation/ActivateInteractables.md) - Manipulation of objects that can be triggered by the user.
* [Station 4: Socket Interactors](Documentation/SocketInteractors.md) - Manipulation of objects that can snap to specific positions.
* [Station 5: Gaze Interaction](Documentation/Gaze.md) - Leverage the eye-tracked or head-based gaze interactor to add assistive interaction.
* [Station 6: Focus Interaction](Documentation/Focus.md) - Interaction with focused objects.
* [Station 7: 2D UI](Documentation/UI-2D.md) - Creation and interaction with [world space](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UICanvas.html#world-space) 2D UI.
* [Station 8: 3D UI](Documentation/UI-3D.md) - Creation and interaction with 3D constrained controls.
* [Station 9: Physics Interactables](Documentation/PhysicsInteractables.md) - Best practices for combining physics and XR input.
* [Station 10: Climb Interactables](Documentation/ClimbInteractables.md) - Interaction with objects that allow for climbing.

For a list of new features and deprecations, see [XRI Examples Changelog](CHANGELOG.md).

For an overview of how the [Input System](https://docs.unity3d.com/Manual/com.unity.inputsystem.html) is used in this example, see [Input](Documentation/Input.md).

## Sharing feedback

The [XR Interaction Toolkit and Input forum](https://forum.unity.com/forums/xr-interaction-toolkit-and-input.519/) is the best place to open discussions and ask questions. Please use the [public roadmap](https://portal.productboard.com/brs5gbymuktquzeomnargn2u) to submit feature requests. If you encounter a bug, please use the Unity Bug Reporter in the Unity Editor, accessible via **Help &gt; Report a Bug**. Include “XR Interaction Toolkit” in the title to help our team triage things appropriately!

## Contributions and pull requests

We are not accepting pull requests at this time.
