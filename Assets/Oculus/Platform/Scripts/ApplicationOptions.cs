// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class ApplicationOptions {

    public ApplicationOptions() {
      Handle = CAPI.ovr_ApplicationOptions_Create();
    }

    /// A message to be passed to a launched app, which can be retrieved with
    /// LaunchDetails.GetDeeplinkMessage()
    public void SetDeeplinkMessage(string value) {
      CAPI.ovr_ApplicationOptions_SetDeeplinkMessage(Handle, value);
    }

    /// If provided, the intended destination to be passed to the launched app
    public void SetDestinationApiName(string value) {
      CAPI.ovr_ApplicationOptions_SetDestinationApiName(Handle, value);
    }

    /// If provided, the intended lobby where the launched app should take the
    /// user. All users with the same lobby_session_id should end up grouped
    /// together in the launched app.
    public void SetLobbySessionId(string value) {
      CAPI.ovr_ApplicationOptions_SetLobbySessionId(Handle, value);
    }

    /// If provided, the intended instance of the destination that a user should be
    /// launched into
    public void SetMatchSessionId(string value) {
      CAPI.ovr_ApplicationOptions_SetMatchSessionId(Handle, value);
    }

    /// [Deprecated]If provided, the intended room where the launched app should
    /// take the user (all users heading to the same place should have the same
    /// value). A room_id of 0 is INVALID.
    public void SetRoomId(UInt64 value) {
      CAPI.ovr_ApplicationOptions_SetRoomId(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(ApplicationOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~ApplicationOptions() {
      CAPI.ovr_ApplicationOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
