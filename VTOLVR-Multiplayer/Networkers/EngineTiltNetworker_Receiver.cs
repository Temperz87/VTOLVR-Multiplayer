using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class EngineTiltNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_EngineTiltUpdate lastMessage;
    private TiltController tiltController;

    private void Awake()
    {
        tiltController = GetComponent<TiltController>();
        lastMessage = new Message_EngineTiltUpdate(networkUID, 0);
        Networker.EngineTiltUpdate += EngineTiltUpdate;
    }

    public void EngineTiltUpdate(Packet packet)
    {
        lastMessage = (Message_EngineTiltUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        tiltController.SetTiltImmediate(lastMessage.angle);
    }

    public void OnDestory()
    {
        Networker.EngineTiltUpdate -= EngineTiltUpdate;
        Debug.Log("Destroyed Engine Tilt Update");
        Debug.Log(gameObject.name);
    }

}