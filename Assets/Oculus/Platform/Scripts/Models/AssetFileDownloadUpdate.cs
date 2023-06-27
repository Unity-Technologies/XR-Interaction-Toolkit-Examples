// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AssetFileDownloadUpdate
  {
    /// DEPRECATED. Use AssetFileDownloadUpdate.GetAssetId().
    public readonly UInt64 AssetFileId;
    /// ID of the asset file
    public readonly UInt64 AssetId;
    /// Total number of bytes.
    public readonly ulong BytesTotal;
    /// Number of bytes have been downloaded. -1 If the download hasn't started
    /// yet.
    public readonly long BytesTransferred;
    /// Flag indicating a download is completed.
    public readonly bool Completed;


    public AssetFileDownloadUpdate(IntPtr o)
    {
      AssetFileId = CAPI.ovr_AssetFileDownloadUpdate_GetAssetFileId(o);
      AssetId = CAPI.ovr_AssetFileDownloadUpdate_GetAssetId(o);
      BytesTotal = CAPI.ovr_AssetFileDownloadUpdate_GetBytesTotalLong(o);
      BytesTransferred = CAPI.ovr_AssetFileDownloadUpdate_GetBytesTransferredLong(o);
      Completed = CAPI.ovr_AssetFileDownloadUpdate_GetCompleted(o);
    }
  }

}
