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
    private float tick;
    public float tickRate = 0.5f;
    private void Awake()
    {
        tiltController = GetComponent<TiltController>();
        lastMessage = new Message_EngineTiltUpdate(networkUID, 0);
    }

    private void LateUpdate()
    {
        tick += Time.deltaTime;
        if (tick > 1.0f / tickRate)
        {
            tick = 0.0f;
            lastMessage.angle = tiltController.currentTilt;
            lastMessage.networkUID = networkUID;

            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}