using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileDetonate : Message
{
    public ulong networkUID;
    public Vector3D targetPosition;

    public Message_MissileDetonate(ulong uid)
    {
        networkUID = uid;
        type = MessageType.MissileDetonate;
    }
}

public class Message_MissileDamage : Message
{
    public ulong networkUID;
    public Vector3D targetPosition;
    public ulong actorTobeDamaged;
    public float damage;

    public Message_MissileDamage(ulong uid)
    {
        networkUID = uid;
        type = MessageType.MissileDamage;
    }
}