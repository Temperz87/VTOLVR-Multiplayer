using System;
[Serializable]
public class Message_Ack : Message
{
    public ulong UID;

    public Message_Ack(ulong uID)
    {
        UID = uID;
        type = MessageType.Ack;
    }
}
