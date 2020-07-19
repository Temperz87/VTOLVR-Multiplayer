using System;
using System.Collections.Generic;

[Serializable]
public class Message_LockingRadarUpdate : Message
{
    public ulong actorUID;
    public bool isLocked;
    public ulong senderUID;

    public Message_LockingRadarUpdate(ulong actorUID, bool isLocked, ulong senderUID)
    {
        this.actorUID = actorUID;
        this.isLocked = isLocked;
        this.senderUID = senderUID;
    }
}
