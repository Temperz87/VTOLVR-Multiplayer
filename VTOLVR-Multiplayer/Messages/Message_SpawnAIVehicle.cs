using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Message_SpawnAIVehicle : Message
{
    public string aiVehicleName;
    public string unitName;
    public Vector3D position;
    public Vector3D rotation;
    // public ulong csteamID;
    public ulong networkID;
    public HPInfo[] hpLoadout;
    public int[] cmLoadout;
    public float normalizedFuel;
    public bool Aggresive;
    public int unitInstanceID;
    public ulong[] radarIDs;
    public ulong[] IRSamMissiles;
    public ulong[] subActors;
    public ulong[,] subIRIDS;
    public PhoneticLetters unitGroup;
    public bool hasGroup { get; private set; }
    // public int playerCount;

    public Message_SpawnAIVehicle(string aiVehicleName, string unitName, Vector3D position, Vector3D rotation, ulong networkID, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool Aggresive, int unitInstanceID, PhoneticLetters unitGroup, ulong[] radarIDs, ulong[] IRSAMissiles, ulong[] subActors, ulong[,] subIRIDS)
    {
        this.aiVehicleName = aiVehicleName;
        this.unitName = unitName;
        this.position = position;
        this.rotation = rotation;
        this.networkID = networkID;
        this.hpLoadout = hpLoadout;
        this.cmLoadout = cmLoadout;
        this.normalizedFuel = normalizedFuel;
        this.Aggresive = Aggresive;
        this.unitInstanceID = unitInstanceID;
        this.unitGroup = unitGroup;
        this.radarIDs = radarIDs;
        hasGroup = true;
        this.IRSamMissiles = IRSAMissiles;
        this.subActors = subActors;
        this.subIRIDS = subIRIDS;
        // this.playerCount = playerCount;
        type = MessageType.SpawnAiVehicle;
    }
    public Message_SpawnAIVehicle(string aiVehicleName, string unitName, Vector3D position, Vector3D rotation, ulong networkID, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool Aggresive, int unitInstanceID, ulong[] radarIDs, ulong[] IRSAMissiles, ulong[] subActors, ulong[,] subIRIDS)
    {
        this.aiVehicleName = aiVehicleName;
        this.unitName = unitName;
        this.position = position;
        this.rotation = rotation;
        this.networkID = networkID;
        this.hpLoadout = hpLoadout;
        this.cmLoadout = cmLoadout;
        this.normalizedFuel = normalizedFuel;
        this.Aggresive = Aggresive;
        this.unitInstanceID = unitInstanceID;
        this.radarIDs = radarIDs;
        hasGroup = false;
        this.IRSamMissiles = IRSAMissiles;
        this.subActors = subActors;
        this.subIRIDS = subIRIDS;
        // this.playerCount = playerCount;
        type = MessageType.SpawnAiVehicle;
    }
}