using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace VTOLVR_Multiplayer
{
    public class Networker : MonoBehaviour
    {
        public static Networker _instance { get; private set; }
        public static bool isHost { get; private set; }
        public enum GameState { Menu,Config,Game};
        public static GameState gameState { get; private set; }
        private void Awake()
        {
            if (_instance != null)
                Debug.LogError("There is already a networker in the game!");
            _instance = this;
            gameState = GameState.Menu;
        }
        private void Update() { ReadP2P(); }

        public static void HostGame()
        {
            if (gameState != GameState.Menu)
            {
                Debug.LogError("Can't host game as already in one");
                return;
            }
            isHost = true;
            FindObjectOfType<MissionBriefingUI>().FlyButton();
        }
        public static void JoinGame()
        {
            if (gameState != GameState.Menu)
            {
                Debug.LogError("Can't join game as already in one");
                return;
            }
            isHost = false;
            FindObjectOfType<MissionBriefingUI>().FlyButton();
        }
        public static void SendP2P(CSteamID remoteID, Message message, EP2PSend sendType)
        {
            PacketSingle packet = new PacketSingle(message, sendType);
            SendP2P(remoteID, packet);
        }
        public static void SendP2P(CSteamID remoteID, Message[] messages, EP2PSend sendType)
        {
            PacketMultiple packet = new PacketMultiple(messages, sendType);
            SendP2P(remoteID, packet);
        }
        private static void SendP2P(CSteamID remoteID,Packet packet)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, packet);
            if (SteamNetworking.SendP2PPacket(remoteID, memoryStream.ToArray(), (uint)memoryStream.Length, packet.sendType))
            {
                Debug.Log($"Sent P2P to {remoteID.m_SteamID}");
            }
            else
            {
                Debug.Log($"Failed to send P2P to {remoteID.m_SteamID}");
            }

        }
        private void ReadP2P()
        {
            uint num;
            while (SteamNetworking.IsP2PPacketAvailable(out num))
            {
                ReadP2PPacket(num);
            }
        }

        private void ReadP2PPacket(uint num)
        {
            byte[] array = new byte[num];
            uint num2;
            CSteamID csteamID;
            if (SteamNetworking.ReadP2PPacket(array, num, out num2, out csteamID, 0))
            {
                MemoryStream serializationStream = new MemoryStream(array);
                Packet packet = new BinaryFormatter().Deserialize(serializationStream) as Packet;
                Debug.Log($"Recived P2P from {csteamID.m_SteamID} as {packet.packetType.ToString()}");
            }
        }
    }
}
