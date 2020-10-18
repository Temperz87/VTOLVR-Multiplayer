using UnityEngine;

class TurretNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    public ulong turretID;
    private Message_TurretUpdate lastMessage;
    public ModuleTurret turret;

    private void Awake()
    {
        lastMessage = new Message_TurretUpdate(new Vector3D(), networkUID, turretID);
        Networker.TurretUpdate += TurretUpdate;
        if (turret == null)
        {
            turret = base.GetComponentInChildren<ModuleTurret>();
            if (turret == null)
            {
                Debug.LogError($"Turret was null on ID {networkUID}");
            }
        }
    }

    public void TurretUpdate(Packet packet)
    {
        lastMessage = (Message_TurretUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;
        if (lastMessage.turretID != turretID)
            return;

        turret.AimToTargetImmediate(lastMessage.direction.toVector3.normalized * 1000);
    }

    public void OnDestroy()
    {
        Networker.TurretUpdate -= TurretUpdate;
        Debug.Log("Destroyed TurretUpdate");
        Debug.Log(gameObject.name);
    }
}
