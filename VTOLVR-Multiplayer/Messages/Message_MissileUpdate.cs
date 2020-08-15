using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileUpdate : Message
{
    public ulong networkUID;
    public Vector3D targetPosition;
    public SerializableQuaternion seekerRotation;
    public bool hasExploded;
    public Missile.GuidanceModes guidanceMode;
    public ulong MissileLauncher;
    public int idx;
    public ulong radarLock;
    public Message_MissileUpdate(ulong uid)
    {
        networkUID = uid;
        type = MessageType.MissileUpdate;
    }
}