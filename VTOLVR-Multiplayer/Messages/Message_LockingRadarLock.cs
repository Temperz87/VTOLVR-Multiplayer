using System;
using System.Collections.Generic;

[Serializable]
public class Message_LockingRadarUpdate : Message
{
    public struct radarLock
    {
        public ulong actorUID;
        public bool isLocked;
        public radarLock(ulong actorUID, bool isLocked)
        {
            this.actorUID = actorUID;
            this.isLocked = isLocked;
        }
    }
    public List<radarLock> radarLocks;
    public ulong senderUID;

    public Message_LockingRadarUpdate(List<radarLock> radarLocks, ulong senderUID)
    {
        this.radarLocks = radarLocks;
        this.senderUID = senderUID;
    }
}
