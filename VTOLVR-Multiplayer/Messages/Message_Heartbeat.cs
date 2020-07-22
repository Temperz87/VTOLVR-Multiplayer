using System;

[Serializable]
public class Message_Heartbeat : Message
{
    public float TimeOnServerGame;

    public Message_Heartbeat() {
        TimeOnServerGame = UnityEngine.Time.fixedTime;
        type = MessageType.ServerHeartbeat;
    }
    public Message_Heartbeat(float time) {
        TimeOnServerGame = time;
        type = MessageType.ServerHeartbeat;
    }
}

[Serializable]
public class Message_Heartbeat_Result : Message
{
    public float TimeOnServerGame;

    public Message_Heartbeat_Result(float time) {
        TimeOnServerGame = time;
        type = MessageType.ServerHeartbeat_Response;
    }
}

[Serializable]
public class Message_ReportPingTime : Message
{
    public float PingTime;

    public Message_ReportPingTime(float pingTime) {
        PingTime = pingTime;
        type = MessageType.ServerReportingPingTime;
    }
}
