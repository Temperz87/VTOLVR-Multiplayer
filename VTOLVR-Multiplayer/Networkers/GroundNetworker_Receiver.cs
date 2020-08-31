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
    public Vector3D targetPositionGlobal;
    public Vector3 targetPosition;
    public Vector3 targetVelocity;
    public Quaternion targetRotation;

    Vector3D smoothedPosition;
    Quaternion smoothedRotation;

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

        smoothedPosition = smoothedPosition + targetVelocity * Time.fixedDeltaTime + ((targetPositionGlobal - smoothedPosition) * Time.fixedDeltaTime) / smoothTime;
        smoothedRotation = Quaternion.Lerp(smoothedRotation, targetRotation, Time.fixedDeltaTime / rotSmoothTime);

        Vector3 adjustedPos = VTMapManager.GlobalToWorldPoint(smoothedPosition);
        Vector3 surfaceNormal = smoothedRotation * Vector3.up;
        Vector3 surfaceRight = smoothedRotation * Vector3.right;
        Vector3 surfaceForward = smoothedRotation * Vector3.forward;
        Quaternion adjustedRotation = smoothedRotation;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + 500 * Vector3.up, Vector3.down, out hit, 1000, 1, QueryTriggerInteraction.Ignore) && false) {
            adjustedPos = hit.point;
            surfaceNormal = hit.normal;
            surfaceRight = Vector3.Cross(surfaceNormal, smoothedRotation * Vector3.forward);
            surfaceForward = Vector3.Cross(surfaceRight, surfaceNormal);

            adjustedRotation = Quaternion.LookRotation(surfaceForward, surfaceNormal);
        }

        rb.MovePosition(adjustedPos);
        rb.transform.rotation = adjustedRotation;//move rotation was throwing "Rotation quaternions must be unit length"
    }

    public void GroundUpdate(Packet packet)
    {
        lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        targetPositionGlobal = lastMessage.position + lastMessage.velocity * Networker.pingToHost;
        targetVelocity = lastMessage.velocity.toVector3;
        targetRotation = lastMessage.rotation;
        targetRotation = targetRotation.normalized;
        

        Debug.Log("Ground reciever rotation is: " + lastMessage.rotation.ToString());

        if ((VTMapManager.GlobalToWorldPoint(lastMessage.position) - groundUnitMover.transform.position).magnitude > 100) {
            Debug.Log("Ground mover is too far, teleporting.");
            groundUnitMover.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
            groundUnitMover.transform.rotation = lastMessage.rotation;
            smoothedPosition = lastMessage.position;
            smoothedRotation = lastMessage.rotation;
        }
    }

    public void OnDestroy()
    {
        Networker.ShipUpdate -= GroundUpdate;
        Debug.Log("Destroyed GroundMoverUpdate");
        Debug.Log(gameObject.name);
    }
}
