using System;

[Serializable]
public class Message_LobbyInfoRequest : Message
{
    public Message_LobbyInfoRequest()
    {
        type = MessageType.LobbyInfoRequest;
    }
}

[Serializable]
public class Message_LobbyInfoRequest_Result : Message
{
    public string username, vehicle, scenario, campaign, playercount;

    public Message_LobbyInfoRequest_Result(string username, string vehicle, string scenario, string campaign, string playercount)
    {
        this.username = username;
        this.vehicle = vehicle;
        this.scenario = scenario;
        this.campaign = campaign;
        this.playercount = playercount;
        type = MessageType.LobbyInfoRequest_Result;
    }
}