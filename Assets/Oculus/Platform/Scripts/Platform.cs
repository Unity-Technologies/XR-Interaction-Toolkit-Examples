// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;

  public sealed class Core {
    private static bool IsPlatformInitialized = false;
    public static bool IsInitialized()
    {
      return IsPlatformInitialized;
    }

    // If LogMessages is true, then the contents of each request response
    // will be printed using Debug.Log. This allocates a lot of heap memory,
    // and so should not be called outside of testing and debugging.
    public static bool LogMessages = false;

    public static string PlatformUninitializedError = "This function requires an initialized Oculus Platform. Run Oculus.Platform.Core.[Initialize|AsyncInitialize] and try again.";

    internal static void ForceInitialized()
    {
      IsPlatformInitialized = true;
    }

    private static string getAppID(string appId = null) {
      string configAppID = GetAppIDFromConfig();
      if (String.IsNullOrEmpty(appId))
      {
        if (String.IsNullOrEmpty(configAppID))
        {
          throw new UnityException("Update your app id by selecting 'Oculus Platform' -> 'Edit Settings'");
        }
        appId = configAppID;
      }
      else
      {
        if (!String.IsNullOrEmpty(configAppID))
        {
          Debug.LogWarningFormat("The 'Oculus App Id ({0})' field in 'Oculus Platform/Edit Settings' is being overridden by the App Id ({1}) that you passed in to Platform.Core.Initialize.  You should only specify this in one place.  We recommend the menu location.", configAppID, appId);
        }
      }
      return appId;
    }

    // Asynchronously Initialize Platform SDK. The result will be put on the message
    // queue with the message type: ovrMessage_PlatformInitializeAndroidAsynchronous
    //
    // While the platform is in an initializing state, it's not fully functional.
    // [Requests]: will queue up and run once platform is initialized.
    //    For example: ovr_User_GetLoggedInUser() can be called immediately after
    //    asynchronous init and once platform is initialized, this request will run
    // [Synchronous Methods]: will return the default value;
    //    For example: ovr_GetLoggedInUserID() will return 0 until platform is
    //    fully initialized
    public static Request<Models.PlatformInitialize> AsyncInitialize(string appId = null) {
      appId = getAppID(appId);

      Request<Models.PlatformInitialize> request;
      if (UnityEngine.Application.isEditor && PlatformSettings.UseStandalonePlatform) {
        var platform = new StandalonePlatform();
        request = platform.InitializeInEditor();
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor ||
               UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer) {
        var platform = new WindowsPlatform();
        request = platform.AsyncInitialize(appId);
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.Android) {
        var platform = new AndroidPlatform();
        request = platform.AsyncInitialize(appId);
      }
      else {
        throw new NotImplementedException("Oculus platform is not implemented on this platform yet.");
      }

      IsPlatformInitialized = (request != null);

      if (!IsPlatformInitialized)
      {
        throw new UnityException("Oculus Platform failed to initialize.");
      }

      if (LogMessages) {
        Debug.LogWarning("Oculus.Platform.Core.LogMessages is set to true. This will cause extra heap allocations, and should not be used outside of testing and debugging.");
      }

      // Create the GameObject that will run the callbacks
      (new GameObject("Oculus.Platform.CallbackRunner")).AddComponent<CallbackRunner>();
      return request;
    }

    /// (BETA) For use on platforms where the Oculus service isn't running with additional
    /// config options to pass in.
    ///
    /// eg:
    ///
    ///  var config = new Dictionary<InitConfigOptions, bool>{
    ///    [InitConfigOptions.DisableP2pNetworking] = true
    ///  };
    /// Platform.Core.AsyncInitialize("{access_token}", config);
    public static Request<Models.PlatformInitialize> AsyncInitialize(string accessToken, Dictionary<InitConfigOptions, bool> initConfigOptions, string appId = null) {
      appId = getAppID(appId);

      Request<Models.PlatformInitialize> request;
      if (UnityEngine.Application.isEditor ||
        UnityEngine.Application.platform == RuntimePlatform.WindowsEditor ||
        UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer) {

        var platform = new StandalonePlatform();
        request = platform.AsyncInitializeWithAccessTokenAndOptions(appId, accessToken, initConfigOptions);
      }
      else {
        throw new NotImplementedException("Initializing with access token is not implemented on this platform yet.");
      }

      IsPlatformInitialized = (request != null);

      if (!IsPlatformInitialized)
      {
        throw new UnityException("Oculus Standalone Platform failed to initialize. Check if the access token or app id is correct.");
      }

      if (LogMessages) {
        Debug.LogWarning("Oculus.Platform.Core.LogMessages is set to true. This will cause extra heap allocations, and should not be used outside of testing and debugging.");
      }

      // Create the GameObject that will run the callbacks
      (new GameObject("Oculus.Platform.CallbackRunner")).AddComponent<CallbackRunner>();
      return request;
    }

    public static void Initialize(string appId = null)
    {
      appId = getAppID(appId);

      if (UnityEngine.Application.isEditor && PlatformSettings.UseStandalonePlatform) {
        var platform = new StandalonePlatform();
        IsPlatformInitialized = platform.InitializeInEditor() != null;
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor ||
               UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer) {
        var platform = new WindowsPlatform();
        IsPlatformInitialized = platform.Initialize(appId);
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.Android) {
        var platform = new AndroidPlatform();
        IsPlatformInitialized = platform.Initialize(appId);
      }
      else {
        throw new NotImplementedException("Oculus platform is not implemented on this platform yet.");
      }

      if (!IsPlatformInitialized)
      {
        throw new UnityException("Oculus Platform failed to initialize.");
      }

      if (LogMessages) {
        Debug.LogWarning("Oculus.Platform.Core.LogMessages is set to true. This will cause extra heap allocations, and should not be used outside of testing and debugging.");
      }

      // Create the GameObject that will run the callbacks
      (new GameObject("Oculus.Platform.CallbackRunner")).AddComponent<CallbackRunner>();
    }

    private static string GetAppIDFromConfig()
    {
      if (UnityEngine.Application.platform == RuntimePlatform.Android)
      {
        return PlatformSettings.MobileAppID;
      }
      else
      {
        return PlatformSettings.AppID;
      }
    }
  }

  public static partial class ApplicationLifecycle
  {
    public static Models.LaunchDetails GetLaunchDetails() {
      return new Models.LaunchDetails(CAPI.ovr_ApplicationLifecycle_GetLaunchDetails());
    }
    public static void LogDeeplinkResult(string trackingID, LaunchResult result) {
      CAPI.ovr_ApplicationLifecycle_LogDeeplinkResult(trackingID, result);
    }
  }

  public static partial class Leaderboards
  {
    public static Request<Models.LeaderboardEntryList> GetNextEntries(Models.LeaderboardEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.NextUrl, (int)Message.MessageType.Leaderboard_GetNextEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.LeaderboardEntryList> GetPreviousEntries(Models.LeaderboardEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.PreviousUrl, (int)Message.MessageType.Leaderboard_GetPreviousEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }
  }

  public static partial class Challenges
  {
    public static Request<Models.ChallengeEntryList> GetNextEntries(Models.ChallengeEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.NextUrl, (int)Message.MessageType.Challenges_GetNextEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.ChallengeEntryList> GetPreviousEntries(Models.ChallengeEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.PreviousUrl, (int)Message.MessageType.Challenges_GetPreviousEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.ChallengeList> GetNextChallenges(Models.ChallengeList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeList>(CAPI.ovr_HTTP_GetWithMessageType(list.NextUrl, (int)Message.MessageType.Challenges_GetNextChallenges));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.ChallengeList> GetPreviousChallenges(Models.ChallengeList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeList>(CAPI.ovr_HTTP_GetWithMessageType(list.PreviousUrl, (int)Message.MessageType.Challenges_GetPreviousChallenges));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }
  }

  public static partial class Voip
  {
    public static void Start(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_Start(userID);
      }
    }

    public static void Accept(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_Accept(userID);
      }
    }

    public static void Stop(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_Stop(userID);
      }
    }

    public static void SetMicrophoneFilterCallback(CAPI.FilterCallback callback)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_SetMicrophoneFilterCallbackWithFixedSizeBuffer(callback, (UIntPtr)CAPI.VoipFilterBufferSize);
      }
    }

    public static void SetMicrophoneMuted(VoipMuteState state)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_SetMicrophoneMuted(state);
      }
    }

    public static VoipMuteState GetSystemVoipMicrophoneMuted()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetSystemVoipMicrophoneMuted();
      }
      return VoipMuteState.Unknown;
    }

    public static SystemVoipStatus GetSystemVoipStatus()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetSystemVoipStatus();
      }
      return SystemVoipStatus.Unknown;
    }

    public static Oculus.Platform.VoipDtxState GetIsConnectionUsingDtx(UInt64 peerID)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetIsConnectionUsingDtx(peerID);
      }
      return Oculus.Platform.VoipDtxState.Unknown;
    }

    public static Oculus.Platform.VoipBitrate GetLocalBitrate(UInt64 peerID)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetLocalBitrate(peerID);
      }
      return Oculus.Platform.VoipBitrate.Unknown;
    }

    public static Oculus.Platform.VoipBitrate GetRemoteBitrate(UInt64 peerID)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetRemoteBitrate(peerID);
      }
      return Oculus.Platform.VoipBitrate.Unknown;
    }

    public static void SetNewConnectionOptions(VoipOptions voipOptions)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_SetNewConnectionOptions((IntPtr)voipOptions);
      }
    }
  }

  public static partial class Users
  {
    public static string GetLoggedInUserLocale()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_GetLoggedInUserLocale();
      }
      return "";
    }
  }

  public static partial class AbuseReport
  {
    /// The currently running application has indicated they want to show their in-
    /// app reporting flow or that they choose to ignore the request.
    ///
    public static Request ReportRequestHandled(ReportRequestResponse response)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_AbuseReport_ReportRequestHandled(response));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// The user has tapped the report button in the panel that appears after
    /// pressing the Oculus button.
    ///
    public static void SetReportButtonPressedNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_AbuseReport_ReportButtonPressed,
        callback
      );
    }

  }

  public static partial class Achievements
  {
    /// Add 'count' to the achievement with the given name. This must be a COUNT
    /// achievement. The largest number that is supported by this method is the max
    /// value of a signed 64-bit integer. If the number is larger than that, it is
    /// clamped to that max value before being passed to the servers.
    ///
    public static Request<Models.AchievementUpdate> AddCount(string name, ulong count)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementUpdate>(CAPI.ovr_Achievements_AddCount(name, count));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Unlock fields of a BITFIELD achievement.
    /// \param name The name of the achievement to unlock
    /// \param fields A string containing either '0' or '1' characters. Every '1' will unlock the field in the corresponding position.
    ///
    public static Request<Models.AchievementUpdate> AddFields(string name, string fields)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementUpdate>(CAPI.ovr_Achievements_AddFields(name, fields));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request all achievement definitions for the app.
    ///
    public static Request<Models.AchievementDefinitionList> GetAllDefinitions()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementDefinitionList>(CAPI.ovr_Achievements_GetAllDefinitions());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request the progress for the user on all achievements in the app.
    ///
    public static Request<Models.AchievementProgressList> GetAllProgress()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementProgressList>(CAPI.ovr_Achievements_GetAllProgress());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request the achievement definitions that match the specified names.
    ///
    public static Request<Models.AchievementDefinitionList> GetDefinitionsByName(string[] names)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementDefinitionList>(CAPI.ovr_Achievements_GetDefinitionsByName(names, (names != null ? names.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request the user's progress on the specified achievements.
    ///
    public static Request<Models.AchievementProgressList> GetProgressByName(string[] names)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementProgressList>(CAPI.ovr_Achievements_GetProgressByName(names, (names != null ? names.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Unlock the achievement with the given name. This can be of any achievement
    /// type.
    ///
    public static Request<Models.AchievementUpdate> Unlock(string name)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementUpdate>(CAPI.ovr_Achievements_Unlock(name));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Application
  {
    /// Requests version information, including the currently installed and latest
    /// available version name and version code.
    ///
    public static Request<Models.ApplicationVersion> GetVersion()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ApplicationVersion>(CAPI.ovr_Application_GetVersion());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launches a different application in the user's library. If the user does
    /// not have that application installed, they will be taken to that app's page
    /// in the Oculus Store
    /// \param appID The ID of the app to launch
    /// \param deeplink_options Additional configuration for this requests. Optional.
    ///
    public static Request<string> LaunchOtherApp(UInt64 appID, ApplicationOptions deeplink_options = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<string>(CAPI.ovr_Application_LaunchOtherApp(appID, (IntPtr)deeplink_options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class ApplicationLifecycle
  {
    /// Sent when a launch intent is received (for both cold and warm starts). The
    /// payload is the type of the intent. ApplicationLifecycle.GetLaunchDetails()
    /// should be called to get the other details.
    ///
    public static void SetLaunchIntentChangedNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_ApplicationLifecycle_LaunchIntentChanged,
        callback
      );
    }

  }

  public static partial class AssetFile
  {
    /// DEPRECATED. Use AssetFile.DeleteById()
    ///
    public static Request<Models.AssetFileDeleteResult> Delete(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDeleteResult>(CAPI.ovr_AssetFile_Delete(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Removes an previously installed asset file from the device by its ID.
    /// Returns an object containing the asset ID and file name, and a success
    /// flag.
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetFileDeleteResult> DeleteById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDeleteResult>(CAPI.ovr_AssetFile_DeleteById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Removes an previously installed asset file from the device by its name.
    /// Returns an object containing the asset ID and file name, and a success
    /// flag.
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetFileDeleteResult> DeleteByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDeleteResult>(CAPI.ovr_AssetFile_DeleteByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use AssetFile.DownloadById()
    ///
    public static Request<Models.AssetFileDownloadResult> Download(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_AssetFile_Download(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Downloads an asset file by its ID on demand. Returns an object containing
    /// the asset ID and filepath. Sends periodic
    /// MessageType.Notification_AssetFile_DownloadUpdate to track the downloads.
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetFileDownloadResult> DownloadById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_AssetFile_DownloadById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Downloads an asset file by its name on demand. Returns an object containing
    /// the asset ID and filepath. Sends periodic
    /// {notifications.asset_file.download_update}} to track the downloads.
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetFileDownloadResult> DownloadByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_AssetFile_DownloadByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use AssetFile.DownloadCancelById()
    ///
    public static Request<Models.AssetFileDownloadCancelResult> DownloadCancel(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadCancelResult>(CAPI.ovr_AssetFile_DownloadCancel(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Cancels a previously spawned download request for an asset file by its ID.
    /// Returns an object containing the asset ID and file path, and a success
    /// flag.
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetFileDownloadCancelResult> DownloadCancelById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadCancelResult>(CAPI.ovr_AssetFile_DownloadCancelById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Cancels a previously spawned download request for an asset file by its
    /// name. Returns an object containing the asset ID and file path, and a
    /// success flag.
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetFileDownloadCancelResult> DownloadCancelByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadCancelResult>(CAPI.ovr_AssetFile_DownloadCancelByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns an array of objects with asset file names and their associated IDs,
    /// and and whether it's currently installed.
    ///
    public static Request<Models.AssetDetailsList> GetList()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetailsList>(CAPI.ovr_AssetFile_GetList());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use AssetFile.StatusById()
    ///
    public static Request<Models.AssetDetails> Status(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_AssetFile_Status(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns the details on a single asset: ID, file name, and whether it's
    /// currently installed
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetDetails> StatusById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_AssetFile_StatusById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns the details on a single asset: ID, file name, and whether it's
    /// currently installed
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetDetails> StatusByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_AssetFile_StatusByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sent to indicate download progress for asset files.
    ///
    public static void SetDownloadUpdateNotificationCallback(Message<Models.AssetFileDownloadUpdate>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_AssetFile_DownloadUpdate,
        callback
      );
    }

  }

  public static partial class Avatar
  {
    /// Launches the Avatar Editor
    ///
    public static Request<Models.AvatarEditorResult> LaunchAvatarEditor(AvatarEditorOptions options = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AvatarEditorResult>(CAPI.ovr_Avatar_LaunchAvatarEditor((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Challenges
  {
    /// DEPRECATED. Use server-to-server API call instead.
    ///
    public static Request<Models.Challenge> Create(string leaderboardName, ChallengeOptions challengeOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Create(leaderboardName, (IntPtr)challengeOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// If the current user has an invite to the challenge, decline the invite
    ///
    public static Request<Models.Challenge> DeclineInvite(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_DeclineInvite(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use server-to-server API call instead.
    ///
    public static Request Delete(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Challenges_Delete(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Gets the information for a single challenge
    /// \param challengeID The id of the challenge whose entries to return.
    ///
    public static Request<Models.Challenge> Get(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Get(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of challenge entries.
    /// \param challengeID The id of the challenge whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param filter By using ovrLeaderboard_FilterFriends, this allows you to filter the returned values to bidirectional followers.
    /// \param startAt Defines whether to center the query on the user or start at the top of the challenge.
    ///
    public static Request<Models.ChallengeEntryList> GetEntries(UInt64 challengeID, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_Challenges_GetEntries(challengeID, limit, filter, startAt));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of challenge entries.
    /// \param challengeID The id of the challenge whose entries to return.
    /// \param limit The maximum number of entries to return.
    /// \param afterRank The position after which to start.  For example, 10 returns challenge results starting with the 11th user.
    ///
    public static Request<Models.ChallengeEntryList> GetEntriesAfterRank(UInt64 challengeID, int limit, ulong afterRank)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_Challenges_GetEntriesAfterRank(challengeID, limit, afterRank));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of challenge entries. Will return only entries matching
    /// the user IDs passed in.
    /// \param challengeID The id of the challenge whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param startAt Defines whether to center the query on the user or start at the top of the challenge. If this is LeaderboardStartAt.CenteredOnViewer or LeaderboardStartAt.CenteredOnViewerOrTop, then the current user's ID will be automatically added to the query.
    /// \param userIDs Defines a list of user ids to get entries for.
    ///
    public static Request<Models.ChallengeEntryList> GetEntriesByIds(UInt64 challengeID, int limit, LeaderboardStartAt startAt, UInt64[] userIDs)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_Challenges_GetEntriesByIds(challengeID, limit, startAt, userIDs, (uint)(userIDs != null ? userIDs.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests for a list of challenge
    ///
    public static Request<Models.ChallengeList> GetList(ChallengeOptions challengeOptions, int limit)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeList>(CAPI.ovr_Challenges_GetList((IntPtr)challengeOptions, limit));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// If the current user has permission, join the challenge
    ///
    public static Request<Models.Challenge> Join(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Join(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// If the current user has permission, leave the challenge
    ///
    public static Request<Models.Challenge> Leave(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Leave(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use server-to-server API call instead.
    ///
    public static Request<Models.Challenge> UpdateInfo(UInt64 challengeID, ChallengeOptions challengeOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_UpdateInfo(challengeID, (IntPtr)challengeOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Entitlements
  {
    /// Returns whether the current user is entitled to the current app.
    ///
    public static Request IsUserEntitledToApplication()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Entitlement_GetIsViewerEntitled());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class GroupPresence
  {
    /// Clear group presence for running app
    ///
    public static Request Clear()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_Clear());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns a list of users that can be invited to your current lobby. These
    /// are pulled from your bidirectional followers and recently met lists.
    ///
    public static Request<Models.UserList> GetInvitableUsers(InviteOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(CAPI.ovr_GroupPresence_GetInvitableUsers((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get the application invites which have been sent by the user.
    ///
    public static Request<Models.ApplicationInviteList> GetSentInvites()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ApplicationInviteList>(CAPI.ovr_GroupPresence_GetSentInvites());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow to allow the user to invite others to their current
    /// session. This can only be used if the user is in a joinable session.
    ///
    public static Request<Models.InvitePanelResultInfo> LaunchInvitePanel(InviteOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.InvitePanelResultInfo>(CAPI.ovr_GroupPresence_LaunchInvitePanel((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch an error dialog with predefined messages for common multiplayer
    /// errors.
    ///
    public static Request LaunchMultiplayerErrorDialog(MultiplayerErrorOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_LaunchMultiplayerErrorDialog((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the dialog which will allow the user to rejoin a previous
    /// lobby/match. Either the lobby_session_id or the match_session_id, or both,
    /// must be populated.
    ///
    public static Request<Models.RejoinDialogResult> LaunchRejoinDialog(string lobby_session_id, string match_session_id, string destination_api_name)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.RejoinDialogResult>(CAPI.ovr_GroupPresence_LaunchRejoinDialog(lobby_session_id, match_session_id, destination_api_name));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the panel which displays the current users in the roster. Users with
    /// the same lobby and match session id as part of their presence will show up
    /// here.
    ///
    public static Request LaunchRosterPanel(RosterOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_LaunchRosterPanel((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Send application invites to the passed in userIDs.
    ///
    public static Request<Models.SendInvitesResult> SendInvites(UInt64[] userIDs)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.SendInvitesResult>(CAPI.ovr_GroupPresence_SendInvites(userIDs, (uint)(userIDs != null ? userIDs.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Set group presence for running app
    ///
    public static Request Set(GroupPresenceOptions groupPresenceOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_Set((IntPtr)groupPresenceOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Set the user's deeplink message while keeping the other group presence
    /// parameters the same. If the destination of the user is not set, the
    /// deeplink message cannot be set as there's no deeplink message to override.
    ///
    public static Request SetDeeplinkMessageOverride(string deeplink_message)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetDeeplinkMessageOverride(deeplink_message));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Replaces the user's current destination for the provided one. All other
    /// existing group presence parameters will remain the same.
    ///
    public static Request SetDestination(string api_name)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetDestination(api_name));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Set if the current user's destination and session is joinable while keeping
    /// the other group presence parameters the same. If the destination or session
    /// ids of the user is not set, they cannot be set to joinable.
    ///
    public static Request SetIsJoinable(bool is_joinable)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetIsJoinable(is_joinable));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Replaces the user's current lobby session id for the provided one. All
    /// other existing group presence parameters will remain the same.
    ///
    public static Request SetLobbySession(string id)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetLobbySession(id));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Replaces the user's current match session id for the provided one. All
    /// other existing group presence parameters will remain the same.
    ///
    public static Request SetMatchSession(string id)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetMatchSession(id));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sent when the user is finished using the invite panel to send out
    /// invitations. Contains a list of invitees.
    ///
    public static void SetInvitationsSentNotificationCallback(Message<Models.LaunchInvitePanelFlowResult>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_GroupPresence_InvitationsSent,
        callback
      );
    }

    /// Sent when a user has chosen to join the destination/lobby/match. Read all
    /// the fields to figure out where the user wants to go and take the
    /// appropriate actions to bring them there. If the user is unable to go there,
    /// provide adequate messaging to the user on why they cannot go there. These
    /// notifications should be responded to immediately.
    ///
    public static void SetJoinIntentReceivedNotificationCallback(Message<Models.GroupPresenceJoinIntent>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_GroupPresence_JoinIntentReceived,
        callback
      );
    }

    /// Sent when the user has chosen to leave the destination/lobby/match from the
    /// Oculus menu. Read the specific fields to check the user is currently from
    /// the destination/lobby/match and take the appropriate actions to remove
    /// them. Update the user's presence clearing the appropriate fields to
    /// indicate the user has left.
    ///
    public static void SetLeaveIntentReceivedNotificationCallback(Message<Models.GroupPresenceLeaveIntent>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_GroupPresence_LeaveIntentReceived,
        callback
      );
    }

  }

  public static partial class IAP
  {
    /// Allow the consumable IAP product to be purchased again. Conceptually, this
    /// indicates that the item was used or consumed.
    ///
    public static Request ConsumePurchase(string sku)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_IAP_ConsumePurchase(sku));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of IAP products that can be purchased.
    /// \param skus The SKUs of the products to retrieve.
    ///
    public static Request<Models.ProductList> GetProductsBySKU(string[] skus)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ProductList>(CAPI.ovr_IAP_GetProductsBySKU(skus, (skus != null ? skus.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of Purchase that the Logged-In-User has made. This list
    /// will also contain consumable purchases that have not been consumed.
    ///
    public static Request<Models.PurchaseList> GetViewerPurchases()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.PurchaseList>(CAPI.ovr_IAP_GetViewerPurchases());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of Purchase that the Logged-In-User has made. This list
    /// will only contain durable purchase (non-consumable) and is populated from a
    /// device cache. It is recommended in all cases to use
    /// ovr_User_GetViewerPurchases first and only check the cache if that fails.
    ///
    public static Request<Models.PurchaseList> GetViewerPurchasesDurableCache()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.PurchaseList>(CAPI.ovr_IAP_GetViewerPurchasesDurableCache());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the checkout flow to purchase the existing product. Oculus Home
    /// tries handle and fix as many errors as possible. Home returns the
    /// appropriate error message and how to resolveit, if possible. Returns a
    /// purchase on success, empty purchase on cancel, and an error on error.
    /// \param sku IAP sku for the item the user wishes to purchase.
    ///
    public static Request<Models.Purchase> LaunchCheckoutFlow(string sku)
    {
      if (Core.IsInitialized())
      {
        if (UnityEngine.Application.isEditor) {
          throw new NotImplementedException("LaunchCheckoutFlow() is not implemented in the editor yet.");
        }

        return new Request<Models.Purchase>(CAPI.ovr_IAP_LaunchCheckoutFlow(sku));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class LanguagePack
  {
    /// Returns currently installed and selected language pack for an app in the
    /// view of the `asset_details`. Use `language` field to extract neeeded
    /// language info. A particular language can be download and installed by a
    /// user from the Oculus app on the application page.
    ///
    public static Request<Models.AssetDetails> GetCurrent()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_LanguagePack_GetCurrent());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sets the current language to specified. The parameter is the BCP47 language
    /// tag. If a language pack is not downloaded yet, spawns automatically the
    /// AssetFile.DownloadByName() request, and sends periodic
    /// MessageType.Notification_AssetFile_DownloadUpdate to track the downloads.
    /// Once the language asset file is downloaded, call LanguagePack.GetCurrent()
    /// to retrive the data, and use the language at runtime.
    /// \param tag BCP47 language tag
    ///
    public static Request<Models.AssetFileDownloadResult> SetCurrent(string tag)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_LanguagePack_SetCurrent(tag));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Leaderboards
  {
    /// Gets the information for a single leaderboard
    /// \param leaderboardName The name of the leaderboard to return.
    ///
    public static Request<Models.LeaderboardList> Get(string leaderboardName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardList>(CAPI.ovr_Leaderboard_Get(leaderboardName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of leaderboard entries.
    /// \param leaderboardName The name of the leaderboard whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param filter By using ovrLeaderboard_FilterFriends, this allows you to filter the returned values to bidirectional followers.
    /// \param startAt Defines whether to center the query on the user or start at the top of the leaderboard.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    /// - \b 100: Something went wrong.
    /// - \b 12074: You're not yet ranked on this leaderboard.
    ///
    public static Request<Models.LeaderboardEntryList> GetEntries(string leaderboardName, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_Leaderboard_GetEntries(leaderboardName, limit, filter, startAt));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of leaderboard entries.
    /// \param leaderboardName The name of the leaderboard.
    /// \param limit The maximum number of entries to return.
    /// \param afterRank The position after which to start.  For example, 10 returns leaderboard results starting with the 11th user.
    ///
    public static Request<Models.LeaderboardEntryList> GetEntriesAfterRank(string leaderboardName, int limit, ulong afterRank)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_Leaderboard_GetEntriesAfterRank(leaderboardName, limit, afterRank));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of leaderboard entries. Will return only entries matching
    /// the user IDs passed in.
    /// \param leaderboardName The name of the leaderboard whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param startAt Defines whether to center the query on the user or start at the top of the leaderboard. If this is LeaderboardStartAt.CenteredOnViewer or LeaderboardStartAt.CenteredOnViewerOrTop, then the current user's ID will be automatically added to the query.
    /// \param userIDs Defines a list of user ids to get entries for.
    ///
    public static Request<Models.LeaderboardEntryList> GetEntriesByIds(string leaderboardName, int limit, LeaderboardStartAt startAt, UInt64[] userIDs)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_Leaderboard_GetEntriesByIds(leaderboardName, limit, startAt, userIDs, (uint)(userIDs != null ? userIDs.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Writes a single entry to a leaderboard.
    /// \param leaderboardName The leaderboard for which to write the entry.
    /// \param score The score to write.
    /// \param extraData A 2KB custom data field that is associated with the leaderboard entry. This can be a game replay or anything that provides more detail about the entry to the viewer.
    /// \param forceUpdate If true, the score always updates.  This happens even if it is not the user's best score.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    /// - \b 100: Something went wrong.
    /// - \b 100: This leaderboard entry is too late for the leaderboard's allowed time window.
    ///
    public static Request<bool> WriteEntry(string leaderboardName, long score, byte[] extraData = null, bool forceUpdate = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<bool>(CAPI.ovr_Leaderboard_WriteEntry(leaderboardName, score, extraData, (uint)(extraData != null ? extraData.Length : 0), forceUpdate));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Writes a single entry to a leaderboard, can include supplementary metrics
    /// \param leaderboardName The leaderboard for which to write the entry.
    /// \param score The score to write.
    /// \param supplementaryMetric A metric that can be used for tiebreakers.
    /// \param extraData A 2KB custom data field that is associated with the leaderboard entry. This can be a game replay or anything that provides more detail about the entry to the viewer.
    /// \param forceUpdate If true, the score always updates. This happens ecen if it is not the user's best score.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    /// - \b 100: Something went wrong.
    /// - \b 100: This leaderboard entry is too late for the leaderboard's allowed time window.
    ///
    public static Request<bool> WriteEntryWithSupplementaryMetric(string leaderboardName, long score, long supplementaryMetric, byte[] extraData = null, bool forceUpdate = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<bool>(CAPI.ovr_Leaderboard_WriteEntryWithSupplementaryMetric(leaderboardName, score, supplementaryMetric, extraData, (uint)(extraData != null ? extraData.Length : 0), forceUpdate));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Livestreaming
  {
    /// Indicates that the livestreaming session has been updated. You can use this
    /// information to throttle your game performance or increase CPU/GPU
    /// performance. Use Message.GetLivestreamingStatus() to extract the updated
    /// livestreaming status.
    ///
    public static void SetStatusUpdateNotificationCallback(Message<Models.LivestreamingStatus>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Livestreaming_StatusChange,
        callback
      );
    }

  }

  public static partial class Media
  {
    /// Launch the Share to Facebook modal via a deeplink to Home on Gear VR,
    /// allowing users to share local media files to Facebook. Accepts a
    /// postTextSuggestion string for the default text of the Facebook post.
    /// Requires a filePath string as the path to the image to be shared to
    /// Facebook. This image should be located in your app's internal storage
    /// directory. Requires a contentType indicating the type of media to be shared
    /// (only 'photo' is currently supported.)
    /// \param postTextSuggestion this text will prepopulate the facebook status text-input box within the share modal
    /// \param filePath path to the file to be shared to facebook
    /// \param contentType content type of the media to be shared
    ///
    public static Request<Models.ShareMediaResult> ShareToFacebook(string postTextSuggestion, string filePath, MediaContentType contentType)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ShareMediaResult>(CAPI.ovr_Media_ShareToFacebook(postTextSuggestion, filePath, contentType));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class NetSync
  {
    /// Sent when the status of a connection has changed.
    ///
    public static void SetConnectionStatusChangedNotificationCallback(Message<Models.NetSyncConnection>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_NetSync_ConnectionStatusChanged,
        callback
      );
    }

    /// Sent when the list of known connected sessions has changed. Contains the
    /// new list of sessions.
    ///
    public static void SetSessionsChangedNotificationCallback(Message<Models.NetSyncSessionsChangedNotification>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_NetSync_SessionsChanged,
        callback
      );
    }

  }

  public static partial class Notifications
  {
    /// Mark a notification as read. This causes it to disappear from the Universal
    /// Menu, the Oculus App, Oculus Home, and in-app retrieval.
    ///
    public static Request MarkAsRead(UInt64 notificationID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Notification_MarkAsRead(notificationID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Parties
  {
    /// Load the party the current user is in.
    ///
    public static Request<Models.Party> GetCurrent()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Party>(CAPI.ovr_Party_GetCurrent());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Indicates that party has been updated
    ///
    public static void SetPartyUpdateNotificationCallback(Message<Models.PartyUpdateNotification>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Party_PartyUpdate,
        callback
      );
    }

  }

  public static partial class RichPresence
  {
    /// DEPRECATED. Use the clear method in group presence
    ///
    public static Request Clear()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_RichPresence_Clear());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Gets all the destinations that the presence can be set to
    ///
    public static Request<Models.DestinationList> GetDestinations()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.DestinationList>(CAPI.ovr_RichPresence_GetDestinations());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use GroupPresence.Set().
    ///
    public static Request Set(RichPresenceOptions richPresenceOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_RichPresence_Set((IntPtr)richPresenceOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Users
  {
    /// Retrieve the user with the given ID. This might fail if the ID is invalid
    /// or the user is blocked.
    ///
    /// NOTE: Users will have a unique ID per application.
    /// \param userID User ID retrieved with this application.
    ///
    public static Request<Models.User> Get(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.User>(CAPI.ovr_User_Get(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Return an access token for this user, suitable for making REST calls
    /// against graph.oculus.com.
    ///
    public static Request<string> GetAccessToken()
    {
      if (Core.IsInitialized())
      {
        return new Request<string>(CAPI.ovr_User_GetAccessToken());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Return the IDs of users entitled to use the current app that are blocked by
    /// the specified user
    ///
    public static Request<Models.BlockedUserList> GetBlockedUsers()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.BlockedUserList>(CAPI.ovr_User_GetBlockedUsers());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve the currently signed in user. This call is available offline.
    ///
    /// NOTE: This will not return the user's presence as it should always be
    /// 'online' in your application.
    ///
    /// NOTE: Users will have a unique ID per application.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Something went wrong.
    ///
    public static Request<Models.User> GetLoggedInUser()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.User>(CAPI.ovr_User_GetLoggedInUser());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of the logged in user's bidirectional followers.
    ///
    public static Request<Models.UserList> GetLoggedInUserFriends()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(CAPI.ovr_User_GetLoggedInUserFriends());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// returns an ovrID which is unique per org. allows different apps within the
    /// same org to identify the user.
    /// \param userID to load the org scoped id of
    ///
    public static Request<Models.OrgScopedID> GetOrgScopedID(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.OrgScopedID>(CAPI.ovr_User_GetOrgScopedID(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns all accounts belonging to this user. Accounts are the Oculus user
    /// and x-users that are linked to this user.
    ///
    public static Request<Models.SdkAccountList> GetSdkAccounts()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.SdkAccountList>(CAPI.ovr_User_GetSdkAccounts());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Part of the scheme to confirm the identity of a particular user in your
    /// backend. You can pass the result of Users.GetUserProof() and a user ID from
    /// Users.Get() to your your backend. Your server can then use our api to
    /// verify identity. 'https://graph.oculus.com/user_nonce_validate?nonce=USER_P
    /// ROOF&user_id=USER_ID&access_token=ACCESS_TOKEN'
    ///
    /// NOTE: The nonce is only good for one check and then it is invalidated.
    ///
    public static Request<Models.UserProof> GetUserProof()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserProof>(CAPI.ovr_User_GetUserProof());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow for blocking the given user. You can't follow, be followed,
    /// invited, or searched by a blocked user, for example. You can remove the
    /// block via ovr_User_LaunchUnblockFlow.
    /// \param userID User ID of user being blocked
    ///
    public static Request<Models.LaunchBlockFlowResult> LaunchBlockFlow(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LaunchBlockFlowResult>(CAPI.ovr_User_LaunchBlockFlow(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow for sending a follow request to a user.
    /// \param userID User ID of user to send a follow request to
    ///
    public static Request<Models.LaunchFriendRequestFlowResult> LaunchFriendRequestFlow(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LaunchFriendRequestFlowResult>(CAPI.ovr_User_LaunchFriendRequestFlow(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow for unblocking a user that the viewer has blocked.
    /// \param userID User ID of user to unblock
    ///
    public static Request<Models.LaunchUnblockFlowResult> LaunchUnblockFlow(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LaunchUnblockFlowResult>(CAPI.ovr_User_LaunchUnblockFlow(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class UserDataStore
  {
    /// Delete an entry by a key from a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    /// \param key The key of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PrivateDeleteEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PrivateDeleteEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get entries from a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    ///
    public static Request<Dictionary<string, string>> PrivateGetEntries(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PrivateGetEntries(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get an entry by a key from a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    /// \param key The key of entry.
    ///
    public static Request<Dictionary<string, string>> PrivateGetEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PrivateGetEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Write a single entry to a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    /// \param key The key of entry.
    /// \param value The value of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PrivateWriteEntry(UInt64 userID, string key, string value)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PrivateWriteEntry(userID, key, value));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Delete an entry by a key from a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    /// \param key The key of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PublicDeleteEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PublicDeleteEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get entries from a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    ///
    public static Request<Dictionary<string, string>> PublicGetEntries(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PublicGetEntries(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get an entry by a key from a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    /// \param key The key of entry.
    ///
    public static Request<Dictionary<string, string>> PublicGetEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PublicGetEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Write a single entry to a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    /// \param key The key of entry.
    /// \param value The value of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PublicWriteEntry(UInt64 userID, string key, string value)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PublicWriteEntry(userID, key, value));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Voip
  {
    /// Gets whether the microphone is currently available to the app. This can be
    /// used to show if the user's voice is able to be heard by other users.
    ///
    public static Request<Models.MicrophoneAvailabilityState> GetMicrophoneAvailability()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MicrophoneAvailabilityState>(CAPI.ovr_Voip_GetMicrophoneAvailability());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sets whether SystemVoip should be suppressed so that this app's Voip can
    /// use the mic and play incoming Voip audio. Once microphone switching
    /// functionality for the user is released, this function will no longer work.
    /// You can use get_microphone_availability to see if the user has allowed the
    /// app access to the microphone.
    ///
    public static Request<Models.SystemVoipState> SetSystemVoipSuppressed(bool suppressed)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.SystemVoipState>(CAPI.ovr_Voip_SetSystemVoipSuppressed(suppressed));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Indicates that the current microphone availability state has been updated.
    /// Use Voip.GetMicrophoneAvailability() to extract the microphone availability
    /// state.
    ///
    public static void SetMicrophoneAvailabilityStateUpdateNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Voip_MicrophoneAvailabilityStateUpdate,
        callback
      );
    }

    /// Sent to indicate that some part of the overall state of SystemVoip has
    /// changed. Use Message.GetSystemVoipState() and the properties of
    /// SystemVoipState to extract the state that triggered the notification.
    ///
    /// Note that the state may have changed further since the notification was
    /// generated, and that you may call the `GetSystemVoip...()` family of
    /// functions at any time to get the current state directly.
    ///
    public static void SetSystemVoipStateNotificationCallback(Message<Models.SystemVoipState>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Voip_SystemVoipState,
        callback
      );
    }

  }

  public static partial class Vrcamera
  {
    /// Get vr camera related webrtc data channel messages for update.
    ///
    public static void SetGetDataChannelMessageUpdateNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Vrcamera_GetDataChannelMessageUpdate,
        callback
      );
    }

    /// Get surface and update action from platform webrtc for update.
    ///
    public static void SetGetSurfaceUpdateNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Vrcamera_GetSurfaceUpdate,
        callback
      );
    }

  }


  public static partial class Achievements {
    public static Request<Models.AchievementDefinitionList> GetNextAchievementDefinitionListPage(Models.AchievementDefinitionList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextAchievementDefinitionListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementDefinitionList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Achievements_GetNextAchievementDefinitionArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.AchievementProgressList> GetNextAchievementProgressListPage(Models.AchievementProgressList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextAchievementProgressListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementProgressList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Achievements_GetNextAchievementProgressArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class GroupPresence {
    public static Request<Models.ApplicationInviteList> GetNextApplicationInviteListPage(Models.ApplicationInviteList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextApplicationInviteListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.ApplicationInviteList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.GroupPresence_GetNextApplicationInviteArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class IAP {
    public static Request<Models.ProductList> GetNextProductListPage(Models.ProductList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextProductListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.ProductList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.IAP_GetNextProductArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.PurchaseList> GetNextPurchaseListPage(Models.PurchaseList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextPurchaseListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.PurchaseList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.IAP_GetNextPurchaseArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Leaderboards {
    public static Request<Models.LeaderboardList> GetNextLeaderboardListPage(Models.LeaderboardList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextLeaderboardListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Leaderboard_GetNextLeaderboardArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Notifications {
  }

  public static partial class RichPresence {
    public static Request<Models.DestinationList> GetNextDestinationListPage(Models.DestinationList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextDestinationListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.DestinationList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.RichPresence_GetNextDestinationArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Users {
    public static Request<Models.BlockedUserList> GetNextBlockedUserListPage(Models.BlockedUserList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextBlockedUserListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.BlockedUserList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextBlockedUserArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.UserList> GetNextUserListPage(Models.UserList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextUserListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextUserArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.UserCapabilityList> GetNextUserCapabilityListPage(Models.UserCapabilityList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextUserCapabilityListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.UserCapabilityList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextUserCapabilityArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }


}
