using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Message_SpawnPlayerVehicle : Message
{
    public VTOLVehicles vehicle;
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
    public string skinName;
    // public int playerCount;
    public Message_SpawnPlayerVehicle(VTOLVehicles vehicle, Vector3D position, Quaternion rotation, ulong csteamID, ulong networkID, HPInfo[] hpLoadout, int[] cmLoadout, float normalizedFuel, bool isLeftie, string tagName, long idiscord, string skinName)
    {
        this.vehicle = vehicle;
        this.position = position;
        this.rotation = rotation;
        this.csteamID = csteamID;
        this.networkID = networkID;
        this.hpLoadout = hpLoadout;
        this.cmLoadout = cmLoadout;
        this.normalizedFuel = normalizedFuel;
        this.leftie = isLeftie;
        this.nameTag = tagName;
        this.discordID = idiscord;
        this.skinName = skinName;
        // this.playerCount = playerCount;
        type = MessageType.SpawnPlayerVehicle;
    }
}

[Serializable]
public class Message_SetFrequency: Message
{
    public string source;
    public int freq;
    // public int playerCount;

    public Message_SetFrequency(string isource,int ifreq)
    {
        this.source = isource;
        this.freq = ifreq;
        // this.playerCount = playerCount;
        type = MessageType.SetFrequency;
    }
}