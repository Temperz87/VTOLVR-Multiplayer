using System;
[Serializable]
public class Message_Death : Message
{
    public ulong UID;
    public bool immediate;

    public Message_Death(ulong uID,bool Immediate)
    {
        UID = uID;
        this.immediate = Immediate;
        type = MessageType.Death;
    }
}
