using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileLaunch : Message
{
    public ulong networkUID;
    public Vector3D targetPosition;
    public SerializableQuaternion seekerRotation;

    public Message_MissileLaunch(ulong uid, Quaternion seekerRotation)
    {
        networkUID = uid;
        this.seekerRotation = seekerRotation;
        type = MessageType.MissileLaunch;
    }
}