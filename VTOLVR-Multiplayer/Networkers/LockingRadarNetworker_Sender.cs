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
    private Message_RadarUpdate lastRadarMessage;
    private Message_LockingRadarUpdate lastLockingMessage;
    public LockingRadar radar;
    bool lastOn;
    float lastFov;
    RadarLockData lastRadarLockData;
    private void Awake()
    {
        lastRadarMessage = new Message_RadarUpdate(true, 0, networkUID);
        lastLockingMessage = new Message_LockingRadarUpdate(0, false, networkUID);
    }

    void FixedUpdate()
    {
        if (radar.radar.radarEnabled != lastOn || radar.radar.sweepFov != lastFov)
        {
            lastRadarMessage.UID = networkUID;
            lastRadarMessage.on = radar.radar.radarEnabled;
            lastRadarMessage.fov = radar.radar.sweepFov;

            if (Networker.isHost)
                Networker.SendGlobalP2P(lastRadarMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            else
                Networker.SendP2P(Networker.hostID, lastRadarMessage, Steamworks.EP2PSend.k_EP2PSendReliable);

            lastOn = radar.radar.radarEnabled;
            lastFov = radar.radar.sweepFov;
        }
        if (radar.IsLocked() != lastLockingMessage.isLocked || radar.currentLock != lastRadarLockData)
        {
            lastRadarLockData = radar.currentLock;
            foreach (var AI in AIManager.AIVehicles)
            {
                if (AI.actor == lastRadarLockData.actor)
                {
                    Debug.Log(lastRadarLockData.actor.name + " radar data found its lock " + AI.actor.name);
                    lastLockingMessage.actorUID = AI.vehicleUID;
                    lastLockingMessage.isLocked = true;
                    lastLockingMessage.senderUID = networkUID;
                    // Networker.Sen
                }
            }
        }
    }
}
