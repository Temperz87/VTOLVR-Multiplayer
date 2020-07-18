using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class LockingRadarNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_RadarUpdate lastMessage;
    public LockingRadar radar;

    bool lastOn;
    float lastFov;

    private void Awake()
    {
        lastMessage = new Message_RadarUpdate(true, 0, networkUID);
    }

    void FixedUpdate()
    {
        lastMessage.UID = networkUID;
        if (radar.radar.radarEnabled != lastOn || radar.radar.sweepFov != lastFov)
        {
            lastMessage.on = radar.radar.radarEnabled;
            lastMessage.fov = radar.radar.sweepFov;

            if (Networker.isHost)
                Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            else
                Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);

            lastOn = radar.radar.radarEnabled;
            lastFov = radar.radar.sweepFov;
        }

    }
}
