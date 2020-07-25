﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Harmony;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

public static class LoadoutManager
{
    void SetLoadout(GameObject vehicle, float fuel, string[] hpLoadoutNames, int[] cmLoadout) {
        WeaponManager weaponManager = vehicle.GetComponent<WeaponManager>();

        Loadout loadout = new Loadout();
        loadout.normalizedFuel = fuel;
        loadout.hpLoadout = hpLoadoutNames;
        loadout.cmLoadout = cmLoadout;
        weaponManager.EquipWeapons(loadout);
        weaponManager.RefreshWeapon();
        //Debug.Log("Refreshed this weapon manager's weapons.");
        MissileNetworker_Receiver lastReciever;
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
                    lastReciever = hpML.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Receiver>();
                    foreach (var thingy in message.hpLoadout) // it's a loop... because fuck you!
                    {
                        //Debug.Log("Try adding missile reciever uID");
                        if (equip.hardpointIdx == thingy.hpIdx)
                        {
                            if (uIDidx < thingy.missileUIDS.Length)
                            {
                                lastReciever.networkUID = thingy.missileUIDS[uIDidx];
                                lastReciever.thisML = hpML.ml;
                                lastReciever.idx = j;
                                uIDidx++;
                            }
                        }
                    }
                }
            }
            else if (equip is HPEquipGunTurret)
            {
                TurretNetworker_Receiver reciever = equip.gameObject.AddComponent<TurretNetworker_Receiver>();
                reciever.networkUID = message.networkID;
                reciever.turret = equip.GetComponent<ModuleTurret>();
                equip.enabled = false;
            }
        }
        FuelTank fuelTank = vehicle.GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("Failed to get fuel tank on " + vehicle.name);
        fuelTank.startingFuel = loadout.normalizedFuel * fuelTank.maxFuel;
        fuelTank.SetNormFuel(loadout.normalizedFuel);
    }
}
