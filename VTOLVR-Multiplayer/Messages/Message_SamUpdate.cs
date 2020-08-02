using System;
[Serializable]
public class Message_SamUpdate : Message
{
    public ulong senderUID;
    public ulong actorUID;

    public Message_SamUpdate(ulong actorUID, ulong senderUID)
    {
        this.senderUID = senderUID;
        this.actorUID = actorUID;
        type = MessageType.SamUpdate;
    }
}
