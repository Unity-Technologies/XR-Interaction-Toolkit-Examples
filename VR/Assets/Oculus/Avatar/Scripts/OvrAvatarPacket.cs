using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;

public class OvrAvatarPacket
{
    // Used with SDK driven packet flow
    public IntPtr ovrNativePacket = IntPtr.Zero;

    // ===============================================================
    // All code below used for unity only pose blending option.
    // ===============================================================
    List<float> frameTimes = new List<float>();
    List<OvrAvatarDriver.PoseFrame> frames = new List<OvrAvatarDriver.PoseFrame>();
    List<byte[]> encodedAudioPackets = new List<byte[]>();

    public float Duration { get { return frameTimes[frameTimes.Count - 1]; } }
    public OvrAvatarDriver.PoseFrame FinalFrame { get { return frames[frames.Count - 1]; } }

    public OvrAvatarPacket()
    {
    }

    public OvrAvatarPacket(OvrAvatarDriver.PoseFrame initialPose)
    {
        frameTimes.Add(0.0f);
        frames.Add(initialPose);
    }

    OvrAvatarPacket(List<float> frameTimes, List<OvrAvatarDriver.PoseFrame> frames, List<byte[]> audioPackets)
    {
        this.frameTimes = frameTimes;
        this.frames = frames;
    }

    public void AddFrame(OvrAvatarDriver.PoseFrame frame, float deltaSeconds)
    {
        frameTimes.Add(Duration + deltaSeconds);
        frames.Add(frame);
    }

    public OvrAvatarDriver.PoseFrame GetPoseFrame(float seconds)
    {
        if (frames.Count == 1)
        {
            return frames[0];
        }

        // This can be replaced with a more efficient binary search
        int tailIndex = 1;
        while (tailIndex < frameTimes.Count && frameTimes[tailIndex] < seconds)
        {
            ++tailIndex;
        }
        OvrAvatarDriver.PoseFrame a = frames[tailIndex - 1];
        OvrAvatarDriver.PoseFrame b = frames[tailIndex];
        float aTime = frameTimes[tailIndex - 1];
        float bTime = frameTimes[tailIndex];
        float t = (seconds - aTime) / (bTime - aTime);
        return OvrAvatarDriver.PoseFrame.Interpolate(a, b, t);
    }

    public static OvrAvatarPacket Read(Stream stream)
    {
        BinaryReader reader = new BinaryReader(stream);

        // Todo: bounds check frame count
        int frameCount = reader.ReadInt32();
        List<float> frameTimes = new List<float>(frameCount);
        for (int i = 0; i < frameCount; ++i)
        {
            frameTimes.Add(reader.ReadSingle());
        }
        List<OvrAvatarDriver.PoseFrame> frames = new List<OvrAvatarDriver.PoseFrame>(frameCount);
        for (int i = 0; i < frameCount; ++i)
        {
            frames.Add(reader.ReadPoseFrame());
        }

        // Todo: bounds check audio packet count
        int audioPacketCount = reader.ReadInt32();
        List<byte[]> audioPackets = new List<byte[]>(audioPacketCount);
        for (int i = 0; i < audioPacketCount; ++i)
        {
            int audioPacketSize = reader.ReadInt32();
            byte[] audioPacket = reader.ReadBytes(audioPacketSize);
            audioPackets.Add(audioPacket);
        }

        return new OvrAvatarPacket(frameTimes, frames, audioPackets);
    }

    public void Write(Stream stream)
    {
        BinaryWriter writer = new BinaryWriter(stream);

        // Write all of the frames
        int frameCount = frameTimes.Count;
        writer.Write(frameCount);
        for (int i = 0; i < frameCount; ++i)
        {
            writer.Write(frameTimes[i]);
        }
        for (int i = 0; i < frameCount; ++i)
        {
            OvrAvatarDriver.PoseFrame frame = frames[i];
            writer.Write(frame);
        }

        // Write all of the encoded audio packets
        int audioPacketCount = encodedAudioPackets.Count;
        writer.Write(audioPacketCount);
        for (int i = 0; i < audioPacketCount; ++i)
        {
            byte[] packet = encodedAudioPackets[i];
            writer.Write(packet.Length);
            writer.Write(packet);
        }
    }
}

static class BinaryWriterExtensions
{
    public static void Write(this BinaryWriter writer, OvrAvatarDriver.PoseFrame frame)
    {
        writer.Write(frame.headPosition);
        writer.Write(frame.headRotation);
        writer.Write(frame.handLeftPosition);
        writer.Write(frame.handLeftRotation);
        writer.Write(frame.handRightPosition);
        writer.Write(frame.handRightRotation);
        writer.Write(frame.voiceAmplitude);

        writer.Write(frame.controllerLeftPose);
        writer.Write(frame.controllerRightPose);
    }

    public static void Write(this BinaryWriter writer, Vector3 vec3)
    {
        writer.Write(vec3.x);
        writer.Write(vec3.y);
        writer.Write(vec3.z);
    }

    public static void Write(this BinaryWriter writer, Vector2 vec2)
    {
        writer.Write(vec2.x);
        writer.Write(vec2.y);
    }

    public static void Write(this BinaryWriter writer, Quaternion quat)
    {
        writer.Write(quat.x);
        writer.Write(quat.y);
        writer.Write(quat.z);
        writer.Write(quat.w);
    }
    public static void Write(this BinaryWriter writer, OvrAvatarDriver.ControllerPose pose)
    {
        writer.Write((uint)pose.buttons);
        writer.Write((uint)pose.touches);
        writer.Write(pose.joystickPosition);
        writer.Write(pose.indexTrigger);
        writer.Write(pose.handTrigger);
        writer.Write(pose.isActive);
    }
}

static class BinaryReaderExtensions
{
    public static OvrAvatarDriver.PoseFrame ReadPoseFrame(this BinaryReader reader)
    {
        return new OvrAvatarDriver.PoseFrame
        {
            headPosition = reader.ReadVector3(),
            headRotation = reader.ReadQuaternion(),
            handLeftPosition = reader.ReadVector3(),
            handLeftRotation = reader.ReadQuaternion(),
            handRightPosition = reader.ReadVector3(),
            handRightRotation = reader.ReadQuaternion(),
            voiceAmplitude = reader.ReadSingle(),

            controllerLeftPose = reader.ReadControllerPose(),
            controllerRightPose = reader.ReadControllerPose(),
        };
    }

    public static Vector2 ReadVector2(this BinaryReader reader)
    {
        return new Vector2
        {
            x = reader.ReadSingle(),
            y = reader.ReadSingle()
        };
    }

    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        return new Vector3
        {
            x = reader.ReadSingle(),
            y = reader.ReadSingle(),
            z = reader.ReadSingle()
        };
    }

    public static Quaternion ReadQuaternion(this BinaryReader reader)
    {
        return new Quaternion
        {
            x = reader.ReadSingle(),
            y = reader.ReadSingle(),
            z = reader.ReadSingle(),
            w = reader.ReadSingle(),
        };
    }
    public static OvrAvatarDriver.ControllerPose ReadControllerPose(this BinaryReader reader)
    {
        return new OvrAvatarDriver.ControllerPose
        {
            buttons = (ovrAvatarButton)reader.ReadUInt32(),
            touches = (ovrAvatarTouch)reader.ReadUInt32(),
            joystickPosition = reader.ReadVector2(),
            indexTrigger = reader.ReadSingle(),
            handTrigger = reader.ReadSingle(),
            isActive = reader.ReadBoolean(),
        };
    }
}
