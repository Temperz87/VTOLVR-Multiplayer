using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class AAANetworker_Receiver : MonoBehaviour
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
