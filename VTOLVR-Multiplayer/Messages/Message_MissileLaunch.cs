using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileLaunch : Message
{
    public ulong networkUID;
    public ulong ownerUID;
    public ulong targetActorUID;
    public Vector3D targetPosition;
    public SerializableQuaternion seekerRotation;
    public Missile.GuidanceModes guidanceType;

    public Message_MissileLaunch(ulong uid)
    {
        networkUID = uid;
        type = MessageType.MissileLaunch;
    }
}