using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class TurretNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_TurretUpdate lastMessage;
    public ModuleTurret turret;

    private void Awake()
    {
        lastMessage = new Message_TurretUpdate(new Vector3D(), networkUID);
        Networker.TurretUpdate += TurretUpdate;
    }

    public void TurretUpdate(Packet packet)
    {
        lastMessage = (Message_TurretUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        turret.AimToTargetImmediate(turret.pitchTransform.position + lastMessage.direction.toVector3.normalized * 1000);
    }

    public void OnDestroy()
    {
        Networker.TurretUpdate -= TurretUpdate;
        Debug.Log("Destroyed TurretUpdate");
        Debug.Log(gameObject.name);
    }
}
