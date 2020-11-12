using System;
using UnityEngine;
[Serializable]
public class Message_SpawnPlayerVehicle : Message
{
    public string vehicleName;
    public Vector3D position;
    public SerializableQuaternion rotation;
    public ulong csteamID;
    public ulong networkID;
    public HPInfo[] hpLoadout;
    public int[] cmLoadout;
    public float normalizedFuel;
    public bool leftie;
    public string nameTag;
    public long discordID;
    // public int playerCount;

    public Message_SpawnPlayerVehicle(string vehicleName, Vector3D position, Quaternion rotation, ulong csteamID, ulong networkID, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool isLeftie, string tagName, long idiscord)
    {
        this.vehicleName = vehicleName;
        this.position = position;
        this.rotation = rotation;
        this.csteamID = csteamID;
        this.networkID = networkID;
        this.hpLoadout = hpLoadout;
        this.cmLoadout = cmLoadout;
        this.normalizedFuel = normalizedFuel;
        leftie = isLeftie;
        nameTag = tagName;
        discordID = idiscord;
        // this.playerCount = playerCount;
        type = MessageType.SpawnPlayerVehicle;
    }
}

[Serializable]
public class Message_SetFrequency : Message
{
    public string source;
    public int freq;
    // public int playerCount;

    public Message_SetFrequency(string isource, int ifreq)
    {
        source = isource;
        freq = ifreq;
        // this.playerCount = playerCount;
        type = MessageType.SetFrequency;
    }
}