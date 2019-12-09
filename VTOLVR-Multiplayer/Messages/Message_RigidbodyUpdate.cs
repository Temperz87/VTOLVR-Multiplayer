using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Message_RigidbodyUpdate : Message
{
    public Vector3 velocity, angularVelocity;
    public Vector3 position;
    public ulong networkUID;

    public Message_RigidbodyUpdate(Vector3 velocity, Vector3 angularVelocity, Vector3 position, ulong networkUID)
    {
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.position = position;
        this.networkUID = networkUID;
        type = MessageType.RigidbodyUpdate;
    }
}