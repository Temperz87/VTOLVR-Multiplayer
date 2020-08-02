using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Oculus.Platform;
using UnityEngine;

[HarmonyPatch(typeof(PlayerVehicleSetup), "SavePersistentData")]
class Patch_PlayerVehicleSetup_SavePersistentData
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(TempPilotDetacher), "OnExitScene")]
class Patch_TempPilotDetacher_OnExitScene
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        return false;
    }
}