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
        // lockingRadar.debugRadar = true;
        lastRadarMessage = new Message_RadarUpdate(false, 0, networkUID);
        Networker.RadarUpdate += RadarUpdate;
        Networker.RadarDetectedUpdate += OnRadarDetectedActor;
        Networker.LockingRadarUpdate += LockingRadarUpdate;
    }

    public void RadarUpdate(Packet packet)
    {
        lastRadarMessage = (Message_RadarUpdate)((PacketSingle)packet).message;
        // Debug.Log("Got a new radar update intended for id " + lastRadarMessage.UID);
        if (lastRadarMessage.UID != networkUID)
            return;

        Debug.Log($"Doing radarupdate for uid {networkUID}");
        lockingRadar.radar.radarEnabled = lastRadarMessage.on;
        lockingRadar.radar.sweepFov = lastRadarMessage.fov;
    }
    public void LockingRadarUpdate(Packet packet)
    {
        lastLockingMessage = (Message_LockingRadarUpdate)((PacketSingle)packet).message;
        // Debug.Log("Got a new locking radar update intended for id " + lastLockingMessage.senderUID);
        if (lastLockingMessage.senderUID != networkUID)
            return;
        if (lockingRadar == null)
        {
            Debug.Log($"Locking radar on networkUID {networkUID} is null.");
            return;
        }
        if (lockingRadar.radar == null)
        {
            lockingRadar.radar = gameObject.GetComponentInChildren<Radar>();
            if (lockingRadar.radar == null)
            {
                Debug.Log($"Radar was null on network uID {networkUID}");
            }
        }
        if (!lockingRadar.radar.radarEnabled)
        {
            lockingRadar.radar.radarEnabled = true;
        }
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

            if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastLockingMessage.actorUID, out lastActor))
            {
                if (gameObject.name == null)
                    Debug.Log($"Radar {networkUID} found its lock " + lastActor.name + $" with an id of {lastLock} while trying to lock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                else
                    Debug.Log($"Radar " + gameObject.name + " found its lock " + lastActor.name + $" with an id of {lastLock} while trying to lock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                lockingRadar.ForceLock(lastActor, out radarLockData);
                lastLock = lastLockingMessage.actorUID;
                lastLocked = true;
                Debug.Log($"The lock data is Locked: {radarLockData.locked}, Locked Actor: " + radarLockData.actor.name);
            }
            else
            {
                Debug.Log($"Could not resolve a lock on uID {lastLockingMessage.actorUID} from sender {lastLockingMessage.senderUID}.");
            }
        }
    }
    public void OnRadarDetectedActor(Packet packet)
    {
        Message_RadarDetectedActor message = (Message_RadarDetectedActor)((PacketSingle)packet).message;
        if (message.senderUID != networkUID)
            return;
        if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(message.detectedUID, out Actor actor))
        {
            lockingRadar.radar.ForceDetect(actor);
        }
    }
    /*private void FixedUpdate()
    {
        if (lastLocked && !lockingRadar.IsLocked() && lastLock != 0)
        {
            if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastLock, out lastActor))
            {
                Debug.Log("Radar " + gameObject.name + $" refound its lock after dropping it at  {lastLock} while trying to relock id {lastLockingMessage.actorUID}. Last locked: {lastLocked}, lockingRadar.isLocked {lockingRadar.IsLocked()}, lastLock: {lastLock}. Trying to force a lock.");
                lockingRadar.ForceLock(lastActor, out radarLockData);
                // lastLocked = true;
                Debug.Log($"The lock data is Locked: {radarLockData.locked}, reLocked Actor: " + radarLockData.actor.name);
            }
        }
        else if (!lastLocked && lockingRadar.IsLocked())
        {
            Debug.Log($"Radar is locked when it shouldn't be, unlocking. LastLocked: {lastLocked}, lockingRadar.IsLocked() {lockingRadar.IsLocked()}");
            lockingRadar.Unlock();
        }
    }*/
    public void OnDestroy()
    {
        Networker.RadarUpdate -= RadarUpdate;
        Networker.LockingRadarUpdate -= LockingRadarUpdate;
        Debug.Log("Radar update and Locking Radar update destroyed");
        Debug.Log(gameObject.name);
    }
}
