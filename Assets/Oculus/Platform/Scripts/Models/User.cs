// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class User
  {
    /// A potentially non unique displayable name chosen by the user. Could also be
    /// the same as the oculus_ID
    public readonly string DisplayName;
    public readonly UInt64 ID;
    public readonly string ImageURL;
    public readonly string OculusID;
    /// Human readable string of what the user is currently doing. Not intended to
    /// be parsed as it might change at anytime or be translated
    public readonly string Presence;
    /// Intended to be parsed and used to deeplink to parts of the app
    public readonly string PresenceDeeplinkMessage;
    /// If provided, the destination this user is currently at in the app
    public readonly string PresenceDestinationApiName;
    /// If provided, the lobby session this user is currently at in the app
    public readonly string PresenceLobbySessionId;
    /// If provided, the match session this user is currently at in the app
    public readonly string PresenceMatchSessionId;
    /// Enum value of what the user is currently doing.
    public readonly UserPresenceStatus PresenceStatus;
    public readonly string SmallImageUrl;


    public User(IntPtr o)
    {
      DisplayName = CAPI.ovr_User_GetDisplayName(o);
      ID = CAPI.ovr_User_GetID(o);
      ImageURL = CAPI.ovr_User_GetImageUrl(o);
      OculusID = CAPI.ovr_User_GetOculusID(o);
      Presence = CAPI.ovr_User_GetPresence(o);
      PresenceDeeplinkMessage = CAPI.ovr_User_GetPresenceDeeplinkMessage(o);
      PresenceDestinationApiName = CAPI.ovr_User_GetPresenceDestinationApiName(o);
      PresenceLobbySessionId = CAPI.ovr_User_GetPresenceLobbySessionId(o);
      PresenceMatchSessionId = CAPI.ovr_User_GetPresenceMatchSessionId(o);
      PresenceStatus = CAPI.ovr_User_GetPresenceStatus(o);
      SmallImageUrl = CAPI.ovr_User_GetSmallImageUrl(o);
    }
  }

  public class UserList : DeserializableList<User> {
    public UserList(IntPtr a) {
      var count = (int)CAPI.ovr_UserArray_GetSize(a);
      _Data = new List<User>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new User(CAPI.ovr_UserArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_UserArray_GetNextUrl(a);
    }

  }
}
