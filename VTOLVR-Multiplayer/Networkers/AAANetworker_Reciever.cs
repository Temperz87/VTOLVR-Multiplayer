using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class AAANetworker_Reciever : MonoBehaviour
{
    public ulong networkUID;
    public ulong gunID;
    private Message_AAAUpdate lastMessage;
    private GunTurretAI gunTurret;

    private void Awake()
    {
        gunTurret = gameObject.GetComponentInChildren<GunTurretAI>();
        Networker.AAAUpdate += AAAUpdate;
        gunTurret.gun.currentAmmo = gunTurret.gun.maxAmmo;
    }
    private void AAAUpdate(Packet packet)
    {
        lastMessage = (Message_AAAUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        if (lastMessage.gunID != gunID)
            return;
        gunTurret.gun.SetFire(lastMessage.isFiring);
    }

    public void OnDestroy()
    {
        Networker.AAAUpdate -= AAAUpdate;
        Debug.Log("Destroyed AAAUpdate");
        Debug.Log(gameObject.name);
    }
}

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
        Debug.Log("Launching rocket!");
        rocketLauncher.FireRocket();
    }

    public void OnDestroy()
    {
        Networker.RocketUpdate -= RocketUpdate;
        Debug.Log("Destroyed RocketUpdate");
        Debug.Log(gameObject.name);
    }
}
