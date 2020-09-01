using System;
using UnityEngine;

[Serializable]
public class Message_Respawn : Message
{
    public ulong UID;
    public Vector3D position;
    public SerializableQuaternion rotation;
    public bool isLeftie;
    public string tagName;
    public Message_Respawn(ulong uID, Vector3D position, Quaternion rotation, bool leftTeam, string name)
    {
        UID = uID;
        this.position = position;
        this.rotation = rotation;
        this.isLeftie = leftTeam;
        this.tagName = name;
        type = MessageType.Respawn;
    }
}