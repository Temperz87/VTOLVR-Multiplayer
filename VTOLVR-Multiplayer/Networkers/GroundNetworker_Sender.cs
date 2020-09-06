using UnityEngine;

class GroundNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    private float timer;
    public GroundUnitMover groundUnitMover;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);//it uses ship update, cause the information really isnt all that different
        groundUnitMover = GetComponent<GroundUnitMover>();
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer > 0.5f) {
            timer = 0;

            lastMessage.position = VTMapManager.WorldToGlobalPoint(groundUnitMover.transform.position);
            lastMessage.rotation = groundUnitMover.transform.rotation.normalized;
            lastMessage.velocity = new Vector3D(groundUnitMover.velocity);

            Debug.Log("Ground sender rotation is: " + lastMessage.rotation.ToString());

            lastMessage.UID = networkUID;
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
    }
}
