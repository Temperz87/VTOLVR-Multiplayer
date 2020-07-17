using System;
[Serializable]
public class Message_WingFold : Message
{
    public bool folded;
    public ulong UID;

    public Message_WingFold(bool folded, ulong uID)
    {
        this.folded = folded;
        UID = uID;
        type = MessageType.WingFold;
    }
}