using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;


[HarmonyPatch(typeof(VTEventTarget), "Invoke")]
class Patch1
{
    static void Postfix(VTEventTarget __instance)
    {

        Debug.Log("Host Ran  " + __instance.eventName + " of type" + __instance.methodName + " for target " + __instance.targetID);
    }
}
