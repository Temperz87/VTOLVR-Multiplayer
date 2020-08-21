using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;


[HarmonyPatch(typeof(AIUnitSpawn), "SpawnUnit")]
class Patch8
{
    static void Postfix(AIUnitSpawn __instance)
    {
        if (Networker.isHost)
        {
            AIManager.setupAIAircraft(__instance.actor);
            AIManager.TellClientAboutAI(new Steamworks.CSteamID(0));
        }
    }
}



