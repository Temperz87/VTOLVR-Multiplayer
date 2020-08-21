using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileUpdate : Message
{
    public ulong networkUID;
    public SerializableQuaternion seekerRotation;

    public Message_MissileUpdate(ulong uid, Quaternion seekerRotation)//unused, maybe usefull in the future
    {
        networkUID = uid;
        this.seekerRotation = seekerRotation;
        type = MessageType.MissileUpdate;
    }
}