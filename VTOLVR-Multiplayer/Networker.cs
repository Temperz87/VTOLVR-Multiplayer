using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;
using TMPro;


public class Networker : MonoBehaviour
{
    public static Networker _instance { get; private set; }
    public static bool isHost { get; private set; }
    public enum GameState { Menu, Config, Game };
    public static GameState gameState { get; private set; }
    public static List<CSteamID> players { get; private set; } = new List<CSteamID>();
    public static Dictionary<CSteamID, bool> readyDic { get; private set; } = new Dictionary<CSteamID, bool>();
    public static bool hostReady, alreadyInGame;
    public static CSteamID hostID { get; private set; }
    private Callback<P2PSessionRequest_t> _p2PSessionRequestCallback;
    //networkUID is used as an identifer for all network object, we are just adding onto this to get a new one
    private static ulong networkUID = 0;
    public static TextMeshPro loadingText;
    #region Message Type Callbacks
    //These callbacks are use for other scripts to know when a network message has been
    //received for them. They should match the name of the message class they relate to.
    public static event UnityAction<Packet, CSteamID> RequestSpawn;
    public static event UnityAction<Packet> RequestSpawn_Result;
    public static event UnityAction<Packet> SpawnVehicle;
    public static event UnityAction<Packet> RigidbodyUpdate;
    public static event UnityAction<Packet> PlaneUpdate;
    public static event UnityAction<Packet> EngineTiltUpdate;
    public static event UnityAction<Packet> Disconnecting;
    public static event UnityAction<Packet> WeaponSet;
    public static event UnityAction<Packet> WeaponSet_Result;
    public static event UnityAction<Packet> WeaponFiring;
    public static event UnityAction<Packet> WeaponStoppedFiring;
    public static event UnityAction<Packet> MissileUpdate;
    public static event UnityAction<Packet> RequestNetworkUID;
    #endregion
    private void Awake()
    {
        if (_instance != null)
            Debug.LogError("There is already a networker in the game!");
        _instance = this;
        gameState = GameState.Menu;
        _p2PSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

        RequestSpawn += PlayerManager.RequestSpawn;
        RequestSpawn_Result += PlayerManager.RequestSpawn_Result;
        SpawnVehicle += PlayerManager.SpawnVehicle;
        VTCustomMapManager.OnLoadedMap += PlayerManager.MapLoaded;
        VTOLAPI.SceneLoaded += SceneChanged;
    }

    private void OnP2PSessionRequest(P2PSessionRequest_t request)
    {
        //Yes this is expecting everyone, even if they are not friends...
        SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
        Debug.Log("Accepting P2P with " + SteamFriends.GetFriendPersonaName(request.m_steamIDRemote));
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
        _instance.StartCoroutine(_instance.FlyButton());
    }
    public static void JoinGame(CSteamID steamID)
    {
        if (gameState != GameState.Menu)
        {
            Debug.LogError("Can't join game as already in one");
            return;
        }
        isHost = false;
        SendP2P(steamID,
            new Message_JoinRequest(PilotSaveManager.currentVehicle.name,
                                    PilotSaveManager.currentScenario.scenarioID,
                                    PilotSaveManager.currentCampaign.campaignID),
            EP2PSend.k_EP2PSendReliable);
    }
    public static void SendExcludeP2P(CSteamID excludeID, Message message, EP2PSend sendType)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != excludeID)
                SendP2P(players[i], message, sendType);
        }
    }
    /// <summary>
    /// Sends a P2P message to all the other players, only works if host.
    /// </summary>
    /// <param name="message">The Message which is being set</param>
    /// <param name="sendType">Specifies how you want the data to be transmitted, such as reliably, unreliable, buffered, etc.</param>
    public static void SendGlobalP2P(Message message, EP2PSend sendType)
    {
        if (!isHost)
        {
            Debug.LogError("Can't send global P2P as user isn't host");
            return;
        }
        if (Multiplayer.SoloTesting)
        {
            SendP2P(new CSteamID(), message, sendType);
            return;
        }
        for (int i = 0; i < players.Count; i++)
        {
            SendP2P(players[i], message, sendType);
        }
    }
    public static void SendGlobalP2P(Packet packet)
    {
        if (!isHost)
        {
            Debug.LogError("Can't send global P2P as user isn't host");
            return;
        }
        if (Multiplayer.SoloTesting)
        {
            SendP2P(new CSteamID(), packet);
            return;
        }
        for (int i = 0; i < players.Count; i++)
        {
            SendP2P(players[i], packet);
        }
    }
    /// <summary>
    /// Sends a P2P Message to another user.
    /// </summary>
    /// <param name="remoteID">The target user to send the packet to.</param>
    /// <param name="message">The message to be send to that user.</param>
    /// <param name="sendType">Specifies how you want the data to be transmitted, such as reliably, unreliable, buffered, etc.</param>
    public static void SendP2P(CSteamID remoteID, Message message, EP2PSend sendType)
    {
        PacketSingle packet = new PacketSingle(message, sendType);
        SendP2P(remoteID, packet);
    }
    /// <summary>
    /// Sends multiple messages to another user. [Currently Doesn't work]
    /// </summary>
    /// <param name="remoteID">The target user to send the packet to.</param>
    /// <param name="messages">The messages to be send to that user.</param>
    /// <param name="sendType">Specifies how you want the data to be transmitted, such as reliably, unreliable, buffered, etc.</param>
    [Obsolete]
    public static void SendP2P(CSteamID remoteID, Message[] messages, EP2PSend sendType)
    {
        PacketMultiple packet = new PacketMultiple(messages, sendType);
        SendP2P(remoteID, packet);
    }
    private static void SendP2P(CSteamID remoteID, Packet packet)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        MemoryStream memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, packet);
        if (memoryStream.ToArray().Length > 1200 && (packet.sendType == EP2PSend.k_EP2PSendUnreliable || packet.sendType == EP2PSend.k_EP2PSendUnreliableNoDelay))
        {
            Debug.LogError("MORE THAN 1200 Bytes for message");
        }
        if (Multiplayer.SoloTesting)
        {
            //This skips sending the network message and gets sent right to ReadP2PPacket so that we can test solo with a fake player.
            _instance.ReadP2PPacket(memoryStream.ToArray(), 0, 0, new CSteamID(1));
            return;
        }
        if (SteamNetworking.SendP2PPacket(remoteID, memoryStream.ToArray(), (uint)memoryStream.Length, packet.sendType))
        {
            
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
            byte[] array = new byte[num];
            uint num2;
            CSteamID csteamID;
            if (SteamNetworking.ReadP2PPacket(array, num, out num2, out csteamID, 0))
            {
                ReadP2PPacket(array, num, num2, csteamID);
            }
        }
    }

    private void ReadP2PPacket(byte[] array, uint num, uint num2, CSteamID csteamID)
    {
        MemoryStream serializationStream = new MemoryStream(array);
        Packet packet = new BinaryFormatter().Deserialize(serializationStream) as Packet;
        if (packet.packetType == PacketType.Single)
        {
            PacketSingle packetS = packet as PacketSingle;
            switch (packetS.message.type)
            {
                case MessageType.None:
                    break;
                case MessageType.LobbyInfoRequest:
                    SendP2P(csteamID,
                        new Message_LobbyInfoRequest_Result(SteamFriends.GetPersonaName(),
                                                                PilotSaveManager.currentVehicle.vehicleName,
                                                                PilotSaveManager.currentScenario.scenarioName,
                                                                PilotSaveManager.currentCampaign.campaignName,
                                                                PlayerManager.players.Count.ToString()),
                        EP2PSend.k_EP2PSendReliable);
                    break;
                case MessageType.LobbyInfoRequest_Result:
                    Message_LobbyInfoRequest_Result result = packetS.message as Message_LobbyInfoRequest_Result;
                    Multiplayer._instance.lobbyInfoText.text = result.username + "'s Game\n" + result.vehicle + "\n" + result.campaign + " " + result.scenario + "\n" + (result.playercount == "1" ? result.playercount + " Player" : result.playercount + " Players");
                    break;
                case MessageType.JoinRequest:
                    if (!isHost)
                    {
                        Debug.LogError($"Recived Join Request when we are not the host");
                        return;
                    }
                    Message_JoinRequest joinRequest = packetS.message as Message_JoinRequest;
                    if (players.Contains(csteamID))
                    {
                        Debug.LogError("The player seemed to send two join requests");
                        return;
                    }
                    if (joinRequest.currentVehicle == PilotSaveManager.currentVehicle.vehicleName &&
                        joinRequest.currentScenario == PilotSaveManager.currentScenario.scenarioID &&
                        joinRequest.currentCampaign == PilotSaveManager.currentCampaign.campaignID)
                    {
                        Debug.Log($"Accepting {csteamID.m_SteamID}");
                        players.Add(csteamID);
                        readyDic.Add(csteamID, false);
                        UpdateLoadingText();
                        SendP2P(csteamID, new Message_JoinRequest_Result(true), EP2PSend.k_EP2PSendReliable);
                    }
                    else
                    {
                        string reason = "Failed to Join Player";
                        if (joinRequest.currentVehicle != PilotSaveManager.currentVehicle.vehicleName)
                            reason += "\nWrong Vehicle.";
                        if (joinRequest.currentScenario != PilotSaveManager.currentScenario.scenarioID)
                            reason += "\nWrong Scenario.";
                        if (joinRequest.currentCampaign != PilotSaveManager.currentCampaign.campaignID)
                            reason += "\nWrong Campaign.";
                        SendP2P(csteamID, new Message_JoinRequest_Result(false, reason), EP2PSend.k_EP2PSendReliable);
                        Debug.Log($"Denied {csteamID}, reason\n{reason}");
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
                    {
                        Debug.LogWarning($"We can't join {csteamID.m_SteamID} reason = \n{joinResult.reason}");
                    }
                    break;
                case MessageType.Ready:
                    //The client has said they are ready to start, so we change it in the dictionary
                    if (readyDic.ContainsKey(csteamID))
                    {
                        Debug.Log($"{csteamID.m_SteamID} has said they are ready!\nHost ready state {hostReady}");
                        readyDic[csteamID] = true;
                        if (alreadyInGame)
                        {
                            //Someone is trying to join when we are already in game.
                            Debug.Log($"We are already in session, {csteamID} is joining in!");
                            SendP2P(csteamID, new Message(MessageType.Ready_Result), EP2PSend.k_EP2PSendReliable);
                            break;
                        }
                        else if (hostReady && EveryoneElseReady())
                        {
                            Debug.Log("The last client has said they are ready, starting");
                            SendGlobalP2P(new Message(MessageType.Ready_Result), EP2PSend.k_EP2PSendReliable);
                            LoadingSceneController.instance.PlayerReady();
                        }
                        UpdateLoadingText();
                    }
                    break;
                case MessageType.Ready_Result:
                    Debug.Log("The host said everyone is ready, launching the mission");
                    hostReady = true;
                    LoadingSceneController.instance.PlayerReady();
                    break;
                case MessageType.RequestSpawn:
                    if (RequestSpawn != null)
                        RequestSpawn.Invoke(packet, csteamID);
                    break;
                case MessageType.RequestSpawn_Result:
                    if (RequestSpawn_Result != null)
                        RequestSpawn_Result.Invoke(packet);
                    break;
                case MessageType.SpawnVehicle:
                    if (SpawnVehicle != null)
                        SpawnVehicle.Invoke(packet);
                    break;
                case MessageType.RigidbodyUpdate:
                    if (RigidbodyUpdate != null)
                        RigidbodyUpdate.Invoke(packet);
                    break;
                case MessageType.PlaneUpdate:
                    if (PlaneUpdate != null)
                        PlaneUpdate.Invoke(packet);
                    break;
                case MessageType.EngineTiltUpdate:
                    if (EngineTiltUpdate != null)
                        EngineTiltUpdate.Invoke(packet);
                    break;
                case MessageType.Disconnecting:
                    
                    if (isHost)
                    {
                        if (Multiplayer.SoloTesting)
                            break;
                        players.Remove(csteamID);
                        SendGlobalP2P(packet);
                    }
                    else
                    {
                        Message_Disconnecting messsage = ((PacketSingle)packet).message as Message_Disconnecting;
                        if (messsage.isHost)
                        {
                            //If it is the host quiting we just need to quit the mission as all networking will be lost.
                            FlightSceneManager flightSceneManager = FindObjectOfType<FlightSceneManager>();
                            if (flightSceneManager == null)
                                Debug.LogError("FlightSceneManager was null when host quit");
                            flightSceneManager.ExitScene();
                        }
                        break;
                    }
                    if (Disconnecting != null)
                        Disconnecting.Invoke(packet);
                    break;
                case MessageType.WeaponsSet:
                    if (WeaponSet != null)
                        WeaponSet.Invoke(packet);
                    break;
                case MessageType.WeaponsSet_Result:
                    if (WeaponSet_Result != null)
                        WeaponSet_Result.Invoke(packet);
                    if (isHost)
                    {
                        SendGlobalP2P(packet);
                    }
                    break;
                case MessageType.WeaponFiring:
                    if (WeaponFiring != null)
                        WeaponFiring.Invoke(packet);
                    break;
                case MessageType.WeaponStoppedFiring:
                    if (WeaponStoppedFiring != null)
                        WeaponStoppedFiring.Invoke(packet);
                    break;
                case MessageType.MissileUpdate:
                    if (MissileUpdate != null)
                        MissileUpdate.Invoke(packet);
                    break;
                case MessageType.RequestNetworkUID:
                    if (RequestNetworkUID != null)
                        RequestNetworkUID.Invoke(packet);
                    break;
                case MessageType.LoadingTextUpdate:
                    if (!isHost)
                        UpdateLoadingText(packet);
                    break;
                default:
                    break;
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

    public static ulong GenerateNetworkUID()
    {
        ulong result = networkUID + 1;
        networkUID = result;
        Debug.Log($"Generated New UID ({result})");
        return result;
    }
    public static void ResetNetworkUID()
    {
        networkUID = 0;
    }
    public static void RequestUID(ulong clientsID)
    {
        if (!isHost)
        {
            SendP2P(hostID, new Message_RequestNetworkUID(clientsID), EP2PSend.k_EP2PSendReliable);
            Debug.Log("Requetsed UID from host");
        }
        else
            Debug.LogError("For some reason the host requested a UID instead of generating one.");
    }

    private void SceneChanged(VTOLScenes scene)
    {
        if (scene == VTOLScenes.ReadyRoom && PlayerManager.gameLoaded)
        {
            Disconnect();
        }
    }

    public static void UpdateLoadingText() //Host Only
    {
        if (!isHost)
            return;
        StringBuilder content = new StringBuilder("Players:\n");
        content.AppendLine(SteamFriends.GetPersonaName() + ": " + (hostReady ? "Ready" : "Not Ready") + "\n");
        for (int i = 0; i < players.Count; i++)
        {
            content.Append(SteamFriends.GetFriendPersonaName(players[i]) + ": " + (readyDic[players[i]]? "Ready": "Not Ready") + "\n");
        }
        if (loadingText != null)
            loadingText.text = content.ToString();

        SendGlobalP2P(new Message_LoadingTextUpdate(content.ToString()), EP2PSend.k_EP2PSendReliable);
    }
    public static void UpdateLoadingText(Packet packet) //Clients Only
    {
        if (isHost)
            return;
        Message_LoadingTextUpdate message = ((PacketSingle)packet).message as Message_LoadingTextUpdate;
        if (loadingText != null)
            loadingText.text = message.content;
        Debug.Log("Updated loading text to \n" + message.content);
    }

    public void OnApplicationQuit()
    {
        if (PlayerManager.gameLoaded)
        {
            Disconnect(true);
        }
    }
    /// <summary>
    /// This will send any messages needed to the host or other players and reset variables.
    /// </summary>
    public void Disconnect(bool applicationClosing = false)
    {
        Debug.Log("Disconnecting from server");
        if (isHost)
        {
            SendGlobalP2P(new Message_Disconnecting(PlayerManager.localUID, true), EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            SendP2P(hostID, new Message_Disconnecting(PlayerManager.localUID, false), EP2PSend.k_EP2PSendReliable);
        }

        if (applicationClosing)
            return;
        isHost = false;
        gameState = GameState.Menu;
        players = new List<CSteamID>();
        readyDic = new Dictionary<CSteamID, bool>();
        hostReady = false;
        alreadyInGame = false;
        hostID = new CSteamID(0);

        PlayerManager.OnDisconnect();
    }
}
