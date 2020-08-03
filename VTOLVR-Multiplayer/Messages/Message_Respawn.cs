using System;
[Serializable]
public class Message_Respawn : Message
{
    public ulong UID;
    public Vector3D position;
    public Vector3D rotation;

    public Message_Respawn(ulong uID, Vector3D position, Vector3D rotation)
    {
        UID = uID;
        this.position = position;
        this.rotation = rotation;
        type = MessageType.Respawn;
    }
}