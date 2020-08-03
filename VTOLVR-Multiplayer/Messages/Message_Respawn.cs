using System;
using UnityEngine;

[Serializable]
public class Message_Respawn : Message
{
    public ulong UID;
    public Vector3D position;
    public SerializableQuaternion rotation;

    public Message_Respawn(ulong uID, Vector3D position, Quaternion rotation)
    {
        UID = uID;
        this.position = position;
        this.rotation = rotation;
        type = MessageType.Respawn;
    }
}