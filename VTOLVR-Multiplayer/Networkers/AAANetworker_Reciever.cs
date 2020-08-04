using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class AAANetworker_Reciever : MonoBehaviour
{
    public ulong networkUID;
    private Message_AAAUpdate lastMessage;
    private GunTurretAI gunTurret;
    private void Awake()
    {
        gunTurret = gameObject.GetComponentInChildren<GunTurretAI>();
        Networker.AAAUpdate += AAAUpdate;
    }
    private void AAAUpdate(Packet packet)
    {
        lastMessage = (Message_AAAUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        Debug.Log("Doing AAA Update");
        gunTurret.gun.SetFire(lastMessage.isFiring);
    }
}
