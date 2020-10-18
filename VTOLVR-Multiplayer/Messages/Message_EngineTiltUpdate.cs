using System;

[Serializable]
class Message_EngineTiltUpdate : Message
{
    public ulong networkUID;
    public float angle = 90;

    public Message_EngineTiltUpdate(ulong networkUID, float angle)
    {
        this.networkUID = networkUID;
        this.angle = angle;
        type = MessageType.EngineTiltUpdate;
    }

    public override string ToString()
    {
        return $"NetworkUID = {networkUID}, angle = {angle}";
    }
}