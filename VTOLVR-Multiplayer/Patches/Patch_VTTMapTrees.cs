using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
[HarmonyPatch(typeof(VTTMapTrees), "Start")]
public static class Patch_VTTMapTrees
{ 
    [HarmonyPrefix]
    public static bool Prefix()
    {
        UnityEngine.Debug.LogWarning("Stopped Generating Trees");
        return false;
    }
}
