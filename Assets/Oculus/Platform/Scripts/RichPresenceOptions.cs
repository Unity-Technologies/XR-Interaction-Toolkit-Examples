// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RichPresenceOptions {

    public RichPresenceOptions() {
      Handle = CAPI.ovr_RichPresenceOptions_Create();
    }


    /// For passing to native C
    public static explicit operator IntPtr(RichPresenceOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~RichPresenceOptions() {
      CAPI.ovr_RichPresenceOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
