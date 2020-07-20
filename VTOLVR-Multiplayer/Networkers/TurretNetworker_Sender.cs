using UnityEngine;

class TurretNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_TurretUpdate lastMessage;
    public ModuleTurret turret;

    private void Awake()
    {
        lastMessage = new Message_TurretUpdate(new Vector3D(), networkUID);
    }

    void FixedUpdate()
    {
        Vector3D dir = new Vector3D(turret.pitchTransform.forward);
        lastMessage.direction = dir;

        lastMessage.UID = networkUID;
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
}
