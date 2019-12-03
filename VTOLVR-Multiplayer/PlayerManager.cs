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
    private static bool hostLoaded;
    public static void MapLoaded(VTMapCustom customMap = null) //Clients and Hosts
    {
        Debug.Log("The map has loaded");
        Networker.CreateWorldCentre();
        //As a client, when the map has loaded we are going to request a spawn point from the host
        if (!Networker.isHost)
            Networker.SendP2P(Networker.hostID, new Message(MessageType.RequestSpawn), EP2PSend.k_EP2PSendReliable);
        else
        {
            hostLoaded = true;
            GameObject localVehicle = VTOLAPI.instance.GetPlayersVehicleGameObject();
            if (localVehicle != null)
                SendSpawnVehicle(localVehicle);
            else
                Debug.Log("Local vehicle for host was null");
            if (spawnRequestQueue.Count != 0)
                SpawnRequestQueue();
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
                new Message_RequestSpawn_Result(lastSpawn.position, lastSpawn.rotation),
                EP2PSend.k_EP2PSendReliable);
        }
    }
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
        Networker.SendP2P(sender, new Message_RequestSpawn_Result(spawn.position, spawn.rotation), EP2PSend.k_EP2PSendReliable);
    }

    public static void RequestSpawn_Result(Packet packet) //Clients Only
    {
        Debug.Log("The host has sent back our spawn point");
        Message_RequestSpawn_Result result = (Message_RequestSpawn_Result)((PacketSingle)packet).message;
        Debug.Log($"We need to move to {result.position} : {result.rotation.eulerAngles}");

        GameObject localVehicle = VTOLAPI.instance.GetPlayersVehicleGameObject();
        if (localVehicle == null)
        {
            Debug.LogError("The local vehicle was null");
            return;
        }
        localVehicle.transform.position = result.position;
        localVehicle.transform.rotation = result.rotation;
        SendSpawnVehicle(localVehicle);
    }
    public static void SendSpawnVehicle(GameObject localVehicle) //Both
    {
        Debug.Log("Sending our location to spawn our vehicle");
        VTOLVehicles currentVehicle = VTOLAPI.GetPlayersVehicleEnum();
        ulong id = Networker.GenerateNetworkUID();
        localVehicle.AddComponent<RigidbodyNetworker_Sender>().networkUID = id;
        if (Networker.isHost)
        {
            Networker.SendGlobalP2P(new Message_SpawnVehicle(
                currentVehicle,
                localVehicle.transform.position,
                localVehicle.transform.rotation,
                SteamUser.GetSteamID().m_SteamID,
                id),
                EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            Networker.SendP2P(Networker.hostID,
                new Message_SpawnVehicle(currentVehicle, localVehicle.transform.position, localVehicle.transform.rotation, SteamUser.GetSteamID().m_SteamID,id),
                EP2PSend.k_EP2PSendReliable);
        }
    }
    public static void SpawnVehicle(Packet packet)
    {
        Debug.Log("Recived a Spawn Vehicle Message");

        Debug.Log("Sending to other clients");
        Message_SpawnVehicle message = (Message_SpawnVehicle)((PacketSingle)packet).message;
        Networker.SendExcludeP2P(new CSteamID(message.csteamID), message, EP2PSend.k_EP2PSendReliable);
        GameObject newVehicle = GameObject.Instantiate(PilotSaveManager.currentVehicle.vehiclePrefab);
        newVehicle.name = $"Client [{message.csteamID}]";
        newVehicle.transform.position = message.position;
        newVehicle.transform.rotation = message.rotation;
        CamRigRotationInterpolator[] camRig = GameObject.FindObjectsOfType<CamRigRotationInterpolator>();
        Debug.Log("Trying to Destroy camera rig");
        Debug.Log($"There are {camRig.Length} CamRigRotationInterpolator's");
        GameObject CameraRigParent = null;
        for (int i = 0; i < camRig.Length; i++)
        {
            Transform parent = camRig[i].transform.parent.parent;
            if (parent.name == newVehicle.name)
            {
                CameraRigParent = camRig[i].gameObject;
                break;
            }
            else if (parent.name != "SEVTF" || parent.name != "FA-26B")
            {
                if (parent.parent.name == newVehicle.name)
                {
                    CameraRigParent = camRig[i].gameObject;
                    break;
                }
            }
        }
        if (CameraRigParent == null)
            Debug.LogError("We didn't find CameraRigParent");
        else
            Debug.Log("We found CameraRigParent");

        GameObject.Destroy(CameraRigParent);

        RigidbodyNetworker_Receiver rbNetworker = newVehicle.AddComponent<RigidbodyNetworker_Receiver>();
        Networker.RigidbodyUpdate += rbNetworker.RigidbodyUpdate;
        rbNetworker.networkUID = message.networkID;
    }

    public static void GenerateSpawns(Transform startPosition)
    {
        spawnPoints = new List<Transform>(spawnsCount);
        GameObject lastSpawn;
        for (int i = 0; i < spawnsCount; i++)
        {
            lastSpawn = new GameObject("MP Spawn Point", typeof(FloatingOriginShifter));
            lastSpawn.transform.position = startPosition.position + startPosition.TransformVector(new Vector3(spawnSpacing * i, 0, 0));
            spawnPoints.Add(lastSpawn.transform);
            Debug.Log("Created MP Spawn at " + lastSpawn.transform.position);
        }
    }

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