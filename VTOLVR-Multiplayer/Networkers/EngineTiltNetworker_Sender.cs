using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class EngineTiltNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_EngineTiltUpdate lastMessage;
    private TiltController tiltController;

    private void Awake()
    {
        tiltController = GetComponent<TiltController>();
        lastMessage = new Message_EngineTiltUpdate(networkUID, 0);
    }

    private void LateUpdate()
    {
        lastMessage.angle = tiltController.currentTilt;
        lastMessage.networkUID = networkUID;

        if (Networker.isHost)
            Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
}