// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using Oculus.Platform.Models;

  public abstract class Message<T> : Message
  {
    public new delegate void Callback(Message<T> message);
    public Message(IntPtr c_message) : base(c_message) {
      if (!IsError)
      {
        data = GetDataFromMessage(c_message);
      }
    }

    public T Data { get { return data; } }
    protected abstract T GetDataFromMessage(IntPtr c_message);
    private T data;
  }

  public class Message
  {
    public delegate void Callback(Message message);
    public Message(IntPtr c_message)
    {
      type = (MessageType)CAPI.ovr_Message_GetType(c_message);
      var isError = CAPI.ovr_Message_IsError(c_message);
      requestID = CAPI.ovr_Message_GetRequestID(c_message);

      if (!isError) {
        var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
        if (CAPI.ovr_Message_IsError(msg)) {
          IntPtr errorHandle = CAPI.ovr_Message_GetError(msg);
          error = new Error(
            CAPI.ovr_Error_GetCode(errorHandle),
            CAPI.ovr_Error_GetMessage(errorHandle),
            CAPI.ovr_Error_GetHttpCode(errorHandle));
        }
      }

      if (isError)
      {
        IntPtr errorHandle = CAPI.ovr_Message_GetError(c_message);
        error = new Error(
          CAPI.ovr_Error_GetCode(errorHandle),
          CAPI.ovr_Error_GetMessage(errorHandle),
          CAPI.ovr_Error_GetHttpCode(errorHandle));
      }
      else if (Core.LogMessages)
      {
        var message = CAPI.ovr_Message_GetString(c_message);
        if (message != null)
        {
          Debug.Log(message);
        }
        else
        {
          Debug.Log(string.Format("null message string {0}", c_message));
        }
      }
    }

    ~Message()
    {
    }

    // Keep this enum in sync with ovrMessageType in OVR_Platform.h
    public enum MessageType : uint
    { //TODO - rename this to type; it's already in Message class
      Unknown,

      AbuseReport_ReportRequestHandled                   = 0x4B8EFC86,
      Achievements_AddCount                              = 0x03E76231,
      Achievements_AddFields                             = 0x14AA2129,
      Achievements_GetAllDefinitions                     = 0x03D3458D,
      Achievements_GetAllProgress                        = 0x4F9FDE1D,
      Achievements_GetDefinitionsByName                  = 0x629101BC,
      Achievements_GetNextAchievementDefinitionArrayPage = 0x2A7DD255,
      Achievements_GetNextAchievementProgressArrayPage   = 0x2F42E727,
      Achievements_GetProgressByName                     = 0x152663B1,
      Achievements_Unlock                                = 0x593CCBDD,
      ApplicationLifecycle_GetRegisteredPIDs             = 0x04E5CF62,
      ApplicationLifecycle_GetSessionKey                 = 0x3AAF591D,
      ApplicationLifecycle_RegisterSessionKey            = 0x4DB6AFF8,
      Application_GetVersion                             = 0x68670A0E,
      Application_LaunchOtherApp                         = 0x54E2D1F8,
      AssetFile_Delete                                   = 0x6D5D7886,
      AssetFile_DeleteById                               = 0x5AE8CD52,
      AssetFile_DeleteByName                             = 0x420AC1CF,
      AssetFile_Download                                 = 0x11449FC5,
      AssetFile_DownloadById                             = 0x2D008992,
      AssetFile_DownloadByName                           = 0x6336CEFA,
      AssetFile_DownloadCancel                           = 0x080AD3C7,
      AssetFile_DownloadCancelById                       = 0x51659514,
      AssetFile_DownloadCancelByName                     = 0x446AECFA,
      AssetFile_GetList                                  = 0x4AFC6F74,
      AssetFile_Status                                   = 0x02D32F60,
      AssetFile_StatusById                               = 0x5D955D38,
      AssetFile_StatusByName                             = 0x41CFDA50,
      Avatar_LaunchAvatarEditor                          = 0x05F1E153,
      Challenges_Create                                  = 0x6859D641,
      Challenges_DeclineInvite                           = 0x568E76C0,
      Challenges_Delete                                  = 0x264885CA,
      Challenges_Get                                     = 0x77584EF3,
      Challenges_GetEntries                              = 0x121AB45F,
      Challenges_GetEntriesAfterRank                     = 0x08891A7F,
      Challenges_GetEntriesByIds                         = 0x316509DC,
      Challenges_GetList                                 = 0x43264356,
      Challenges_GetNextChallenges                       = 0x5B7CA1B6,
      Challenges_GetNextEntries                          = 0x7F4CA0C6,
      Challenges_GetPreviousChallenges                   = 0x0EB4040D,
      Challenges_GetPreviousEntries                      = 0x78C90470,
      Challenges_Join                                    = 0x21248069,
      Challenges_Leave                                   = 0x296116E5,
      Challenges_UpdateInfo                              = 0x1175BE60,
      Entitlement_GetIsViewerEntitled                    = 0x186B58B1,
      GroupPresence_Clear                                = 0x6DAA9CC3,
      GroupPresence_GetInvitableUsers                    = 0x234BC3F1,
      GroupPresence_GetNextApplicationInviteArrayPage    = 0x04F8C0F2,
      GroupPresence_GetSentInvites                       = 0x08260AB1,
      GroupPresence_LaunchInvitePanel                    = 0x0F9ECF9F,
      GroupPresence_LaunchMultiplayerErrorDialog         = 0x2955AF24,
      GroupPresence_LaunchRejoinDialog                   = 0x1577036F,
      GroupPresence_LaunchRosterPanel                    = 0x35728882,
      GroupPresence_SendInvites                          = 0x0DCBD364,
      GroupPresence_Set                                  = 0x675F5C24,
      GroupPresence_SetDeeplinkMessageOverride           = 0x521ADF0D,
      GroupPresence_SetDestination                       = 0x4C5B268A,
      GroupPresence_SetIsJoinable                        = 0x2A8F1055,
      GroupPresence_SetLobbySession                      = 0x48FF55BE,
      GroupPresence_SetMatchSession                      = 0x314C84B8,
      IAP_ConsumePurchase                                = 0x1FBB72D9,
      IAP_GetNextProductArrayPage                        = 0x1BD94AAF,
      IAP_GetNextPurchaseArrayPage                       = 0x47570A95,
      IAP_GetProductsBySKU                               = 0x7E9ACAF5,
      IAP_GetViewerPurchases                             = 0x3A0F8419,
      IAP_GetViewerPurchasesDurableCache                 = 0x63599E2B,
      IAP_LaunchCheckoutFlow                             = 0x3F9B0D0D,
      LanguagePack_GetCurrent                            = 0x1F90F0D5,
      LanguagePack_SetCurrent                            = 0x5B4FBBE0,
      Leaderboard_Get                                    = 0x6AD44EF8,
      Leaderboard_GetEntries                             = 0x5DB3474C,
      Leaderboard_GetEntriesAfterRank                    = 0x18378BEF,
      Leaderboard_GetEntriesByIds                        = 0x39607BFC,
      Leaderboard_GetNextEntries                         = 0x4E207CD9,
      Leaderboard_GetNextLeaderboardArrayPage            = 0x35F6769B,
      Leaderboard_GetPreviousEntries                     = 0x4901DAC0,
      Leaderboard_WriteEntry                             = 0x117FC8FE,
      Leaderboard_WriteEntryWithSupplementaryMetric      = 0x72C692FA,
      Media_ShareToFacebook                              = 0x00E38AEF,
      Notification_MarkAsRead                            = 0x717259E3,
      Party_GetCurrent                                   = 0x47933760,
      RichPresence_Clear                                 = 0x57B752B3,
      RichPresence_GetDestinations                       = 0x586F2D14,
      RichPresence_GetNextDestinationArrayPage           = 0x67367F45,
      RichPresence_Set                                   = 0x3C147509,
      UserDataStore_PrivateDeleteEntryByKey              = 0x5C896F3E,
      UserDataStore_PrivateGetEntries                    = 0x6C8A8228,
      UserDataStore_PrivateGetEntryByKey                 = 0x1C068319,
      UserDataStore_PrivateWriteEntry                    = 0x41D2828B,
      UserDataStore_PublicDeleteEntryByKey               = 0x1DD5E5FB,
      UserDataStore_PublicGetEntries                     = 0x167D4BC2,
      UserDataStore_PublicGetEntryByKey                  = 0x195C66C6,
      UserDataStore_PublicWriteEntry                     = 0x34364A0A,
      User_Get                                           = 0x6BCF9E47,
      User_GetAccessToken                                = 0x06A85ABE,
      User_GetBlockedUsers                               = 0x7D201556,
      User_GetLoggedInUser                               = 0x436F345D,
      User_GetLoggedInUserFriends                        = 0x587C2A8D,
      User_GetNextBlockedUserArrayPage                   = 0x7C2AFDCB,
      User_GetNextUserArrayPage                          = 0x267CF743,
      User_GetNextUserCapabilityArrayPage                = 0x2309F399,
      User_GetOrgScopedID                                = 0x18F0B01B,
      User_GetSdkAccounts                                = 0x67526A83,
      User_GetUserProof                                  = 0x22810483,
      User_LaunchBlockFlow                               = 0x6FD62528,
      User_LaunchFriendRequestFlow                       = 0x0904B598,
      User_LaunchUnblockFlow                             = 0x14A22A97,
      Voip_GetMicrophoneAvailability                     = 0x744CE345,
      Voip_SetSystemVoipSuppressed                       = 0x453FC9AA,

      /// The user has tapped the report button in the panel that appears after
      /// pressing the Oculus button.
      Notification_AbuseReport_ReportButtonPressed = 0x24472F6C,

      /// Sent when a launch intent is received (for both cold and warm starts). The
      /// payload is the type of the intent. ApplicationLifecycle.GetLaunchDetails()
      /// should be called to get the other details.
      Notification_ApplicationLifecycle_LaunchIntentChanged = 0x04B34CA3,

      /// Sent to indicate download progress for asset files.
      Notification_AssetFile_DownloadUpdate = 0x2FDD0CCD,

      /// Sent when the user is finished using the invite panel to send out
      /// invitations. Contains a list of invitees.
      Notification_GroupPresence_InvitationsSent = 0x679A84B6,

      /// Sent when a user has chosen to join the destination/lobby/match. Read all
      /// the fields to figure out where the user wants to go and take the
      /// appropriate actions to bring them there. If the user is unable to go there,
      /// provide adequate messaging to the user on why they cannot go there. These
      /// notifications should be responded to immediately.
      Notification_GroupPresence_JoinIntentReceived = 0x773889F6,

      /// Sent when the user has chosen to leave the destination/lobby/match from the
      /// Oculus menu. Read the specific fields to check the user is currently from
      /// the destination/lobby/match and take the appropriate actions to remove
      /// them. Update the user's presence clearing the appropriate fields to
      /// indicate the user has left.
      Notification_GroupPresence_LeaveIntentReceived = 0x4737EA1D,

      /// Sent to indicate that more data has been read or an error occured.
      Notification_HTTP_Transfer = 0x7DD46E2F,

      /// Indicates that the livestreaming session has been updated. You can use this
      /// information to throttle your game performance or increase CPU/GPU
      /// performance. Use Message.GetLivestreamingStatus() to extract the updated
      /// livestreaming status.
      Notification_Livestreaming_StatusChange = 0x2247596E,

      /// Sent when the status of a connection has changed.
      Notification_NetSync_ConnectionStatusChanged = 0x073484CA,

      /// Sent when the list of known connected sessions has changed. Contains the
      /// new list of sessions.
      Notification_NetSync_SessionsChanged = 0x387E7F36,

      /// Indicates that party has been updated
      Notification_Party_PartyUpdate = 0x1D118AB2,

      /// Indicates that the current microphone availability state has been updated.
      /// Use Voip.GetMicrophoneAvailability() to extract the microphone availability
      /// state.
      Notification_Voip_MicrophoneAvailabilityStateUpdate = 0x3E20CB57,

      /// Sent to indicate that some part of the overall state of SystemVoip has
      /// changed. Use Message.GetSystemVoipState() and the properties of
      /// SystemVoipState to extract the state that triggered the notification.
      ///
      /// Note that the state may have changed further since the notification was
      /// generated, and that you may call the `GetSystemVoip...()` family of
      /// functions at any time to get the current state directly.
      Notification_Voip_SystemVoipState = 0x58D254A5,

      /// Get vr camera related webrtc data channel messages for update.
      Notification_Vrcamera_GetDataChannelMessageUpdate = 0x6EE4F33C,

      /// Get surface and update action from platform webrtc for update.
      Notification_Vrcamera_GetSurfaceUpdate = 0x37F21084,


      Platform_InitializeWithAccessToken = 0x35692F2B,
      Platform_InitializeStandaloneOculus = 0x51F8CE0C,
      Platform_InitializeAndroidAsynchronous = 0x1AD307B4,
      Platform_InitializeWindowsAsynchronous = 0x6DA7BA8F,
    };

    public MessageType Type { get { return type; } }
    public bool IsError { get { return error != null; } }
    public ulong RequestID { get { return requestID; } }

    private MessageType type;
    private ulong requestID;
    private Error error;

    public virtual Error GetError() { return error; }
    public virtual HttpTransferUpdate GetHttpTransferUpdate() { return null; }

    public virtual PlatformInitialize GetPlatformInitialize() { return null; }

    public virtual AbuseReportRecording GetAbuseReportRecording() { return null; }
    public virtual AchievementDefinitionList GetAchievementDefinitions() { return null; }
    public virtual AchievementProgressList GetAchievementProgressList() { return null; }
    public virtual AchievementUpdate GetAchievementUpdate() { return null; }
    public virtual ApplicationInviteList GetApplicationInviteList() { return null; }
    public virtual ApplicationVersion GetApplicationVersion() { return null; }
    public virtual AssetDetails GetAssetDetails() { return null; }
    public virtual AssetDetailsList GetAssetDetailsList() { return null; }
    public virtual AssetFileDeleteResult GetAssetFileDeleteResult() { return null; }
    public virtual AssetFileDownloadCancelResult GetAssetFileDownloadCancelResult() { return null; }
    public virtual AssetFileDownloadResult GetAssetFileDownloadResult() { return null; }
    public virtual AssetFileDownloadUpdate GetAssetFileDownloadUpdate() { return null; }
    public virtual AvatarEditorResult GetAvatarEditorResult() { return null; }
    public virtual BlockedUserList GetBlockedUserList() { return null; }
    public virtual Challenge GetChallenge() { return null; }
    public virtual ChallengeEntryList GetChallengeEntryList() { return null; }
    public virtual ChallengeList GetChallengeList() { return null; }
    public virtual Dictionary<string, string> GetDataStore() { return null; }
    public virtual DestinationList GetDestinationList() { return null; }
    public virtual GroupPresenceJoinIntent GetGroupPresenceJoinIntent() { return null; }
    public virtual GroupPresenceLeaveIntent GetGroupPresenceLeaveIntent() { return null; }
    public virtual InstalledApplicationList GetInstalledApplicationList() { return null; }
    public virtual InvitePanelResultInfo GetInvitePanelResultInfo() { return null; }
    public virtual LaunchBlockFlowResult GetLaunchBlockFlowResult() { return null; }
    public virtual LaunchFriendRequestFlowResult GetLaunchFriendRequestFlowResult() { return null; }
    public virtual LaunchInvitePanelFlowResult GetLaunchInvitePanelFlowResult() { return null; }
    public virtual LaunchReportFlowResult GetLaunchReportFlowResult() { return null; }
    public virtual LaunchUnblockFlowResult GetLaunchUnblockFlowResult() { return null; }
    public virtual bool GetLeaderboardDidUpdate() { return false; }
    public virtual LeaderboardEntryList GetLeaderboardEntryList() { return null; }
    public virtual LeaderboardList GetLeaderboardList() { return null; }
    public virtual LinkedAccountList GetLinkedAccountList() { return null; }
    public virtual LivestreamingApplicationStatus GetLivestreamingApplicationStatus() { return null; }
    public virtual LivestreamingStartResult GetLivestreamingStartResult() { return null; }
    public virtual LivestreamingStatus GetLivestreamingStatus() { return null; }
    public virtual LivestreamingVideoStats GetLivestreamingVideoStats() { return null; }
    public virtual MicrophoneAvailabilityState GetMicrophoneAvailabilityState() { return null; }
    public virtual NetSyncConnection GetNetSyncConnection() { return null; }
    public virtual NetSyncSessionList GetNetSyncSessionList() { return null; }
    public virtual NetSyncSessionsChangedNotification GetNetSyncSessionsChangedNotification() { return null; }
    public virtual NetSyncSetSessionPropertyResult GetNetSyncSetSessionPropertyResult() { return null; }
    public virtual NetSyncVoipAttenuationValueList GetNetSyncVoipAttenuationValueList() { return null; }
    public virtual OrgScopedID GetOrgScopedID() { return null; }
    public virtual Party GetParty() { return null; }
    public virtual PartyID GetPartyID() { return null; }
    public virtual PartyUpdateNotification GetPartyUpdateNotification() { return null; }
    public virtual PidList GetPidList() { return null; }
    public virtual ProductList GetProductList() { return null; }
    public virtual Purchase GetPurchase() { return null; }
    public virtual PurchaseList GetPurchaseList() { return null; }
    public virtual RejoinDialogResult GetRejoinDialogResult() { return null; }
    public virtual SdkAccountList GetSdkAccountList() { return null; }
    public virtual SendInvitesResult GetSendInvitesResult() { return null; }
    public virtual ShareMediaResult GetShareMediaResult() { return null; }
    public virtual string GetString() { return null; }
    public virtual SystemVoipState GetSystemVoipState() { return null; }
    public virtual User GetUser() { return null; }
    public virtual UserCapabilityList GetUserCapabilityList() { return null; }
    public virtual UserDataStoreUpdateResponse GetUserDataStoreUpdateResponse() { return null; }
    public virtual UserList GetUserList() { return null; }
    public virtual UserProof GetUserProof() { return null; }
    public virtual UserReportID GetUserReportID() { return null; }

    internal static Message ParseMessageHandle(IntPtr messageHandle)
    {
      if (messageHandle.ToInt64() == 0)
      {
        return null;
      }

      Message message = null;
      Message.MessageType message_type = (Message.MessageType)CAPI.ovr_Message_GetType(messageHandle);

      switch(message_type) {
        // OVR_MESSAGE_TYPE_START
        case Message.MessageType.Achievements_GetAllDefinitions:
        case Message.MessageType.Achievements_GetDefinitionsByName:
        case Message.MessageType.Achievements_GetNextAchievementDefinitionArrayPage:
          message = new MessageWithAchievementDefinitions(messageHandle);
          break;

        case Message.MessageType.Achievements_GetAllProgress:
        case Message.MessageType.Achievements_GetNextAchievementProgressArrayPage:
        case Message.MessageType.Achievements_GetProgressByName:
          message = new MessageWithAchievementProgressList(messageHandle);
          break;

        case Message.MessageType.Achievements_AddCount:
        case Message.MessageType.Achievements_AddFields:
        case Message.MessageType.Achievements_Unlock:
          message = new MessageWithAchievementUpdate(messageHandle);
          break;

        case Message.MessageType.GroupPresence_GetNextApplicationInviteArrayPage:
        case Message.MessageType.GroupPresence_GetSentInvites:
          message = new MessageWithApplicationInviteList(messageHandle);
          break;

        case Message.MessageType.Application_GetVersion:
          message = new MessageWithApplicationVersion(messageHandle);
          break;

        case Message.MessageType.AssetFile_Status:
        case Message.MessageType.AssetFile_StatusById:
        case Message.MessageType.AssetFile_StatusByName:
        case Message.MessageType.LanguagePack_GetCurrent:
          message = new MessageWithAssetDetails(messageHandle);
          break;

        case Message.MessageType.AssetFile_GetList:
          message = new MessageWithAssetDetailsList(messageHandle);
          break;

        case Message.MessageType.AssetFile_Delete:
        case Message.MessageType.AssetFile_DeleteById:
        case Message.MessageType.AssetFile_DeleteByName:
          message = new MessageWithAssetFileDeleteResult(messageHandle);
          break;

        case Message.MessageType.AssetFile_DownloadCancel:
        case Message.MessageType.AssetFile_DownloadCancelById:
        case Message.MessageType.AssetFile_DownloadCancelByName:
          message = new MessageWithAssetFileDownloadCancelResult(messageHandle);
          break;

        case Message.MessageType.AssetFile_Download:
        case Message.MessageType.AssetFile_DownloadById:
        case Message.MessageType.AssetFile_DownloadByName:
        case Message.MessageType.LanguagePack_SetCurrent:
          message = new MessageWithAssetFileDownloadResult(messageHandle);
          break;

        case Message.MessageType.Notification_AssetFile_DownloadUpdate:
          message = new MessageWithAssetFileDownloadUpdate(messageHandle);
          break;

        case Message.MessageType.Avatar_LaunchAvatarEditor:
          message = new MessageWithAvatarEditorResult(messageHandle);
          break;

        case Message.MessageType.User_GetBlockedUsers:
        case Message.MessageType.User_GetNextBlockedUserArrayPage:
          message = new MessageWithBlockedUserList(messageHandle);
          break;

        case Message.MessageType.Challenges_Create:
        case Message.MessageType.Challenges_DeclineInvite:
        case Message.MessageType.Challenges_Get:
        case Message.MessageType.Challenges_Join:
        case Message.MessageType.Challenges_Leave:
        case Message.MessageType.Challenges_UpdateInfo:
          message = new MessageWithChallenge(messageHandle);
          break;

        case Message.MessageType.Challenges_GetList:
        case Message.MessageType.Challenges_GetNextChallenges:
        case Message.MessageType.Challenges_GetPreviousChallenges:
          message = new MessageWithChallengeList(messageHandle);
          break;

        case Message.MessageType.Challenges_GetEntries:
        case Message.MessageType.Challenges_GetEntriesAfterRank:
        case Message.MessageType.Challenges_GetEntriesByIds:
        case Message.MessageType.Challenges_GetNextEntries:
        case Message.MessageType.Challenges_GetPreviousEntries:
          message = new MessageWithChallengeEntryList(messageHandle);
          break;

        case Message.MessageType.UserDataStore_PrivateGetEntries:
        case Message.MessageType.UserDataStore_PrivateGetEntryByKey:
          message = new MessageWithDataStoreUnderPrivateUserDataStore(messageHandle);
          break;

        case Message.MessageType.UserDataStore_PublicGetEntries:
        case Message.MessageType.UserDataStore_PublicGetEntryByKey:
          message = new MessageWithDataStoreUnderPublicUserDataStore(messageHandle);
          break;

        case Message.MessageType.RichPresence_GetDestinations:
        case Message.MessageType.RichPresence_GetNextDestinationArrayPage:
          message = new MessageWithDestinationList(messageHandle);
          break;

        case Message.MessageType.AbuseReport_ReportRequestHandled:
        case Message.MessageType.ApplicationLifecycle_RegisterSessionKey:
        case Message.MessageType.Challenges_Delete:
        case Message.MessageType.Entitlement_GetIsViewerEntitled:
        case Message.MessageType.GroupPresence_Clear:
        case Message.MessageType.GroupPresence_LaunchMultiplayerErrorDialog:
        case Message.MessageType.GroupPresence_LaunchRosterPanel:
        case Message.MessageType.GroupPresence_Set:
        case Message.MessageType.GroupPresence_SetDeeplinkMessageOverride:
        case Message.MessageType.GroupPresence_SetDestination:
        case Message.MessageType.GroupPresence_SetIsJoinable:
        case Message.MessageType.GroupPresence_SetLobbySession:
        case Message.MessageType.GroupPresence_SetMatchSession:
        case Message.MessageType.IAP_ConsumePurchase:
        case Message.MessageType.Notification_MarkAsRead:
        case Message.MessageType.RichPresence_Clear:
        case Message.MessageType.RichPresence_Set:
          message = new Message(messageHandle);
          break;

        case Message.MessageType.Notification_GroupPresence_JoinIntentReceived:
          message = new MessageWithGroupPresenceJoinIntent(messageHandle);
          break;

        case Message.MessageType.Notification_GroupPresence_LeaveIntentReceived:
          message = new MessageWithGroupPresenceLeaveIntent(messageHandle);
          break;

        case Message.MessageType.GroupPresence_LaunchInvitePanel:
          message = new MessageWithInvitePanelResultInfo(messageHandle);
          break;

        case Message.MessageType.User_LaunchBlockFlow:
          message = new MessageWithLaunchBlockFlowResult(messageHandle);
          break;

        case Message.MessageType.User_LaunchFriendRequestFlow:
          message = new MessageWithLaunchFriendRequestFlowResult(messageHandle);
          break;

        case Message.MessageType.Notification_GroupPresence_InvitationsSent:
          message = new MessageWithLaunchInvitePanelFlowResult(messageHandle);
          break;

        case Message.MessageType.User_LaunchUnblockFlow:
          message = new MessageWithLaunchUnblockFlowResult(messageHandle);
          break;

        case Message.MessageType.Leaderboard_Get:
        case Message.MessageType.Leaderboard_GetNextLeaderboardArrayPage:
          message = new MessageWithLeaderboardList(messageHandle);
          break;

        case Message.MessageType.Leaderboard_GetEntries:
        case Message.MessageType.Leaderboard_GetEntriesAfterRank:
        case Message.MessageType.Leaderboard_GetEntriesByIds:
        case Message.MessageType.Leaderboard_GetNextEntries:
        case Message.MessageType.Leaderboard_GetPreviousEntries:
          message = new MessageWithLeaderboardEntryList(messageHandle);
          break;

        case Message.MessageType.Leaderboard_WriteEntry:
        case Message.MessageType.Leaderboard_WriteEntryWithSupplementaryMetric:
          message = new MessageWithLeaderboardDidUpdate(messageHandle);
          break;

        case Message.MessageType.Notification_Livestreaming_StatusChange:
          message = new MessageWithLivestreamingStatus(messageHandle);
          break;

        case Message.MessageType.Voip_GetMicrophoneAvailability:
          message = new MessageWithMicrophoneAvailabilityState(messageHandle);
          break;

        case Message.MessageType.Notification_NetSync_ConnectionStatusChanged:
          message = new MessageWithNetSyncConnection(messageHandle);
          break;

        case Message.MessageType.Notification_NetSync_SessionsChanged:
          message = new MessageWithNetSyncSessionsChangedNotification(messageHandle);
          break;

        case Message.MessageType.User_GetOrgScopedID:
          message = new MessageWithOrgScopedID(messageHandle);
          break;

        case Message.MessageType.Party_GetCurrent:
          message = new MessageWithPartyUnderCurrentParty(messageHandle);
          break;

        case Message.MessageType.Notification_Party_PartyUpdate:
          message = new MessageWithPartyUpdateNotification(messageHandle);
          break;

        case Message.MessageType.ApplicationLifecycle_GetRegisteredPIDs:
          message = new MessageWithPidList(messageHandle);
          break;

        case Message.MessageType.IAP_GetNextProductArrayPage:
        case Message.MessageType.IAP_GetProductsBySKU:
          message = new MessageWithProductList(messageHandle);
          break;

        case Message.MessageType.IAP_LaunchCheckoutFlow:
          message = new MessageWithPurchase(messageHandle);
          break;

        case Message.MessageType.IAP_GetNextPurchaseArrayPage:
        case Message.MessageType.IAP_GetViewerPurchases:
        case Message.MessageType.IAP_GetViewerPurchasesDurableCache:
          message = new MessageWithPurchaseList(messageHandle);
          break;

        case Message.MessageType.GroupPresence_LaunchRejoinDialog:
          message = new MessageWithRejoinDialogResult(messageHandle);
          break;

        case Message.MessageType.User_GetSdkAccounts:
          message = new MessageWithSdkAccountList(messageHandle);
          break;

        case Message.MessageType.GroupPresence_SendInvites:
          message = new MessageWithSendInvitesResult(messageHandle);
          break;

        case Message.MessageType.Media_ShareToFacebook:
          message = new MessageWithShareMediaResult(messageHandle);
          break;

        case Message.MessageType.ApplicationLifecycle_GetSessionKey:
        case Message.MessageType.Application_LaunchOtherApp:
        case Message.MessageType.Notification_AbuseReport_ReportButtonPressed:
        case Message.MessageType.Notification_ApplicationLifecycle_LaunchIntentChanged:
        case Message.MessageType.Notification_Voip_MicrophoneAvailabilityStateUpdate:
        case Message.MessageType.Notification_Vrcamera_GetDataChannelMessageUpdate:
        case Message.MessageType.Notification_Vrcamera_GetSurfaceUpdate:
        case Message.MessageType.User_GetAccessToken:
          message = new MessageWithString(messageHandle);
          break;

        case Message.MessageType.Voip_SetSystemVoipSuppressed:
          message = new MessageWithSystemVoipState(messageHandle);
          break;

        case Message.MessageType.User_Get:
        case Message.MessageType.User_GetLoggedInUser:
          message = new MessageWithUser(messageHandle);
          break;

        case Message.MessageType.GroupPresence_GetInvitableUsers:
        case Message.MessageType.User_GetLoggedInUserFriends:
        case Message.MessageType.User_GetNextUserArrayPage:
          message = new MessageWithUserList(messageHandle);
          break;

        case Message.MessageType.User_GetNextUserCapabilityArrayPage:
          message = new MessageWithUserCapabilityList(messageHandle);
          break;

        case Message.MessageType.UserDataStore_PrivateDeleteEntryByKey:
        case Message.MessageType.UserDataStore_PrivateWriteEntry:
        case Message.MessageType.UserDataStore_PublicDeleteEntryByKey:
        case Message.MessageType.UserDataStore_PublicWriteEntry:
          message = new MessageWithUserDataStoreUpdateResponse(messageHandle);
          break;

        case Message.MessageType.User_GetUserProof:
          message = new MessageWithUserProof(messageHandle);
          break;

        case Message.MessageType.Notification_Voip_SystemVoipState:
          message = new MessageWithSystemVoipState(messageHandle);
          break;

        case Message.MessageType.Notification_HTTP_Transfer:
          message = new MessageWithHttpTransferUpdate(messageHandle);
          break;

        case Message.MessageType.Platform_InitializeWithAccessToken:
        case Message.MessageType.Platform_InitializeStandaloneOculus:
        case Message.MessageType.Platform_InitializeAndroidAsynchronous:
        case Message.MessageType.Platform_InitializeWindowsAsynchronous:
          message = new MessageWithPlatformInitialize(messageHandle);
          break;

        default:
          message = PlatformInternal.ParseMessageHandle(messageHandle, message_type);
          if (message == null)
          {
            Debug.LogError(string.Format("Unrecognized message type {0}\n", message_type));
          }
          break;

        // OVR_MESSAGE_TYPE_END
      }

      return message;
    }

    public static Message PopMessage()
    {
      if (!Core.IsInitialized())
      {
        return null;
      }

      var messageHandle = CAPI.ovr_PopMessage();

      Message message = ParseMessageHandle(messageHandle);

      CAPI.ovr_FreeMessage(messageHandle);
      return message;
    }

    internal delegate Message ExtraMessageTypesHandler(IntPtr messageHandle, Message.MessageType message_type);
    internal static ExtraMessageTypesHandler HandleExtraMessageTypes { set; private get; }
  }

  public class MessageWithAbuseReportRecording : Message<AbuseReportRecording>
  {
    public MessageWithAbuseReportRecording(IntPtr c_message) : base(c_message) { }
    public override AbuseReportRecording GetAbuseReportRecording() { return Data; }
    protected override AbuseReportRecording GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAbuseReportRecording(msg);
      return new AbuseReportRecording(obj);
    }

  }
  public class MessageWithAchievementDefinitions : Message<AchievementDefinitionList>
  {
    public MessageWithAchievementDefinitions(IntPtr c_message) : base(c_message) { }
    public override AchievementDefinitionList GetAchievementDefinitions() { return Data; }
    protected override AchievementDefinitionList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAchievementDefinitionArray(msg);
      return new AchievementDefinitionList(obj);
    }

  }
  public class MessageWithAchievementProgressList : Message<AchievementProgressList>
  {
    public MessageWithAchievementProgressList(IntPtr c_message) : base(c_message) { }
    public override AchievementProgressList GetAchievementProgressList() { return Data; }
    protected override AchievementProgressList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAchievementProgressArray(msg);
      return new AchievementProgressList(obj);
    }

  }
  public class MessageWithAchievementUpdate : Message<AchievementUpdate>
  {
    public MessageWithAchievementUpdate(IntPtr c_message) : base(c_message) { }
    public override AchievementUpdate GetAchievementUpdate() { return Data; }
    protected override AchievementUpdate GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAchievementUpdate(msg);
      return new AchievementUpdate(obj);
    }

  }
  public class MessageWithApplicationInviteList : Message<ApplicationInviteList>
  {
    public MessageWithApplicationInviteList(IntPtr c_message) : base(c_message) { }
    public override ApplicationInviteList GetApplicationInviteList() { return Data; }
    protected override ApplicationInviteList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetApplicationInviteArray(msg);
      return new ApplicationInviteList(obj);
    }

  }
  public class MessageWithApplicationVersion : Message<ApplicationVersion>
  {
    public MessageWithApplicationVersion(IntPtr c_message) : base(c_message) { }
    public override ApplicationVersion GetApplicationVersion() { return Data; }
    protected override ApplicationVersion GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetApplicationVersion(msg);
      return new ApplicationVersion(obj);
    }

  }
  public class MessageWithAssetDetails : Message<AssetDetails>
  {
    public MessageWithAssetDetails(IntPtr c_message) : base(c_message) { }
    public override AssetDetails GetAssetDetails() { return Data; }
    protected override AssetDetails GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAssetDetails(msg);
      return new AssetDetails(obj);
    }

  }
  public class MessageWithAssetDetailsList : Message<AssetDetailsList>
  {
    public MessageWithAssetDetailsList(IntPtr c_message) : base(c_message) { }
    public override AssetDetailsList GetAssetDetailsList() { return Data; }
    protected override AssetDetailsList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAssetDetailsArray(msg);
      return new AssetDetailsList(obj);
    }

  }
  public class MessageWithAssetFileDeleteResult : Message<AssetFileDeleteResult>
  {
    public MessageWithAssetFileDeleteResult(IntPtr c_message) : base(c_message) { }
    public override AssetFileDeleteResult GetAssetFileDeleteResult() { return Data; }
    protected override AssetFileDeleteResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAssetFileDeleteResult(msg);
      return new AssetFileDeleteResult(obj);
    }

  }
  public class MessageWithAssetFileDownloadCancelResult : Message<AssetFileDownloadCancelResult>
  {
    public MessageWithAssetFileDownloadCancelResult(IntPtr c_message) : base(c_message) { }
    public override AssetFileDownloadCancelResult GetAssetFileDownloadCancelResult() { return Data; }
    protected override AssetFileDownloadCancelResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAssetFileDownloadCancelResult(msg);
      return new AssetFileDownloadCancelResult(obj);
    }

  }
  public class MessageWithAssetFileDownloadResult : Message<AssetFileDownloadResult>
  {
    public MessageWithAssetFileDownloadResult(IntPtr c_message) : base(c_message) { }
    public override AssetFileDownloadResult GetAssetFileDownloadResult() { return Data; }
    protected override AssetFileDownloadResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAssetFileDownloadResult(msg);
      return new AssetFileDownloadResult(obj);
    }

  }
  public class MessageWithAssetFileDownloadUpdate : Message<AssetFileDownloadUpdate>
  {
    public MessageWithAssetFileDownloadUpdate(IntPtr c_message) : base(c_message) { }
    public override AssetFileDownloadUpdate GetAssetFileDownloadUpdate() { return Data; }
    protected override AssetFileDownloadUpdate GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAssetFileDownloadUpdate(msg);
      return new AssetFileDownloadUpdate(obj);
    }

  }
  public class MessageWithAvatarEditorResult : Message<AvatarEditorResult>
  {
    public MessageWithAvatarEditorResult(IntPtr c_message) : base(c_message) { }
    public override AvatarEditorResult GetAvatarEditorResult() { return Data; }
    protected override AvatarEditorResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetAvatarEditorResult(msg);
      return new AvatarEditorResult(obj);
    }

  }
  public class MessageWithBlockedUserList : Message<BlockedUserList>
  {
    public MessageWithBlockedUserList(IntPtr c_message) : base(c_message) { }
    public override BlockedUserList GetBlockedUserList() { return Data; }
    protected override BlockedUserList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetBlockedUserArray(msg);
      return new BlockedUserList(obj);
    }

  }
  public class MessageWithChallenge : Message<Challenge>
  {
    public MessageWithChallenge(IntPtr c_message) : base(c_message) { }
    public override Challenge GetChallenge() { return Data; }
    protected override Challenge GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetChallenge(msg);
      return new Challenge(obj);
    }

  }
  public class MessageWithChallengeList : Message<ChallengeList>
  {
    public MessageWithChallengeList(IntPtr c_message) : base(c_message) { }
    public override ChallengeList GetChallengeList() { return Data; }
    protected override ChallengeList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetChallengeArray(msg);
      return new ChallengeList(obj);
    }

  }
  public class MessageWithChallengeEntryList : Message<ChallengeEntryList>
  {
    public MessageWithChallengeEntryList(IntPtr c_message) : base(c_message) { }
    public override ChallengeEntryList GetChallengeEntryList() { return Data; }
    protected override ChallengeEntryList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetChallengeEntryArray(msg);
      return new ChallengeEntryList(obj);
    }

  }
  public class MessageWithDataStoreUnderPrivateUserDataStore : Message<Dictionary<string, string>>
  {
    public MessageWithDataStoreUnderPrivateUserDataStore(IntPtr c_message) : base(c_message) { }
    public override Dictionary<string, string> GetDataStore() { return Data; }
    protected override Dictionary<string, string> GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetDataStore(msg);
      return CAPI.DataStoreFromNative(obj);
    }

  }
  public class MessageWithDataStoreUnderPublicUserDataStore : Message<Dictionary<string, string>>
  {
    public MessageWithDataStoreUnderPublicUserDataStore(IntPtr c_message) : base(c_message) { }
    public override Dictionary<string, string> GetDataStore() { return Data; }
    protected override Dictionary<string, string> GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetDataStore(msg);
      return CAPI.DataStoreFromNative(obj);
    }

  }
  public class MessageWithDestinationList : Message<DestinationList>
  {
    public MessageWithDestinationList(IntPtr c_message) : base(c_message) { }
    public override DestinationList GetDestinationList() { return Data; }
    protected override DestinationList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetDestinationArray(msg);
      return new DestinationList(obj);
    }

  }
  public class MessageWithGroupPresenceJoinIntent : Message<GroupPresenceJoinIntent>
  {
    public MessageWithGroupPresenceJoinIntent(IntPtr c_message) : base(c_message) { }
    public override GroupPresenceJoinIntent GetGroupPresenceJoinIntent() { return Data; }
    protected override GroupPresenceJoinIntent GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetGroupPresenceJoinIntent(msg);
      return new GroupPresenceJoinIntent(obj);
    }

  }
  public class MessageWithGroupPresenceLeaveIntent : Message<GroupPresenceLeaveIntent>
  {
    public MessageWithGroupPresenceLeaveIntent(IntPtr c_message) : base(c_message) { }
    public override GroupPresenceLeaveIntent GetGroupPresenceLeaveIntent() { return Data; }
    protected override GroupPresenceLeaveIntent GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetGroupPresenceLeaveIntent(msg);
      return new GroupPresenceLeaveIntent(obj);
    }

  }
  public class MessageWithInstalledApplicationList : Message<InstalledApplicationList>
  {
    public MessageWithInstalledApplicationList(IntPtr c_message) : base(c_message) { }
    public override InstalledApplicationList GetInstalledApplicationList() { return Data; }
    protected override InstalledApplicationList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetInstalledApplicationArray(msg);
      return new InstalledApplicationList(obj);
    }

  }
  public class MessageWithInvitePanelResultInfo : Message<InvitePanelResultInfo>
  {
    public MessageWithInvitePanelResultInfo(IntPtr c_message) : base(c_message) { }
    public override InvitePanelResultInfo GetInvitePanelResultInfo() { return Data; }
    protected override InvitePanelResultInfo GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetInvitePanelResultInfo(msg);
      return new InvitePanelResultInfo(obj);
    }

  }
  public class MessageWithLaunchBlockFlowResult : Message<LaunchBlockFlowResult>
  {
    public MessageWithLaunchBlockFlowResult(IntPtr c_message) : base(c_message) { }
    public override LaunchBlockFlowResult GetLaunchBlockFlowResult() { return Data; }
    protected override LaunchBlockFlowResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLaunchBlockFlowResult(msg);
      return new LaunchBlockFlowResult(obj);
    }

  }
  public class MessageWithLaunchFriendRequestFlowResult : Message<LaunchFriendRequestFlowResult>
  {
    public MessageWithLaunchFriendRequestFlowResult(IntPtr c_message) : base(c_message) { }
    public override LaunchFriendRequestFlowResult GetLaunchFriendRequestFlowResult() { return Data; }
    protected override LaunchFriendRequestFlowResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLaunchFriendRequestFlowResult(msg);
      return new LaunchFriendRequestFlowResult(obj);
    }

  }
  public class MessageWithLaunchInvitePanelFlowResult : Message<LaunchInvitePanelFlowResult>
  {
    public MessageWithLaunchInvitePanelFlowResult(IntPtr c_message) : base(c_message) { }
    public override LaunchInvitePanelFlowResult GetLaunchInvitePanelFlowResult() { return Data; }
    protected override LaunchInvitePanelFlowResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLaunchInvitePanelFlowResult(msg);
      return new LaunchInvitePanelFlowResult(obj);
    }

  }
  public class MessageWithLaunchReportFlowResult : Message<LaunchReportFlowResult>
  {
    public MessageWithLaunchReportFlowResult(IntPtr c_message) : base(c_message) { }
    public override LaunchReportFlowResult GetLaunchReportFlowResult() { return Data; }
    protected override LaunchReportFlowResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLaunchReportFlowResult(msg);
      return new LaunchReportFlowResult(obj);
    }

  }
  public class MessageWithLaunchUnblockFlowResult : Message<LaunchUnblockFlowResult>
  {
    public MessageWithLaunchUnblockFlowResult(IntPtr c_message) : base(c_message) { }
    public override LaunchUnblockFlowResult GetLaunchUnblockFlowResult() { return Data; }
    protected override LaunchUnblockFlowResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLaunchUnblockFlowResult(msg);
      return new LaunchUnblockFlowResult(obj);
    }

  }
  public class MessageWithLeaderboardList : Message<LeaderboardList>
  {
    public MessageWithLeaderboardList(IntPtr c_message) : base(c_message) { }
    public override LeaderboardList GetLeaderboardList() { return Data; }
    protected override LeaderboardList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLeaderboardArray(msg);
      return new LeaderboardList(obj);
    }

  }
  public class MessageWithLeaderboardEntryList : Message<LeaderboardEntryList>
  {
    public MessageWithLeaderboardEntryList(IntPtr c_message) : base(c_message) { }
    public override LeaderboardEntryList GetLeaderboardEntryList() { return Data; }
    protected override LeaderboardEntryList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLeaderboardEntryArray(msg);
      return new LeaderboardEntryList(obj);
    }

  }
  public class MessageWithLinkedAccountList : Message<LinkedAccountList>
  {
    public MessageWithLinkedAccountList(IntPtr c_message) : base(c_message) { }
    public override LinkedAccountList GetLinkedAccountList() { return Data; }
    protected override LinkedAccountList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLinkedAccountArray(msg);
      return new LinkedAccountList(obj);
    }

  }
  public class MessageWithLivestreamingApplicationStatus : Message<LivestreamingApplicationStatus>
  {
    public MessageWithLivestreamingApplicationStatus(IntPtr c_message) : base(c_message) { }
    public override LivestreamingApplicationStatus GetLivestreamingApplicationStatus() { return Data; }
    protected override LivestreamingApplicationStatus GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLivestreamingApplicationStatus(msg);
      return new LivestreamingApplicationStatus(obj);
    }

  }
  public class MessageWithLivestreamingStartResult : Message<LivestreamingStartResult>
  {
    public MessageWithLivestreamingStartResult(IntPtr c_message) : base(c_message) { }
    public override LivestreamingStartResult GetLivestreamingStartResult() { return Data; }
    protected override LivestreamingStartResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLivestreamingStartResult(msg);
      return new LivestreamingStartResult(obj);
    }

  }
  public class MessageWithLivestreamingStatus : Message<LivestreamingStatus>
  {
    public MessageWithLivestreamingStatus(IntPtr c_message) : base(c_message) { }
    public override LivestreamingStatus GetLivestreamingStatus() { return Data; }
    protected override LivestreamingStatus GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLivestreamingStatus(msg);
      return new LivestreamingStatus(obj);
    }

  }
  public class MessageWithLivestreamingVideoStats : Message<LivestreamingVideoStats>
  {
    public MessageWithLivestreamingVideoStats(IntPtr c_message) : base(c_message) { }
    public override LivestreamingVideoStats GetLivestreamingVideoStats() { return Data; }
    protected override LivestreamingVideoStats GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLivestreamingVideoStats(msg);
      return new LivestreamingVideoStats(obj);
    }

  }
  public class MessageWithMicrophoneAvailabilityState : Message<MicrophoneAvailabilityState>
  {
    public MessageWithMicrophoneAvailabilityState(IntPtr c_message) : base(c_message) { }
    public override MicrophoneAvailabilityState GetMicrophoneAvailabilityState() { return Data; }
    protected override MicrophoneAvailabilityState GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetMicrophoneAvailabilityState(msg);
      return new MicrophoneAvailabilityState(obj);
    }

  }
  public class MessageWithNetSyncConnection : Message<NetSyncConnection>
  {
    public MessageWithNetSyncConnection(IntPtr c_message) : base(c_message) { }
    public override NetSyncConnection GetNetSyncConnection() { return Data; }
    protected override NetSyncConnection GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetNetSyncConnection(msg);
      return new NetSyncConnection(obj);
    }

  }
  public class MessageWithNetSyncSessionList : Message<NetSyncSessionList>
  {
    public MessageWithNetSyncSessionList(IntPtr c_message) : base(c_message) { }
    public override NetSyncSessionList GetNetSyncSessionList() { return Data; }
    protected override NetSyncSessionList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetNetSyncSessionArray(msg);
      return new NetSyncSessionList(obj);
    }

  }
  public class MessageWithNetSyncSessionsChangedNotification : Message<NetSyncSessionsChangedNotification>
  {
    public MessageWithNetSyncSessionsChangedNotification(IntPtr c_message) : base(c_message) { }
    public override NetSyncSessionsChangedNotification GetNetSyncSessionsChangedNotification() { return Data; }
    protected override NetSyncSessionsChangedNotification GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetNetSyncSessionsChangedNotification(msg);
      return new NetSyncSessionsChangedNotification(obj);
    }

  }
  public class MessageWithNetSyncSetSessionPropertyResult : Message<NetSyncSetSessionPropertyResult>
  {
    public MessageWithNetSyncSetSessionPropertyResult(IntPtr c_message) : base(c_message) { }
    public override NetSyncSetSessionPropertyResult GetNetSyncSetSessionPropertyResult() { return Data; }
    protected override NetSyncSetSessionPropertyResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetNetSyncSetSessionPropertyResult(msg);
      return new NetSyncSetSessionPropertyResult(obj);
    }

  }
  public class MessageWithNetSyncVoipAttenuationValueList : Message<NetSyncVoipAttenuationValueList>
  {
    public MessageWithNetSyncVoipAttenuationValueList(IntPtr c_message) : base(c_message) { }
    public override NetSyncVoipAttenuationValueList GetNetSyncVoipAttenuationValueList() { return Data; }
    protected override NetSyncVoipAttenuationValueList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetNetSyncVoipAttenuationValueArray(msg);
      return new NetSyncVoipAttenuationValueList(obj);
    }

  }
  public class MessageWithOrgScopedID : Message<OrgScopedID>
  {
    public MessageWithOrgScopedID(IntPtr c_message) : base(c_message) { }
    public override OrgScopedID GetOrgScopedID() { return Data; }
    protected override OrgScopedID GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetOrgScopedID(msg);
      return new OrgScopedID(obj);
    }

  }
  public class MessageWithParty : Message<Party>
  {
    public MessageWithParty(IntPtr c_message) : base(c_message) { }
    public override Party GetParty() { return Data; }
    protected override Party GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetParty(msg);
      return new Party(obj);
    }

  }
  public class MessageWithPartyUnderCurrentParty : Message<Party>
  {
    public MessageWithPartyUnderCurrentParty(IntPtr c_message) : base(c_message) { }
    public override Party GetParty() { return Data; }
    protected override Party GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetParty(msg);
      return new Party(obj);
    }

  }
  public class MessageWithPartyID : Message<PartyID>
  {
    public MessageWithPartyID(IntPtr c_message) : base(c_message) { }
    public override PartyID GetPartyID() { return Data; }
    protected override PartyID GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetPartyID(msg);
      return new PartyID(obj);
    }

  }
  public class MessageWithPartyUpdateNotification : Message<PartyUpdateNotification>
  {
    public MessageWithPartyUpdateNotification(IntPtr c_message) : base(c_message) { }
    public override PartyUpdateNotification GetPartyUpdateNotification() { return Data; }
    protected override PartyUpdateNotification GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetPartyUpdateNotification(msg);
      return new PartyUpdateNotification(obj);
    }

  }
  public class MessageWithPidList : Message<PidList>
  {
    public MessageWithPidList(IntPtr c_message) : base(c_message) { }
    public override PidList GetPidList() { return Data; }
    protected override PidList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetPidArray(msg);
      return new PidList(obj);
    }

  }
  public class MessageWithProductList : Message<ProductList>
  {
    public MessageWithProductList(IntPtr c_message) : base(c_message) { }
    public override ProductList GetProductList() { return Data; }
    protected override ProductList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetProductArray(msg);
      return new ProductList(obj);
    }

  }
  public class MessageWithPurchase : Message<Purchase>
  {
    public MessageWithPurchase(IntPtr c_message) : base(c_message) { }
    public override Purchase GetPurchase() { return Data; }
    protected override Purchase GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetPurchase(msg);
      return new Purchase(obj);
    }

  }
  public class MessageWithPurchaseList : Message<PurchaseList>
  {
    public MessageWithPurchaseList(IntPtr c_message) : base(c_message) { }
    public override PurchaseList GetPurchaseList() { return Data; }
    protected override PurchaseList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetPurchaseArray(msg);
      return new PurchaseList(obj);
    }

  }
  public class MessageWithRejoinDialogResult : Message<RejoinDialogResult>
  {
    public MessageWithRejoinDialogResult(IntPtr c_message) : base(c_message) { }
    public override RejoinDialogResult GetRejoinDialogResult() { return Data; }
    protected override RejoinDialogResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetRejoinDialogResult(msg);
      return new RejoinDialogResult(obj);
    }

  }
  public class MessageWithSdkAccountList : Message<SdkAccountList>
  {
    public MessageWithSdkAccountList(IntPtr c_message) : base(c_message) { }
    public override SdkAccountList GetSdkAccountList() { return Data; }
    protected override SdkAccountList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetSdkAccountArray(msg);
      return new SdkAccountList(obj);
    }

  }
  public class MessageWithSendInvitesResult : Message<SendInvitesResult>
  {
    public MessageWithSendInvitesResult(IntPtr c_message) : base(c_message) { }
    public override SendInvitesResult GetSendInvitesResult() { return Data; }
    protected override SendInvitesResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetSendInvitesResult(msg);
      return new SendInvitesResult(obj);
    }

  }
  public class MessageWithShareMediaResult : Message<ShareMediaResult>
  {
    public MessageWithShareMediaResult(IntPtr c_message) : base(c_message) { }
    public override ShareMediaResult GetShareMediaResult() { return Data; }
    protected override ShareMediaResult GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetShareMediaResult(msg);
      return new ShareMediaResult(obj);
    }

  }
  public class MessageWithString : Message<string>
  {
    public MessageWithString(IntPtr c_message) : base(c_message) { }
    public override string GetString() { return Data; }
    protected override string GetDataFromMessage(IntPtr c_message)
    {
      return CAPI.ovr_Message_GetString(c_message);
    }
  }
  public class MessageWithSystemVoipState : Message<SystemVoipState>
  {
    public MessageWithSystemVoipState(IntPtr c_message) : base(c_message) { }
    public override SystemVoipState GetSystemVoipState() { return Data; }
    protected override SystemVoipState GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetSystemVoipState(msg);
      return new SystemVoipState(obj);
    }

  }
  public class MessageWithUser : Message<User>
  {
    public MessageWithUser(IntPtr c_message) : base(c_message) { }
    public override User GetUser() { return Data; }
    protected override User GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetUser(msg);
      return new User(obj);
    }

  }
  public class MessageWithUserList : Message<UserList>
  {
    public MessageWithUserList(IntPtr c_message) : base(c_message) { }
    public override UserList GetUserList() { return Data; }
    protected override UserList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetUserArray(msg);
      return new UserList(obj);
    }

  }
  public class MessageWithUserCapabilityList : Message<UserCapabilityList>
  {
    public MessageWithUserCapabilityList(IntPtr c_message) : base(c_message) { }
    public override UserCapabilityList GetUserCapabilityList() { return Data; }
    protected override UserCapabilityList GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetUserCapabilityArray(msg);
      return new UserCapabilityList(obj);
    }

  }
  public class MessageWithUserDataStoreUpdateResponse : Message<UserDataStoreUpdateResponse>
  {
    public MessageWithUserDataStoreUpdateResponse(IntPtr c_message) : base(c_message) { }
    public override UserDataStoreUpdateResponse GetUserDataStoreUpdateResponse() { return Data; }
    protected override UserDataStoreUpdateResponse GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetUserDataStoreUpdateResponse(msg);
      return new UserDataStoreUpdateResponse(obj);
    }

  }
  public class MessageWithUserProof : Message<UserProof>
  {
    public MessageWithUserProof(IntPtr c_message) : base(c_message) { }
    public override UserProof GetUserProof() { return Data; }
    protected override UserProof GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetUserProof(msg);
      return new UserProof(obj);
    }

  }
  public class MessageWithUserReportID : Message<UserReportID>
  {
    public MessageWithUserReportID(IntPtr c_message) : base(c_message) { }
    public override UserReportID GetUserReportID() { return Data; }
    protected override UserReportID GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetUserReportID(msg);
      return new UserReportID(obj);
    }

  }

  public class MessageWithLeaderboardDidUpdate : Message<bool>
  {
    public MessageWithLeaderboardDidUpdate(IntPtr c_message) : base(c_message) { }
    public override bool GetLeaderboardDidUpdate() { return Data; }
    protected override bool GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetLeaderboardUpdateStatus(msg);
      return CAPI.ovr_LeaderboardUpdateStatus_GetDidUpdate(obj);
    }
  }

  public class MessageWithHttpTransferUpdate : Message<HttpTransferUpdate>
  {
    public MessageWithHttpTransferUpdate(IntPtr c_message) : base(c_message) {}
    public override HttpTransferUpdate GetHttpTransferUpdate() { return Data; }
    protected override HttpTransferUpdate GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetHttpTransferUpdate(msg);
      return new HttpTransferUpdate(obj);
    }
  }

  public class MessageWithPlatformInitialize : Message<PlatformInitialize>
  {
    public MessageWithPlatformInitialize(IntPtr c_message) : base(c_message) {}
    public override PlatformInitialize GetPlatformInitialize() { return Data; }
    protected override PlatformInitialize GetDataFromMessage(IntPtr c_message)
    {
      var msg = CAPI.ovr_Message_GetNativeMessage(c_message);
      var obj = CAPI.ovr_Message_GetPlatformInitialize(msg);
      return new PlatformInitialize(obj);
    }
  }

}
