// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Destination
  {
    /// Pass it into RichPresenceOptions.SetApiName()() when calling
    /// RichPresence.Set() to set this user's rich presence
    public readonly string ApiName;
    /// The information that will be in LaunchDetails.GetDeeplinkMessage() when a
    /// user enters via a deeplink. Alternatively will be in
    /// User.GetPresenceDeeplinkMessage() if the rich presence is set for the user.
    public readonly string DeeplinkMessage;
    public readonly string DisplayName;


    public Destination(IntPtr o)
    {
      ApiName = CAPI.ovr_Destination_GetApiName(o);
      DeeplinkMessage = CAPI.ovr_Destination_GetDeeplinkMessage(o);
      DisplayName = CAPI.ovr_Destination_GetDisplayName(o);
    }
  }

  public class DestinationList : DeserializableList<Destination> {
    public DestinationList(IntPtr a) {
      var count = (int)CAPI.ovr_DestinationArray_GetSize(a);
      _Data = new List<Destination>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Destination(CAPI.ovr_DestinationArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_DestinationArray_GetNextUrl(a);
    }

  }
}
