using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;

class ShipNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    public ShipMover ship;
    public Traverse shipTraverse;

    public float smoothTime = 5f;
    public float rotSmoothTime = 5f;
    public Vector3D targetPositionGlobal;
    public Vector3 targetPosition;
    public Vector3 targetVelocity;
    public Quaternion targetRotation;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);
        Networker.ShipUpdate += ShipUpdate;

        ship = GetComponent<ShipMover>();
        ship.enabled = false;
        shipTraverse = Traverse.Create(ship);
    }

    void FixedUpdate() {
        targetPositionGlobal += targetVelocity * Time.fixedDeltaTime;
        targetPosition = VTMapManager.GlobalToWorldPoint(targetPositionGlobal);
        ship.rb.MovePosition(ship.transform.position + targetVelocity * Time.fixedDeltaTime + ((targetPosition - ship.transform.position) * Time.fixedDeltaTime) / smoothTime);
        ship.rb.velocity = targetVelocity + (targetPosition - ship.transform.position)/smoothTime;
        shipTraverse.Field("_velocity").SetValue(ship.rb.velocity);
        ship.rb.MoveRotation(Quaternion.Lerp(ship.transform.rotation, targetRotation, Time.fixedDeltaTime/rotSmoothTime));
    }

    public void ShipUpdate(Packet packet)
    {
        lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        targetPositionGlobal = lastMessage.position + lastMessage.velocity * Networker.pingToHost;
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
