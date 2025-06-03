# Meta Utilities Package

This package contains general utilities for Unity development. You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-Discover.git?path=Packages/com.meta.utilities
```

|Utility|Description|
|-|-|
|[AutoSet](./AutoSet.cs) attributes|<p>This attribute is useful for eliminating calls to `GetComponent`. By annotating a serialized field with `[AutoSet]`, every instance of that field in a Prefab or Scene will automatically be assigned in editor (by calling `GetComponent`). This assignment is done both in the inspector (using a [property drawer](./Editor/AutoSetDrawer.cs)) as well as every time the object is saved (using an [asset postprocessor](./Editor/AutoSetPostprocessor.cs)).</p>Note, you can also use `[AutoSetFromParent]` or `[AutoSetFromChildren]`.|
|[Singleton](./Singleton.cs)|Simple implementation of the singleton pattern for `MonoBehaviour`s. Access the global instance through the static `Instance` property. Utilize in your own class through inheritance, for example:<br />`public class MyBehaviour : Singleton<MyBehaviour>`|
|[Multiton](./Multiton.cs)|Similar to `Singleton`, this class gives global access to *all* enabled instances of a `MonoBehaviour` through its static `Instances` property. Utilize in your own class through inheritance, for example:<br />`public class MyBehaviour : Multiton<MyBehaviour>`|
|[EnumDictionary](./EnumDictionary.cs)|This is an optimized Dictionary class for use with enum keys. It works by allocating an array that is indexed by the enum key. It can be used as a serialized field, unlike `System.Dictionary`.|
|[Extension Methods](./ExtensionMethods.cs)|A library of useful extension methods for Unity classes.|
|[Netcode Hash Fixer](./Editor/NetcodeHashFixer.cs)|The `NetworkObject` component uses a unique id (`GlobalObjectIdHash`) to identify objects across the network. However, certain instances (for example, instances of prefab variants) do not generate these IDs properly. This asset postprocessor ensures that the IDs are always regenerated, which prevents issues networking between the Editor and builds.|
|[Network Settings Toolbar](./Editor/NetworkSettingsToolbar.cs)|<img src="./Documentation~/NetworkSettingsToolbar.png" width="512" /><br />This toolbar allows for improved iteration speed while working with [ParrelSync](https://github.com/brogan89/ParrelSync) clones. By consuming the properties set in the [NetworkSettings](./NetworkSettings.cs) class, multiple Editor instances of the project can automatically join the same instance.|
|[Settings Warning Toolbar](./Editor/SettingsWarningsToolbar.cs)|<img src="./Documentation~/SettingsWarningsToolbar.png" width="512" /><br />This toolbar gives a helpful warning when the build platform is not set to Android, and gives an option to switch it. This is useful for ensuring that the build platform is Android while doing Quest development.|
|[Build Tools](./Editor/BuildTools.cs)|The `BuildTools` class contains methods for use by Continuous Integration systems.|
|[Menu Helpers](./Editor/MenuHelpers.cs)|This class adds many useful menu items to the `Tools` menu in the Unity Editor. When the [Unity Search Extensions package](https://github.com/Unity-Technologies/com.unity.search.extensions) is enabled, this adds a helpful asset context menu item "Graph Dependencies" and adds the "Tools/Find MIssing Dependencies" menu item.|
|[Android Helpers](./AndroidHelpers.cs)|This class gives access to Android [Intent](https://developer.android.com/reference/android/content/Intent) extras.|
|Animation State [Triggers](./AnimationStateTriggers.cs) / [Listeners](./AnimationStateTriggerListener.cs)|These classes enable any `Object` to bind methods to respond to its `Animator`'s `OnStateEnter` and `OnStateExit` events.|
|[Camera Facing](./CameraFacing.cs)|Simple component for billboarding a renderer.|
|[CameraFollowing](./CameraFollowing.cs)|Objects with this component attached to them will follow the position and rotation of the main camera. It can be configured with an offset.|
|[Dont Destroy On Load (On Enable)](./DontDestroyOnLoadOnEnable.cs)|Simple component that calls `DontDestroyOnLoad` in its `OnEnable`.|
|[Set Material Properties (On Enable)](./SetMaterialPropertiesOnEnable.cs)|Simple component that sets up a `MaterialPropertyBlock` for a renderer in its `OnEnable`.|
|[Nullable Float](./NullableFloat.cs)|A serializeable wrapper around `float` that exposes a `float?` through its `Value` property. It uses `NaN` as a sentinel for `null`.|
|[ResetTransform](./ResetTransform.cs)|This component stores the state of an object's transform on awake and provides a public method for returning it to the stored position and rotation at any time.|
|[HoverAbove](./HoverAbove.cs)|A simple script that allows an object to hover over another object. Useful to have a child follow a specific position of the parent ignoring relative rotation.|
