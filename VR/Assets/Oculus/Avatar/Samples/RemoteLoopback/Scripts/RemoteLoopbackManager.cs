using UnityEngine;
using System.Collections;
using System;
using System.IO;
using Oculus.Avatar;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class RemoteLoopbackManager : MonoBehaviour
{
    class PacketLatencyPair
    {
        public byte[] PacketData;
        public float FakeLatency;
    };

    public OvrAvatar LocalAvatar;
    public OvrAvatar LoopbackAvatar;

    [System.Serializable]
    public class SimulatedLatencySettings
    {
        [Range(0.0f, 0.5f)]
        public float FakeLatencyMax = 0.25f; //250 ms max latency

        [Range(0.0f, 0.5f)]
        public float FakeLatencyMin = 0.002f; //2ms min latency

        [Range(0.0f, 1.0f)]
        public float LatencyWeight = 0.25f;  // How much the latest sample impacts the current latency

        [Range(0,10)]
        public int MaxSamples = 4; //How many samples in our window

        internal float AverageWindow = 0f;
        internal float LatencySum = 0f;
        internal LinkedList<float> LatencyValues = new LinkedList<float>();

        public float NextValue()
        {
            AverageWindow = LatencySum / (float)LatencyValues.Count;
            float RandomLatency = UnityEngine.Random.Range(FakeLatencyMin, FakeLatencyMax);
            float FakeLatency = AverageWindow * (1f - LatencyWeight) + LatencyWeight * RandomLatency;

            if (LatencyValues.Count >= MaxSamples)
            {
                LatencySum -= LatencyValues.First.Value;
                LatencyValues.RemoveFirst();
            }

            LatencySum += FakeLatency;
            LatencyValues.AddLast(FakeLatency);

            return FakeLatency;
        }
    };

    public SimulatedLatencySettings LatencySettings = new SimulatedLatencySettings();

    private int PacketSequence = 0;

    LinkedList<PacketLatencyPair> packetQueue = new LinkedList<PacketLatencyPair>();

    void Start()
    {
        LocalAvatar.RecordPackets = true;
        LocalAvatar.PacketRecorded += OnLocalAvatarPacketRecorded;
        float FirstValue = UnityEngine.Random.Range(LatencySettings.FakeLatencyMin, LatencySettings.FakeLatencyMax);
        LatencySettings.LatencyValues.AddFirst(FirstValue);
        LatencySettings.LatencySum += FirstValue;
    }

    void OnLocalAvatarPacketRecorded(object sender, OvrAvatar.PacketEventArgs args)
    {
        using (MemoryStream outputStream = new MemoryStream())
        {
            BinaryWriter writer = new BinaryWriter(outputStream);

            if (LocalAvatar.UseSDKPackets)
            {
                var size = CAPI.ovrAvatarPacket_GetSize(args.Packet.ovrNativePacket);
                byte[] data = new byte[size];
                CAPI.ovrAvatarPacket_Write(args.Packet.ovrNativePacket, size, data);

                writer.Write(PacketSequence++);
                writer.Write(size);
                writer.Write(data);
            }
            else
            {
                writer.Write(PacketSequence++);
                args.Packet.Write(outputStream);
            }

            SendPacketData(outputStream.ToArray());
        }
    }

    void Update()
    {
        if (packetQueue.Count > 0)
        {
            List<PacketLatencyPair> deadList = new List<PacketLatencyPair>();
            foreach (var packet in packetQueue)
            {
                packet.FakeLatency -= Time.deltaTime;

                if (packet.FakeLatency < 0f)
                {
                    ReceivePacketData(packet.PacketData);
                    deadList.Add(packet);
                }
            }

            foreach (var packet in deadList)
            {
                packetQueue.Remove(packet);
            }
        }
    }
    
    void SendPacketData(byte[] data)
    {
        PacketLatencyPair PacketPair = new PacketLatencyPair();
        PacketPair.PacketData = data;
        PacketPair.FakeLatency = LatencySettings.NextValue();

        packetQueue.AddLast(PacketPair);
    }

    void ReceivePacketData(byte[] data)
    {
        using (MemoryStream inputStream = new MemoryStream(data))
        {
            BinaryReader reader = new BinaryReader(inputStream);
            int sequence = reader.ReadInt32();

            OvrAvatarPacket avatarPacket;
            if (LoopbackAvatar.UseSDKPackets)
            {
                int size = reader.ReadInt32();
                byte[] sdkData = reader.ReadBytes(size);

                IntPtr packet = CAPI.ovrAvatarPacket_Read((UInt32)data.Length, sdkData);
                avatarPacket = new OvrAvatarPacket { ovrNativePacket = packet };
            }
            else
            {
                avatarPacket = OvrAvatarPacket.Read(inputStream);
            }

            LoopbackAvatar.GetComponent<OvrAvatarRemoteDriver>().QueuePacket(sequence, avatarPacket);
        }
    }
}
