using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class RocketLauncherNetworker_Reciever : MonoBehaviour
{
    public ulong networkUID;
    private Message_RocketLauncherUpdate lastMessage;
    private RocketLauncher rocketLauncher;

    private void Awake()
    {
        rocketLauncher = gameObject.GetComponentInChildren<RocketLauncher>();
        Networker.RocketUpdate += RocketUpdate;
    }
    private void RocketUpdate(Packet packet)
    {
        lastMessage = (Message_RocketLauncherUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        rocketLauncher.FireRocket();
    }

    public void OnDestroy()
    {
        Networker.RocketUpdate -= RocketUpdate;
        Debug.Log("Destroyed RocketUpdate");
        Debug.Log(gameObject.name);
    }
}
