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
            {
                GenerateSpawns(localVehicle.transform);
                Transform temp = FindFreeSpawn();
                FlightInfo flightInfo = localVehicle.GetComponent<FlightInfo>();
                flightInfo.PauseGCalculations();
                localVehicle.GetComponent<PlayerVehicleSetup>().LandVehicle(null);
                Debug.Log($"Moving local vehicle from {localVehicle.GetComponent<Rigidbody>().position} to {temp.position}");
                localVehicle.GetComponent<Rigidbody>().position = temp.position;
                localVehicle.transform.position = temp.position;
                Debug.Log($"Moved local vehicle to {localVehicle.GetComponent<Rigidbody>().position} : {localVehicle.transform.position}");
                GameObject.CreatePrimitive(PrimitiveType.Cube).transform.position = localVehicle.transform.position;
                SendSpawnVehicle(localVehicle);
                flightInfo.UnpauseGCalculations();
                
            }                
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
                new Message_RequestSpawn_Result(new Vector3D(lastSpawn.position), new Vector3D(lastSpawn.rotation.eulerAngles)),
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
        Networker.SendP2P(sender, new Message_RequestSpawn_Result(new Vector3D(spawn.position), new Vector3D(spawn.rotation.eulerAngles)), EP2PSend.k_EP2PSendReliable);
    }

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
                new Vector3D(localVehicle.transform.position),
                new Vector3D(localVehicle.transform.rotation.eulerAngles),
                SteamUser.GetSteamID().m_SteamID,
                id),
                EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            Networker.SendP2P(Networker.hostID,
                new Message_SpawnVehicle(currentVehicle, new Vector3D(localVehicle.transform.position), new Vector3D(localVehicle.transform.rotation.eulerAngles), SteamUser.GetSteamID().m_SteamID,id),
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
        newVehicle.transform.position = message.position.toVector3;
        newVehicle.transform.rotation = Quaternion.Euler(message.rotation.toVector3);
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

        Debug.Log("Destroying the suit");
        Transform suit = CameraRigParent.transform.parent.Find("RiggedSuit");
        if (suit != null)
        {
            Debug.Log("Found the suit, destorying it");
            GameObject.Destroy(suit.gameObject);
        }
        GameObject.Destroy(CameraRigParent);

        RigidbodyNetworker_Receiver rbNetworker = newVehicle.AddComponent<RigidbodyNetworker_Receiver>();
        Networker.RigidbodyUpdate += rbNetworker.RigidbodyUpdate;
        rbNetworker.networkUID = message.networkID;
    }

    public static void GenerateSpawns(Transform startPosition)
    {
        spawnPoints = new List<Transform>(spawnsCount);
        GameObject lastSpawn;
        for (int i = 1; i <= spawnsCount; i++)
        {
            lastSpawn = new GameObject("MP Spawn " + i);
            lastSpawn.AddComponent<FloatingOriginShifter>();
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