using System;

[Serializable]
public class Message
{
    public Message() { }
    public Message(MessageType type, ulong id = 0)
    {
        this.type = type;
        this.id = id;
    }
    /// <summary>
    /// The type of message we are sending, this needs to be set 
    /// otherwise we won't know what to convert the message to.
    /// </summary>
    public MessageType type;
    public ulong id;
}
