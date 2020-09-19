using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Oculus.Platform;
using UnityEngine;
using UnityEngine.UI;

[HarmonyPatch(typeof(PlayerSpawn), "OnPreSpawnUnit")]
class Patch_OnPreSpawnUnit
{
    public static bool Prefix(PlayerSpawn __instance)
    {
        VTScenario.current.vehicle = VTResources.GetPlayerVehicle(Multiplayer._instance.selectedVehicle);
        return true;
    }
}
[HarmonyPatch(typeof(LoadoutConfigurator), "EquipCompatibilityMask")]
public static class Patch_DrawButton
{
    public static bool Prefix(HPEquippable equip)
    {
        if (!Multiplayer._instance.buttonMade)
        {
            Multiplayer._instance.CreateVehicleButton();
        }
        return true;
    }
}