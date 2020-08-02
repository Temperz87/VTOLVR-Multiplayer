using System;
[Serializable]
public class Message_SamUpdate : Message
{
    public ulong senderUID;
    public ulong actorUID;
    public ulong missileUID;
    public Message_SamUpdate(ulong actorUID, ulong missileUID, ulong senderUID)
    {
        this.actorUID = actorUID;
        this.missileUID = missileUID;
        this.senderUID = senderUID;
        type = MessageType.SamUpdate;
    }
}
