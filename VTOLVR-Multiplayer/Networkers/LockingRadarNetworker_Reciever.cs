using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class LockingRadarNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_RadarUpdate lastRadarMessage;
    private Message_LockingRadarUpdate lastLockingMessage;
    private LockingRadar lockingRadar;
    private RadarLockData radarLockData;
    private ulong lastLock;
    private bool lastLocked;
    private Actor lastActor;
    private void Awake()
    {
        lockingRadar = gameObject.GetComponentInChildren<LockingRadar>();
        if (lockingRadar == null)
        {
            Debug.Log($"Locking radar on networkUID {networkUID} is null.");
            return;
        }
        lockingRadar.radar = gameObject.GetComponentInChildren<Radar>();
        if (lockingRadar.radar == null)
        {
            Debug.Log($"Radar was null on network uID {networkUID}");
        }
        lockingRadar.debugRadar = true;
        lastRadarMessage = new Message_RadarUpdate(false, 0, networkUID);
        Networker.RadarUpdate += RadarUpdate;
        Networker.LockingRadarUpdate += LockingRadarUpdate;
    }

    public void RadarUpdate(Packet packet)
    {
        lastRadarMessage = (Message_RadarUpdate)((PacketSingle)packet).message;
        Debug.Log("Got a new radar update intended for id " + lastRadarMessage.UID);
        if (lastRadarMessage.UID != networkUID)
            return;

        Debug.Log($"Doing radarupdate for uid {networkUID}");
        lockingRadar.radar.radarEnabled = lastRadarMessage.on;
        lockingRadar.radar.sweepFov = lastRadarMessage.fov;
    }
    public void LockingRadarUpdate(Packet packet)
    {
        lastLockingMessage = (Message_LockingRadarUpdate)((PacketSingle)packet).message;
        Debug.Log("Got a new locking radar update intended for id " + lastLockingMessage.senderUID);
        if (lastLockingMessage.senderUID != networkUID)
            return;

        Debug.Log($"Doing LockingRadarupdate for uid {networkUID} which is intended for uID {lastLockingMessage.senderUID}");
        if (!lastLockingMessage.isLocked && lockingRadar.IsLocked())
        {
            Debug.Log("Unlocking radar " + gameObject.name);
            lockingRadar.Unlock();
            lastLock = 0;
            lastLocked = false;
            return;
        }
        else if (lastLockingMessage.actorUID != lastLock || (lastLockingMessage.isLocked && !lockingRadar.IsLocked()))
        {
            Debug.Log("Trying to lock radar.");

            if (AIDictionaries.allActors.TryGetValue(lastLockingMessage.actorUID, out lastActor))
            {
                Debug.Log($"Radar " + gameObject.name + " found its lock " + lastActor.name + $" with an id of {lastLock} while trying to lock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                lockingRadar.ForceLock(lastActor, out radarLockData);
                Debug.Log($"The lock data is Locked: {radarLockData.locked}, Locked Actor: " + radarLockData.actor.name);
            }
            else
            {
                Debug.Log($"Could not resolve a lock on uID {lastLockingMessage.actorUID}.");
            }
            /*foreach (var AI in AIManager.AIVehicles)
            {
                if (AI.vehicleName == null)
                {
                    Debug.LogError($"AI uID null at id {AI.vehicleUID}");
                }
                if (AI.vehicleUID == lastLockingMessage.actorUID)
                { 
                    Debug.Log($"Radar " + gameObject.name + " found its lock " + AI.vehicleName + $" with an id of {AI.vehicleUID} while trying to lock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                    lockingRadar.ForceLock(AI.actor, out radarLockData);
                    Debug.Log($"The lock data is Locked: {radarLockData.locked}, Locked Actor: " + radarLockData.actor.name);
                    if (radarLockData.locked)
                    {
                        lastLock = AI.vehicleUID;
                        lastLocked = true;
                    }
                    break;
                }
            }*/
        }
    }
    private void FixedUpdate()
    {
        if (lastLocked && !lockingRadar.IsLocked() && lastLock != 0)
        {
            if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastLock, out lastActor))
            {
                Debug.Log("Radar" + gameObject.name + $"refound its lock after dropping it at  {lastLock} while trying to relock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                lockingRadar.ForceLock(lastActor, out radarLockData);
                Debug.Log($"The lock data is Locked: {radarLockData.locked}, reLocked Actor: " + radarLockData.actor.name);
            }
            /*foreach (var AI in AIManager.AIVehicles)
            {
                if (AI.vehicleUID == lastLock)
                {
                    Debug.Log("Radar" + gameObject.name + $"refound its lock after dropping it at {AI.vehicleName} with an id of {AI.vehicleUID} while trying to relock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                    lockingRadar.ForceLock(AI.actor, out radarLockData);
                    Debug.Log($"The lock data is Locked: {radarLockData.locked}, reLocked Actor: " + radarLockData.actor.name);
                    if (radarLockData.locked)
                    {
                        lastLock = AI.vehicleUID;
                    }
                    break;
                }
            }*/
        }
    }
    public void OnDestroy()
    {
        Networker.RadarUpdate -= RadarUpdate;
        Debug.Log("Radar update");
        Debug.Log(gameObject.name);
    }
}
