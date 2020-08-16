using System;
[Serializable]
public class Message_WeaponFiring : Message
{
    public int weaponIdx;
    public bool isFiring;
    public ulong UID;
    public int missileIdx;
    public bool noAmmo;
    public Message_WeaponFiring(int weaponIdx, bool isFiring, bool noAmmo, ulong uID)
    {
        this.weaponIdx = weaponIdx;
        this.isFiring = isFiring;
        this.noAmmo = noAmmo;
        UID = uID;
        type = MessageType.WeaponFiring;
    }
}