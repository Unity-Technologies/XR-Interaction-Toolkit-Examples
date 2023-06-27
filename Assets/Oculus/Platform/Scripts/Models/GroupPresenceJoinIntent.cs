// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class GroupPresenceJoinIntent
  {
    /// An opaque string provided by the developer to help them deeplink to
    /// content.
    public readonly string DeeplinkMessage;
    /// If populated, the destination the current user wants to go to
    public readonly string DestinationApiName;
    /// If populated, the lobby session the current user wants to go to
    public readonly string LobbySessionId;
    /// If populated, the match session the current user wants to go to
    public readonly string MatchSessionId;


    public GroupPresenceJoinIntent(IntPtr o)
    {
      DeeplinkMessage = CAPI.ovr_GroupPresenceJoinIntent_GetDeeplinkMessage(o);
      DestinationApiName = CAPI.ovr_GroupPresenceJoinIntent_GetDestinationApiName(o);
      LobbySessionId = CAPI.ovr_GroupPresenceJoinIntent_GetLobbySessionId(o);
      MatchSessionId = CAPI.ovr_GroupPresenceJoinIntent_GetMatchSessionId(o);
    }
  }

}
