using System;
[Serializable]
public class Message_Death : Message
{
    public ulong UID;

    public Message_Death(ulong uID)
    {
        UID = uID;
        type = MessageType.Death;
    }
}
