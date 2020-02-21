using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileUpdate : Message
{
    public ulong networkUID;
    public Vector3D position, velocity, rotation, targetPosition;
    public bool hasMissed;

    public Message_MissileUpdate(ulong uid)
    {
        networkUID = uid;
        type = MessageType.MissileUpdate;
    }
}