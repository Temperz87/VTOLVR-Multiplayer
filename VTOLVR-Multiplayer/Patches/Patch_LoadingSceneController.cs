using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using UnityEngine.SceneManagement;
[HarmonyPatch(typeof(LoadingSceneController))]
[HarmonyPatch("PlayerReady")]
class Patch_LoadingSceneController_PlayerReady
{
    static bool Prefix()
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
}