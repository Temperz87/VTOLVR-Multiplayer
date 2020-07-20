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
    private LockingRadar radar;
    bool lastOn;
    float lastFov;
    RadarLockData lastRadarLockData;
    private void Awake()
    {
        radar = gameObject.GetComponent<LockingRadar>();
        if (radar == null)
        {
            Debug.Log($"Radar on networkUID {networkUID} is null");
            return;
        }
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
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastRadarMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastRadarMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            lastOn = radar.radar.radarEnabled;
            lastFov = radar.radar.sweepFov;
        }
        if (radar.IsLocked() != lastLockingMessage.isLocked || radar.currentLock != lastRadarLockData)
        {
            lastRadarLockData = radar.currentLock;
            if (radar.currentLock == null)
            {
                lastLockingMessage.actorUID = 0;
                lastLockingMessage.isLocked = false;
                lastLockingMessage.senderUID = networkUID;
                if (Networker.isHost)
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastLockingMessage, EP2PSend.k_EP2PSendUnreliable);
                else
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastLockingMessage, EP2PSend.k_EP2PSendUnreliable);
            }
            foreach (var AI in AIManager.AIVehicles)
            {
                if (AI.actor == lastRadarLockData.actor)
                {
                    Debug.Log(lastRadarLockData.actor.name + " radar data found its lock " + AI.actor.name + " at id " + AI.vehicleUID);
                    lastLockingMessage.actorUID = AI.vehicleUID;
                    lastLockingMessage.isLocked = true;
                    lastLockingMessage.senderUID = networkUID;
                    if (Networker.isHost)
                        NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastLockingMessage, EP2PSend.k_EP2PSendUnreliable);
                    else
                        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastLockingMessage, EP2PSend.k_EP2PSendUnreliable);
                    break;
                }
            }
        }

    }
}
