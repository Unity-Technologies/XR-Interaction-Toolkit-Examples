// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LaunchDetails
  {
    /// An opaque string provided by the developer to help them deeplink to content
    /// on app startup.
    public readonly string DeeplinkMessage;
    /// If provided, the intended destination the user would like to go to
    public readonly string DestinationApiName;
    /// A string typically used to distinguish where the deeplink came from. For
    /// instance, a DEEPLINK launch type could be coming from events or rich
    /// presence.
    public readonly string LaunchSource;
    public readonly LaunchType LaunchType;
    /// A unique identifer to keep track of a user going through the deeplinking
    /// flow
    public readonly string TrackingID;
    /// If provided, the intended users the user would like to be with
    // May be null. Check before using.
    public readonly UserList UsersOptional;
    [Obsolete("Deprecated in favor of UsersOptional")]
    public readonly UserList Users;


    public LaunchDetails(IntPtr o)
    {
      DeeplinkMessage = CAPI.ovr_LaunchDetails_GetDeeplinkMessage(o);
      DestinationApiName = CAPI.ovr_LaunchDetails_GetDestinationApiName(o);
      LaunchSource = CAPI.ovr_LaunchDetails_GetLaunchSource(o);
      LaunchType = CAPI.ovr_LaunchDetails_GetLaunchType(o);
      TrackingID = CAPI.ovr_LaunchDetails_GetTrackingID(o);
      {
        var pointer = CAPI.ovr_LaunchDetails_GetUsers(o);
        Users = new UserList(pointer);
        if (pointer == IntPtr.Zero) {
          UsersOptional = null;
        } else {
          UsersOptional = Users;
        }
      }
    }
  }

}
