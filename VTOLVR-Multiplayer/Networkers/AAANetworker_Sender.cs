using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class AAANetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    public ulong gunID;
    private GunTurretAI gunTurret;
    private void Awake()
    {
        gunTurret = gameObject.GetComponentInChildren<GunTurretAI>();
        gunTurret.gun.OnSetFire.AddListener(AAAUpdate);
    }
    private void AAAUpdate(bool isFiring)
    {
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_AAAUpdate(isFiring, networkUID, gunID), Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
}
