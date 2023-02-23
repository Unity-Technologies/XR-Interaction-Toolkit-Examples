# 3D UI

![3D UI interactables - Levers, joysticks, and wheels](Images/Station-06-3DUI.jpg)

## Station descriptor

3D UI components can be selected and manipulated using the collider or trigger on your controller.

The components shown here have a variety of state outputs: a toggle between 0 and 1, a float between 0 and 1, a float between 0 and 1 along two axes, etc. 

## Basic example

3D UI demonstrates custom interactables that move with specific constraints when grabbed, such as only having one part of the interactable face towards a controller, or move along a specific axis. We have created these as examples of common controls that we expect XR users will want to manipulate.

The common element between all the 3D UI components is the **Handle** element. This is the GameObject that will move in a constrained manner to follow the **interactor**. All 3D UI components are grabbed and manipulated with the **select** action, with the exception of the push button. All 3D UI elements also provide **value changed** events, matching as closely as possible their 2D equivalents.

All 3D UI elements can be used as-is, or reskinned to fit your specific application.

We provide implementations of the following types of 3D UI:
* **Lever** - A flip-switch. When grabbed, the handle will rotate along one axis to point as closely to the selecting interactor as possible.
* **Joystick** - When grabbed, the handle will rotate along two axes to point as closely to the selecting interactor as possible.
* **Knob** - This control will rotate based on the user's hand rotation when the interactor is near the center of the control. When the hand is further away, it will rotate to match the interactor's position. We provide two variations of this control by tweaking how **large** the control is - a dial and a wheel. Another way of thinking of this interaction is screwdriver style when close, and wrench style when further away. This is an extremely versatile control, able to simulate objects like doorknobs and even turntables.
* **Slider** - When grabbed, this handle will move along one axis to maintain as close a position as possible to the selecting interactor.
* **Grip Button** - A button that activates or toggles when hovered and selected.
* **Push Button** - A button that activates or toggles when an interactor moves through the control - pushing it past a threshold.

### Intention Filtering

Additionally, this station demonstrates the usage of the **Target Filter** feature of XRI. With target filters enabled, you will be able to see how other elements can assist in object selection, such as the direction you are looking, what object was last selected or the nearest distance to the collider vs the center of the object. To explore this feature, turn on the checkboxes one at a time in the 2D UI above the 3D elements. The best example of the power of this feature is to place your controller between the larger wheel in the middle and the turn-knob just to the left of it. Hold your hand steady and notice how the selection is modified by which control your head is looking at. This functionality can greatly improve usability and reduce user frustration.

## Advanced examples

![3D UI interactables used to control a claw game](Images/Station-06-3DUI-Advanced.jpg)

The advanced example of the 3D UI station utilizes a **Joystick** control and a **Push Button** to simulate a classic arcade claw game. Enjoy your plushy farm animal, they are grab interactable objects as well.
