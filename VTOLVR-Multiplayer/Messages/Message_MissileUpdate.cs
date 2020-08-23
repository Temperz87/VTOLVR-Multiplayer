using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileUpdate : Message
{
    public ulong networkUID;
    public Vector3D targetPosition;
    public Vector3D lastTargetPosition;
    public Message_MissileUpdate(ulong uid, Vector3D targetPosition, Vector3D lastTargetPosition)//unused, maybe usefull in the future
    {
        networkUID = uid;
        this.targetPosition = targetPosition;
        this.lastTargetPosition = lastTargetPosition;
        type = MessageType.MissileUpdate;
    }
}