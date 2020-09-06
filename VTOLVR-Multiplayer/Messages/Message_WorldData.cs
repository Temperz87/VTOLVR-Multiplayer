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

[Serializable]
public class Message_GPSData: Message
{
    public Vector3D pos;
    public string prefix;
    public bool teamLeft;

    public Message_GPSData(Vector3D ipos, string iprefix, bool team)
    {
        this.pos = ipos;
        this.prefix = iprefix;
        this.teamLeft = team;
        type = MessageType.GPSTarget;
    }
}