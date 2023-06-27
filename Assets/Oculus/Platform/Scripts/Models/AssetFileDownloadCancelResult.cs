// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AssetFileDownloadCancelResult
  {
    /// DEPRECATED. Use AssetFileDownloadCancelResult.GetAssetId().
    public readonly UInt64 AssetFileId;
    /// ID of the asset file
    public readonly UInt64 AssetId;
    /// File path of the asset file.
    public readonly string Filepath;
    /// Whether the cancel request is succeeded.
    public readonly bool Success;


    public AssetFileDownloadCancelResult(IntPtr o)
    {
      AssetFileId = CAPI.ovr_AssetFileDownloadCancelResult_GetAssetFileId(o);
      AssetId = CAPI.ovr_AssetFileDownloadCancelResult_GetAssetId(o);
      Filepath = CAPI.ovr_AssetFileDownloadCancelResult_GetFilepath(o);
      Success = CAPI.ovr_AssetFileDownloadCancelResult_GetSuccess(o);
    }
  }

}
