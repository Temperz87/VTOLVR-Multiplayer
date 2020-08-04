using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Oculus.Platform;
using UnityEngine;

[HarmonyPatch(typeof(GunTurretAI), "AIRoutine")]
class Patch11
{
    [HarmonyPostfix]
    static void Postfix(GunTurretAI __instance)
    {
        if (Networker.isHost)
        {
            Actor actor = __instance.gameObject.GetComponent<Actor>();
            if (actor == null)
            {
                Debug.LogError("Actor AAA is null.");
            }
            if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(actor, out ulong uID))
            {
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_AAAUpdate((bool)Traverse.Create(__instance.gun).Field("firing").GetValue(), uID), Steamworks.EP2PSend.k_EP2PSendUnreliable);
            }
            else
            {
                Debug.LogError("Failed to get AAA actor in dictionary..");
            }
        }
    }
}