# Grab Interactables

![Grab interactables with basic objects](Images/Station-02-GrabInteractables.jpg)

## Station descriptor

Grab Interactables can be picked up using the grip button on your controller. Each controller will use ray selection at a distance and direct selection up close.

The interactables shown demonstrate the three different tracking types you can use when grabbed: instantaneous (pyramid), kinematic(ring) and velocity tracked(cube).

Notice the difference in tracking latency. We recommend using instantaneous by default and kinematic or velocity tracked depending on the needs of your use case.

## Basic examples

Each interactable example contains the required components for grabbing; a **Rigidbody**, **XR Grab Interactable**, and **Collider**.

In addition, each example object contains an example of how to use the Affordance System. Each Interactable contains a XR Interactable Affordance State Provider component, which sends state changes to the Affordance Receiver components on child GameObjects of the interactables (typically named Audio Feedback and Visual Feedback). There are receivers configured for both sound and color changes so that objects will automatically be highlighted when they are hovered along with providing haptic and audio feedback. Different variants are provided throughout the scene depending on the specific needs of the interaction.

The major difference between the example objects is the movement type selected.
* **Instantaneous** movement is for objects that do not need to interact with other physics objects.
* **Kinematic** similarly moves objects instantly and can interact better with physics objects. However, as these objects move instantly, they can exert unrealistic levels of force on physics objects.
* **VelocityTracked** objects react more naturally with other physics objects in the scene. However, the held objects will have a noticeable latency in their motion when the Fixed Timestep for physics occurs less frequently than the framerate of the application. See [Fixed Timestep (fixedDeltaTime)](https://docs.unity3d.com/Manual/TimeFrameManagement.html).

## Advanced examples

![Grab interactables being used for advanced interactions](Images/Station-02-GrabInteractables-Advanced.jpg)

The advanced examples show different uses for grabbed objects in XR. This includes:
* **Watering can**, with included script for detecting tilt (to pour water).

![Grab interactables being used for a watering can](Images/Station-02-grab-interactables-01.gif)
* **Hammer**, which does a simple trigger detection to break the destructible piggy bank.

![Grab interactables being used for a hammer](Images/Station-02-grab-interactables-02.gif)
* **Ribbon-on-a-string**, which demonstrates how cloth simulation can be leveraged to give grabbed objects more life.

![Grab interactables being used for a ribbon-on-a-string](Images/Station-02-grab-interactables-03.gif)
