using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class ShipNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    public ShipMover ship;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), networkUID);
        Networker.ShipUpdate += ShipUpdate;
    }

    public void ShipUpdate(Packet packet)
    {
        lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        ship.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
        ship.transform.rotation = Quaternion.Euler(lastMessage.rotation.toVector3);
    }

    public void OnDestroy()
    {
        Networker.ShipUpdate -= ShipUpdate;
        Debug.Log("Destroyed ShipUpdate");
        Debug.Log(gameObject.name);
    }
}
