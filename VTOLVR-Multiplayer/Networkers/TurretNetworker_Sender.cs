using UnityEngine;

class TurretNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_TurretUpdate lastMessage;
    public ModuleTurret turret;

    bool lastOn;
    float lastFov;

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
            Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
}
