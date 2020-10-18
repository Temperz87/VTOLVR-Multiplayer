using System;

[Serializable]
public class HPInfo
{
    public string hpName;
    public int hpIdx;
    public HPEquippable.WeaponTypes weaponType;
    public ulong[] missileUIDS;

    public HPInfo() { }

    public HPInfo(string hpName, int hpIdx, HPEquippable.WeaponTypes weaponType, ulong[] missileUIDS)
    {
        this.hpName = hpName;
        this.hpIdx = hpIdx;
        this.weaponType = weaponType;
        this.missileUIDS = missileUIDS;
    }
}