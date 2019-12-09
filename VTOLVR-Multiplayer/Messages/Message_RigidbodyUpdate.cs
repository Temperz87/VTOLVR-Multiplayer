using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Message_RigidbodyUpdate : Message
{
    public V3 velocity, angularVelocity;
    public V3 position;
    public ulong networkUID;

    public Message_RigidbodyUpdate(V3 velocity, V3 angularVelocity, V3 position, ulong networkUID)
    {
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.position = position;
        this.networkUID = networkUID;
        type = MessageType.RigidbodyUpdate;
    }
}