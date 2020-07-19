using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Steamworks;

class NetworkSenderThread
{
    private static readonly Lazy<NetworkSenderThread> lazy = new Lazy<NetworkSenderThread>(() => new NetworkSenderThread());
    public static NetworkSenderThread Instance { get { return lazy.Value; } }
    
    private NetworkSenderThread() {
        waitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        messageQueue = new ConcurrentQueue<OutgoingNetworkPacketContainer>();

        packetSingle = new PacketSingle();

        newPlayerQueue = new ConcurrentQueue<CSteamID>();
        internalPlayerList = new List<CSteamID>();

        networkThread = new Thread(ThreadMethod);

        dumpAllExistingPlayers = false;

        networkThread.Start();
    }

    private class OutgoingNetworkPacketContainer
    {
        public OutgoingNetworkPacketContainer(CSteamID receiver, Message message, EP2PSend packetType) {
            hostToAllClientsPacket = false;
            SteamId = receiver;
            Message = message;
            PacketType = packetType;
        }

        public OutgoingNetworkPacketContainer(Message message, EP2PSend packetType) {
            hostToAllClientsPacket = true;
            Message = message;
            PacketType = packetType;
        }

        public bool IsHostToAllClientsPacket() {
            return hostToAllClientsPacket;
        }

        private bool hostToAllClientsPacket;
        public CSteamID SteamId;
        public Message Message;
        public EP2PSend PacketType;
    }

    private readonly Thread networkThread;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<OutgoingNetworkPacketContainer> messageQueue;
    private readonly PacketSingle packetSingle;

    private readonly ConcurrentQueue<CSteamID> newPlayerQueue;
    private readonly List<CSteamID> internalPlayerList;

    private readonly Lazy<BinaryFormatter> binaryFormatterLazy = new Lazy<BinaryFormatter>(() => new BinaryFormatter());
    private BinaryFormatter BinaryFormatter { get { return binaryFormatterLazy.Value; } }
    private readonly Lazy<MemoryStream> memoryStreamLazy = new Lazy<MemoryStream>(() => new MemoryStream());
    private MemoryStream MemoryStream { get { return memoryStreamLazy.Value; } }

    private bool dumpAllExistingPlayers;

    public void SendPacketAsHostToAllClients(Message message, EP2PSend packetType) {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(message, packetType);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void SendPacketToSpecificPlayer(CSteamID receiver, Message message, EP2PSend packetType) {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(receiver, message, packetType);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void DumpAllExistingPlayers() {
        dumpAllExistingPlayers = true;
    }

    private void ThreadMethod() {
        while (true) {
            waitHandle.Reset();

            while (newPlayerQueue.TryDequeue(out CSteamID newPlayer)) {
                if (!internalPlayerList.Contains(newPlayer)) {
                    internalPlayerList.Add(newPlayer);
                }
            }

            if (dumpAllExistingPlayers) {
                internalPlayerList.Clear();
            }

            while (messageQueue.TryDequeue(out OutgoingNetworkPacketContainer packet)) {
                // Null checks first
                if (packet.Message == null) {
                    // Skip this one
                    continue;
                }

                if (packet.IsHostToAllClientsPacket()) {
                    if (!Networker.isHost) {
                        //Debug.LogError("Can't send global P2P as user isn't host");
                        continue;
                    }
                    if (Multiplayer.SoloTesting) {
                        continue;
                    }

                    // Now that we're going to try to send the packets, format the outgoing memory ONCE
                    packetSingle.message = packet.Message;
                    packetSingle.sendType = packet.PacketType;
                    BinaryFormatter.Serialize(MemoryStream, packetSingle);
                    byte[] memoryStreamArray = MemoryStream.ToArray();

                    foreach (CSteamID player in internalPlayerList) {
                        SendP2P(player, memoryStreamArray, packetSingle.sendType);
                    }
                }
                else {
                    // Check for valid id
                    if (packet.SteamId == null) {
                        // Skip this one
                        continue;
                    }

                    packetSingle.message = packet.Message;
                    packetSingle.sendType = packet.PacketType;
                    BinaryFormatter.Serialize(MemoryStream, packetSingle);
                    byte[] memoryStreamArray = MemoryStream.ToArray();

                    SendP2P(packet.SteamId, memoryStreamArray, packetSingle.sendType);
                }
            }

            waitHandle.WaitOne();
        }
    }

    private void SendP2P(CSteamID remoteID, byte[] serializedPacketData, EP2PSend sendType) {
        if (serializedPacketData.Length > 1200 && (sendType == EP2PSend.k_EP2PSendUnreliable || sendType == EP2PSend.k_EP2PSendUnreliableNoDelay)) {
            //Debug.LogError("MORE THAN 1200 Bytes for message");
        }
        if (Multiplayer.SoloTesting) {
            //This skips sending the network message and gets sent right to ReadP2PPacket so that we can test solo with a fake player.
            //_instance.ReadP2PPacket(memoryStream.ToArray(), 0, 0, new CSteamID(1));
            return;
        }
        if (!SteamNetworking.SendP2PPacket(remoteID, serializedPacketData, (uint)MemoryStream.Length, sendType)) {
            //Debug.Log($"Failed to send P2P to {remoteID.m_SteamID}");
        }
    }
}
