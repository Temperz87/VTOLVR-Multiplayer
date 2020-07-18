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
public class Message_JoinRequestAccepted_Result : Message
{
    public string reason;
    public string campaignId;
    public string scenarioId;

    public Message_JoinRequestAccepted_Result(string campaignId, string scenarioId) {
        this.campaignId = campaignId;
        this.scenarioId = scenarioId;
        type = MessageType.JoinRequestAccepted_Result;
    }
}
[Serializable]
public class Message_JoinRequestRejected_Result : Message
{
    public string reason;

    public Message_JoinRequestRejected_Result(string reason) {
        this.reason = reason;
        type = MessageType.JoinRequestRejected_Result;
    }
}
[Serializable]
public class Message_JoinRequestClientFinal_Result : Message
{
    public bool joined;

    public Message_JoinRequestClientFinal_Result(bool joined) {
        this.joined = joined;
        type = MessageType.JoinRequestClientFinal_Result;
    }
}
