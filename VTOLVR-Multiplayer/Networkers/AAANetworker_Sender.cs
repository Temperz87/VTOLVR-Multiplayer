using Harmony;
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

class RocketLauncherNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private RocketLauncher rocketLauncher;

    private Traverse traverse;
    int lastRocketCount;

    private void Awake()
    {
        rocketLauncher = gameObject.GetComponentInChildren<RocketLauncher>();
        traverse = Traverse.Create(rocketLauncher);
    }

    void FixedUpdate()
    {
        int rocketCount = (int)traverse.Field("rocketCount").GetValue();
        if (lastRocketCount != rocketCount)
        {
            Debug.Log("Rocket ammount changed.");
            if (rocketCount < lastRocketCount)
            {
                Debug.Log("It has decreased, launching missles!");
                RocketUpdate();
            }
            lastRocketCount = rocketCount;
        }
    }

    private void RocketUpdate()
    {
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_RocketLauncherUpdate(networkUID), Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
}