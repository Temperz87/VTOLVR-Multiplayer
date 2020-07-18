using System;
[Serializable]
public class Message_WorldData : Message
{
    public float timeScale;
    public ulong UID;

    public Message_WorldData(float timeScaleData, ulong uID)
    {
        this.timeScale = timeScaleData;
        UID = uID;
        type = MessageType.WorldData;
    }
}