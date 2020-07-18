using System;
[Serializable]
public class Message_RadarUpdate : Message
{
    public bool on;
    public float fov;
    public ulong UID;

    public Message_RadarUpdate(bool on, float fov, ulong uID)
    {
        this.on = on;
        this.fov = fov;
        UID = uID;
        type = MessageType.RadarUpdate;
    }
}
