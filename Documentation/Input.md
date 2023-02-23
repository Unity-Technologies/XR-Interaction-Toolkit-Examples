# Input

## Setup

The included **XR Origin** is set up for interaction via the Input System and use `input actions` and `action maps`. The actions are enabled with the **Input Action Manager**. **Input actions** are referenced in the locomotion components on the **XR Origin** and **XR Controller** components on the GameObject for each hand.

The input actions are references to the **XRI Default Input Actions** asset referenced by the **Input Action Manager**. These are the same actions that are available from the **Starter Assets** Sample package within the XRI package itself. They include all the actions needed for interaction and locomotion.

## Mediation

This sample project also includes an example of input mediation. A common challenge when developing XR software is that one or more input bindings are used across multiple actions. Commonly you will want specific context or certain actions to disable others when actively in use. An example is using the thumbstick to teleport in normal cases, but manipulate (translate and rotate) a selected object when using the ray interactor.

The included **InputMediator** script performs some examples of this sort of functionality. Actions are able to **consume** their associated controls, preventing lower priority actions from using the same bindings and causing unintended behavior. These actions are also able to automatically **release** their controls as soon as the input is released, allowing other, lower priority actions to fire again. The **ActionBasedControllerManager** makes use of the **InputMediator** to ensure that interaction and locomotion do not conflict.
