using System;
[Serializable]
public class Message_WorldData : Message
{
    public float timeScale;


    public Message_WorldData(float timeScaleData)
    {
        timeScale = timeScaleData;
        type = MessageType.WorldData;
    }
}

[Serializable]
public class Message_GPSData : Message
{
    public Vector3D pos;
    public string prefix;
    public bool teamLeft;
    public string GPName;
    public ulong uid;

    public Message_GPSData(ulong ida, Vector3D ipos, string iprefix, bool team, string group)
    {
        uid = ida;
        pos = ipos;
        prefix = iprefix;
        teamLeft = team;
        GPName = group;
        type = MessageType.GPSTarget;
    }
}