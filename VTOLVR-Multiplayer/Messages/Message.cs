using System;

[Serializable]
public class Message
{
    public Message() { }
    public Message(MessageType type) { this.type = type; }
    /// <summary>
    /// The type of message we are sending, this needs to be set 
    /// otherwise we won't know what to convert the message to.
    /// </summary>
    public MessageType type;
}
