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
    // public int playerCount;

    public Message_SpawnAIVehicle(string aiVehicleName, string unitName, Vector3D position, Vector3D rotation, ulong networkID, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool Aggresive, int unitInstanceID)
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
        // this.playerCount = playerCount;
        type = MessageType.SpawnAiVehicle;
    }
}