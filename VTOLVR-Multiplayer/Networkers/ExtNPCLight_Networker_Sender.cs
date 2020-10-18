using Harmony;
using UnityEngine;

class ExtNPCLight_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ExtLight lastMessage;
    public ExteriorLightsController lightsController;

    bool lastNav;
    bool lastStrobe;
    bool lastLand;

    private Traverse traverse;
    private Traverse traverse2;

    private void Awake()
    {
        lastMessage = new Message_ExtLight(false, false, false, networkUID);
        lightsController = GetComponentInChildren<ExteriorLightsController>();
        traverse = Traverse.Create(lightsController.navLights[0]);
        traverse2 = Traverse.Create(lightsController.landingLights[0]);
    }

    void FixedUpdate()
    {
        lastMessage.UID = networkUID;
        if ((bool)traverse.Field("connected").GetValue() != lastNav || lightsController.strobeLights.onByDefault != lastStrobe || (bool)traverse2.Field("connected").GetValue() != lastLand)
        {
            Debug.Log("The lights on " + networkUID + " have changed, sending");

            lastMessage.nav = (bool)traverse.Field("connected").GetValue();
            lastMessage.strobe = lightsController.strobeLights.onByDefault;
            if (traverse2 != null)
                lastMessage.strobe = (bool)traverse2.Field("connected").GetValue();

            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);

            lastNav = (bool)traverse.Field("connected").GetValue();
            lastStrobe = lightsController.strobeLights.onByDefault;
            lastLand = (bool)traverse2.Field("connected").GetValue();
        }

    }
}
