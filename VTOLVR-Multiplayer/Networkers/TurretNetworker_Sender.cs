using UnityEngine;

class TurretNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    public ulong turretID;
    private Message_TurretUpdate lastMessage;
    public ModuleTurret turret;
    private float tick;
    public float tickRate = 4.0f;
    private void Awake()
    {
        lastMessage = new Message_TurretUpdate(new Vector3D(), networkUID, turretID);
        if (turret == null)
        {
            turret = base.GetComponentInChildren<ModuleTurret>();
            if (turret == null)
            {
                Debug.LogError($"Turret was null on ID {networkUID}");
            }
        }

        tick += UnityEngine.Random.Range(0.0f, 1.0f / tickRate);
    }

    private void LateUpdate()
    {
        if (turret == null)
            return;

        tick += Time.deltaTime;
        if (tick > 1.0f / tickRate)
        {
            tick = 0.0f;
            Vector3D dir = new Vector3D(turret.pitchTransform.forward);
            lastMessage.direction = dir;

            lastMessage.UID = networkUID;
            lastMessage.turretID = turretID;
            if (Networker.isHost)
                Networker.addToUnreliableSendBuffer(lastMessage);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}
