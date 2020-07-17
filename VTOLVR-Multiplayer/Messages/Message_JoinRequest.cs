using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Message_JoinRequest : Message
{
    public string currentVehicle, currentScenario, currentCampaign, vtolVrVersion, multiplayerBranch, multiplayerModVersion;

    public Message_JoinRequest(string currentVehicle, string currentScenario, string currentCampaign)
    {
        this.currentVehicle = currentVehicle;
        this.currentScenario = currentScenario;
        this.currentCampaign = currentCampaign;
        vtolVrVersion = GameStartup.versionString;
        multiplayerBranch = ModVersionString.ReleaseBranch;
        multiplayerModVersion = ModVersionString.ModVersionNumber;

        type = MessageType.JoinRequest;
    }
}
[Serializable]
public class Message_JoinRequest_Result : Message
{
    public bool canJoin;
    public string reason;
    public Message_JoinRequest_Result(bool canJoin)
    {
        this.canJoin = canJoin;
        type = MessageType.JoinRequest_Result;
    }

    public Message_JoinRequest_Result(bool canJoin, string reason)
    {
        this.canJoin = canJoin;
        this.reason = reason;
        type = MessageType.JoinRequest_Result;
    }
}
