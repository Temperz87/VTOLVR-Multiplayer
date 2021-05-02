using System;
[Serializable]
public class Message_RadarDetectedActor : Message
{
    public ulong detectedUID;
    public ulong senderUID;
    public Message_RadarDetectedActor(ulong detectedUID, ulong senderUID)
    {
        this.detectedUID = detectedUID;
        this.senderUID = senderUID;
        type = MessageType.RadarDetectedActor;
    }
}

public class Message_DiscoveredActor : Message
{
    public ulong detectedUID;
    public ulong senderUID;
    public bool team;
    public Message_DiscoveredActor(ulong detectedUID, ulong senderUID, bool t)
    {
        this.detectedUID = detectedUID;
        this.senderUID = senderUID;
        this.team = t;
        type = MessageType.ActorDiscovery;
    }
}