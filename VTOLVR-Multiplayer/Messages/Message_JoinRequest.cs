using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Message_JoinRequest : Message
{
    public string currentVehicle, vtolVrVersion, multiplayerBranch, multiplayerModVersion, scenarioId;
    public byte[] mapHash, scenarioHash, campaignHash;
    public bool builtInCampaign;

    public Message_JoinRequest(string currentVehicle, bool builtInCampaign, string scenarioId, byte[] mapHash, byte[] scenarioHash, byte[] campaignHash)
    {
        this.currentVehicle = currentVehicle;
        this.builtInCampaign = builtInCampaign;
        this.scenarioId = scenarioId;
        this.mapHash = mapHash;
        this.scenarioHash = scenarioHash;
        this.campaignHash = campaignHash;
        vtolVrVersion = GameStartup.versionString;
        multiplayerBranch = ModVersionString.ReleaseBranch;
        multiplayerModVersion = ModVersionString.ModVersionNumber;

        type = MessageType.JoinRequest;
    }
}
[Serializable]
public class Message_JoinRequestAccepted_Result : Message
{
    public Message_JoinRequestAccepted_Result() {
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
