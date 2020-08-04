using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
class Message_AAAUpdate : Message
{
    public bool isFiring;
    public ulong networkUID;

    public Message_AAAUpdate(bool isFiring, ulong networkUID)
    {
        this.isFiring = isFiring;
        this.networkUID = networkUID;
        type = MessageType.AAAUpdate;
    }
}