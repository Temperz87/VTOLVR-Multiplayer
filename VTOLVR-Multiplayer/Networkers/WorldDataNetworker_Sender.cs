using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class WorldDataNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_WorldData lastMessage;
    float lastTimeScale;

    private void Awake()
    {
        lastTimeScale = Time.timeScale;
        lastMessage = new Message_WorldData(lastTimeScale, networkUID);
    }

    void LateUpdate() {

        float curTimeScale = Time.timeScale;

        if (curTimeScale != lastTimeScale) {
            lastMessage.UID = networkUID;
            lastMessage.timeScale = curTimeScale;
            if (Networker.isHost)
            {
                Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            
            lastTimeScale = curTimeScale;
        }
    }
}
