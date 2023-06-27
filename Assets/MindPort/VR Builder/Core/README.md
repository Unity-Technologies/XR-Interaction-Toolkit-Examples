# VR Builder

## Introduction

VR Builder helps you create interactive VR applications better and faster. By setting up a Unity scene for VR Builder, you will pair it with a VR Builder *process*. Through the VR Builder process, you can define a sequence of actions the user can take in the scene and the resulting consequences.

You can easily edit a process without coding through VR Builder's Workflow Editor. The Workflow Editor is a node editor where the user can arrange and connect the *steps* of the process. Each step is a different node and can include any number of *behaviors*, which make things happen in the scene. Likewise, a step will have at least one *transition* leading to another step. Every transition can list several *conditions* which have to be completed for the transition to trigger. For example, step B can be reached only after the user has grabbed the object specified in step A.

Behaviors and conditions are the "building blocks" of VR Builder. Several of them are provided in the free version already. Additional behaviors and conditions are available in our paid add-ons. Since VR Builder is open source, you can always write your own behaviors and conditions as well.

Behaviors and conditions can interact only with *process scene objects*. These are game objects in the scene which have a `Process Scene Object` component on them.

The interaction capabilities of a process scene object can be increased by adding *scene object properties* to it. For example, adding a `Grabbable Property` component to a game object will let VR Builder know that the object is grabbable, and when it is grabbed.

Normally it is not necessary to add properties manually to an object. When an object is dragged in the inspector of a condition or behavior, the user has the option to automatically configure it with a single click.

Where possible, properties try to add and configure required components by themselves. If you add a `Grabbable Property` to a game object, this will automatically be made grabbable in VR (it still needs to have a collider and a mesh, of course).

This makes it very easy to start from some generic assets and build a fully interactive scene.

## Requirements

VR Builder is supported on Unity 2020.1 or later.

VR Builder works out of the box with any headset compatible with Unity's XR Interaction Toolkit.

## Installation

The GitHub repository should be cloned in a Unity project's Assets folder. The recommended subfolder path is `Assets/MindPort/VR Builder/Core`. UnityPackages and Asset Store package will automatically place the files in the aforementioned subfolder.

After importing, please refer to the [user manual](/Documentation/vr-builder-manual.md#installation) for details on the VR Builder import process.

## Documentation

You can find comprehensive documentation in the [Documentation](/Documentation/vr-builder-manual.md) folder.

## Acknowledgements

VR Builder is based on the open source edition of the [Innoactive Creator](https://www.innoactive.io/creator). While Innoactive helps enterprises to scale VR training, we adopted this tool to provide value for content creators looking to streamline their VR development processes. 

Like Innoactive, we believe in the value of open source and will continue to support this approach together with them and the open source community.

## Contact and Support

Join our official [Discord server](http://community.mindport.co) for quick support from the developer and fellow users. Suggest and vote on new ideas to influence the future of the VR Builder.

Make sure to review VR Builder on the [Unity Asset Store](https://assetstore.unity.com/packages/tools/visual-scripting/vr-builder-201913) if you like it. This will help us sustain the development of VR Builder.

If you have any issues, please contact [contact@mindport.co](mailto:contact@mindport.co). We'd love to get your feedback, both positive and constructive. By sharing your feedback you help us improve - thank you in advance!
Let's build something extraordinary!

You can also visit our website at [mindport.co](http://www.mindport.co).
