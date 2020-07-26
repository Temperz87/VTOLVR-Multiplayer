using UnityEngine;

class ShipNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    private float timer;
    public ShipMover ship;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Vector3D(), new Vector3D(), new Vector3D(), networkUID);
        ship = GetComponent<ShipMover>();
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer > 1) {
            timer = 0;

            lastMessage.position = VTMapManager.WorldToGlobalPoint(ship.transform.position);
            lastMessage.rotation = new Vector3D(ship.transform.rotation.eulerAngles);
            lastMessage.velocity = new Vector3D(ship.velocity);

            if (ship.currWpt != null) {
                lastMessage.destination = ship.currWpt.globalPoint;
            }
            else if (ship.currPath != null) {
                lastMessage.destination = VTMapManager.WorldToGlobalPoint(ship.transform.position + ship.transform.forward * 10000);
            }
            else {
                lastMessage.destination = new Vector3D(0,0,0);
            }

            lastMessage.UID = networkUID;
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}
