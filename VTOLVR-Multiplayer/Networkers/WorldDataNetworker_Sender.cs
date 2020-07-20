using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class WorldDataNetworker_Sender : MonoBehaviour
{

    private Message_WorldData lastMessage;
    float lastTimeScale;

    private void Awake()
    {
        lastTimeScale = Time.timeScale;
        lastMessage = new Message_WorldData(lastTimeScale);

    }

    void LateUpdate() {

        float curTimeScale = Time.timeScale;

        if (curTimeScale != lastTimeScale) {
            lastMessage.timeScale = curTimeScale;
            if (Networker.isHost)
            {
                Debug.Log($"Sending the timescale {lastMessage.timeScale}");
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            
            lastTimeScale = curTimeScale;
        }
    }

}
