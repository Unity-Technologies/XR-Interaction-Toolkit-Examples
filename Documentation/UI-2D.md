# 2D UI

![World space 2D UI interactions with VR](Images/Station-06-2DUI.jpg)

## Station descriptor

2D UI components can be selected and manipulated using the ray and trigger button on your controller.

The components shown here have 2 states: default and selected.

## Overview

XRI provides the means of interacting with Unity's UGUI system, and this station demonstrates its use. UI interaction has the following requirements:
* An **XR UI Input Module** - there is one located in the **Complete Set Up** Prefab, attached to the **EventSystem** GameObject.
* The **Render Mode** of the Canvas must be set to **World Space**.
* The top-level UI Canvas must also include a **Tracked Device Graphic Raycaster** component.

The input used to activate UI is the UI Press Action, located in the **XRI Default Input Actions** on each hand.