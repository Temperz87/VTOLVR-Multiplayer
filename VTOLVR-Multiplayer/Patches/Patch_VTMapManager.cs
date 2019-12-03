using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
[HarmonyPatch(typeof(VTMapManager))]
[HarmonyPatch("ScenarioStartRoutine")]
class Patch_VTMapManager
{
    public static void Postfix()
    {
        Debug.Log("POST FIX AFTER ScenarioStartRoutine");
        PlayerManager.MapLoaded();
    }
}
