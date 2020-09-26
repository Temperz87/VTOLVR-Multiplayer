using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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