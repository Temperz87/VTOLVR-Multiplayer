using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Oculus.Platform;
using UnityEngine;
using UnityEngine.UI;

/* This code is like a museum... HISTRORY!!!!!!!!!!!!!!!!
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
        if (!Multiplayer._instance.playingMP)
            return true;
        Traverse t = Traverse.Create(__instance);
        bool grabbed = (bool)t.Field("grabbed").GetValue();
        VRHandController c = (VRHandController)t.Field("c").GetValue();
        if(PlayerManager.OPFORbuttonMade)
        Debug.Log("OPFORbuttonMade is made");
        if (!PlayerManager.OPFORbuttonMade)
        {
            Debug.Log("OPFORbuttonMade eneter");
            var refrence = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name.Contains("RecenterCanvas"));
            GameObject button = GameObject.Instantiate(refrence);
             PlayerManager.OPFORbuttonMade = false;
            foreach (var controller in GameObject.FindObjectsOfType<VRHandController>())
            {
                if (!controller.isLeft)
                {

                    Debug.Log("OPFORbuttonMade setting transform");
                    button.transform.SetParent(controller.transform);
                    button.transform.localPosition = new Vector3(0.101411f, 0.02100047f, -0.128024f);
                    button.transform.localRotation = Quaternion.Euler(-5.834f, 283.583f, 328.957f);
                    button.transform.localScale = new Vector3(button.transform.localScale.x * -1, button.transform.localScale.y * -1, button.transform.localScale.z);
                    VRInteractable bInteractable = button.GetComponentInChildren<VRInteractable>();
                    Text text = button.GetComponentInChildren<Text>();
                    text.transform.localScale = text.transform.localScale * 0.75f;
                    PlayerManager.text = text;
                    text.text = "Current Team: " + (PlayerManager.teamLeftie == true ? "REDFOR" : "BLUFOR");
                    if (!Networker.isHost)
                    {
                        bInteractable.interactableName = "Swap Teams.";
                        bInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
                        bInteractable.OnInteract.AddListener(new UnityEngine.Events.UnityAction(() =>
                        {
                            PlayerManager.teamLeftie = !PlayerManager.teamLeftie;
                            PlayerManager.text.text = "Current Team: " + (PlayerManager.teamLeftie == true ? "REDFOR" : "BLUFOR");
                            if (Networker.readySent)
                            {
                                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, new Message_Ready(PlayerManager.localUID, Networker.isHost, PlayerManager.teamLeftie), Steamworks.EP2PSend.k_EP2PSendReliable);
                            }
                        }));
                    }
                    else
                    {
                        GameObject.Destroy(bInteractable.gameObject);
                        GameObject.Destroy(button.transform.GetChild(1).gameObject);
                        bInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
                    }
                    button.SetActive(true);
                     PlayerManager.OPFORbuttonMade = true;
                    break;
                }
            }
            
        }
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

                if (Networker.isHost || Networker.isClient)
                {
                    if (Networker.isHost)
                    {
                        PlayerManager.teamLeftie = false; //host cant be team leftie so ai doesnt break;
                        if (Networker.EveryoneElseReady())
                        {
                            Debug.Log("Everyone is ready, starting game");
                            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message(MessageType.AllPlayersReady), Steamworks.EP2PSend.k_EP2PSendReliable);
                            Networker.SetHostReady(true);
                            LoadingSceneController.instance.PlayerReady();
                            PlayerManager.OPFORbuttonMade = false;
                        }
                        else
                        {
                            Debug.Log("I'm ready but others are not, waiting");
                            Networker.SetHostReady(false);
                        }
                    }
                    else
                    {
                        if (!Networker.readySent)
                        {
                            Networker.readySent = true;
                            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, new Message_Ready(PlayerManager.localUID, Networker.isHost, PlayerManager.teamLeftie), Steamworks.EP2PSend.k_EP2PSendReliable);
                            Debug.Log("Waiting for the host to say everyone is ready");
                        }
                    }
                    if (!Networker.hostLoaded && !Networker.isHost)
                    {
                        Debug.Log("Waiting for host to load");
                       
                    }
                    __instance.equipAudioSource.Play();
                    return false;
                }
                else
                {
                    Debug.Log("Player is not a MP host or client.");
                    LoadingSceneController.instance.PlayerReady();
                    PlayerManager.OPFORbuttonMade = false;
                }
            }
        }
        return true;
    }
}
