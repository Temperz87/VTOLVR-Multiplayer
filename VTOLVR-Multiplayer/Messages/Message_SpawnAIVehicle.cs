using System;
using UnityEngine;
[Serializable]
public class Message_SpawnAIVehicle : Message
{
    public string aiVehicleName;
    public string unitName;
    public Vector3D position;
    public SerializableQuaternion rotation;
    // public ulong csteamID;
    public ulong rootActorNetworkID;
    public ulong[] networkIDs;
    public HPInfo[] hpLoadout;
    public int[] cmLoadout;
    public float normalizedFuel;
    public bool Aggresive;
    public int unitInstanceID;
    public ulong[] radarIDs;
    public ulong[] IRSamMissiles;
    public PhoneticLetters unitGroup;
    public bool redfor;
    public bool hasAirport;
    public bool hasGroup { get; private set; }
    // public int playerCount;

    public Message_SpawnAIVehicle(string aiVehicleName, string unitName, bool redforb, bool airport, Vector3D position, Quaternion rotation, ulong rootActorNetworkID, ulong[] networkIDs, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool Aggresive, int unitInstanceID, PhoneticLetters unitGroup, ulong[] radarIDs, ulong[] IRSAMissiles)
    {
        this.aiVehicleName = aiVehicleName;
        this.unitName = unitName;
        this.position = position;
        this.rotation = rotation;
        this.rootActorNetworkID = rootActorNetworkID;
        this.networkIDs = networkIDs;
        this.hpLoadout = hpLoadout;
        this.cmLoadout = cmLoadout;
        this.normalizedFuel = normalizedFuel;
        this.Aggresive = Aggresive;
        this.unitInstanceID = unitInstanceID;
        this.unitGroup = unitGroup;
        this.radarIDs = radarIDs;
        redfor = redforb;
        hasGroup = true;
        IRSamMissiles = IRSAMissiles;
        hasAirport = airport;
        // this.playerCount = playerCount;
        type = MessageType.SpawnAiVehicle;
    }
    public Message_SpawnAIVehicle(string aiVehicleName, string unitName, bool redforb, bool airport, Vector3D position, Quaternion rotation, ulong rootActorNetworkID, ulong[] networkIDs, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool Aggresive, int unitInstanceID, ulong[] radarIDs, ulong[] IRSAMissiles)
    {
        this.aiVehicleName = aiVehicleName;
        this.unitName = unitName;
        this.position = position;
        this.rotation = rotation;
        this.rootActorNetworkID = rootActorNetworkID;
        this.networkIDs = networkIDs;
        this.hpLoadout = hpLoadout;
        this.cmLoadout = cmLoadout;
        this.normalizedFuel = normalizedFuel;
        this.Aggresive = Aggresive;
        this.unitInstanceID = unitInstanceID;
        this.radarIDs = radarIDs;
        hasGroup = false;
        redfor = redforb;
        IRSamMissiles = IRSAMissiles;
        hasAirport = airport;
        // this.playerCount = playerCount;
        type = MessageType.SpawnAiVehicle;
    }
}