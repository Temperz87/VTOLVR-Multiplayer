using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Harmony;
using System.Collections;

namespace VTOLVR_Multiplayer
{
    public static class PlaneEquippableManager
    {
        public enum HPInfoListGenerateNetworkType {
            generate,
            sender,
            receiver
        }

        public static List<HPInfo> generateHpInfoListFromWeaponManager(WeaponManager weaponManager, HPInfoListGenerateNetworkType networkType, ulong networkID = 0) {
            List<HPInfo> hpInfos = new List<HPInfo>();
            HPEquippable lastEquippable;

            for (int i = 0; i < weaponManager.equipCount; i++) {
                Debug.Log("Weapon Manager, Equip " + i);
                lastEquippable = weaponManager.GetEquip(i);
                if (lastEquippable == null) //If this is null, it means there isn't any weapon in that slot.
                    continue;
                Debug.Log("Last Equippable = " + lastEquippable.fullName);
                List<ulong> missileUIDS = new List<ulong>();
                if (lastEquippable is HPEquipMissileLauncher HPml) {
                    Debug.Log("This last equip is a missile launcher");
                    if (HPml.ml == null) {
                        Debug.LogError("The Missile Launcher was null on this Missile Launcher");
                        Debug.LogError("Type was = " + lastEquippable.weaponType);
                        continue;
                    }
                    if (HPml.ml.missiles == null) {
                        Debug.LogError("The missile list is null");
                        continue;
                    }
                    Debug.Log($"This has {HPml.ml.missiles.Length} missiles");
                    for (int j = 0; j < HPml.ml.missiles.Length; j++) {
                        //There shouldn't be any shot missiles, but if so this skips them as they are null.
                        if (HPml.ml.missiles[j] == null) {
                            missileUIDS.Add(0);
                            Debug.LogError("It seems there was a missile shot as it was null");
                            continue;
                        }
                        Debug.Log("Adding Missle Networker to missile");
                        switch (networkType) {
                            case HPInfoListGenerateNetworkType.generate:
                                MissileNetworker_Sender mnSender = HPml.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Sender>();
                                mnSender.networkUID = Networker.GenerateNetworkUID();
                                missileUIDS.Add(mnSender.networkUID);
                                break;
                            case HPInfoListGenerateNetworkType.sender:
                                MissileNetworker_Sender sender = HPml.ml.missiles[j].gameObject.GetComponent<MissileNetworker_Sender>();
                                if (sender != null) {
                                    missileUIDS.Add(sender.networkUID);
                                }
                                else {
                                    Debug.LogError($"Failed to get NetworkUID for missile ({HPml.ml.missiles[j].gameObject.name})");
                                }
                                break;
                            case HPInfoListGenerateNetworkType.receiver:
                                MissileNetworker_Receiver reciever = HPml.ml.missiles[j].gameObject.GetComponent<MissileNetworker_Receiver>();
                                if (reciever != null) {
                                    missileUIDS.Add(reciever.networkUID);
                                }
                                else {
                                    Debug.LogError($"Receiver null, Failed to get NetworkUID for missile ({HPml.ml.missiles[j].gameObject.name})");
                                }
                                break;
                        }
                    }
                }
                else if (lastEquippable is HPEquipGunTurret HPm230 && networkID != 0) {
                    switch (networkType)
                    {
                        case HPInfoListGenerateNetworkType.sender:
                            TurretNetworker_Sender sender = HPm230.gameObject.AddComponent<TurretNetworker_Sender>();
                            sender.networkUID = networkID;
                            sender.turret = HPm230.GetComponent<ModuleTurret>();
                            break;
                        case HPInfoListGenerateNetworkType.receiver:
                            TurretNetworker_Sender reciever = HPm230.gameObject.AddComponent<TurretNetworker_Sender>();
                            reciever.networkUID = networkID;
                            reciever.turret = HPm230.GetComponent<ModuleTurret>();
                            HPm230.enabled = false;
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

        public static List<HPInfo> generateLocalHpInfoList() {
            GameObject localVehicle = VTOLAPI.GetPlayersVehicleGameObject();
            WeaponManager localWeaponManager = localVehicle.GetComponent<WeaponManager>();
            return generateHpInfoListFromWeaponManager(localWeaponManager, HPInfoListGenerateNetworkType.generate);
        }

        public static List<int> generateCounterMeasuresFromCmManager(CountermeasureManager cmManager) {
            List<int> cm = new List<int>();

            for (int i = 0; i < cmManager.countermeasures.Count; i++) {
                cm.Add(cmManager.countermeasures[i].count);
            }

            return cm;
        }

        public static float generateLocalFuelValue() {
            float fuel = 0.65f;

            if (VehicleEquipper.loadoutSet) {
                fuel = VehicleEquipper.loadout.normalizedFuel;
            }

            return fuel;
        }
    }
}
