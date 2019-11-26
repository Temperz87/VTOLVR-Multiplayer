using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

namespace VTOLVR_Multiplayer
{
    public class Networker : MonoBehaviour
    {
        public static Networker _instance { get; private set; }
        public static bool isHost { get; private set; }
        public enum GameState { Menu,Config,Game};
        public static GameState gameState { get; private set; }
        public static List<CSteamID> players { get; private set; } = new List<CSteamID>();
        public static Dictionary<CSteamID, bool> readyDic { get; private set; } = new Dictionary<CSteamID, bool>();
        public static bool hostReady { get; private set; }
        public static CSteamID hostID { get; private set; }
        private static ulong TestSteamID = 76561198085673453;
        private void Awake()
        {
            if (_instance != null)
                Debug.LogError("There is already a networker in the game!");
            _instance = this;
            gameState = GameState.Menu;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            switch (arg0.buildIndex)
            {
                case 3: //Vehicle Config Room
                    break;
                case 6: //OpenWater
                case 7: //Akutan
                    StartCoroutine(GameSceneLoaded());
                    break;
                case 11: //CustomMapBase
                default:
                    break;
            }
        }

        private void Update()
        {
            ReadP2P();
        }

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
            SendP2P(new CSteamID(TestSteamID),
                new Message_JoinRequest(PilotSaveManager.currentVehicle.name,
                                        PilotSaveManager.currentScenario.scenarioID,
                                        PilotSaveManager.currentCampaign.campaignID),
                EP2PSend.k_EP2PSendReliable);
        }
        public static void SendGlobalP2P(Message message, EP2PSend sendType)
        {
            for (int i = 0; i < players.Count; i++)
            {
                SendP2P(players[i], message, sendType);
            }
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
                Debug.Log($"Recived P2P from {csteamID.m_SteamID}");
                if (packet.packetType == PacketType.Single)
                {
                    PacketSingle packetS = packet as PacketSingle;
                    switch (packetS.message.type)
                    {
                        case MessageType.None:
                            break;
                        case MessageType.JoinRequest:
                            if (!isHost)
                            {
                                Debug.LogError($"Recived Join Request when we are not the host");
                                //return; //<---- Disable when testing on single client
                            }
                            Message_JoinRequest joinRequest = packetS.message as Message_JoinRequest;
                            if (joinRequest.currentVehicle == PilotSaveManager.currentVehicle.vehicleName &&
                                joinRequest.currentScenario == PilotSaveManager.currentScenario.scenarioID &&
                                joinRequest.currentCampaign == PilotSaveManager.currentCampaign.campaignID)
                            {
                                Debug.Log($"Accepting {csteamID.m_SteamID}");
                                players.Add(csteamID);
                                readyDic.Add(csteamID, false);
                                SendP2P(csteamID, new Message_JoinRequest_Result(true), EP2PSend.k_EP2PSendReliable);
                            }
                            break;
                        case MessageType.JoinRequest_Result:
                            Message_JoinRequest_Result joinResult = packetS.message as Message_JoinRequest_Result;
                            if (joinResult.canJoin)
                            {
                                Debug.Log($"Joining {csteamID.m_SteamID}");
                                hostID = csteamID;
                                StartCoroutine(FlyButton());
                            }
                            else
                                Debug.LogWarning($"We can't join {csteamID.m_SteamID}");
                            break;
                        case MessageType.Ready:
                            //The client has said they are ready to start, so we change it in the dictionary
                            if (readyDic.ContainsKey(csteamID))
                            {
                                Debug.Log($"{csteamID.m_SteamID} has said they are ready!");
                                readyDic[csteamID] = true;
                            }
                            break;
                        case MessageType.Ready_Result:
                            Debug.Log("The host is ready, launching the mission");
                            hostReady = true;
                            FindObjectOfType<VehicleConfigSceneSetup>().LaunchMission();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private IEnumerator FlyButton()
        {
            ControllerEventHandler.PauseEvents();
            ScreenFader.FadeOut(Color.black, 0.85f);
            yield return new WaitForSeconds(1f);
            if (PilotSaveManager.currentScenario.equipConfigurable)
            {
                LoadingSceneController.LoadSceneImmediate("VehicleConfiguration");
            }
            else
            {
                BGMManager.FadeOut(2f);
                Loadout loadout = new Loadout();
                loadout.normalizedFuel = PilotSaveManager.currentScenario.forcedFuel;
                loadout.hpLoadout = new string[PilotSaveManager.currentVehicle.hardpointCount];
                loadout.cmLoadout = new int[]
                {
                99999,
                99999
                };
                if (PilotSaveManager.currentScenario.forcedEquips != null)
                {
                    foreach (CampaignScenario.ForcedEquip forcedEquip in PilotSaveManager.currentScenario.forcedEquips)
                    {
                        loadout.hpLoadout[forcedEquip.hardpointIdx] = forcedEquip.weaponName;
                    }
                }
                VehicleEquipper.loadout = loadout;
                if (PilotSaveManager.currentCampaign.isCustomScenarios)
                {
                    VTScenario.LaunchScenario(VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign), false);
                }
                else
                {
                    LoadingSceneController.LoadScene(PilotSaveManager.currentScenario.mapSceneName);
                }
            }
            ControllerEventHandler.UnpauseEvents();
        }
        //Checks if everyone had sent the Ready Message Type saying they are ready in the vehicle config room
        public static bool EveryoneElseReady()
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!readyDic[players[i]])
                    return false;
            }
            return true;
        }
        private IEnumerator GameSceneLoaded()
        {
            yield return new WaitForSeconds(2);
        }
    }
}
