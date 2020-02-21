using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

/*
[HarmonyPatch(typeof(LoadingSceneController),"PlayerReady")]
public class Patch_LoadingSceneController_PlayerReady
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        Debug.Log("Start of Prefix");
        if (Networker.isHost)
        {
            if (Networker.EveryoneElseReady())
            {
                Debug.Log("Everyone is ready, starting game");
                Networker.SendGlobalP2P(new Message(MessageType.Ready_Result), Steamworks.EP2PSend.k_EP2PSendReliable);
                Networker.hostReady = true;
            }
            else
            {
                Debug.Log("I'm ready but others are not, waiting");
                return false;
            }
        }
        else if (!Networker.hostReady)
        {
            Networker.SendP2P(Networker.hostID, new Message(MessageType.Ready), Steamworks.EP2PSend.k_EP2PSendReliable);
            Debug.Log("Waiting for the host to say everyone is ready");
            return false;
        }
        Debug.Log("Player is ready!!!!");
        return true;
    }
    [HarmonyPostfix]
    static void PostFix()
    {
        Debug.Log("After the player is ready");
    }
}
*/
// Have to catch it in the update instead as patch above isn't working
[HarmonyPatch(typeof(LoadingSceneHelmet), "Update")]
class Patch_LoadingSceneHelmet_Update
{
    [HarmonyPrefix]
    static bool Prefix(LoadingSceneHelmet __instance)
    {
        Traverse t = Traverse.Create(__instance);
        bool grabbed = (bool)t.Field("grabbed").GetValue();
        VRHandController c = (VRHandController)t.Field("c").GetValue();


        if (grabbed || __instance.GetComponent<Rigidbody>().velocity.sqrMagnitude > 0.1f)
        {
            if (Vector3.Distance(__instance.transform.position, __instance.headTransform.position) < __instance.radius)
            {
                if (c)
                {
                    c.ReleaseFromInteractable();
                }
                __instance.headHelmet.SetActive(true);
                __instance.gameObject.SetActive(false);

                if (Networker.isHost)
                {
                    if (Networker.EveryoneElseReady())
                    {
                        Debug.Log("Everyone is ready, starting game");
                        Networker.SendGlobalP2P(new Message(MessageType.Ready_Result), Steamworks.EP2PSend.k_EP2PSendReliable);
                        Networker.hostReady = true;
                        LoadingSceneController.instance.PlayerReady();
                    }
                    else
                    {
                        Debug.Log("I'm ready but others are not, waiting");
                        Networker.hostReady = true;
                    }
                }
                else if (!Networker.hostReady)
                {
                    Networker.SendP2P(Networker.hostID, new Message(MessageType.Ready), Steamworks.EP2PSend.k_EP2PSendReliable);
                    Debug.Log("Waiting for the host to say everyone is ready");
                }

                __instance.equipAudioSource.Play();
                return false;
            }
        }
        return true;
    }
}
