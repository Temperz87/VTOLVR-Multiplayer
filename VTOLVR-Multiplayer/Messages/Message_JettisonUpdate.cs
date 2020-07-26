using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
class Message_JettisonUpdate : Message
{
    public int[] toJettison;
    public ulong networkUID;

    public Message_JettisonUpdate(int[] toJettison, ulong networkUID)
    {
        this.toJettison = toJettison;
        this.networkUID = networkUID;
        type = MessageType.JettisonUpdate;
    }
}