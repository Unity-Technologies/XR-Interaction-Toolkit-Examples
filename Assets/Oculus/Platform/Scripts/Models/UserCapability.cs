// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class UserCapability
  {
    /// Human readable description of the capability describing what possessing it
    /// entails for a given user
    public readonly string Description;
    /// Whether the capability is currently enabled for the user
    public readonly bool IsEnabled;
    /// Unique identifer for the capability
    public readonly string Name;
    /// If present, specifies the reason the capability was enabled or disabled
    public readonly string ReasonCode;


    public UserCapability(IntPtr o)
    {
      Description = CAPI.ovr_UserCapability_GetDescription(o);
      IsEnabled = CAPI.ovr_UserCapability_GetIsEnabled(o);
      Name = CAPI.ovr_UserCapability_GetName(o);
      ReasonCode = CAPI.ovr_UserCapability_GetReasonCode(o);
    }
  }

  public class UserCapabilityList : DeserializableList<UserCapability> {
    public UserCapabilityList(IntPtr a) {
      var count = (int)CAPI.ovr_UserCapabilityArray_GetSize(a);
      _Data = new List<UserCapability>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new UserCapability(CAPI.ovr_UserCapabilityArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_UserCapabilityArray_GetNextUrl(a);
    }

  }
}
