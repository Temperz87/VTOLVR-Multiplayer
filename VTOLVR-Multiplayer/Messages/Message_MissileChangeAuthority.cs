using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class Message_MissileChangeAuthority : Message
{
    public ulong networkUID;
    public ulong newOwnerUID;
    public Message_MissileChangeAuthority(ulong uid, ulong newOwnerUID)
    {
        networkUID = uid;
        this.newOwnerUID = newOwnerUID;
        type = MessageType.MissileChangeAuthority;
    }
}