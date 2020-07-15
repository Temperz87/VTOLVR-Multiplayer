using System;
[Serializable]
public class Message_FireCountermeasure : Message
{
    public bool flares;
    public bool chaff;
    public ulong UID;

    public Message_FireCountermeasure(bool flares, bool chaff, ulong uID)
    {
        this.flares = flares;
        this.chaff = chaff;
        UID = uID;
        type = MessageType.FireCountermeasure;
    }
}
