using System;
[Serializable]
public class Message_Respawn : Message
{
    public ulong UID;

    public Message_Respawn(ulong uID)
    {
        UID = uID;
        type = MessageType.Respawn;
    }
}