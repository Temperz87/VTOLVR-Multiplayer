using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class ShipNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    public ShipMover ship;
    public Waypoint waypoint;

    public float smoothTime = 5;
    public Vector3 targetPosition;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Vector3D(), new Vector3D(), new Vector3D(), networkUID);
        Networker.ShipUpdate += ShipUpdate;

        waypoint = new Waypoint();
        GameObject wptTransform = new GameObject();
        waypoint.SetTransform(wptTransform.transform);
    }

    void FixedUpdate() {
        targetPosition += lastMessage.velocity.toVector3 * Time.fixedDeltaTime;
        ship.transform.position += (targetPosition - ship.transform.position) * Time.fixedDeltaTime / smoothTime;
    }

    public void ShipUpdate(Packet packet)
    {
        lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        targetPosition = VTMapManager.GlobalToWorldPoint(lastMessage.position);
        ship.transform.rotation = Quaternion.Euler(lastMessage.rotation.toVector3);

        if (lastMessage.destination.toVector3 != Vector3.zero)
        {
            waypoint.GetTransform().position = VTMapManager.GlobalToWorldPoint(lastMessage.destination);
            ship.MoveTo(waypoint);
        }
        else {
            waypoint.GetTransform().position = ship.transform.position;
            ship.MoveTo(waypoint);
        }
    }

    public void OnDestroy()
    {
        Networker.ShipUpdate -= ShipUpdate;
        Debug.Log("Destroyed ShipUpdate");
        Debug.Log(gameObject.name);
    }
}
