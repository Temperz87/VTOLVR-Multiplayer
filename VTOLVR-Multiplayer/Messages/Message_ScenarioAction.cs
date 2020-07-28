using System;
[Serializable]
public class Message_ScenarioAction : Message
{
    public ulong UID;
    public int scenarioActionHash;

    public Message_ScenarioAction(ulong uID, int sActionHash)
    {
        UID = uID;
        type = MessageType.ScenarioAction;
        scenarioActionHash = sActionHash;
    }
}
