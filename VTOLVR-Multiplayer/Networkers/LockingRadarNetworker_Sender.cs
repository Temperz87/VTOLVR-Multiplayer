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
    RadarLockData lastRadarLockData = null;
    private void Awake()
    {
        Debug.Log("Radar sender awoken for object " + gameObject.name);
        radar = gameObject.GetComponentInChildren<LockingRadar>();
        if (radar == null)
        {
            Debug.LogError($"Radar on networkUID {networkUID} is null");
            return;
        }
        else
        {
            Debug.Log($"Radar sender successfully attached to object {gameObject.name}.");
        }
        lastRadarMessage = new Message_RadarUpdate(true, 0, networkUID);
        lastLockingMessage = new Message_LockingRadarUpdate(0, false, networkUID);
    }

    private void FixedUpdate()
    {
        Debug.Log($"Doing fixed update for vehicle containing radar known as {gameObject.name} with a networkUID of {networkUID}.");
        if (radar == null)
        {
            Debug.LogError($"Radar is null for object {gameObject.name} with an uid of {networkUID}.");
            radar = gameObject.GetComponentInChildren<LockingRadar>();
        }
        if (radar.radar.radarEnabled != lastOn || radar.radar.sweepFov != lastFov)
        {
            Debug.Log("radar.radar is not equal to last on");
            lastRadarMessage.UID = networkUID;
            Debug.Log("last uid");
            lastRadarMessage.on = radar.radar.radarEnabled;
            Debug.Log("on enabled");
            lastRadarMessage.fov = radar.radar.sweepFov;
            Debug.Log("sweep fov and SENDING!");

            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastRadarMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastRadarMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            Debug.Log("last 2");
            lastOn = radar.radar.radarEnabled;
            Debug.Log("last one");
            lastFov = radar.radar.sweepFov;
        }
        if (radar.IsLocked() != lastLockingMessage.isLocked || radar.currentLock != lastRadarLockData)
        {
            Debug.Log("is lock not equal to last message is locked");
            lastRadarLockData = radar.currentLock;
            Debug.Log("lockdata set");
            if (lastRadarLockData == null)
            {
                Debug.Log("lock data nulll");
                lastLockingMessage.actorUID = 0;
                lastLockingMessage.isLocked = false;
                lastLockingMessage.senderUID = networkUID;
                if (Networker.isHost)
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastLockingMessage, EP2PSend.k_EP2PSendUnreliable);
                else
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastLockingMessage, EP2PSend.k_EP2PSendUnreliable);
            }
            else
            {
                Debug.Log("else going into foreach");
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
}
