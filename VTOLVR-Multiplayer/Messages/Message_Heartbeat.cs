using System;
using UnityEngine;

[Serializable]
public class Message_Heartbeat : Message
{
    public float TimeOnServerGame;

    public Message_Heartbeat() {
        TimeOnServerGame = Time.unscaledTime;
        type = MessageType.ServerHeartbeat;
    }
}

[Serializable]
public class Message_Heartbeat_Result : Message
{
    public float TimeOnServerGame;
    public ulong from;

    public Message_Heartbeat_Result(float time, ulong from) {
        TimeOnServerGame = time;
        this.from = from;
        type = MessageType.ServerHeartbeat_Response;
    }
}

[Serializable]
public class Message_ReportPingTime : Message
{
    public float PingTime;
    public ulong from;

    public Message_ReportPingTime(float pingTime, ulong from) {
        PingTime = pingTime;
        this.from = from;
        type = MessageType.ServerReportingPingTime;
    }
}
