// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LaunchFriendRequestFlowResult
  {
    /// Whether the viewer chose to cancel the friend request flow.
    public readonly bool DidCancel;
    /// Whether the viewer successfully sent the friend request.
    public readonly bool DidSendRequest;


    public LaunchFriendRequestFlowResult(IntPtr o)
    {
      DidCancel = CAPI.ovr_LaunchFriendRequestFlowResult_GetDidCancel(o);
      DidSendRequest = CAPI.ovr_LaunchFriendRequestFlowResult_GetDidSendRequest(o);
    }
  }

}
