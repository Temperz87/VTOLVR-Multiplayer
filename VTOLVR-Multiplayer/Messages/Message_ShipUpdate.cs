using System;
[Serializable]
public class Message_ShipUpdate : Message
{
    public Vector3D position;
    public Vector3D rotation;
    public ulong UID;

    public Message_ShipUpdate (Vector3D position, Vector3D rotation, ulong uID)
    {
        this.position = position;
        this.rotation = rotation;
        UID = uID;
        type = MessageType.ShipUpdate;
    }
}
