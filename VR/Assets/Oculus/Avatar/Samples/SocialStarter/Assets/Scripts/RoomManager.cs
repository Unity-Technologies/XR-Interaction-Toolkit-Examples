using UnityEngine;
using System;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;

// Helper class to manage Room creation, membership and invites.
// Rooms are a mechanism to help Oculus users create a shared experience.
// Users can only be in one Room at a time.  If the Owner of a room
// leaves, then ownership is transferred to some other member.
// Here we use rooms to create the notion of a 'call' to help us
// invite a Friend and establish a VOIP and P2P connection.
public class RoomManager
{
    // the ID of the Room that I'm in
    public ulong roomID;

    // the ID of the Room that I'm invited to
    private ulong invitedRoomID;

    // Am I the server?
    private bool amIServer;

    // Have we already gone through the startup?
    private bool startupDone;

    public RoomManager()
    {
        amIServer = false;
        startupDone = false;
        Rooms.SetRoomInviteAcceptedNotificationCallback(AcceptingInviteCallback);
        Rooms.SetUpdateNotificationCallback(RoomUpdateCallback);
    }

    #region Launched Application from Accepting Invite

    // Callback to check whether the User accepted an invite
    void AcceptingInviteCallback(Message<string> msg)
    {
        if (msg.IsError)
        {
            SocialPlatformManager.TerminateWithError(msg);
            return;
        }

        SocialPlatformManager.LogOutput("Launched Invite to join Room: " + msg.Data);

        invitedRoomID = Convert.ToUInt64(msg.GetString());

        if (startupDone)
        {
            CheckForInvite();
        }
    }

    // Check to see if the App was launched by accepting the Notication from the main Oculus app.
    // If so, we can directly join that room.  (If it's still available.)
    public bool CheckForInvite()
    {
        startupDone = true;

        if (invitedRoomID != 0)
        {
            JoinExistingRoom(invitedRoomID);
            return true;
        }
        else
        {
            return false;
        }
    }

    #endregion

    #region Create a Room and Invite Friend(s) from the Oculus Universal Menu

    public void CreateRoom()
    {
        Rooms.CreateAndJoinPrivate(RoomJoinPolicy.FriendsOfOwner, 4, true)
             .OnComplete(CreateAndJoinPrivateRoomCallback);
    }

    void CreateAndJoinPrivateRoomCallback(Message<Oculus.Platform.Models.Room> msg)
    {
        if (msg.IsError)
        {
            SocialPlatformManager.TerminateWithError(msg);
            return;
        }

        roomID = msg.Data.ID;

        if (msg.Data.OwnerOptional != null && msg.Data.OwnerOptional.ID == SocialPlatformManager.MyID)
        {
            amIServer = true;
        }
        else
        {
            amIServer = false;
        }

        SocialPlatformManager.TransitionToState(SocialPlatformManager.State.WAITING_IN_A_ROOM);
        SocialPlatformManager.SetFloorColorForState(amIServer);
    }

    void OnLaunchInviteWorkflowComplete(Message msg)
    {
        if (msg.IsError)
        {
            SocialPlatformManager.TerminateWithError(msg);
            return;
        }
    }

    #endregion

    #region Accept Invite

    public void JoinExistingRoom(ulong roomID)
    {
        SocialPlatformManager.TransitionToState(SocialPlatformManager.State.JOINING_A_ROOM);
        Rooms.Join(roomID, true).OnComplete(JoinRoomCallback);
    }

    void JoinRoomCallback(Message<Oculus.Platform.Models.Room> msg)
    {
        if (msg.IsError)
        {
            // is reasonable if caller called more than 1 person, and I didn't answer first
            return;
        }

        var ownerOculusId = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "null";
        var userCount = msg.Data.UsersOptional != null ? msg.Data.UsersOptional.Count : 0;

        SocialPlatformManager.LogOutput("Joined Room " + msg.Data.ID + " owner: " + ownerOculusId + " count: " + userCount);
        roomID = msg.Data.ID;
        ProcessRoomData(msg);
    }

    #endregion

    #region Room Updates

    void RoomUpdateCallback(Message<Oculus.Platform.Models.Room> msg)
    {
        if (msg.IsError)
        {
            SocialPlatformManager.TerminateWithError(msg);
            return;
        }

        var ownerOculusId = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "null";
        var userCount = msg.Data.UsersOptional != null ? msg.Data.UsersOptional.Count : 0;

        SocialPlatformManager.LogOutput("Room Update " + msg.Data.ID + " owner: " + ownerOculusId + " count: " + userCount);
        ProcessRoomData(msg);
    }

    #endregion

    #region Room Exit

    public void LeaveCurrentRoom()
    {
        if (roomID != 0)
        {
            Rooms.Leave(roomID);
            roomID = 0;
        }
        SocialPlatformManager.TransitionToState(SocialPlatformManager.State.LEAVING_A_ROOM);
    }

    #endregion

    #region Process Room Data

    void ProcessRoomData(Message<Oculus.Platform.Models.Room> msg)
    {
        if (msg.Data.OwnerOptional != null && msg.Data.OwnerOptional.ID == SocialPlatformManager.MyID)
        {
            amIServer = true;
        }
        else
        {
            amIServer = false;
        }

        // if the caller left while I was in the process of joining, just use that as our new room
        if (msg.Data.UsersOptional != null && msg.Data.UsersOptional.Count == 1)
        {
            SocialPlatformManager.TransitionToState(SocialPlatformManager.State.WAITING_IN_A_ROOM);
        }
        else
        {
            SocialPlatformManager.TransitionToState(SocialPlatformManager.State.CONNECTED_IN_A_ROOM);
        }

        // Look for users that left
        SocialPlatformManager.MarkAllRemoteUsersAsNotInRoom();

        if (msg.Data.UsersOptional != null)
        {
            foreach (User user in msg.Data.UsersOptional)
            {
                if (user.ID != SocialPlatformManager.MyID)
                {
                    if (!SocialPlatformManager.IsUserInRoom(user.ID))
                    {
                        SocialPlatformManager.AddRemoteUser(user.ID);
                    }
                    else
                    {
                        SocialPlatformManager.MarkRemoteUserInRoom(user.ID);
                    }
                }
            }
        }

        SocialPlatformManager.ForgetRemoteUsersNotInRoom();
        SocialPlatformManager.SetFloorColorForState(amIServer);
    }

    #endregion
}
