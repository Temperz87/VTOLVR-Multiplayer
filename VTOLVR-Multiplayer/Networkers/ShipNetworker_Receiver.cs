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

    public float smoothTime = 5f;
    public float rotSmoothTime = 5f;
    public Vector3 targetPositionGlobal;
    public Vector3 targetPosition;
    public Vector3 targetVelocity;
    public Quaternion targetRotation;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);
        Networker.ShipUpdate += ShipUpdate;

        waypoint = new Waypoint();
        GameObject wptTransform = new GameObject();
        waypoint.SetTransform(wptTransform.transform);

        ship = GetComponent<ShipMover>();
        ship.enabled = false;
    }

    void FixedUpdate() {
        targetPositionGlobal += targetVelocity * Time.fixedDeltaTime;
        targetPosition = VTMapManager.GlobalToWorldPoint(new Vector3D(targetPositionGlobal));
        ship.rb.MovePosition(ship.transform.position + targetVelocity * Time.fixedDeltaTime + ((targetPosition - ship.transform.position) * Time.fixedDeltaTime) / smoothTime);
        ship.rb.velocity = targetVelocity + (targetPosition - ship.transform.position)/smoothTime;
        ship.rb.MoveRotation(Quaternion.Lerp(ship.transform.rotation, targetRotation, Time.fixedDeltaTime/rotSmoothTime));
    }

    public void ShipUpdate(Message message)
    {
        lastMessage = (Message_ShipUpdate)message;
        if (lastMessage.UID != networkUID)
            return;

        targetPositionGlobal = lastMessage.position.toVector3 + lastMessage.velocity.toVector3 * Networker.pingToHost;
        targetVelocity = lastMessage.velocity.toVector3;
        targetRotation =  lastMessage.rotation;

        if ((VTMapManager.GlobalToWorldPoint(lastMessage.position) - ship.transform.position).magnitude > 100) {
            Debug.Log("Ship is too far, teleporting. This message should apear once per ship at spawn, if ur seeing more something is probably fucky");
            ship.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
        }
    }

    public void OnDestroy()
    {
        Networker.ShipUpdate -= ShipUpdate;
        Debug.Log("Destroyed ShipUpdate");
        Debug.Log(gameObject.name);
    }
}
