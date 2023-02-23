# Socket Interactors

![Socket interactors used for a perler machine](Images/Station-04-SocketInteractables.jpg)

## Station descriptor

Socket Interactors will grab nearby valid interactables, snapping them into place using the socket transform.

The interactables below have two socket types, open socket (accepts any interactable) and specific socket (accepts only one kind of interactable).

## Basic example

The basic example demonstrates the required components to create a socket interaction: an **XR Socket Interactor** and a **Collider** with **Is Trigger** enabled. In addition, the **Attach Transform** is set, which indicates where an object will snap to. This is not required but will give much more consistent results.

## Advanced examples

![Socket interactors used for a perler machine](Images/Station-04-SocketInteractables-Advanced.jpg)

The advanced examples demonstrate the common use-case of allowing only specific objects to be grabbed by a socket. This is achieved with the **XR Closed Socket Interactor**, which is a derived version of the XR Socket Interactor. It contains references to keys that the socket will accept. These are scriptable objects available under **Create > XR > Key Lock System > Key**.

Interactables that will work with this socket contain a **Keychain** component, which also contains references to the key assets.

The battery socket demonstrates a single interactable and socket pairing, while the perler grid demonstrates multiple interactables working with multiple sockets.
