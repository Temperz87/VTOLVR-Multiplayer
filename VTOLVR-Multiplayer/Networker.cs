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
using System.Security.Cryptography;
using TMPro;


static class MapAndScenarioVersionChecker
{
    static private SHA256 hashCalculator = SHA256.Create();
    static private string filePath;

    static public bool builtInCampaign = false;
    static public string scenarioId;
    static public byte[] mapHash;
    static public byte[] scenarioHash;
    static public byte[] campaignHash;
    static public byte[] modloaderHash;
    static public byte[] modHash;
    static public Dictionary<string, string> modsLoadedHashes = new Dictionary<string, string>();


    // Make hashes of the map, scenario and campaign IDs so the server can check that we're loading the right mission
    public static void CreateHashes()
    {
        Debug.Log("Creating Hashes");
        // if (PilotSaveManager.currentCampaign.isBuiltIn)
        if (true)
        {
            // Only need to get the scenario ID in this case
            builtInCampaign = true;
            // Don't send null arrays over network
            mapHash = new byte[0];
            scenarioHash = new byte[0];
            campaignHash = new byte[0];
        }
        else
        {


            Debug.Log("Found custom scenario - setting map hash to 0.");
            mapHash = new byte[0];

            filePath = PilotSaveManager.currentScenario.customScenarioInfo.filePath;
            Debug.Log($"Custom Scenario Location: {filePath}");
            using (FileStream scenarioFile = File.OpenRead(filePath))
            {
                scenarioHash = hashCalculator.ComputeHash(scenarioFile);
            }

            filePath = null;

            if (PilotSaveManager.currentCampaign.campaignID != null)
            {
                Debug.Log("Campaign ID Is not null");


                //VTCampaignInfo campaignInfo = VTResources.GetCustomCampaigns().Find(id => id.campaignID == PilotSaveManager.currentCampaign.campaignID);


                VTCampaignInfo campaignInfo = VTResources.GetSteamWorkshopCampaign(PilotSaveManager.currentCampaign.campaignID);

                if (campaignInfo != null)
                {
                    filePath = campaignInfo.filePath;

                    if (filePath != null)
                    {
                        Debug.Log($"Campaign File path: {filePath}");
                        using (FileStream campaignFile = File.OpenRead(filePath))
                        {
                            campaignHash = hashCalculator.ComputeHash(campaignFile);
                        }
                    }
                    else
                    {
                        Debug.Log("Campaign file path is null, we may not be playing a campaign? Setting the hash to 0.");
                        campaignHash = new byte[0];
                    }
                }
                else
                {
                    Debug.Log("Campaign info is null");
                    campaignHash = new byte[0];
                }
            }
            else
            {
                Debug.Log("Campaign ID is null!");
                campaignHash = new byte[0];
            }

        }
        Debug.Log($"Campaign File Path: {filePath}");
        Debug.Log($"Campaign Hash: {campaignHash}");
        scenarioId = PilotSaveManager.currentScenario.scenarioID;

        Debug.Log("Getting Modloader hash");
        filePath = "VTOLVR_ModLoader\\ModLoader.dll";
        using (FileStream modloaderDLL = File.OpenRead(filePath))
        {
            modloaderHash = hashCalculator.ComputeHash(modloaderDLL);
        }
        Debug.Log($"Modloader hash is: {modloaderHash}");
        Debug.Log("Getting loaded mods");
        List<Mod> mods = new List<Mod>();
        try
        {
            mods = VTOLAPI.GetUsersMods();
        }
        catch (Exception)
        {
            Debug.Log("Exception caught while getting user's mods. Perhaps they aren't on the correct mod loader version?");
        }

        foreach (Mod mod in mods)
        {
            if (mod.isLoaded)
            {
                Debug.Log($"Mod found: {mod.name}");

                if (File.Exists(mod.dllPath))
                {
                    using (FileStream modDLL = File.OpenRead(mod.dllPath))
                    {
                        modHash = hashCalculator.ComputeHash(modDLL);
                    }
                    if (!modsLoadedHashes.ContainsKey(BitConverter.ToString(modHash).Replace("-", "").ToLowerInvariant().Substring(0, 20)))
                        modsLoadedHashes.Add(BitConverter.ToString(modHash).Replace("-", "").ToLowerInvariant().Substring(0, 20), Path.GetFileName(mod.dllPath));
                    Debug.Log($"Added {mod.dllPath} to dictionary with key {BitConverter.ToString(modHash).Replace("-", "").ToLowerInvariant().Substring(0, 20)}");
                }
                else
                {
                    Debug.LogError($"Mod DLL Path Doesn't Exist: {mod.dllPath}");
                }
            }
        }
        Debug.Log("Done Creating Hashes");
    }
}

public class Networker : MonoBehaviour
{
    private Campaign pilotSaveManagerControllerCampaign;
    private CampaignScenario pilotSaveManagerControllerCampaignScenario;
    public static Networker _instance { get; private set; }
    private static readonly object isHostLock = new object();
    private static bool isHostInternal = false;
    public static bool isHost
    {
        get
        {
            lock (isHostLock)
            {
                return isHostInternal;
            }
        }
        private set
        {
            lock (isHostLock)
            {
                isHostInternal = value;
            }
        }
    }
    private static readonly object timeoutCounterLock = new object();
    private static int timeoutCounterInternal = 0;
    private static int TimeoutCounter
    {
        get { lock (timeoutCounterLock) { return timeoutCounterInternal; } }
        set { lock (timeoutCounterLock) { timeoutCounterInternal = value; } }
    }
    private static readonly int clientTimeoutInSeconds = 60;
    private static readonly object disconnectForClientTimeoutLock = new object();
    private static bool disconnectForClientTimeoutInternal = false;
    private static bool disconnectForClientTimeout
    {
        get { lock (disconnectForClientTimeoutLock) { return disconnectForClientTimeoutInternal; } }
        set { lock (disconnectForClientTimeoutLock) { disconnectForClientTimeoutInternal = value; } }
    }
    public static bool isClient { get; private set; }
    public enum GameState { Menu, Config, Game };
    public static GameState gameState { get; private set; }
    public static List<CSteamID> players { get; private set; } = new List<CSteamID>();
    public static Dictionary<CSteamID, bool> readyDic { get; private set; } = new Dictionary<CSteamID, bool>();

    // Pretty lazy way of doing this, maybe we should use a struct instead? But eh it should work a bit better for now.
    // playerStatusDic int definitions
    // 0 = Not Ready
    // 1 = Ready
    // 2 = Loading
    // 3 = In Game
    // 4 = Disconnected
    public enum PlayerStatus {
        Loadout,
        NotReady,
        ReadyREDFOR,
        ReadyBLUFOR,
        Loading,
        InGame,
        Disconected
    }

    public static Dictionary<CSteamID, PlayerStatus> playerStatusDic { get; private set; } = new Dictionary<CSteamID, PlayerStatus>();

    public static bool allPlayersReadyHasBeenSentFirstTime;
    public static bool readySent;
    public static bool hostReady, alreadyInGame, hostLoaded;

    public bool playingMP { get; private set; }

    public static CSteamID hostID { get; private set; }
    private Callback<P2PSessionRequest_t> _p2PSessionRequestCallback;
    //networkUID is used as an identifer for all network object, we are just adding onto this to get a new one
    private static ulong networkUID = 0;
    public static TextMeshPro loadingText;

    public static Multiplayer multiplayerInstance = null;

    public static bool HeartbeatTimerRunning = false;
    public static readonly System.Timers.Timer HeartbeatTimer = new System.Timers.Timer(1000);

    public static float pingToHost = 0;

    #region Message Type Callbacks
    //These callbacks are use for other scripts to know when a network message has been
    //received for them. They should match the name of the message class they relate to.
    public static event UnityAction<Packet, CSteamID> RequestSpawn;
    public static event UnityAction<Packet> RequestSpawn_Result;
    public static event UnityAction<Packet, CSteamID> SpawnVehicle;
    public static event UnityAction<Packet> RigidbodyUpdate;
    public static event UnityAction<Packet> PlaneUpdate;
    public static event UnityAction<Packet> EngineTiltUpdate;
    public static event UnityAction<Packet> Disconnecting;
    public static event UnityAction<Packet> WeaponSet;
    public static event UnityAction<Packet> WeaponSet_Result;
    public static event UnityAction<Packet> WeaponFiring;
    public static event UnityAction<Packet> WeaponStoppedFiring;
    public static event UnityAction<Packet> FireCountermeasure;
    public static event UnityAction<Packet> Rearm;
    public static event UnityAction<Packet> Death;
    public static event UnityAction<Packet> WingFold;
    public static event UnityAction<Packet> ExtLight;
    public static event UnityAction<Packet> ShipUpdate;
    public static event UnityAction<Packet> RadarUpdate;
    public static event UnityAction<Packet> TurretUpdate;
    public static event UnityAction<Packet> MissileUpdate;
    public static event UnityAction<Packet> MissileLaunch;
    public static event UnityAction<Packet> MissileDetonate;
    public static event UnityAction<Packet> MissileChangeAuthority;
    public static event UnityAction<Packet> WorldDataUpdate;
    public static event UnityAction<Packet> RequestNetworkUID;
    public static event UnityAction<Packet> LockingRadarUpdate;
    public static event UnityAction<Packet> JettisonUpdate;
    public static event UnityAction<Packet> SAMUpdate;
    public static event UnityAction<Packet> AAAUpdate;
    public static event UnityAction<Packet> BulletHit;
    #endregion
    #region Host Forwarding Suppress By Message Type List
    private List<MessageType> hostMessageForwardingSuppressList = new List<MessageType> {
        MessageType.None,
        MessageType.JoinRequest,
        MessageType.JoinRequestAccepted_Result,
        MessageType.JoinRequestRejected_Result,
        MessageType.SpawnPlayerVehicle,
        MessageType.RequestSpawn,
        MessageType.RequestSpawn_Result,
        MessageType.LobbyInfoRequest,
        MessageType.LobbyInfoRequest_Result,
        MessageType.WeaponsSet_Result,
        MessageType.RequestNetworkUID,
        MessageType.Ready
    };
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
        SpawnVehicle += PlayerManager.SpawnPlayerVehicle;

        // Is this line actually needed?
        //VTCustomMapManager.OnLoadedMap += (customMap) => { StartCoroutine(PlayerManager.MapLoaded(customMap)); };

        VTOLAPI.SceneLoaded += SceneChanged;
        HeartbeatTimer.Elapsed += HeartbeatCallback;
        HeartbeatTimer.AutoReset = true;
    }
    private void OnP2PSessionRequest(P2PSessionRequest_t request)
    {
        //Yes this is expecting everyone, even if they are not friends...
        SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
        Debug.Log("Accepting P2P with " + SteamFriends.GetFriendPersonaName(request.m_steamIDRemote));
    }


    private void Update()
    {
        if (PilotSaveManager.currentScenario != null)
        {
            if (pilotSaveManagerControllerCampaign != PilotSaveManager.currentCampaign)
            {
                pilotSaveManagerControllerCampaign = PilotSaveManager.currentCampaign;
            }
            if (pilotSaveManagerControllerCampaignScenario != PilotSaveManager.currentScenario)
            {
                pilotSaveManagerControllerCampaignScenario = PilotSaveManager.currentScenario;
            }
        }
        ReadP2P();
    }

    private void LateUpdate()
    {
        if (disconnectForClientTimeout)
        {
            disconnectForClientTimeout = false;

            Debug.Log("Connection to host timed out");

            // Make sure time is moving normally so exit scene transition will work
            WorldDataNetworker_Receiver timeController = PlayerManager.worldData.GetComponent<WorldDataNetworker_Receiver>();
            timeController.ClientNeedsNormalTimeFlowBecauseHostDisconnected();
            FlightSceneManager flightSceneManager = FindObjectOfType<FlightSceneManager>();
            if (flightSceneManager == null)
                Debug.LogError("FlightSceneManager was null when host timed out");
            flightSceneManager.ExitScene();

            Disconnect(false);
        }
    }

    public static void HeartbeatCallback(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (isHost)
        {
            // Host, send heartbeat
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_Heartbeat(), EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
        else
        {
            // Client, increment timeout counter
            if (++TimeoutCounter > clientTimeoutInSeconds)
            {
                // Disconnected from host
                disconnectForClientTimeout = true;
                HeartbeatTimerRunning = false;
                HeartbeatTimer.Stop();
            }
        }
    }

    public void HostGame()
    {
        if (gameState != GameState.Menu)
        {
            Debug.LogError("Can't host game as already in one");
            Multiplayer._instance.displayError("Can't host game as already in one");
            return;
        }
        Debug.Log("Hosting game");
        isHost = true;

        TimeoutCounter = 0;
        HeartbeatTimerRunning = true;
        HeartbeatTimer.Start();
        playerStatusDic.Add(hostID, PlayerStatus.NotReady);
        _instance.StartCoroutine(_instance.FlyButton());
    }

    public static void SetHostReady(bool everyoneReady)
    {
        if (everyoneReady)
        {
            playerStatusDic[hostID] = PlayerStatus.Loading;
        }
        else
        {
            playerStatusDic[hostID] = PlayerStatus.ReadyBLUFOR;
        }

        hostReady = true;
        UpdateLoadingText();
    }

    public static void JoinGame(CSteamID steamID)
    {
        if (gameState != GameState.Menu)
        {
            Debug.LogError("Can't join game as already in one");
            Multiplayer._instance.displayError("Can't join game as already in one");
            return;
        }
        isHost = false;
        isClient = true;

        MapAndScenarioVersionChecker.CreateHashes();

        Debug.Log("Attempting to join game");

        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(steamID,
            new Message_JoinRequest(PilotSaveManager.currentVehicle.name,
                                    MapAndScenarioVersionChecker.builtInCampaign,
                                    MapAndScenarioVersionChecker.scenarioId,
                                    MapAndScenarioVersionChecker.mapHash,
                                    MapAndScenarioVersionChecker.scenarioHash,
                                    MapAndScenarioVersionChecker.campaignHash,
                                    MapAndScenarioVersionChecker.modsLoadedHashes,
                                    MapAndScenarioVersionChecker.modloaderHash),
            EP2PSend.k_EP2PSendReliable);
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

    private bool MessageTypeShouldBeForwarded(MessageType messageType)
    {
        if (hostMessageForwardingSuppressList.Contains(messageType))
        {
            return (false);
        }
        return (true);
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
                    Debug.Log("case none");
                    break;
                case MessageType.LobbyInfoRequest:
                    Debug.Log("case lobby info request");
                    if (SteamFriends.GetPersonaName() == null)
                    {
                        Debug.LogError("Persona name null");
                    }
                    if (PilotSaveManager.currentVehicle == null)
                    {
                        Debug.LogError("vehicle name null");
                    }
                    if (PilotSaveManager.currentScenario == null)
                    {
                        Debug.LogError("current scenario null");
                    }
                    if (PilotSaveManager.currentCampaign == null)
                    {
                        Debug.LogError("Persona name null");
                    }
                    if (PlayerManager.players == null)
                    {
                        Debug.Log("PLayer manager.players == null");
                    } // fuck you c#
                    if (PilotSaveManager.currentScenario.scenarioID == null)
                    {
                        Debug.LogError("current scenario name null");
                    }
                    if (PilotSaveManager.currentCampaign.campaignName == null)
                    {
                        Debug.LogError("current campaign campaign name ");
                    }
                    if (PlayerManager.players.Count.ToString() == null)
                    {
                        Debug.LogError("players count to string somehow null");
                    } // Fuck you again unity
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID,
                        new Message_LobbyInfoRequest_Result(SteamFriends.GetPersonaName(),
                                                                PilotSaveManager.currentVehicle.vehicleName,
                                                                PilotSaveManager.currentScenario.scenarioName,
                                                                PilotSaveManager.currentCampaign.campaignName,
                                                                PlayerManager.players.Count.ToString()),
                        EP2PSend.k_EP2PSendReliable);
                    break;
                case MessageType.LobbyInfoRequest_Result:
                    Debug.Log("case lobby info request result");
                    Message_LobbyInfoRequest_Result result = packetS.message as Message_LobbyInfoRequest_Result;
                    Debug.Log("Set result");
                    if (packetS == null)
                    {
                        Debug.LogError("packetS is null");
                    }
                    if (packetS.message == null)
                    {
                        Debug.LogError("packetS.message is null");
                    }
                    if (result == null)
                    {
                        Debug.LogError("Result is null");
                    }
                    if (result.username == null)
                    {
                        Debug.LogError("Result name is null");
                    }
                    if (result.vehicle == null)
                    {
                        Debug.LogError("Result vehicle is null");
                    }
                    if (result.campaign == null)
                    {
                        Debug.LogError("Result campaign is null");
                    }
                    if (result.scenario == null)
                    {
                        Debug.LogError("Result scenario is null");
                    }
                    if (result.playercount == null)
                    {
                        Debug.LogError("Result playercount is null");
                    }
                    if (Multiplayer._instance.lobbyInfoText.text == null)
                    {
                        Debug.LogError("Multiplayer _instance lobbyinfotext.text is null");
                    }
                    if (Multiplayer._instance == null)
                    {
                        Debug.LogError("Multiplayer _instance is null");
                    }
                    if (Multiplayer._instance.lobbyInfoText == null)
                    {
                        Debug.LogError("Multiplayer _instance lobbyinfotext is null");
                    }
                    Multiplayer._instance.lobbyInfoText.text = result.username + "'s Game\n" + result.vehicle + "\n" + result.campaign + " " + result.scenario + "\n" + (result.playercount == "1" ? result.playercount + " Player" : result.playercount + " Players");
                    Debug.Log("Breaking case set lobby info request result");
                    break;
                case MessageType.JoinRequest:
                    Debug.Log("case join request");
                    HandleJoinRequest(csteamID, packetS);
                    break;
                case MessageType.JoinRequestAccepted_Result:
                    Debug.Log($"case join request accepted result, joining {csteamID.m_SteamID}");
                    Multiplayer._instance.displayInfo($"case join request accepted result, joining {csteamID.m_SteamID}");
                    hostID = csteamID;
                    TimeoutCounter = 0;
                    HeartbeatTimerRunning = true;
                    HeartbeatTimer.Start();
                    StartCoroutine(FlyButton());
                    UpdateLoadingText();
                    break;
                case MessageType.JoinRequestRejected_Result:
                    Debug.Log("case join request rejected result");
                    Message_JoinRequestRejected_Result joinResultRejected = packetS.message as Message_JoinRequestRejected_Result;
                    Debug.LogWarning($"We can't join {csteamID.m_SteamID} reason = \n{joinResultRejected.reason}");
                    Multiplayer._instance.displayError($"We can't join {csteamID.m_SteamID} reason = \n{joinResultRejected.reason}");
                    break;
                case MessageType.Ready:
                    if (!isHost)
                    {
                        Debug.Log("We shouldn't have gotten a ready message");
                        break;
                    }
                    Debug.Log("case ready");
                    Message_Ready readyMessage = packetS.message as Message_Ready;



                    //The client has said they are ready to start, so we change it in the dictionary

                    if (readyDic.ContainsKey(csteamID) && playerStatusDic.ContainsKey(csteamID))
                    {
                        if (readyMessage.isLeft) {
                            playerStatusDic[csteamID] = PlayerStatus.ReadyREDFOR;
                        }
                        else {
                            playerStatusDic[csteamID] = PlayerStatus.ReadyBLUFOR;
                        }

                        if (readyDic[csteamID])
                        {
                            Debug.Log("Received ready message from the same user twice");
                            break;
                        }

                        Debug.Log($"{csteamID.m_SteamID} has said they are ready!\nHost ready state {hostReady}");
                        readyDic[csteamID] = true;
                        if (alreadyInGame)
                        {
                            //Someone is trying to join when we are already in game.
                            Debug.Log($"We are already in session, {csteamID} is joining in!");
                            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message(MessageType.AllPlayersReady), EP2PSend.k_EP2PSendReliable);

                            // Send host loaded message right away
                            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_HostLoaded(true), EP2PSend.k_EP2PSendReliable);
                            break;
                        }
                        else if (hostReady && EveryoneElseReady())
                        {
                            Debug.Log("The last client has said they are ready, starting");
                            if (!allPlayersReadyHasBeenSentFirstTime)
                            {
                                allPlayersReadyHasBeenSentFirstTime = true;
                                playerStatusDic[hostID] = PlayerStatus.Loading;
                                UpdateLoadingText();

                                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message(MessageType.AllPlayersReady), EP2PSend.k_EP2PSendReliable);
                            }
                            else
                            {
                                // Send only to this player
                                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message(MessageType.AllPlayersReady), EP2PSend.k_EP2PSendReliable);
                                playerStatusDic[hostID] = PlayerStatus.Loading;
                                UpdateLoadingText();
                            }
                            LoadingSceneController.instance.PlayerReady();
                        }
                        UpdateLoadingText();
                    }
                    break;
                case MessageType.AllPlayersReady:
                    Debug.Log("The host said everyone is ready, waiting for the host to load.");
                    playerStatusDic[hostID] = PlayerStatus.Loading;
                    UpdateLoadingText();
                    hostReady = true;
                    // LoadingSceneController.instance.PlayerReady();
                    break;
                case MessageType.RequestSpawn:
                    Debug.Log($"case request spawn from: {csteamID.m_SteamID}, we are {SteamUser.GetSteamID().m_SteamID}, host is {hostID}");
                    if (RequestSpawn != null)
                    { RequestSpawn.Invoke(packet, csteamID); }
                    break;
                case MessageType.RequestSpawn_Result:
                    Debug.Log("case request spawn result");
                    if (RequestSpawn_Result != null)
                        RequestSpawn_Result.Invoke(packet);
                    break;
                case MessageType.SpawnAiVehicle:
                    Debug.Log("case spawn ai vehicle");
                    AIManager.SpawnAIVehicle(packet);
                    break;
                case MessageType.SpawnPlayerVehicle:
                    Debug.Log("case spawn vehicle");
                    if (SpawnVehicle != null)
                        SpawnVehicle.Invoke(packet, csteamID);
                    break;
                case MessageType.RigidbodyUpdate:
                    // Debug.Log("case rigid body update");
                    if (RigidbodyUpdate != null)
                        RigidbodyUpdate.Invoke(packet);
                    break;
                case MessageType.PlaneUpdate:
                    // Debug.Log("case plane update");
                    if (PlaneUpdate != null)
                        PlaneUpdate.Invoke(packet);
                    break;
                case MessageType.EngineTiltUpdate:
                    // Debug.Log("case engine tilt update");
                    if (EngineTiltUpdate != null)
                        EngineTiltUpdate.Invoke(packet);
                    break;
                case MessageType.WorldData:
                    Debug.Log("case world data");
                    if (WorldDataUpdate != null)
                        WorldDataUpdate.Invoke(packet);
                    break;
                case MessageType.Disconnecting:
                    Debug.Log("case disconnecting");
                    if (isHost)
                    {
                        Debug.Log("Client disconnected");
                        if (Multiplayer.SoloTesting)
                            break;

                        playerStatusDic[csteamID] = PlayerStatus.Disconected;
                        players.Remove(csteamID);
                        NetworkSenderThread.Instance.RemovePlayer(csteamID);
                        NetworkSenderThread.Instance.SendPacketAsHostToAllClients(packet, packet.sendType);
                    }
                    else
                    {
                        Message_Disconnecting messsage = ((PacketSingle)packet).message as Message_Disconnecting;
                        playerStatusDic[csteamID] = PlayerStatus.Disconected;
                        if (messsage.isHost)
                        {
                            Debug.Log("Host disconnected");
                            //If it is the host quiting we just need to quit the mission as all networking will be lost.
                            // Make sure time is moving normally so exit scene transition will work
                            WorldDataNetworker_Receiver timeController = PlayerManager.worldData.GetComponent<WorldDataNetworker_Receiver>();
                            timeController.ClientNeedsNormalTimeFlowBecauseHostDisconnected();
                            FlightSceneManager flightSceneManager = FindObjectOfType<FlightSceneManager>();
                            if (flightSceneManager == null)
                                Debug.LogError("FlightSceneManager was null when host quit");
                            flightSceneManager.ExitScene();
                            Multiplayer._instance.displayError($"Host disconnected from session.");
                        }
                        else
                        {
                            Debug.Log("Other client disconnected");
                            foreach (var player in PlayerManager.players)
                            {
                                if (player.cSteamID == new CSteamID(messsage.UID))
                                {
                                    PlayerManager.players.Remove(player);
                                }
                            }
                        }
                        break;
                    }
                    if (Disconnecting != null)
                        Disconnecting.Invoke(packet);
                    break;
                case MessageType.WeaponsSet:
                    Debug.Log("case weapon set");
                    if (WeaponSet != null)
                        WeaponSet.Invoke(packet);
                    break;
                case MessageType.WeaponsSet_Result:
                    Debug.Log("case weapon set result");
                    if (WeaponSet_Result != null)
                        WeaponSet_Result.Invoke(packet);
                    if (isHost)
                    {
                        NetworkSenderThread.Instance.SendPacketAsHostToAllClients(packet, packet.sendType);
                    }
                    break;
                case MessageType.WeaponFiring:
                    Debug.Log("case weapon firing");
                    if (WeaponFiring != null)
                        WeaponFiring.Invoke(packet);
                    break;
                case MessageType.WeaponStoppedFiring:
                    Debug.Log("case weapon stopped firing");
                    if (WeaponStoppedFiring != null)
                        WeaponStoppedFiring.Invoke(packet);
                    break;
                case MessageType.FireCountermeasure:
                    Debug.Log("case countermeasure fired");
                    if (FireCountermeasure != null)
                        FireCountermeasure.Invoke(packet);
                    break;
                case MessageType.Death:
                    Debug.Log("case death");
                    if (Death != null)
                        Death.Invoke(packet);
                    break;
                case MessageType.Respawn:
                    Debug.Log("case respawn");
                    Message_Respawn respawnMessage = ((PacketSingle)packet).message as Message_Respawn;
                    PlayerManager.SpawnRepresentation(respawnMessage.UID, respawnMessage.position, respawnMessage.rotation,respawnMessage.isLeftie);
                    break;
                case MessageType.WingFold:
                    Debug.Log("case wingfold");
                    if (WingFold != null)
                        WingFold.Invoke(packet);
                    break;
                case MessageType.ExtLight:
                    Debug.Log("case external light");
                    if (ExtLight != null)
                        ExtLight.Invoke(packet);
                    break;
                case MessageType.ShipUpdate:
                    //Debug.Log("case ship update");
                    if (ShipUpdate != null)
                        ShipUpdate.Invoke(packet);
                    break;
                case MessageType.RadarUpdate:
                    Debug.Log("case radar update");
                    if (RadarUpdate != null)
                        RadarUpdate.Invoke(packet);
                    break;
                case MessageType.LockingRadarUpdate:
                    Debug.Log("case locking radar update");
                    if (LockingRadarUpdate != null)
                        LockingRadarUpdate.Invoke(packet);
                    break;
                case MessageType.TurretUpdate:
                    //Debug.Log("turret update update");
                    if (TurretUpdate != null)
                        TurretUpdate.Invoke(packet);
                    break;
                case MessageType.MissileUpdate:
                    // Debug.Log("case missile update");
                    if (MissileUpdate != null)
                        MissileUpdate.Invoke(packet);
                    break;
                case MessageType.MissileLaunch:
                    Debug.Log("case missile launch");
                    if (MissileLaunch != null)
                        MissileLaunch.Invoke(packet);
                    if (isHost)
                    {
                        Message_MissileLaunch launchMessage = ((PacketSingle)packet).message as Message_MissileLaunch;
                        StartCoroutine(ChangeMissileAuthority(launchMessage.ownerUID, launchMessage.networkUID, launchMessage.targetActorUID));
                    }
                    break;
                case MessageType.MissileDetonate:
                    Debug.Log("case missile detonate");
                    if (MissileDetonate != null)
                        MissileDetonate.Invoke(packet);
                    break;
                case MessageType.MissileChangeAuthority:
                    Debug.Log("case missile change authority");
                    if (MissileChangeAuthority != null)
                        MissileChangeAuthority.Invoke(packet);
                    break;
                case MessageType.RequestNetworkUID:
                    Debug.Log("case request network UID");
                    if (RequestNetworkUID != null)
                        RequestNetworkUID.Invoke(packet);
                    break;
                case MessageType.LoadingTextUpdate:
                    Debug.Log("case loading text update");
                    if (!isHost)
                        UpdateLoadingText(packet);
                    break;
                case MessageType.HostLoaded:
                    Debug.Log("case host loaded");
                    if (!hostLoaded)
                    {
                        if (isHost)
                        {
                            Debug.Log("we shouldn't have gotten a host loaded....");
                            playerStatusDic[hostID] = PlayerStatus.InGame;
                        }
                        else
                        {
                            hostLoaded = true;
                            playerStatusDic[hostID] = PlayerStatus.InGame;
                            LoadingSceneController.instance.PlayerReady();
                        }
                    }
                    else
                    {
                        Debug.Log("Host is already loaded");
                    }
                    break;
                case MessageType.ServerHeartbeat:
                    if (!isHost)
                    {
                        Message_Heartbeat heartbeatMessage = ((PacketSingle)packet).message as Message_Heartbeat;

                        TimeoutCounter = 0;
                        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(hostID, new Message_Heartbeat_Result(heartbeatMessage.TimeOnServerGame, PlayerManager.localUID), EP2PSend.k_EP2PSendUnreliableNoDelay);
                    }
                    break;
                case MessageType.ServerHeartbeat_Response:
                    if (isHost)
                    {
                        Message_Heartbeat_Result heartbeatResult = ((PacketSingle)packet).message as Message_Heartbeat_Result;

                        float pingTime = Time.unscaledTime - heartbeatResult.TimeOnServerGame;

                        int playerID = PlayerManager.FindPlayerIDFromNetworkUID(heartbeatResult.from);
                        if (playerID != -1) {
                            PlayerManager.players[playerID].ping = pingTime / 2.0f;
                        }

                        NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_ReportPingTime(pingTime/2.0f, heartbeatResult.from), EP2PSend.k_EP2PSendUnreliableNoDelay);
                    }
                    break;
                case MessageType.ServerReportingPingTime:
                    if (!isHost)
                    {
                        // You can use ping report however you want
                        Message_ReportPingTime pingTimeMessage = packetS.message as Message_ReportPingTime;
                        if (pingTimeMessage.from == PlayerManager.localUID)
                        {
                            pingToHost = pingTimeMessage.PingTime;
                            int playerID = PlayerManager.GetPlayerIDFromCSteamID(hostID);
                            if (playerID != -1)
                            {
                                PlayerManager.players[playerID].ping = pingTimeMessage.PingTime;
                            }
                        }
                        else {
                            int playerID = PlayerManager.FindPlayerIDFromNetworkUID(pingTimeMessage.from);
                            if (playerID != -1)
                            {
                                PlayerManager.players[playerID].ping = pingToHost + pingTimeMessage.PingTime;
                            }
                        }
                    }
                    break;
                case MessageType.LoadingTextRequest:
                    Debug.Log("case LoadingTextRequest");
                    if (isHost)
                    {
                        UpdateLoadingText();
                    }
                    else
                    {
                        Debug.Log("Received loading text request and we're not the host.");
                    }
                    break;
                case MessageType.JettisonUpdate:
                    Debug.Log("case jettison update");
                    if (JettisonUpdate != null)
                        JettisonUpdate.Invoke(packet);
                    break;
                case MessageType.SamUpdate:
                    Debug.Log("case sam update");
                    if (SAMUpdate != null)
                        SAMUpdate.Invoke(packet);
                    break;
                case MessageType.AAAUpdate:
                    Debug.Log("case AAA update");
                    if (AAAUpdate != null)
                        AAAUpdate.Invoke(packet);
                    break;
                case MessageType.ScenarioAction:
                    Debug.Log("case scenario action packet");
                    Message_ScenarioAction lastMessage = (Message_ScenarioAction)((PacketSingle)packet).message;

                    Debug.Log("recieved action from other");
                    // do not run scenarios on self
                    if (lastMessage.UID == PlayerManager.localUID)
                    {
                        Debug.Log("ignored action as local event");

                    }
                    else
                    {
                        Debug.Log("running event from another person");
                        ObjectiveNetworker_Reciever.runScenarioAction(lastMessage.scenarioActionHash);
                    }

                    break;
                case MessageType.BulletHit:
                    BulletHit.Invoke(packet);
                    break;
                case MessageType.ObjectiveSync:
                    Debug.Log("case Objective");

                    Message_ObjectiveSync lastMessageobbj = (Message_ObjectiveSync)((PacketSingle)packet).message;

                    Debug.Log("received objective action from other");
                    // do not run scenarios on self
                    if (lastMessageobbj.UID == PlayerManager.localUID)
                    {
                        Debug.Log("ignored objective as local obj objective");

                    }
                    else
                    {
                        Debug.Log("running obj event from another person");
                        ObjectiveNetworker_Reciever.objectiveUpdate(lastMessageobbj.objID, lastMessageobbj.status);
                    }

                    break;
                default:
                    Debug.Log("default case");
                    break;
            }
            if (isHost)
            {
                if (MessageTypeShouldBeForwarded(packetS.message.type))
                {
                    NetworkSenderThread.Instance.SendPacketAsHostToAllButOneSpecificClient((CSteamID)packetS.networkUID, packetS.message, EP2PSend.k_EP2PSendUnreliableNoDelay);
                }
            }
        }
    }


    private IEnumerator FlyButton()
    {
        PilotSaveManager.currentCampaign = pilotSaveManagerControllerCampaign;
        PilotSaveManager.currentScenario = pilotSaveManagerControllerCampaignScenario;

        if (PilotSaveManager.currentScenario == null)
        {
            Debug.LogError("A null scenario was used on flight button!");
            yield break;
        }

        ControllerEventHandler.PauseEvents();
        ScreenFader.FadeOut(Color.black, 0.85f);
        yield return new WaitForSeconds(1f);
        Debug.Log("Continueing fly button lmao i typod like marsh.");
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
        Debug.Log("Fly button successful, unpausing events.");
        ControllerEventHandler.UnpauseEvents();
    }

    public static IEnumerator ChangeMissileAuthority(ulong launcherUID, ulong missileUID, ulong targetUID) {
        if (isClient)
        {
            Debug.Log("Client should not run code to change missile authority.");
            yield break;
        }
        
        yield return new WaitForSeconds(0.5f);
        switch (Multiplayer._instance.missileMode) {
            case Multiplayer.MissileSimMode.Host:
                Debug.Log("Switching the missiles to host side simulation!");
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_MissileChangeAuthority(missileUID, 0), Steamworks.EP2PSend.k_EP2PSendReliable);
                MissileChangeAuthority.Invoke(new PacketSingle(new Message_MissileChangeAuthority(missileUID, 0), Steamworks.EP2PSend.k_EP2PSendReliable));
                break;
            case Multiplayer.MissileSimMode.Target:
                Debug.Log("Switching the missiles to target side simulation!");
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_MissileChangeAuthority(missileUID, targetUID), Steamworks.EP2PSend.k_EP2PSendReliable);
                MissileChangeAuthority.Invoke(new PacketSingle(new Message_MissileChangeAuthority(missileUID, targetUID), Steamworks.EP2PSend.k_EP2PSendReliable));
                break;
            case Multiplayer.MissileSimMode.Launcher://this is the default lmao, nothing needs to change
            default:
                Debug.Log("The missile is being simulated on the player who launched it");
                break;
        }
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
        //Debug.Log($"Generated New UID ({result})");
        if (isClient)
        {
            Debug.Log("why is client generating uids? this is fubar...");
        }
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
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(hostID, new Message_RequestNetworkUID(clientsID), EP2PSend.k_EP2PSendUnreliableNoDelay);
            Debug.Log("Requetsed UID from host");
        }
        else
            Debug.LogError("For some reason the host requested a UID instead of generating one.");
    }

    private void SceneChanged(VTOLScenes scene)
    {
        if (scene == VTOLScenes.ReadyRoom && PlayerManager.gameLoaded)
        {
            Disconnect(false);
        }
    }

    public static void UpdateLoadingText() //Host Only
    {
        if (!isHost)
            return;
        StringBuilder content = new StringBuilder("<color=#FCB722><b><align=\"center\"><size=120%>Multiplayer Lobby</size></align></b></color>\n");

        switch (playerStatusDic[hostID]) {
            case PlayerStatus.Loadout:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"red\">Loadout</color>" + "\n");
                break;
            case PlayerStatus.NotReady:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"red\">Not Ready</color>" + "\n");
                break;
            case PlayerStatus.ReadyREDFOR:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"green\">Ready</color>" + " REDFOR" + "\n");
                break;
            case PlayerStatus.ReadyBLUFOR:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"green\">Ready</color>" + " BLUFOR" + "\n");
                break;
            case PlayerStatus.Loading:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"blue\">Loading</color>" + "\n");
                break;
            case PlayerStatus.InGame:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"green\">In Game</color>" + "\n");
                break;
            case PlayerStatus.Disconected:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"red\">Disconected</color>" + "\n");
                break;
            default:
                content.AppendLine("<b>" + SteamFriends.GetPersonaName() + "</b>" + ": " + "<color=\"red\">Other</color>" + "\n");
                break;
        }

        for (int i = 0; i < players.Count; i++)
        {
            switch (playerStatusDic[players[i]])
            {
                case PlayerStatus.Loadout:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"red\">Loadout</color>" + "\n");
                    break;
                case PlayerStatus.NotReady:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"red\">Not Ready</color>" + "\n");
                    break;
                case PlayerStatus.ReadyREDFOR:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"green\">Ready</color>" + " REDFOR" + "\n");
                    break;
                case PlayerStatus.ReadyBLUFOR:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"green\">Ready</color>" + " BLUFOR" + "\n");
                    break;
                case PlayerStatus.Loading:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"blue\">Loading</color>" + "\n");
                    break;
                case PlayerStatus.InGame:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"green\">In Game</color>" + "\n");
                    break;
                case PlayerStatus.Disconected:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"red\">Disconected</color>" + "\n");
                    break;
                default:
                    content.AppendLine("<b>" + SteamFriends.GetFriendPersonaName(players[i]) + "</b>" + ": " + "<color=\"red\">Other</color>" + "\n");
                    break;
            }
        }
        if (loadingText != null)
            loadingText.text = content.ToString();

        NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_LoadingTextUpdate(content.ToString()), EP2PSend.k_EP2PSendReliable);
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

    private static void HandleJoinRequest(CSteamID csteamID, PacketSingle packetS)
    {
        // Sanity checks
        if (!isHost)
        {
            Debug.LogError($"Recived Join Request when we are not the host");
            string notHostStr = "Failed to Join Player, they are not hosting a lobby";
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(notHostStr), EP2PSend.k_EP2PSendReliable);
            return;
        }

        if (players.Contains(csteamID))
        {
            Debug.LogError("The player seemed to send two join requests");
            return;
        }

        // Check version match
        Message_JoinRequest joinRequest = packetS.message as Message_JoinRequest;
        if (joinRequest.vtolVrVersion != GameStartup.versionString)
        {
            string vtolMismatchVersion = "Failed to Join Player, mismatched vtol vr versions (please both update to latest version)";
            Debug.Log($"Player {csteamID} had the wrong VTOL VR version");
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(vtolMismatchVersion), EP2PSend.k_EP2PSendReliable);
            return;
        }
        if (joinRequest.multiplayerBranch != ModVersionString.ReleaseBranch)
        {
            string branchMismatch = "Failed to Join Player, host branch is )" + ModVersionString.ReleaseBranch + ", client is " + joinRequest.multiplayerBranch;
            Debug.Log($"Player {csteamID} had the wrong Multiplayer.dll version");
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(branchMismatch), EP2PSend.k_EP2PSendReliable);
            return;
        }
        if (joinRequest.multiplayerModVersion != ModVersionString.ModVersionNumber)
        {
            string multiplayerVersionMismatch = "Failed to Join Player, host version is )" + ModVersionString.ModVersionNumber + ", client is " + joinRequest.multiplayerModVersion;
            Debug.Log($"Player {csteamID} had the wrong Multiplayer.dll version");
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(multiplayerVersionMismatch), EP2PSend.k_EP2PSendReliable);
            return;
        }

        // Check vehicle, campaign, scenario, map
        if (joinRequest.currentVehicle == "FA-26B")
        {
            joinRequest.currentVehicle = "F/A-26B";
        }
        if (joinRequest.currentVehicle != PilotSaveManager.currentVehicle.vehicleName)
        {
            string wrongVehicle = "Failed to Join Player, host vehicle is )" + PilotSaveManager.currentVehicle.vehicleName + ", client is " + joinRequest.currentVehicle;
            Debug.Log($"Player {csteamID} attempted to join with {joinRequest.currentVehicle}, server is {PilotSaveManager.currentVehicle.vehicleName}");
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(wrongVehicle), EP2PSend.k_EP2PSendReliable);
            return;
        }

        if (joinRequest.builtInCampaign != MapAndScenarioVersionChecker.builtInCampaign)
        {
            string wrongCampaignType = "Failed to Join Player, host campaign type is )" + MapAndScenarioVersionChecker.builtInCampaign.ToString();
            Debug.Log($"Player {csteamID} had the wrong campaign type");
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(wrongCampaignType), EP2PSend.k_EP2PSendReliable);
            return;
        }

        if (joinRequest.builtInCampaign)
        {
            if (joinRequest.scenarioId != MapAndScenarioVersionChecker.scenarioId)
            {
                string wrongScenarioId = "Failed to Join Player, host scenario is )" + MapAndScenarioVersionChecker.scenarioId + ", yours is " + joinRequest.scenarioId;
                Debug.Log($"Player {csteamID} had the wrong scenario");
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(wrongScenarioId), EP2PSend.k_EP2PSendReliable);
                return;
            }
        }
        else
        {
            // Custom campaign
            if (joinRequest.campaignHash != MapAndScenarioVersionChecker.campaignHash)
            {
                string badCampaignHash = "Failed to Join Player, custom campaign mismatch";
                Debug.Log($"Player {csteamID} had a mismatched campaign (wrong id or version)");
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(badCampaignHash), EP2PSend.k_EP2PSendReliable);
                return;
            }
            if (joinRequest.scenarioHash != MapAndScenarioVersionChecker.scenarioHash)
            {
                string badScenarioHash = "Failed to Join Player, custom scenario mismatch";
                Debug.Log($"Player {csteamID} had a mismatched scenario (wrong id or version)");
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(badScenarioHash), EP2PSend.k_EP2PSendReliable);
                return;
            }
            if (joinRequest.mapHash != MapAndScenarioVersionChecker.mapHash)
            {
                string badMapHash = "Failed to Join Player, custom map mismatch";
                Debug.Log($"Player {csteamID} had a mismatched map (wrong id or version)");
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(badMapHash), EP2PSend.k_EP2PSendReliable);
                return;
            }
        }
        if (Multiplayer._instance.restrictToHostMods)
        {
            if (BitConverter.ToString(joinRequest.modloaderHash).Replace("-", "").ToLowerInvariant() != BitConverter.ToString(MapAndScenarioVersionChecker.modloaderHash).Replace("-", "").ToLowerInvariant())
            {
                string badModLoaderHash = "Failed to Join Player, modloader hash mismatch";
                Debug.Log($"Player {csteamID} had a different modloader hash than host");
                Debug.Log($"Player has {BitConverter.ToString(joinRequest.modloaderHash).Replace("-", "").ToLowerInvariant()}. Host Has {BitConverter.ToString(MapAndScenarioVersionChecker.modloaderHash).Replace("-", "").ToLowerInvariant()}.");
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(badModLoaderHash), EP2PSend.k_EP2PSendReliable);
                return;
            }

            // Looping through host's mods to see if the client is missing any

            Debug.Log($"Client has {joinRequest.modsLoadedHashes.Count} mods loaded");
            Debug.Log($"Server has {MapAndScenarioVersionChecker.modsLoadedHashes.Count} mods loaded");

            foreach (KeyValuePair<string, string> mod in MapAndScenarioVersionChecker.modsLoadedHashes)
            {
                if (!joinRequest.modsLoadedHashes.ContainsKey(mod.Key))
                {
                    string missingHostMod = "Failed to Join Player, host requires forced mods. Missing mod: " + mod.Value;
                    Debug.Log($"Player {csteamID} is missing mod: " + mod.Value);
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(missingHostMod), EP2PSend.k_EP2PSendReliable);
                    return;
                }
                else
                {
                    Debug.Log($"Both host and client have {mod.Value}");
                }
            }

            // Looping through client's mods to see if the host is missing any
            foreach (KeyValuePair<string, string> mod in joinRequest.modsLoadedHashes)
            {
                if (!MapAndScenarioVersionChecker.modsLoadedHashes.ContainsKey(mod.Key))
                {
                    string missingClientMod = "Failed to Join Player, host requires forced mods. You have mod: " + mod.Value + " and the host doesn't have that loaded";
                    Debug.Log($"Player {csteamID} has mod: " + mod.Value + " and you don't");
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestRejected_Result(missingClientMod), EP2PSend.k_EP2PSendReliable);
                    return;
                }
                else
                {
                    Debug.Log($"Both host and client have {mod.Value}");
                }
            }

        }


        // Made it past all checks, we can join
        Debug.Log($"Accepting {csteamID.m_SteamID}, adding to players list");
        players.Add(csteamID);
        readyDic.Add(csteamID, false);
        Debug.Log($"Adding {csteamID} to status dict, with status of not ready");
        playerStatusDic.Add(csteamID, PlayerStatus.NotReady);//future people, please implement PlayerStatus.Loadout so we can see who is customising still
        Debug.Log("Done adding to status dict");
        NetworkSenderThread.Instance.AddPlayer(csteamID);
        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(csteamID, new Message_JoinRequestAccepted_Result(), EP2PSend.k_EP2PSendReliable);
        UpdateLoadingText();
    }

    public static void SetMultiplayerInstance(Multiplayer instance)
    {
        multiplayerInstance = instance;
    }

    public static void OnMultiplayerDestroy()
    {
        multiplayerInstance = null;
    }

    public void OnApplicationQuit()
    {
        if (HeartbeatTimerRunning)
        {
            HeartbeatTimer.Stop();
            HeartbeatTimerRunning = false;
        }

        if (PlayerManager.gameLoaded)
        {
            Disconnect(true);
        }
    }
    /// <summary>
    /// This will send any messages needed to the host or other players and reset variables.
    /// </summary>
    public void Disconnect(bool applicationClosing)
    {
        Debug.Log("Disconnecting from server");
        if (isHost)
        {
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_Disconnecting(PlayerManager.localUID, true), EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(hostID, new Message_Disconnecting(PlayerManager.localUID, false), EP2PSend.k_EP2PSendReliable);
        }

        if (applicationClosing)
            return;

        PlayerManager.CleanUpPlayerManagerStaticVariables();
        DisconnectionTasks();
    }

    public void PlayerManagerReportsDisconnect()
    {
        DisconnectionTasks();
    }
    public void DisconnectionTasks()
    {
        Debug.Log("Running disconnection tasks");
        if (HeartbeatTimerRunning)
        {
            HeartbeatTimer.Stop();
            HeartbeatTimerRunning = false;
        }

        isHost = false;
        isClient = false;
        gameState = GameState.Menu;
        players?.Clear();
        NetworkSenderThread.Instance.DumpAllExistingPlayers();
        readyDic?.Clear();
        playerStatusDic?.Clear();
        hostReady = false;
        allPlayersReadyHasBeenSentFirstTime = false;
        readySent = false;
        alreadyInGame = false;
        hostID = new CSteamID(0);
        pingToHost = 0;

        AIManager.CleanUpOnDisconnect();
        multiplayerInstance?.CleanUpOnDisconnect();
        hostLoaded = false;
    }
}
