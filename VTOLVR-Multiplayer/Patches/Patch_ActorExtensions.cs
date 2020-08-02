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
    static bool Prefix(ref string __result)
    {
        __result = "string lmao";
        Debug.Log("DebugName");
        return false;
    }
}