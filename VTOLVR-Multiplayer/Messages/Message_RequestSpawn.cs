using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public Message_RequestSpawn() { }
    public Message_RequestSpawn(bool team) { this.type = MessageType.RequestSpawn; this.teaml = team; }
    /// <summary>
    /// The type of message we are sending, this needs to be set 
    /// otherwise we won't know what to convert the message to.
    /// </summary>
    public MessageType type;
    public bool teaml;
}
