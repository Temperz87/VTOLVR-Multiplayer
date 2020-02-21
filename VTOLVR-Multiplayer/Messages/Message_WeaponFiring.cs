using System;
[Serializable]
public class Message_WeaponFiring : Message
{
    public int weaponIdx;
    public ulong UID;

    public Message_WeaponFiring(int weaponIdx, ulong uID)
    {
        this.weaponIdx = weaponIdx;
        UID = uID;
        type = MessageType.WeaponFiring;
    }
}