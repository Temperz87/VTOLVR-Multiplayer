using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class ExtLight_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ExtLight lastMessage;
    public StrobeLightController strobeLight;

    bool lastStrobe;

    private void Awake()
    {
        lastMessage = new Message_ExtLight(false, false, false, networkUID);
    }

    void FixedUpdate()
    {
        lastMessage.UID = networkUID;
        if (strobeLight.onByDefault != lastStrobe)
        {
            lastMessage.strobe = strobeLight.onByDefault;
            lastMessage.nav = strobeLight.onByDefault;//obviously change this at some point, i just think it will make it more obvious while i test

            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);

            lastStrobe = strobeLight.onByDefault;
        }
        
    }
}
