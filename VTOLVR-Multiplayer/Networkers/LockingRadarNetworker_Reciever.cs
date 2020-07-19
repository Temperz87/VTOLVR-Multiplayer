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
    public LockingRadar lockingRadar;
    private RadarLockData radarLockData;
    private void Awake()
    {
        lastRadarMessage = new Message_RadarUpdate(false, 0, networkUID);
        Networker.RadarUpdate += RadarUpdate;
        Networker.LockingRadarUpdate += LockingRadarUpdate;
    }

    public void RadarUpdate(Packet packet)
    {
        lastRadarMessage = (Message_RadarUpdate)((PacketSingle)packet).message;
        if (lastRadarMessage.UID != networkUID)
            return;

        lockingRadar.radar.radarEnabled = lastRadarMessage.on;
        lockingRadar.radar.sweepFov = lastRadarMessage.fov;
    }
    public void LockingRadarUpdate(Packet packet)
    {
        lastLockingMessage = (Message_LockingRadarUpdate)((PacketSingle)packet).message;
        if (lastLockingMessage.senderUID != networkUID)
            return;

        if (!lastLockingMessage.isLocked && lockingRadar.IsLocked())
        {
            lockingRadar.Unlock();
            return;
        }
        if (lastLockingMessage.isLocked && !lockingRadar.IsLocked())
        {
            foreach (var AI in AIManager.AIVehicles)
            {
                if (AI.vehicleUID == lastLockingMessage.actorUID)
                {
                    lockingRadar.ForceLock(AI.actor, out radarLockData);
                    Debug.Log($"Radar {gameObject.name} found its lock {AI.vehicleName} at id {AI.vehicleUID}.");
                    break;
                }
            }
        }
    }
    public void OnDestroy()
    {
        Networker.RadarUpdate -= RadarUpdate;
        Debug.Log("Destroyed ExtLight");
        Debug.Log(gameObject.name);
    }
}
