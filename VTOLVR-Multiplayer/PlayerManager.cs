using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Harmony;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

public static class PlayerManager
{
    public static List<Transform> spawnPoints { private set; get; }

    private static float spawnSpacing = 20;
    private static int spawnsCount = 20;
    /// <summary>
    /// This is the queue for people waiting to get a spawn point,
    /// incase the host hasn't loaded in, in time.
    /// </summary>
    private static Queue<CSteamID> spawnRequestQueue = new Queue<CSteamID>();
    private static Queue<Packet> playersToSpawnQueue = new Queue<Packet>();
    private static Queue<CSteamID> playersToSpawnIdQueue = new Queue<CSteamID>();
    public static bool gameLoaded;
    private static GameObject av42cPrefab, fa26bPrefab, f45Prefab;
    private static List<ulong> spawnedVehicles = new List<ulong>();
    public static ulong localUID;

    public static VTScenario currentScenario;
    public static GameObject worldData;

    public struct Player
    {
        public CSteamID cSteamID;
        public GameObject vehicle;
        
        public VTOLVehicles vehicleName;
        public ulong vehicleUID;

        public Player(CSteamID cSteamID, GameObject vehicle, VTOLVehicles vehicleName, ulong vehicleUID)
        {
            this.cSteamID = cSteamID;
            this.vehicle = vehicle;
            this.vehicleName = vehicleName;
            this.vehicleUID = vehicleUID;
        }
    }
    public static List<Player> players = new List<Player>(); //This is the list of players
    /// <summary>
    /// This runs when the map has finished loading and hopefully 
    /// when the player first can interact with the vehicle.
    /// </summary>
    public static IEnumerator MapLoaded()
    {
        Debug.Log("map loading started");
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady || FlightSceneManager.instance.switchingScene)
        {
            yield return null;
        }
        Debug.Log("The map has loaded");
        gameLoaded = true;
        //As a client, when the map has loaded we are going to request a spawn point from the host
        SetPrefabs();
        if (!Networker.isHost)
        {
            Debug.Log($"Sending spawn request to host, host id: {Networker.hostID}, client id: {SteamUser.GetSteamID().m_SteamID}");
            Debug.Log("Killing all units currently on the map.");
            foreach (var actor in TargetManager.instance.allActors)
            {
                if (!actor.isPlayer)
                {
                    TargetManager.instance.UnregisterActor(actor);
                    actor.gameObject.SetActive(false);
                }
            }
            Networker.SendP2P(Networker.hostID, new Message(MessageType.RequestSpawn), EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            Debug.Log("Starting map loaded host routines");
            Networker.hostLoaded = true;
            Networker.hostReady = true;
            PlaneNetworker_Sender lastPlaneSender;
            RigidbodyNetworker_Sender lastRigidSender;
            foreach (var actor in TargetManager.instance.allActors)
            {
                lastRigidSender = actor.gameObject.AddComponent<RigidbodyNetworker_Sender>();
                lastRigidSender.networkUID = Networker.GenerateNetworkUID();
                if (!actor.isPlayer && actor.role == Actor.Roles.Air)
                {
                    lastPlaneSender = actor.gameObject.AddComponent<PlaneNetworker_Sender>();
                    lastPlaneSender.networkUID = lastRigidSender.networkUID;
                }
            }
            Networker.SendGlobalP2P(new Message_HostLoaded(true), EP2PSend.k_EP2PSendReliable);
            GameObject localVehicle = VTOLAPI.GetPlayersVehicleGameObject();
            if (localVehicle != null)
            {
                GenerateSpawns(localVehicle.transform);
                localUID = Networker.GenerateNetworkUID();
                SpawnLocalVehicleAndInformOtherClients(localVehicle, localVehicle.transform.position, localVehicle.transform.rotation.eulerAngles, localUID);
            }
            else
                Debug.Log("Local vehicle for host was null");
            if (spawnRequestQueue.Count != 0)
                SpawnRequestQueue();
            Networker.alreadyInGame = true;



        }

        while (AIManager.AIsToSpawnQueue.Count > 0)
        {
            AIManager.SpawnAIVehicle(AIManager.AIsToSpawnQueue.Dequeue());
        }
        while (playersToSpawnQueue.Count > 0) {
            SpawnPlayerVehicle(playersToSpawnQueue.Dequeue(), playersToSpawnIdQueue.Dequeue());
        }

        
        if (!Networker.isHost)
        {
            // If the player is not the host, they only need a receiver?
            Debug.Log($"Player not the host, adding world data receiver");
            worldData = new GameObject();
            WorldDataNetworker_Receiver WorldDataNetworker = worldData.AddComponent<WorldDataNetworker_Receiver>();
        }
        else
        {
            // If the player is the host, setup the sender so they can send world data
            Debug.Log($"Player is the host, setting up the world data sender");
            worldData = new GameObject();
            WorldDataNetworker_Sender WorldDataNetworker = worldData.AddComponent<WorldDataNetworker_Sender>();
        }
    }

    /// <summary>
    /// This is a way to invoke SpawnRequstQueue() if the queue is loaded
    /// </summary>
    public static void SpawnRequestQueuePublic()
    {
        if (spawnRequestQueue.Count != 0)
        {
            SpawnRequestQueue();
        }
    }
    /// <summary>
    /// This gives all the people waiting their spawn points
    /// </summary>
    private static void SpawnRequestQueue() //Run by Host Only
    {
        Debug.Log($"Giving {spawnRequestQueue.Count} people their spawns");
        Transform lastSpawn;
        while (spawnRequestQueue.Count > 0)
        {
            lastSpawn = FindFreeSpawn();
            Debug.Log("The players spawn will be " + lastSpawn);
            Networker.SendP2P(
                spawnRequestQueue.Dequeue(),
                new Message_RequestSpawn_Result(new Vector3D(lastSpawn.position), new Vector3D(lastSpawn.rotation.eulerAngles), Networker.GenerateNetworkUID(), players.Count),
                EP2PSend.k_EP2PSendReliable);
        }
    }
    /// <summary>
    /// This is when a client has requested a spawn point from the host,
    /// the host gets a spawn point and sends back the position. This does
    /// not actually spawn the vehicle yet, just makes the request to the
    /// host. 
    /// </summary>
    /// <param name="packet">The Message</param>
    /// <param name="sender">The client who sent it</param>
    public static void RequestSpawn(Packet packet, CSteamID sender) //Run by Host Only
    {
        Debug.Log("A player has requested for a spawn point");
        if (!Networker.hostLoaded)
        {
            Debug.Log("The host isn't ready yet, adding to queue");
            spawnRequestQueue.Enqueue(sender);
            return;
        }
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("Spawn points was null, we won't be able to find any spawn point then");
            return;
        }
        Transform spawn = FindFreeSpawn();
        Debug.Log("The players spawn will be " + spawn);
        Networker.SendP2P(sender, new Message_RequestSpawn_Result(new Vector3D(spawn.position), new Vector3D(spawn.rotation.eulerAngles), Networker.GenerateNetworkUID(), players.Count), EP2PSend.k_EP2PSendReliable);
    }
    /// <summary>
    /// When the client receives a P2P message of their spawn point, 
    /// this will move them to that location before sending their vehicle 
    /// to the host. This will call the function that spawns the local
    /// vehicle. 
    /// </summary>
    /// <param name="packet">The message sent over the network</param>
    public static void RequestSpawn_Result(Packet packet) //Run by Clients Only
    {
        Debug.Log("The host has sent back our spawn point");
        Message_RequestSpawn_Result result = (Message_RequestSpawn_Result)((PacketSingle)packet).message;
        Debug.Log($"We need to move to {result.position} : {result.rotation}");

        GameObject localVehicle = VTOLAPI.GetPlayersVehicleGameObject();
        if (localVehicle == null)
        {
            Debug.LogError("The local vehicle was null");
            return;
        }
        localVehicle.transform.position = result.position.toVector3;
        localVehicle.transform.rotation = Quaternion.Euler(result.rotation.toVector3);
        SpawnLocalVehicleAndInformOtherClients(localVehicle, result.position.toVector3, result.rotation.toVector3, result.vehicleUID);
        localUID = result.vehicleUID;
    }
    /// <summary>
    /// Spawns a local vehicle, and sends the message to other clients to 
    /// spawn their representation of this vehicle
    /// </summary>
    /// <param name="localVehicle">The local clients gameobject</param>
    public static void SpawnLocalVehicleAndInformOtherClients(GameObject localVehicle, Vector3 pos, Vector3 rot, ulong UID) //Both
    {
        Debug.Log("Sending our location to spawn our vehicle");
        VTOLVehicles currentVehicle = VTOLAPI.GetPlayersVehicleEnum();
        Player localPlayer = new Player(SteamUser.GetSteamID(), localVehicle, currentVehicle, UID);
        players.Add(localPlayer);

        RigidbodyNetworker_Sender rbSender = localVehicle.AddComponent<RigidbodyNetworker_Sender>();
        rbSender.networkUID = UID;
        rbSender.spawnPos = pos;
        rbSender.spawnRot = rot;
        rbSender.SetSpawn();

        Debug.Log("Adding Plane Sender");
        PlaneNetworker_Sender planeSender = localVehicle.AddComponent<PlaneNetworker_Sender>();
        planeSender.networkUID = UID;

        if (currentVehicle == VTOLVehicles.AV42C || currentVehicle == VTOLVehicles.F45A)
        {
            Debug.Log("Added Tilt Updater to our vehicle");
            EngineTiltNetworker_Sender tiltSender = localVehicle.AddComponent<EngineTiltNetworker_Sender>();
            tiltSender.networkUID = UID;
        }

        if (localVehicle.GetComponentInChildren<WingFoldController>() != null) {
            WingFoldNetworker_Sender wingFold = localVehicle.AddComponent<WingFoldNetworker_Sender>();
            wingFold.wingController = localVehicle.GetComponentInChildren<WingFoldController>().toggler;
            wingFold.networkUID = UID;
        }

        if (localVehicle.GetComponentInChildren<StrobeLightController>() != null)
        {
            ExtLight_Sender extLight = localVehicle.AddComponent<ExtLight_Sender>();
            extLight.strobeLight = localVehicle.GetComponentInChildren<StrobeLightController>();
            extLight.networkUID = UID;
        }

        if (localVehicle.GetComponentInChildren<LockingRadar>() != null)
        {
            LockingRadarNetworker_Sender radarSender = localVehicle.AddComponent<LockingRadarNetworker_Sender>();
            radarSender.radar = localVehicle.GetComponentInChildren<LockingRadar>();
            radarSender.networkUID = UID;
        }

        if (Multiplayer.SoloTesting)
            pos += new Vector3(20, 0, 0);

        List<HPInfo> hpInfos = VTOLVR_Multiplayer.PlaneEquippableManager.generateLocalHpInfoList(UID);
        CountermeasureManager cmManager = localVehicle.GetComponentInChildren<CountermeasureManager>();
        List<int> cm = VTOLVR_Multiplayer.PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager);
        float fuel = VTOLVR_Multiplayer.PlaneEquippableManager.generateLocalFuelValue();

        Debug.Log("Assembled our local vehicle");
        if (!Networker.isHost || Multiplayer.SoloTesting)
        {
            // Not host, so send host the spawn vehicle message
            Debug.Log($"Sending spawn vehicle message to: {Networker.hostID}");
            Networker.SendP2P(Networker.hostID,
                new Message_SpawnPlayerVehicle(currentVehicle, 
                    new Vector3D(pos), 
                    new Vector3D(rot), 
                    SteamUser.GetSteamID().m_SteamID, 
                    UID, 
                    hpInfos.ToArray(), 
                    cm.ToArray(), 
                    fuel),
                EP2PSend.k_EP2PSendReliable);
        }
        else {
            Debug.Log("I am host, no need to immediately forward my assembled vehicle");
        }
    }
    /// <summary>
    /// When the user has received a message of spawn player vehicle, 
    /// this creates the player vehicle and removes any thing which shouldn't
    /// be on it. 
    /// </summary>
    /// <param name="packet">The message</param>
    public static void SpawnPlayerVehicle(Packet packet, CSteamID sender) //Both, but never spawns the local vehicle, only executes spawn vehicle messages from other clients
    {
        // We don't actually need the "sender" id, unless we're a client and want to check that the packet came from the host
        // which we're not doing right now. 

        Message_SpawnPlayerVehicle message = (Message_SpawnPlayerVehicle)((PacketSingle)packet).message;
        Debug.Log($"Recived a Spawn Vehicle Message from: {message.csteamID}");

        if (!gameLoaded)
        {
            Debug.LogWarning("Our game isn't loaded, adding spawn vehicle to queue");
            playersToSpawnQueue.Enqueue(packet);
            playersToSpawnIdQueue.Enqueue(sender);
            return;
        }
        foreach (ulong id in spawnedVehicles)
        {
            if (id == message.csteamID)
            {
                Debug.Log("Got a spawnedVehicle message for a vehicle we have already added! Returning....");
                return;
            }
        }
        spawnedVehicles.Add(message.csteamID);
        Debug.Log("Got a new spawnVehicle uID.");
        if (Networker.isHost)
        {
            Debug.Log("Generating UIDS for any missiles the new vehicle has");
            for (int i = 0; i < message.hpLoadout.Length; i++)
            {
                for (int j = 0; j < message.hpLoadout[i].missileUIDS.Length; j++)
                {
                    if (message.hpLoadout[i].missileUIDS[j] != 0)
                    {
                        //Storing the old one
                        ulong clientsUID = message.hpLoadout[i].missileUIDS[j];
                        //Generating a new global UID for that missile
                        message.hpLoadout[i].missileUIDS[j] = Networker.GenerateNetworkUID();
                        //Sending it back to that client
                        Networker.SendP2P(new CSteamID(message.csteamID),
                            new Message_RequestNetworkUID(clientsUID, message.hpLoadout[i].missileUIDS[j]),
                            EP2PSend.k_EP2PSendReliable);
                    }
                }
            }

            Debug.Log("Telling other clients about new player and new player about other clients. Player count = " + players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].cSteamID == SteamUser.GetSteamID())
                {
                    //Debug.LogWarning("Skiping this one as it's the host");
                    //Send the host player to the new player.
                    Debug.Log($"Running host code to tell new player about host vehicle.");

                    GameObject localVehicle = VTOLAPI.GetPlayersVehicleGameObject();
                    WeaponManager localWeaponManager = localVehicle.GetComponent<WeaponManager>();

                    List<HPInfo> hpInfos = VTOLVR_Multiplayer.PlaneEquippableManager.generateHpInfoListFromWeaponManager(localWeaponManager,
                        VTOLVR_Multiplayer.PlaneEquippableManager.HPInfoListGenerateNetworkType.sender);
                    CountermeasureManager cmManager = localVehicle.GetComponentInChildren<CountermeasureManager>();
                    List<int> cm = VTOLVR_Multiplayer.PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager);
                    float fuel = VTOLVR_Multiplayer.PlaneEquippableManager.generateLocalFuelValue();

                    Networker.SendP2P(new CSteamID(message.csteamID),
                        new Message_SpawnPlayerVehicle(
                            players[i].vehicleName,
                            VTMapManager.WorldToGlobalPoint(players[i].vehicle.transform.position),
                            new Vector3D(players[i].vehicle.transform.rotation.eulerAngles),
                            players[i].cSteamID.m_SteamID,
                            players[i].vehicleUID,
                            hpInfos.ToArray(),
                            cm.ToArray(),
                            fuel),
                        EP2PSend.k_EP2PSendReliable);

                    Debug.Log($"We have told the new player about the host and NOT the other way around.");
                    Debug.Log($"We don't need to resync the host weapons, that's guaranteed to already be up to date.");
                    continue;
                }
                PlaneNetworker_Receiver existingPlayersPR = players[i].vehicle.GetComponent<PlaneNetworker_Receiver>();
                //We first send the new player to an existing spawned in player
                Networker.SendP2P(players[i].cSteamID, message, EP2PSend.k_EP2PSendReliable);
                //Then we send this current player to the new player.
                Networker.SendP2P(new CSteamID(message.csteamID),
                    new Message_SpawnPlayerVehicle(
                        players[i].vehicleName,
                        VTMapManager.WorldToGlobalPoint(players[i].vehicle.transform.position),
                        new Vector3D(players[i].vehicle.transform.rotation.eulerAngles),
                        players[i].cSteamID.m_SteamID,
                        players[i].vehicleUID,
                        existingPlayersPR.GenerateHPInfo(),
                        existingPlayersPR.GetCMS(),
                        existingPlayersPR.GetFuel()),
                    EP2PSend.k_EP2PSendReliable);
                Debug.Log($"We have told {players[i].cSteamID.m_SteamID} about the new player ({message.csteamID}) and the other way round.");

                //We ask the existing player what their load out just incase the host's player receiver was out of sync.
                Networker.SendP2P(players[i].cSteamID,
                    new Message(MessageType.WeaponsSet),
                    EP2PSend.k_EP2PSendReliable);
                Debug.Log($"We have asked {players[i].cSteamID.m_SteamID} what their current weapons are, and now waiting for a responce."); // marsh typo response lmaooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo
            }
            AIManager.TellClientAboutAI((CSteamID)message.csteamID);
        }

        GameObject newVehicle = null;
        switch (message.vehicle)
        {
            case VTOLVehicles.None:
                Debug.LogError("Vehcile Enum seems to be none, couldn't spawn player vehicle");
                return;
            case VTOLVehicles.AV42C:
                newVehicle = GameObject.Instantiate(av42cPrefab, message.position.toVector3, Quaternion.Euler(message.rotation.toVector3));
                break;
            case VTOLVehicles.FA26B:
                newVehicle = GameObject.Instantiate(fa26bPrefab, new Vector3(message.position.toVector3.x, message.position.toVector3.y + 2f, message.position.toVector3.z), Quaternion.Euler(message.rotation.toVector3));
                break;
            case VTOLVehicles.F45A:
                newVehicle = GameObject.Instantiate(f45Prefab, message.position.toVector3, Quaternion.Euler(message.rotation.toVector3));
                break;
        }
        Debug.Log("Setting vehicle name");
        newVehicle.name = $"Client [{message.csteamID}]";
        Debug.Log($"Spawned new vehicle at {newVehicle.transform.position}");

        RigidbodyNetworker_Receiver rbNetworker = newVehicle.AddComponent<RigidbodyNetworker_Receiver>();
        rbNetworker.networkUID = message.networkID;

        PlaneNetworker_Receiver planeReceiver = newVehicle.AddComponent<PlaneNetworker_Receiver>();
        planeReceiver.networkUID = message.networkID;

        if (message.vehicle == VTOLVehicles.AV42C || message.vehicle == VTOLVehicles.F45A)
        {
            Debug.Log("Adding Tilt Controller to this vehicle " + message.networkID);
            EngineTiltNetworker_Receiver tiltReceiver = newVehicle.AddComponent<EngineTiltNetworker_Receiver>();
            tiltReceiver.networkUID = message.networkID;
        }

        

        Rigidbody rb = newVehicle.GetComponent<Rigidbody>();
        AIPilot aIPilot = newVehicle.GetComponent<AIPilot>();
        Health health = newVehicle.GetComponent<Health>();
        health.invincible = true;

        RotationToggle wingRotator = aIPilot.wingRotator;
        if (wingRotator != null) {
            WingFoldNetworker_Receiver wingFoldReceiver = newVehicle.AddComponent<WingFoldNetworker_Receiver>();
            wingFoldReceiver.networkUID = message.networkID;
            wingFoldReceiver.wingController = wingRotator;
        }

        ExteriorLightsController extLight = newVehicle.GetComponentInChildren<ExteriorLightsController>();
        if (extLight != null)
        {
            ExtLight_Receiver extLightReceiver = newVehicle.AddComponent<ExtLight_Receiver>();
            extLightReceiver.lightsController = extLight;
            extLightReceiver.networkUID = message.networkID;
        }

        LockingRadar lockingRadar = newVehicle.GetComponentInChildren<LockingRadar>();
        if (lockingRadar != null)
        {
            LockingRadarNetworker_Receiver lockingRadarReceiver = newVehicle.AddComponent<LockingRadarNetworker_Receiver>();
            lockingRadarReceiver.lockingRadar = lockingRadar;
            lockingRadarReceiver.networkUID = message.networkID;
        }

        foreach (Collider collider in newVehicle.GetComponentsInChildren<Collider>())
        {
            if (collider)
            {
                collider.gameObject.layer = 9;
            }
        }
        aIPilot.enabled = false;
        Debug.Log($"Changing {newVehicle.name}'s position and rotation\nPos:{rb.position} Rotation:{rb.rotation.eulerAngles}");
        aIPilot.kPlane.SetToKinematic();
        aIPilot.kPlane.enabled = false;
        rb.interpolation = RigidbodyInterpolation.None;
        aIPilot.commandState = AIPilot.CommandStates.Override;


        rb.position = message.position.toVector3;
        rb.rotation = Quaternion.Euler(message.rotation.toVector3);
        aIPilot.kPlane.enabled = true;
        aIPilot.kPlane.SetVelocity(Vector3.zero);
        aIPilot.kPlane.SetToDynamic();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Debug.Log($"Finished changing {newVehicle.name}\n Pos:{rb.position} Rotation:{rb.rotation.eulerAngles}");

        GameObject parent = new GameObject("Name Tag Holder");
        GameObject nameTag = new GameObject("Name Tag");
        parent.transform.SetParent(newVehicle.transform);
        parent.transform.localRotation = Quaternion.Euler(0, 180, 0);
        nameTag.transform.SetParent(parent.transform);
        nameTag.AddComponent<Nametag>().SetText(
            SteamFriends.GetFriendPersonaName(new CSteamID(message.csteamID)),
            newVehicle.transform, VRHead.instance.transform);
        Debug.Log("Doing weapon manager shit on " + newVehicle.name + ".");
        WeaponManager weaponManager = newVehicle.GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Failed to get weapon manager on " + newVehicle.name);
        string[] hpLoadoutNames = new string[30];
        Debug.Log("foreach var equip in message.hpLoadout");
        int debugInteger = 0;
        foreach (var equip in message.hpLoadout)
        {
            Debug.Log(debugInteger);
            hpLoadoutNames[equip.hpIdx] = equip.hpName;
            debugInteger++;
        }

        Debug.Log("Setting Loadout on this new vehicle spawned");
        for (int i = 0; i < hpLoadoutNames.Length; i++)
        {
            Debug.Log("HP " + i + " Name: " + hpLoadoutNames[i]);
        }
        Debug.Log("Now doing loadout shit.");
        Loadout loadout = new Loadout();
        loadout.normalizedFuel = message.normalizedFuel;
        loadout.hpLoadout = hpLoadoutNames;
        loadout.cmLoadout = message.cmLoadout;
        weaponManager.EquipWeapons(loadout);
        weaponManager.RefreshWeapon();
        Debug.Log("Refreshed this weapon manager's weapons.");
        MissileNetworker_Receiver lastReciever;
        for (int i = 0; i < 30; i++)
        {
            int uIDidx = 0;
            HPEquippable equip = weaponManager.GetEquip(i);
            if (equip is HPEquipMissileLauncher)
            {
                Debug.Log(equip.name + " is a missile launcher");
                HPEquipMissileLauncher hpML = equip as HPEquipMissileLauncher;
                Debug.Log("This missile launcher has " + hpML.ml.missiles.Length + " missiles.");
                for(int j = 0; j < hpML.ml.missiles.Length; j++)
                {
                    Debug.Log("Adding missile reciever");
                    lastReciever = hpML.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Receiver>();
                    foreach (var thingy in message.hpLoadout) // it's a loop... because fuck you!
                    {
                        Debug.Log("Try adding missile reciever uID");
                        if (equip.hardpointIdx == thingy.hpIdx)
                        {
                            if (uIDidx < thingy.missileUIDS.Length)
                            {
                                lastReciever.networkUID = thingy.missileUIDS[uIDidx];
                                lastReciever.thisML = hpML.ml;
                                lastReciever.idx = j;
                                uIDidx++;
                            }
                        }
                    }
                }
            }
        }
            FuelTank fuelTank = newVehicle.GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("Failed to get fuel tank on " + newVehicle.name);
        fuelTank.startingFuel = loadout.normalizedFuel * fuelTank.maxFuel;
        fuelTank.SetNormFuel(loadout.normalizedFuel);

        players.Add(new Player(new CSteamID(message.csteamID), newVehicle, message.vehicle, message.networkID));
    }
    /// <summary>
    /// Finds the prefabs which are used for spawning the other players on our client
    /// </summary>
    private static void SetPrefabs()
    {
        UnitCatalogue.UpdateCatalogue();
        av42cPrefab = UnitCatalogue.GetUnitPrefab("AV-42CAI");
        fa26bPrefab = UnitCatalogue.GetUnitPrefab("FA-26B AI");
        f45Prefab = UnitCatalogue.GetUnitPrefab("F-45A AI");

        if (!av42cPrefab)
            Debug.LogError("Couldn't find the prefab for the AV-42C");
        if (!fa26bPrefab)
            Debug.LogError("Couldn't find the prefab for the F/A-26B");
        if (!f45Prefab)
            Debug.LogError("Couldn't find the prefab for the F-45A");
    }
    /// <summary>
    /// Creates the spawn points for the other players.
    /// </summary>
    /// <param name="startPosition">The location of where the first spawn should be</param>
    public static void GenerateSpawns(Transform startPosition)
    {
        spawnPoints = new List<Transform>(spawnsCount);
        GameObject lastSpawn;
        for (int i = 1; i <= spawnsCount; i++)
        {
            lastSpawn = new GameObject("MP Spawn " + i);
            lastSpawn.AddComponent<FloatingOriginTransform>();
            lastSpawn.transform.position = startPosition.position + startPosition.TransformVector(new Vector3(spawnSpacing * i, 0, 0));
            lastSpawn.transform.rotation = startPosition.rotation;
            spawnPoints.Add(lastSpawn.transform);
            Debug.Log("Created MP Spawn at " + lastSpawn.transform.position);
        }
    }
    /// <summary>
    /// Returns a spawn point which isn't blocked by another player
    /// </summary>
    /// <returns>A free spawn point</returns>
    public static Transform FindFreeSpawn()
    {
        //Later on this will check the spawns if there is anyone sitting still at this spawn
        if (spawnPoints == null)
        {
            Transform returnValue = new GameObject().transform;
            Debug.LogError("Spawn Points was null, we can't find a spawn point.\nReturning a new transform at " + returnValue.position);
            return returnValue;
        }
        return spawnPoints[UnityEngine.Random.Range(0, spawnsCount - 1)];
    }

    public static void OnDisconnect()
    {
        spawnPoints = new List<Transform>();
        spawnRequestQueue = new Queue<CSteamID>();
        playersToSpawnQueue = new Queue<Packet>();
        playersToSpawnIdQueue = new Queue<CSteamID>();
        AIManager.AIsToSpawnQueue = new Queue<Packet>();
        spawnedVehicles = new List<ulong>();
        Networker.hostLoaded = false;
        gameLoaded = false;
        localUID = 0;
    }
}