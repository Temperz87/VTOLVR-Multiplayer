using System;

[Serializable]
public class Message_HostLoaded : Message
{
    public bool isReady;
    public Message_HostLoaded(bool isReady)
    {
        this.isReady = isReady;
        type = MessageType.HostLoaded;
    }
}