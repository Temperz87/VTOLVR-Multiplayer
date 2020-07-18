using System;
[Serializable]
public class Message_WorldData : Message
{
    public float timeScale;


    public Message_WorldData(float timeScaleData)
    {
        this.timeScale = timeScaleData;
        type = MessageType.WorldData;
    }
}