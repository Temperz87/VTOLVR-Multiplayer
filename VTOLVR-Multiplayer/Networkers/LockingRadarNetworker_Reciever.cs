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
    private Message_RadarUpdate lastMessage;
    public LockingRadar lockingRadar;

    private void Awake()
    {
        lastMessage = new Message_RadarUpdate(false, 0, networkUID);
        Networker.RadarUpdate += RadarUpdate;
    }

    public void RadarUpdate(Packet packet)
    {
        lastMessage = (Message_RadarUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        lockingRadar.radar.radarEnabled = lastMessage.on;
        lockingRadar.radar.sweepFov = lastMessage.fov;
    }

    public void OnDestroy()
    {
        Networker.RadarUpdate -= RadarUpdate;
        Debug.Log("Destroyed ExtLight");
        Debug.Log(gameObject.name);
    }
}
