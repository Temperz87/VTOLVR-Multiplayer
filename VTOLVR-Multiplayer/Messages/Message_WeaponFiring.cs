using System;
[Serializable]
public class Message_WeaponFiring : Message
{
    public int weaponIdx;
    public bool isFiring;
    public ulong UID;

    public Message_WeaponFiring(int weaponIdx, bool isFiring, ulong uID)
    {
        this.weaponIdx = weaponIdx;
        this.isFiring = isFiring;
        UID = uID;
        type = MessageType.WeaponFiring;
    }
}