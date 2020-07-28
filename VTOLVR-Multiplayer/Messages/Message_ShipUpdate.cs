using System;
[Serializable]
public class Message_ShipUpdate : Message
{
    public Vector3D position;
    public Vector3D rotation;
    public Vector3D destination;
    public Vector3D velocity;
    public ulong UID;

    public Message_ShipUpdate (Vector3D position, Vector3D rotation, Vector3D destination, Vector3D velocity, ulong uID)
    {
        this.position = position;
        this.rotation = rotation;
        this.destination = destination;
        this.velocity = velocity;
        UID = uID;
        type = MessageType.ShipUpdate;
    }
}
