using System;
[Serializable]
public class Message_ExtLight : Message
{
    public bool nav;
    public bool strobe;
    public bool land;
    public ulong UID;

    public Message_ExtLight(bool nav, bool strobe, bool land, ulong uID)
    {
        this.nav = nav;
        this.strobe = strobe;
        this.land = land;
        UID = uID;
        type = MessageType.ExtLight;
    }
}
