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

    private NetworkSenderThread()
    {
        waitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        messageQueue = new ConcurrentQueue<OutgoingNetworkPacketContainer>();

        packetSingle = new PacketSingle();

        binaryFormatter = new BinaryFormatter();

        newPlayerQueue = new ConcurrentQueue<CSteamID>();
        removePlayerQueue = new ConcurrentQueue<CSteamID>();
        internalPlayerList = new List<CSteamID>();

        networkThread = new Thread(ThreadMethod);

        dumpAllExistingPlayers = false;

        networkThread.IsBackground = true;
        networkThread.Start();

        networkThread.Priority = ThreadPriority.AboveNormal;
    }

    private class OutgoingNetworkPacketContainer
    {
        public enum OutgoingReceivers
        {
            ToSinglePeer,
            HostToAllClients,
            HostToAllButOneSpecificClient,
        }

        public OutgoingNetworkPacketContainer(object message, EP2PSend packetType)
        {
            outgoingReceivers = OutgoingReceivers.HostToAllClients;
            Message = message;
            PacketType = packetType;
        }

        public OutgoingNetworkPacketContainer(CSteamID receiver, object message, EP2PSend packetType, OutgoingReceivers outgoingReceivers)
        {
            this.outgoingReceivers = outgoingReceivers;
            SteamId = receiver;
            Message = message;
            PacketType = packetType;
        }

        public OutgoingReceivers ToWhichReceivers()
        {
            return outgoingReceivers;
        }

        private readonly OutgoingReceivers outgoingReceivers;
        public CSteamID SteamId;
        public object Message;
        public EP2PSend PacketType;
    }

    private readonly Thread networkThread;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<OutgoingNetworkPacketContainer> messageQueue;
    private readonly PacketSingle packetSingle;

    private readonly ConcurrentQueue<CSteamID> newPlayerQueue;
    private readonly ConcurrentQueue<CSteamID> removePlayerQueue;
    private readonly List<CSteamID> internalPlayerList;

    private BinaryFormatter binaryFormatter;

    private bool dumpAllExistingPlayers;
    private readonly object dumpAllExistingPlayersLock = new object();

    public void SendPacketAsHostToAllClients(Message message, EP2PSend packetType)
    {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(message, packetType);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void SendPacketAsHostToAllClients(Packet existingPacket, EP2PSend packetType)
    {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(existingPacket, packetType);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void SendPacketAsHostToAllButOneSpecificClient(CSteamID nonReceiver, Message message, EP2PSend packetType)
    {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(nonReceiver, message, packetType, OutgoingNetworkPacketContainer.OutgoingReceivers.HostToAllButOneSpecificClient);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void SendPacketAsHostToAllButOneSpecificClient(CSteamID nonReceiver, Packet existingPacket, EP2PSend packetType)
    {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(nonReceiver, existingPacket, packetType, OutgoingNetworkPacketContainer.OutgoingReceivers.HostToAllButOneSpecificClient);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void SendPacketToSpecificPlayer(CSteamID receiver, Message message, EP2PSend packetType)
    {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(receiver, message, packetType, OutgoingNetworkPacketContainer.OutgoingReceivers.ToSinglePeer);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void SendPacketToSpecificPlayer(CSteamID receiver, Packet existingPacket, EP2PSend packetType)
    {
        OutgoingNetworkPacketContainer packet = new OutgoingNetworkPacketContainer(receiver, existingPacket, packetType, OutgoingNetworkPacketContainer.OutgoingReceivers.ToSinglePeer);
        messageQueue.Enqueue(packet);
        waitHandle.Set();
    }

    public void AddPlayer(CSteamID player)
    {
        newPlayerQueue.Enqueue(player);
        waitHandle.Set();
    }

    public void RemovePlayer(CSteamID player)
    {
        removePlayerQueue.Enqueue(player);
        waitHandle.Set();
    }

    public void DumpAllExistingPlayers()
    {
        lock (dumpAllExistingPlayersLock)
        {
            dumpAllExistingPlayers = true;
        }
        waitHandle.Set();
    }

    private void ThreadMethod()
    {
        byte[] memoryStreamArray;
        bool dumpAllExistingPlayersLocal;
        uint length;

        while (true)
        {
            waitHandle.Reset();

            while (newPlayerQueue.TryDequeue(out CSteamID newPlayer))
            {
                if (!internalPlayerList.Contains(newPlayer))
                {
                    internalPlayerList.Add(newPlayer);
                }
            }

            while (removePlayerQueue.TryDequeue(out CSteamID removePlayer))
            {
                if (internalPlayerList.Contains(removePlayer))
                {
                    internalPlayerList.Remove(removePlayer);
                }
            }

            lock (dumpAllExistingPlayersLock)
            {
                dumpAllExistingPlayersLocal = dumpAllExistingPlayers;
            }
            if (dumpAllExistingPlayersLocal)
            {
                lock (dumpAllExistingPlayersLock)
                {
                    dumpAllExistingPlayers = false;
                }
                internalPlayerList.Clear();
            }

            while (messageQueue.TryDequeue(out OutgoingNetworkPacketContainer outgoingData))
            {
                // Null checks first
                if (outgoingData.Message == null)
                {
                    // Skip this one
                    continue;
                }

                if (outgoingData.ToWhichReceivers() == OutgoingNetworkPacketContainer.OutgoingReceivers.HostToAllClients || outgoingData.ToWhichReceivers() == OutgoingNetworkPacketContainer.OutgoingReceivers.HostToAllButOneSpecificClient)
                {
                    if (!Networker.isHost)
                    {
                        //Debug.LogError("Can't send global P2P as user isn't host");
                        continue;
                    }
                    if (Multiplayer.SoloTesting)
                    {
                        continue;
                    }


                    // Now that we're going to try to send the packets, format the outgoing memory ONCE based on packet or message type
                    if (outgoingData.Message is Message message)
                    {
                        memoryStreamArray = getByteArrayFromMessage(message, outgoingData.PacketType, out length);
                    }
                    else if (outgoingData.Message is Packet packet)
                    {
                        memoryStreamArray = getByteArrayFromPacket(packet, out length);
                    }
                    else
                    {
                        // Not a recognized type
                        continue;
                    }

                    if (outgoingData.ToWhichReceivers() == OutgoingNetworkPacketContainer.OutgoingReceivers.HostToAllClients)
                    {
                        foreach (CSteamID player in internalPlayerList)
                        {
                            SendP2P(player, memoryStreamArray, packetSingle.sendType, length);
                        }
                    }
                    else
                    {
                        foreach (CSteamID player in internalPlayerList)
                        {
                            if (player != outgoingData.SteamId)
                            {
                                SendP2P(player, memoryStreamArray, packetSingle.sendType, length);
                            }
                        }
                    }
                }
                else
                {
                    // Send to single client
                    // Check for valid id
                    if (outgoingData.SteamId == null)
                    {
                        // Skip this one
                        continue;
                    }

                    // Now that we're going to try to send the packets, format the outgoing memory ONCE based on packet or message type
                    if (outgoingData.Message is Message message)
                    {
                        memoryStreamArray = getByteArrayFromMessage(message, outgoingData.PacketType, out length);
                    }
                    else if (outgoingData.Message is Packet packet)
                    {
                        memoryStreamArray = getByteArrayFromPacket(packet, out length);
                    }
                    else
                    {
                        // Not a recognized type
                        continue;
                    }

                    SendP2P(outgoingData.SteamId, memoryStreamArray, packetSingle.sendType, length);
                }
            }

            waitHandle.WaitOne();
        }
    }

    private byte[] getByteArrayFromMessage(Message message, EP2PSend packetType, out uint length)
    {
        packetSingle.message = message;
        packetSingle.sendType = packetType;
        MemoryStream memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, packetSingle);
        length = (uint)memoryStream.Length;
        return memoryStream.ToArray();
    }

    private byte[] getByteArrayFromPacket(Packet packet, out uint length)
    {
        MemoryStream memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, packet);
        length = (uint)memoryStream.Length;
        return memoryStream.ToArray();
    }

    private void SendP2P(CSteamID remoteID, byte[] serializedPacketData, EP2PSend sendType, uint length)
    {
        if (serializedPacketData.Length > 1200 && (sendType == EP2PSend.k_EP2PSendUnreliable || sendType == EP2PSend.k_EP2PSendUnreliableNoDelay))
        {
            //Debug.LogError("MORE THAN 1200 Bytes for message");
        }
        if (Multiplayer.SoloTesting)
        {
            //This skips sending the network message and gets sent right to ReadP2PPacket so that we can test solo with a fake player.
            //_instance.ReadP2PPacket(memoryStream.ToArray(), 0, 0, new CSteamID(1));
            return;
        }
        if (!SteamNetworking.SendP2PPacket(remoteID, serializedPacketData, length, sendType))
        {
            //Debug.Log($"Failed to send P2P to {remoteID.m_SteamID}");
        }
    }
}