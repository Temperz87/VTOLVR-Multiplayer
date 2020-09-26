using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class AAANetworker_Sender : MonoBehaviour
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

    void FixedUpdate() {
        int rocketCount = traverse.Field("rocketCount").GetValue();
        if (lastRocketCount != rocketCount) {
            if (rocketCount < lastRocketCount) {
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
