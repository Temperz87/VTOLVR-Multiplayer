using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Oculus.Platform;
using UnityEngine;

[HarmonyPatch(typeof(VTMapManager), "ReloadScene")]
class Patch_ReloadScene
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        Debug.Log("Not restarting");
        return false;
    }
}

[HarmonyPatch(typeof(QuicksaveManager), "Quickload")]
class Patch_Quickload
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        Debug.Log("Not quickloading");
        return false;
    }
}
[HarmonyPatch(typeof(VRQuadHandMenu), "Quickload")]
class Patch_VRQuandReload
{
    [HarmonyPrefix]
    static bool Prefix(VRQuadHandMenu __instance)
    {
        Debug.Log("Not quad reloading");
        __instance.ShowError();
        return false;
    }
}
[HarmonyPatch(typeof(VRQuadHandMenu), "RestartMission")]
class Patch_VRQuandLoad
{
    [HarmonyPrefix]
    static bool Prefix(VRQuadHandMenu __instance)
    {
        Debug.Log("Not quad restarting");
        __instance.ShowError();
        return false;
    }
}