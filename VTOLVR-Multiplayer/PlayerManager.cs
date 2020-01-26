using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
public static class PlayerManager
{
    public static List<Transform> spawnPoints { private set; get; }

    private static float spawnSpacing = 20;
    private static int spawnsCount = 4;
    /// <summary>
    /// This is the queue for people waiting to get a spawn point,
    /// incase the host hasn't loaded in, in time.
    /// </summary>
    private static Queue<CSteamID> spawnRequestQueue = new Queue<CSteamID>();
    private static Queue<Packet> playersToSpawnQueue = new Queue<Packet>();
    private static bool hostLoaded, gameLoaded;
    private static GameObject av42cPrefab, fa26bPrefab, f45Prefab;
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
    /// <param name="customMap"></param>
    public static void MapLoaded(VTMapCustom customMap = null) //Clients and Hosts
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = Vector3.zero;
        cube.transform.localScale = new Vector3(50, 1000, 50);
        Debug.Log("The map has loaded");
        gameLoaded = true;
        //As a client, when the map has loaded we are going to request a spawn point from the host
        SetPrefabs();
        if (!Networker.isHost)
            Networker.SendP2P(Networker.hostID, new Message(MessageType.RequestSpawn), EP2PSend.k_EP2PSendReliable);
        else
        {
            hostLoaded = true;
            GameObject localVehicle = VTOLAPI.instance.GetPlayersVehicleGameObject();
            if (localVehicle != null)
            {
                GenerateSpawns(localVehicle.transform);
                SendSpawnVehicle(localVehicle, localVehicle.transform.position, localVehicle.transform.rotation.eulerAngles, Networker.GenerateNetworkUID());
            }
            else
                Debug.Log("Local vehicle for host was null");
            if (spawnRequestQueue.Count != 0)
                SpawnRequestQueue();
            Networker.alreadyInGame = true;
        }

        if (playersToSpawnQueue.Count != 0)
        {
            for (int i = 0; i < playersToSpawnQueue.Count; i++)
            {
                SpawnVehicle(playersToSpawnQueue.Dequeue());
            }
        }
    }

    /// <summary>
    /// This gives all the people waiting their spawn points
    /// </summary>
    private static void SpawnRequestQueue() //Host Only
    {
        Debug.Log($"Giving {spawnRequestQueue.Count} people their spawns");
        Transform lastSpawn;
        for (int i = 0; i < spawnRequestQueue.Count; i++)
        {
            lastSpawn = FindFreeSpawn();
            Networker.SendP2P(
                spawnRequestQueue.Dequeue(),
                new Message_RequestSpawn_Result(new Vector3D(lastSpawn.position), new Vector3D(lastSpawn.rotation.eulerAngles), Networker.GenerateNetworkUID()),
                EP2PSend.k_EP2PSendReliable);
        }
    }
    /// <summary>
    /// This is when a client has requested a spawn point from the host,
    /// the host gets a spawn point and sends back the position
    /// </summary>
    /// <param name="packet">The Message</param>
    /// <param name="sender">The client who sent it</param>
    public static void RequestSpawn(Packet packet, CSteamID sender) //Host Only
    {
        Debug.Log("A player has requested for a spawn point");
        if (!hostLoaded)
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
        Networker.SendP2P(sender, new Message_RequestSpawn_Result(new Vector3D(spawn.position), new Vector3D(spawn.rotation.eulerAngles), Networker.GenerateNetworkUID()), EP2PSend.k_EP2PSendReliable);
    }
    /// <summary>
    /// When the client receives a P2P message of their spawn point, 
    /// this will move them to that location before sending their vehicle 
    /// to the host.
    /// </summary>
    /// <param name="packet">The message sent over the network</param>
    public static void RequestSpawn_Result(Packet packet) //Clients Only
    {
        Debug.Log("The host has sent back our spawn point");
        Message_RequestSpawn_Result result = (Message_RequestSpawn_Result)((PacketSingle)packet).message;
        Debug.Log($"We need to move to {result.position} : {result.rotation}");

        GameObject localVehicle = VTOLAPI.instance.GetPlayersVehicleGameObject();
        if (localVehicle == null)
        {
            Debug.LogError("The local vehicle was null");
            return;
        }
        localVehicle.transform.position = result.position.toVector3;
        localVehicle.transform.rotation = Quaternion.Euler(result.rotation.toVector3);
        SendSpawnVehicle(localVehicle, result.position.toVector3, result.rotation.toVector3, result.vehicleUID);
    }
    /// <summary>
    /// Sends the message to other clients to spawn their vehicles
    /// </summary>
    /// <param name="localVehicle">The local clients gameobject</param>
    public static void SendSpawnVehicle(GameObject localVehicle, Vector3 pos, Vector3 rot, ulong UID) //Both
    {
        Debug.Log("Sending our location to spawn our vehicle");
        VTOLVehicles currentVehicle = VTOLAPI.GetPlayersVehicleEnum();
        players.Add(new Player(SteamUser.GetSteamID(), localVehicle,currentVehicle, UID));

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

        if (Multiplayer.SoloTesting)
            pos += new Vector3(20, 0, 0);

        if (!Networker.isHost)
        {
            Networker.SendP2P(Networker.hostID,
                new Message_SpawnVehicle(currentVehicle, new Vector3D(pos), new Vector3D(rot), SteamUser.GetSteamID().m_SteamID, UID),
                EP2PSend.k_EP2PSendReliable);
        }
    }
    /// <summary>
    /// When the user has received a message of spawn vehicle, 
    /// this creates the vehilc and removes any thing which shouldn't
    /// be on it.
    /// </summary>
    /// <param name="packet">The message</param>
    public static void SpawnVehicle(Packet packet)
    {
        Debug.Log("Recived a Spawn Vehicle Message");

        if (!gameLoaded)
        {
            Debug.LogWarning("Our game isn't loaded, adding spawn vehicle to queue");
            playersToSpawnQueue.Enqueue(packet);
            return;
        }
        
        Message_SpawnVehicle message = (Message_SpawnVehicle)((PacketSingle)packet).message;
        if (Networker.isHost)
        {
            Debug.Log("Telling other clients about new player and new player about other clients");
            Networker.SendExcludeP2P(new CSteamID(message.csteamID), message, EP2PSend.k_EP2PSendReliable);
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].cSteamID == SteamUser.GetSteamID())
                {
                    Debug.LogWarning("Skiping this one as it's the host");
                    continue;
                }
                //We first send the new player to an existing spawned in player
                Networker.SendP2P(players[i].cSteamID, message, EP2PSend.k_EP2PSendReliable);
                //Then we send this current player to the new player.
                Networker.SendP2P(new CSteamID(message.csteamID), 
                    new Message_SpawnVehicle(
                        players[i].vehicleName,
                        VTMapManager.WorldToGlobalPoint(players[i].vehicle.transform.position),
                        new Vector3D(players[i].vehicle.transform.rotation.eulerAngles),
                        players[i].cSteamID.m_SteamID,
                        players[i].vehicleUID), 
                    EP2PSend.k_EP2PSendReliable);
                Debug.Log($"We have told {players[i].cSteamID.m_SteamID} about the new player ({message.csteamID}) and the other way round");
            }
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
                newVehicle = GameObject.Instantiate(fa26bPrefab, message.position.toVector3, Quaternion.Euler(message.rotation.toVector3));
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

        players.Add(new Player(new CSteamID(message.csteamID), newVehicle,message.vehicle,message.networkID));
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
            lastSpawn.AddComponent<FloatingOriginShifter>();
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
}