using System;
[Serializable]
public class Message_TurretUpdate : Message
{
    public Vector3D direction;
    public ulong UID;

    public Message_TurretUpdate(Vector3D direction, ulong uID)
    {
        this.direction = direction;
        UID = uID;
        type = MessageType.TurretUpdate;
    }
}
