using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;

class ExtNPCLight_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ExtLight lastMessage;
    public ExteriorLightsController lightsController;

    bool lastNav;
    bool lastStrobe;
    bool lastLand;

    private Traverse traverse;
    private Traverse traverse2;

    private void Awake()
    {
        Debug.Log("Hello, its me, npc light sender, im here to break everything");
        lastMessage = new Message_ExtLight(false, false, false, networkUID);
        lightsController = GetComponentInChildren<ExteriorLightsController>();
        if (lightsController.navLights[0] != null)
        {
            traverse = Traverse.Create(lightsController.navLights[0]);
        }
        else {
            Debug.Log("navlights were null on this AI");
        }
        if (lightsController.landingLights[0] != null)
        {
            traverse2 = Traverse.Create(lightsController.landingLights[0]);
        }
        else {
            Debug.Log("landing lights were null on this AI");
        }
    }

    /*void FixedUpdate()
    {
        bool hasChanged = false;

        lastMessage.UID = networkUID;

        if (traverse != null && (bool)traverse.Field("connected").GetValue() != lastNav)
        {
            lastMessage.nav = (bool)traverse.Field("connected").GetValue();
            hasChanged = true;
            lastNav = (bool)traverse.Field("connected").GetValue();
        }
        if (lightsController.strobeLights != null && lightsController.strobeLights.onByDefault != lastStrobe)
        {
            lastMessage.strobe = lightsController.strobeLights.onByDefault;
            if (traverse2 != null)
            lastMessage.strobe = (bool)traverse2.Field("connected").GetValue();
            hasChanged = true;
            lastLand = (bool)traverse2.Field("connected").GetValue();
        }
        
        if (hasChanged) {
            Debug.Log("The lights on " + networkUID + " have changed, sending");
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }

    }*/
}
