using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Message_RigidbodyUpdate : Message
{
    public Vector3D velocity, angularVelocity;
    public Vector3D position, rotation;
    public ulong networkUID;

    public Message_RigidbodyUpdate(Vector3D velocity, Vector3D angularVelocity, Vector3D position, Vector3D rotation, ulong networkUID)
    {
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.position = position;
        this.rotation = rotation;
        this.networkUID = networkUID;
        type = MessageType.RigidbodyUpdate;
    }
}