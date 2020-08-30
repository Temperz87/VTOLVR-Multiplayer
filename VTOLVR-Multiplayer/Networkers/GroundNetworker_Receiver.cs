using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class GroundNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    public GroundUnitMover groundUnitMover;
    public Rigidbody rb;

    public float smoothTime = 2f;
    public float rotSmoothTime = 0.5f;
    public Vector3 targetPositionGlobal;
    public Vector3 targetPosition;
    public Vector3 targetVelocity;
    public Quaternion targetRotation;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);//it uses ship update, cause the information really isnt all that different
        Networker.ShipUpdate += GroundUpdate;

        groundUnitMover = GetComponent<GroundUnitMover>();
        groundUnitMover.enabled = false;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        targetPositionGlobal += targetVelocity * Time.fixedDeltaTime;
        targetPosition = VTMapManager.GlobalToWorldPoint(new Vector3D(targetPositionGlobal));
        rb.MovePosition(groundUnitMover.transform.position + targetVelocity * Time.fixedDeltaTime + ((targetPosition - groundUnitMover.transform.position) * Time.fixedDeltaTime) / smoothTime);
        rb.MoveRotation(Quaternion.Lerp(groundUnitMover.transform.rotation, targetRotation, Time.fixedDeltaTime/rotSmoothTime));
    }

    public void GroundUpdate(Packet packet)
    {
        lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        targetPositionGlobal = lastMessage.position.toVector3 + lastMessage.velocity.toVector3 * Networker.pingToHost;
        targetVelocity = lastMessage.velocity.toVector3;
        targetRotation =  lastMessage.rotation;

        if ((VTMapManager.GlobalToWorldPoint(lastMessage.position) - groundUnitMover.transform.position).magnitude > 100) {
            Debug.Log("Ground mover is too far, teleporting.");
            groundUnitMover.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
        }
    }

    public void OnDestroy()
    {
        Networker.ShipUpdate -= GroundUpdate;
        Debug.Log("Destroyed GroundMoverUpdate");
        Debug.Log(gameObject.name);
    }
}
