using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Oculus.Platform;
using UnityEngine;

[HarmonyPatch(typeof(ActorExtensions), "DebugName")]
class Patch_ActorExtensions_DebugName
{
    [HarmonyPrefix]
    static bool Prefix(Actor a, ref string __result)
    {

        __result = a.actorName + a.gameObject.name;
        Debug.Log("DebugName");
        return false;
    }
}
[HarmonyPatch(typeof(UIUtils), "GetUnitName")]
class Patch10
{
    [HarmonyPrefix]
    static bool Prefix(ref string __result)
    {
        __result = "This method has been prefixed by Temperz Inc. See back for details.";
        return false;
    }
}