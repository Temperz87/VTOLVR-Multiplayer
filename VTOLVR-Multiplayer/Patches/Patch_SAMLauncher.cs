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
    [HarmonyPrefix]
    public static bool Prefix(SAMLauncher __instance, ulong __state)
    {
        Debug.Log("Beginning sam launch prefix.");
        int j = 0;
        Missile[] missiles = (Missile[])Traverse.Create(__instance).Field("missiles").GetValue();
        for (int i = 0; i < missiles.Length; i = j + 1)
        {
            if (missiles[i] != null)
            {
                Debug.Log("Found a suitable missile to attach a sender to.");
                MissileNetworker_Sender missileSender = missiles[i].gameObject.AddComponent<MissileNetworker_Sender>();
                missileSender.networkUID = Networker.GenerateNetworkUID();
                __state = missileSender.networkUID;
                break;
            }
        }
        return true;
    }
    [HarmonyPostfix]
    public static void Postix(SAMLauncher __instance, RadarLockData ___lockData, ulong __state)
    {
        if (Networker.isHost)
        {
            Debug.Log("A sam has fired, attempting to send it to the client.");
            if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(__instance.actor, out ulong senderUID))
            {
                if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(___lockData.actor, out ulong actorUID))
                {
                    /*Missile missile = (Missile)Traverse.Create(__instance).Field("firedMissile").GetValue();
                    if (missile == null)
                    {
                        Debug.LogError($"last fired missile on " + __instance.name + " is null.");
                    }
                    else
                    {
                        MissileNetworker_Sender missileSender = missile.gameObject.AddComponent<MissileNetworker_Sender>();
                        missileSender.networkUID = Networker.GenerateNetworkUID();*/
                }
                Debug.Log($"Sending sam launch with uID {__state}.");
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_SamUpdate(actorUID, __state, senderUID), Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            else
                Debug.LogWarning($"Could not resolve SAMLauncher {senderUID}'s target.");
        }
        else
            Debug.LogWarning($"Could not resolve a SAMLauncher's uid.");
    }
}
