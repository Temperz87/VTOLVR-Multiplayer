using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

[HarmonyPatch(typeof(SAMLauncher), "FireMissileRoutine")]
class Patch9
{
    [HarmonyPostfix]
    public static void Postix(SAMLauncher __instance, RadarLockData ___lockData)
    {
        if (Networker.isHost)
        {
            if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(__instance.actor, out ulong senderUID))
            {
                if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(___lockData.actor, out ulong actorUID))
                {
                    Missile missile = (Missile)Traverse.Create(__instance).Field("firedMissile").GetValue();
                    if (missile == null)
                    {
                        Debug.LogError($"last fired missile on " + __instance.name + " is null.");
                    }
                    MissileNetworker_Sender sender = missile.gameObject.AddComponent<MissileNetworker_Sender>();
                    sender.networkUID = Networker.GenerateNetworkUID();
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_SamUpdate(actorUID, sender.networkUID, senderUID), Steamworks.EP2PSend.k_EP2PSendReliable);
                }
                else
                    Debug.LogWarning($"Could not resolve SAMLauncher {senderUID}'s target.");
            }
            else
                Debug.LogWarning($"Could not resolve a SAMLauncher's uid.");
        }
    }
}