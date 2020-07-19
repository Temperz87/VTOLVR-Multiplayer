using UnityEngine;

class ShipNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    private float timer;
    public ShipMover ship;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Vector3D(), networkUID);
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer > 1) {
            timer = 0;

            lastMessage.position = VTMapManager.WorldToGlobalPoint(ship.transform.position);
            lastMessage.rotation = new Vector3D(ship.transform.rotation.eulerAngles);

            lastMessage.UID = networkUID;
            if (Networker.isHost)
                Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}
