using System;
using UnityEngine;

[Serializable]
public class Message_RequestSpawn_Result : Message
{
    public Vector3D position;
    public SerializableQuaternion rotation;
    public ulong vehicleUID;
    public int playerCount;
    public Message_RequestSpawn_Result(Vector3D position, Quaternion rotation, ulong vehicleUID, int playerCount)
    {
        this.position = position;
        this.rotation = rotation;
        this.vehicleUID = vehicleUID;
        this.playerCount = playerCount;
        type = MessageType.RequestSpawn_Result;
    }
}


[Serializable]
public class Message_RequestSpawn : Message
{
    public bool teaml;
    public ulong senderSteamID;
    public Message_RequestSpawn(bool team, ulong senderSteamID)
    {
        type = MessageType.RequestSpawn;
        teaml = team;
        this.senderSteamID = senderSteamID;

    }
}
