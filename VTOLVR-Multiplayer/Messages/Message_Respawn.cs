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
    public VTOLVehicles vehicle;
    public Message_Respawn(ulong uID, Vector3D position, Quaternion rotation, bool leftTeam, string name, VTOLVehicles vehicle)
    {
        UID = uID;
        this.position = position;
        this.rotation = rotation;
        this.isLeftie = leftTeam;
        this.tagName = name;
        this.vehicle = vehicle;
        type = MessageType.Respawn;
    }
}