using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Oculus.Avatar;

public class OvrAvatarRemoteDriver : OvrAvatarDriver
{
    Queue<OvrAvatarPacket> packetQueue = new Queue<OvrAvatarPacket>();

    IntPtr CurrentSDKPacket = IntPtr.Zero;
    float CurrentPacketTime = 0f;

    const int MinPacketQueue = 1;
    const int MaxPacketQueue = 4;

    int CurrentSequence = -1;

    // Used for legacy Unity only packet blending
    bool isStreaming = false;
    OvrAvatarPacket currentPacket = null;

    public void QueuePacket(int sequence, OvrAvatarPacket packet)
    {
        if (sequence > CurrentSequence)
        {
            CurrentSequence = sequence;
            packetQueue.Enqueue(packet);
        }
    }

    public override void UpdateTransforms(IntPtr sdkAvatar)
    {
        switch(Mode)
        {
            case PacketMode.SDK:
                UpdateFromSDKPacket(sdkAvatar);
                break;
            case PacketMode.Unity:
                UpdateFromUnityPacket(sdkAvatar);
                break;
            default:
                break;
        }
    }

    private void UpdateFromSDKPacket(IntPtr sdkAvatar)
    {

        if (CurrentSDKPacket == IntPtr.Zero && packetQueue.Count >= MinPacketQueue)
        {
            CurrentSDKPacket = packetQueue.Dequeue().ovrNativePacket;
        }

        if (CurrentSDKPacket != IntPtr.Zero)
        {
            float PacketDuration = CAPI.ovrAvatarPacket_GetDurationSeconds(CurrentSDKPacket);
            CAPI.ovrAvatar_UpdatePoseFromPacket(sdkAvatar, CurrentSDKPacket, Mathf.Min(PacketDuration, CurrentPacketTime));
            CurrentPacketTime += Time.deltaTime;

            if (CurrentPacketTime > PacketDuration)
            {
                CAPI.ovrAvatarPacket_Free(CurrentSDKPacket);
                CurrentSDKPacket = IntPtr.Zero;
                CurrentPacketTime = CurrentPacketTime - PacketDuration;

                //Throw away packets deemed too old.
                while (packetQueue.Count > MaxPacketQueue)
                {
                    packetQueue.Dequeue();
                }
            }
        }
    }

    private void UpdateFromUnityPacket(IntPtr sdkAvatar)
    {
        // If we're not currently streaming, check to see if we've buffered enough
        if (!isStreaming && packetQueue.Count > MinPacketQueue)
        {
            currentPacket = packetQueue.Dequeue();
            isStreaming = true;
        }

        // If we are streaming, update our pose
        if (isStreaming)
        {
            CurrentPacketTime += Time.deltaTime;

            // If we've elapsed past our current packet, advance
            while (CurrentPacketTime > currentPacket.Duration)
            {

                // If we're out of packets, stop streaming and
                // lock to the final frame
                if (packetQueue.Count == 0)
                {
                    CurrentPose = currentPacket.FinalFrame;
                    CurrentPacketTime = 0.0f;
                    currentPacket = null;
                    isStreaming = false;
                    return;
                }

                while (packetQueue.Count > MaxPacketQueue)
                {
                    packetQueue.Dequeue();
                }

                // Otherwise, dequeue the next packet
                CurrentPacketTime -= currentPacket.Duration;
                currentPacket = packetQueue.Dequeue();
            }

            // Compute the pose based on our current time offset in the packet
            CurrentPose = currentPacket.GetPoseFrame(CurrentPacketTime);

            UpdateTransformsFromPose(sdkAvatar);
        }
    }
}
