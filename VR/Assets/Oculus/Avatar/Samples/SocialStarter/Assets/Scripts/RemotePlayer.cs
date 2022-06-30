using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Platform;

public class RemotePlayer
{
    public ulong remoteUserID;
    public bool stillInRoom;

    // the result of the last connection state update message
    public PeerConnectionState p2pConnectionState;
    // the last reported state of the VOIP connection
    public PeerConnectionState voipConnectionState;

    public OvrAvatar RemoteAvatar;

    // the last received root transform position updates, equivalent to local tracking space transform
    public Vector3 receivedRootPosition;

    // the previous received positions to interpolate from
    public Vector3 receivedRootPositionPrior;

    // the last received root transform rotation updates, equivalent to local tracking space transform
    public Quaternion receivedRootRotation;

    // the previous received rotations to interpolate from
    public Quaternion receivedRootRotationPrior;

    // the voip tracker for the player
    public VoipAudioSourceHiLevel voipSource;
}
