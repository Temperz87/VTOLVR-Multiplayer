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
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
    }
}
