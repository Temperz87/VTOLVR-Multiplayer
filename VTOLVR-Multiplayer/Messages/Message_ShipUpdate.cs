using System;
[Serializable]
public class Message_ShipUpdate : Message
{
    public Vector3D position;
    public SerializableQuaternion rotation;
    public SerializableVector3 velocity;
    public ulong UID;

    public Message_ShipUpdate (Vector3D position, SerializableQuaternion rotation, Vector3D velocity, ulong uID)
    {
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        UID = uID;
        type = MessageType.ShipUpdate;
    }
}
