# XRI Examples Overview

## Introduction

This project provides examples that use Unity's [XR Interaction Toolkit](https://docs.unity3d.com/Manual/com.unity.xr.interaction.toolkit.html) (XRI) to demonstrate its functionality with sample assets and behaviors.
The intention of this project is to provide a means for getting started with the features in the XR Interaction Toolkit package.

## Requirements
The current version of the XRI Examples is compatible with the following versions of the Unity Editor:

* 2020.3 and later

## General setup

The main example scene is located at `Assets/XRI_Examples/Scenes/XRI_Examples_Main`. This example scene is laid out as a ring with different stations along it. The first examples you will encounter are the simplest use-cases of XRI features. Behind each example is a doorway leading to advanced uses of each feature.

Use the simple examples when you need objects you can copy-and-paste, while leveraging the advanced examples when needing to achieve complex outcomes.

The **XR Origin** is located within the **Complete Set Up** prefab. This prefab contains everything needed for a fully functional user interaction with XRI. This includes the components needed for general input, interaction, and UI interaction.

Scripts, assets, and prefabs related to each feature or use case are located in the associated folder in `Assets/XRI_Examples`.

The following stations are available in the XRI Examples:

* [Station 1: Locomotion Setup](LocomotionSetup.md) - Overview of the built-in locomotion options and how to configure them.
* [Station 2: Grab Interactables](GrabInteractables.md) - Basic object manipulation.
* [Station 3: Activate Interactables](ActivateInteractables.md) - Manipulation of objects that can be triggered by the user.
* [Station 4: Socket Interactors](SocketInteractors.md) - Manipulation of objects that can snap to specific positions.
* [Station 5: Gaze Interaction](Gaze.md) - Object interaction with gaze.
* [Station 6: 2D UI](UI-2D.md) - Creation and interaction with [world space](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UICanvas.html#world-space) 2D UI.
* [Station 7: 3D UI](UI-3D.md) - Creation and interaction with 3D constrained controls.
* [Station 8: Physics Interactables](PhysicsInteractables.md) - Best practices for combining physics and XR input.

For a list of new features and deprecations, see [XRI Examples Changelog](../CHANGELOG.md).

For an overview of how the [Input System](https://docs.unity3d.com/Manual/com.unity.inputsystem.html) is used in this example, see [Input](Input.md).