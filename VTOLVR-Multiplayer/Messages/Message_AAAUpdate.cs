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
    public ulong gunID;

    public Message_AAAUpdate(bool isFiring, ulong networkUID, ulong gunID)
    {
        this.isFiring = isFiring;
        this.networkUID = networkUID;
        this.gunID = gunID;
        type = MessageType.AAAUpdate;
    }
}

[Serializable]
class Message_RocketLauncherUpdate : Message
{
    public ulong networkUID;

    public Message_RocketLauncherUpdate(ulong networkUID)
    {
        this.networkUID = networkUID;
        type = MessageType.RocketLauncherUpdate;
    }
}