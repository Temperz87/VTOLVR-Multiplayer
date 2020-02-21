using System;
[Serializable]
public class Message_WeaponStoppedFiring : Message
{
    public ulong UID;
    public Message_WeaponStoppedFiring(ulong uID)
    {
        UID = uID;
        type = MessageType.WeaponStoppedFiring;
    }
}