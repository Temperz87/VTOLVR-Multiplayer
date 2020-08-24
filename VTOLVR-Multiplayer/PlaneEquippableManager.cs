﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Harmony;
using System.Collections;

public static class PlaneEquippableManager
{
    public enum HPInfoListGenerateNetworkType
    {
        generate,
        sender,
        receiver
    }

    public static List<HPInfo> generateHpInfoListFromWeaponManager(WeaponManager weaponManager, HPInfoListGenerateNetworkType networkType, ulong networkID = 0)
    {
        List<HPInfo> hpInfos = new List<HPInfo>();
        HPEquippable lastEquippable;

        for (int i = 0; i < weaponManager.equipCount; i++)
        {
            //Debug.Log("Weapon Manager, Equip " + i);
            lastEquippable = weaponManager.GetEquip(i);
            if (lastEquippable == null) //If this is null, it means there isn't any weapon in that slot.
                continue;
            //Debug.Log("Last Equippable = " + lastEquippable.fullName);
            List<ulong> missileUIDS = new List<ulong>();
            if (lastEquippable is HPEquipMissileLauncher HPml)
            {
                //Debug.Log("This last equip is a missile launcher");
                if (HPml.ml == null)
                {
                    Debug.LogError("The Missile Launcher was null on this Missile Launcher");
                    Debug.LogError("Type was = " + lastEquippable.weaponType);
                    continue;
                }
                if (HPml.ml.missiles == null)
                {
                    Debug.LogError("The missile list is null");
                    continue;
                }
                //Debug.Log($"This has {HPml.ml.missiles.Length} missiles");
                for (int j = 0; j < HPml.ml.missiles.Length; j++)
                {
                    //There shouldn't be any shot missiles, but if so this skips them as they are null.
                    if (HPml.ml.missiles[j] == null)
                    {
                        missileUIDS.Add(0);
                        //Debug.LogError("It seems there was a missile shot as it was null");
                        continue;
                    }
                    //Debug.Log("Adding Missle Networker to missile");
                    switch (networkType)
                    {
                        case HPInfoListGenerateNetworkType.generate:
                            MissileNetworker_Sender mnSender = HPml.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Sender>();
                            mnSender.networkUID = Networker.GenerateNetworkUID();
                            mnSender.ownerUID = networkID;
                            missileUIDS.Add(mnSender.networkUID);
                            break;
                        case HPInfoListGenerateNetworkType.sender:
                            MissileNetworker_Sender sender = HPml.ml.missiles[j].gameObject.GetComponent<MissileNetworker_Sender>();
                            if (sender != null)
                            {
                                missileUIDS.Add(sender.networkUID);
                            }
                            else
                            {
                                Debug.LogError($"Failed to get NetworkUID for missile ({HPml.ml.missiles[j].gameObject.name})");
                            }
                            break;
                        case HPInfoListGenerateNetworkType.receiver:
                            MissileNetworker_Receiver receiver = HPml.ml.missiles[j].gameObject.GetComponent<MissileNetworker_Receiver>();
                            if (receiver != null)
                            {
                                missileUIDS.Add(receiver.networkUID);
                            }
                            else
                            {
                                Debug.LogError($"Receiver null, Failed to get NetworkUID for missile ({HPml.ml.missiles[j].gameObject.name})");
                            }
                            break;
                    }
                }
            }
            else if (lastEquippable is HPEquipGunTurret HPm230 && networkID != 0)
            {
                switch (networkType)
                {
                    case HPInfoListGenerateNetworkType.generate:
                        Debug.Log("Added m230 turret sender");
                        TurretNetworker_Sender sender = HPm230.gameObject.AddComponent<TurretNetworker_Sender>();
                        sender.networkUID = networkID;
                        sender.turret = HPm230.GetComponent<ModuleTurret>();
                        break;
                    default:
                        break;
                }
            }

            hpInfos.Add(new HPInfo(
                    lastEquippable.gameObject.name.Replace("(Clone)", ""),
                    lastEquippable.hardpointIdx,
                    lastEquippable.weaponType,
                    missileUIDS.ToArray()));
        }

        return hpInfos;
    }

    public static List<HPInfo> generateLocalHpInfoList(ulong UID = 0)
    {
        GameObject localVehicle = VTOLAPI.GetPlayersVehicleGameObject();
        WeaponManager localWeaponManager = localVehicle.GetComponent<WeaponManager>();
        return generateHpInfoListFromWeaponManager(localWeaponManager, HPInfoListGenerateNetworkType.generate, UID);
    }

    public static List<int> generateCounterMeasuresFromCmManager(CountermeasureManager cmManager)
    {
        List<int> cm = new List<int>();

        for (int i = 0; i < cmManager.countermeasures.Count; i++)
        {
            cm.Add(cmManager.countermeasures[i].count);
        }

        return cm;
    }

    public static float generateLocalFuelValue()
    {
        float fuel = 0.65f;

        if (VehicleEquipper.loadoutSet)
        {
            fuel = VehicleEquipper.loadout.normalizedFuel;
        }

        return fuel;
    }

    public static void SetLoadout(GameObject vehicle, ulong networkID, float fuel, HPInfo[] hpLoadout, int[] cmLoadout)
    {
        WeaponManager weaponManager = vehicle.GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Failed to get weapon manager on " + vehicle.name);
        string[] hpLoadoutNames = new string[30];
        //Debug.Log("foreach var equip in message.hpLoadout");
        int debugInteger = 0;
        foreach (var equip in hpLoadout)
        {
            Debug.Log(debugInteger);
            hpLoadoutNames[equip.hpIdx] = equip.hpName;
            debugInteger++;
        }

        Loadout loadout = new Loadout();
        loadout.normalizedFuel = fuel;
        loadout.hpLoadout = hpLoadoutNames;
        loadout.cmLoadout = cmLoadout;
        weaponManager.EquipWeapons(loadout);
        weaponManager.RefreshWeapon();
        //Debug.Log("Refreshed this weapon manager's weapons.");
        MissileNetworker_Receiver lastReceiver;
        for (int i = 0; i < 30; i++)
        {
            int uIDidx = 0;
            HPEquippable equip = weaponManager.GetEquip(i);
            if (equip is HPEquipMissileLauncher)
            {
                //Debug.Log(equip.name + " is a missile launcher");
                HPEquipMissileLauncher hpML = equip as HPEquipMissileLauncher;
                //Debug.Log("This missile launcher has " + hpML.ml.missiles.Length + " missiles.");
                for (int j = 0; j < hpML.ml.missiles.Length; j++)
                {
                    //Debug.Log("Adding missile reciever");
                    lastReceiver = hpML.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Receiver>();
                    foreach (var thingy in hpLoadout) // it's a loop... because fuck you!
                    {
                        //Debug.Log("Try adding missile reciever uID");
                        if (equip.hardpointIdx == thingy.hpIdx)
                        {
                            if (uIDidx < thingy.missileUIDS.Length)
                            {
                                lastReceiver.networkUID = thingy.missileUIDS[uIDidx];
                                lastReceiver.ownerUID = networkID;
                                Debug.Log($"Missile ({lastReceiver.gameObject.name}) has received their UID from the host. \n Missiles UID = {lastReceiver.networkUID}");
                                lastReceiver.thisML = hpML.ml;
                                lastReceiver.idx = j;
                                uIDidx++;
                            }
                        }
                    }
                }
            }
            else if (equip is HPEquipGunTurret)
            {
                TurretNetworker_Receiver reciever = equip.gameObject.AddComponent<TurretNetworker_Receiver>();
                reciever.networkUID = networkID;
                reciever.turret = equip.GetComponent<ModuleTurret>();
                equip.enabled = false;
                Debug.Log("Added m230 turret reciever");
            }
        }
        FuelTank fuelTank = vehicle.GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("Failed to get fuel tank on " + vehicle.name);
        fuelTank.startingFuel = loadout.normalizedFuel * fuelTank.maxFuel;
        fuelTank.SetNormFuel(loadout.normalizedFuel);
    }
}
