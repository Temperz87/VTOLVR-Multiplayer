using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;

class GroundNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    public GroundUnitMover groundUnitMover;
    public bool isSoldier;
    public SoldierAnimator soldier;
    public int walkingAnimation = Animator.StringToHash("moveSpeed");
    //public Traverse groundTraverse;
    public Rigidbody rb;

    public float smoothTime = 1f;
    public float rotSmoothTime = 0.5f;
    public Vector3D targetPositionGlobal;
    public Vector3 targetVelocity;
    public Quaternion targetRotation;

    Vector3D smoothedPosition;
    Quaternion smoothedRotation;

    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);//it uses ship update, cause the information really isnt all that different
        Networker.ShipUpdate += GroundUpdate;
         
        groundUnitMover = GetComponent<GroundUnitMover>();
        soldier = GetComponentInChildren<SoldierAnimator>();
        groundUnitMover.enabled = false;
        rb = GetComponent<Rigidbody>();

        if (soldier != null) {
            Debug.Log("uwu, i am a soldier!");
            soldier.enabled = false;
            isSoldier = true;
        }
        //groundTraverse = Traverse.Create(groundUnitMover);
    }

    void FixedUpdate()
    {
        if (targetVelocity.sqrMagnitude > 1)
        {
            targetRotation = Quaternion.LookRotation(targetVelocity);
        }

        targetPositionGlobal += targetVelocity * Time.fixedDeltaTime;

        smoothedPosition = smoothedPosition + targetVelocity * Time.fixedDeltaTime + ((targetPositionGlobal - smoothedPosition) * Time.fixedDeltaTime) / smoothTime;
        smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRotation, Time.fixedDeltaTime / rotSmoothTime);
        smoothedRotation = smoothedRotation.normalized;
        Vector3 adjustedPos = VTMapManager.GlobalToWorldPoint(smoothedPosition);
        Vector3 surfaceNormal = smoothedRotation * Vector3.up;
        Vector3 surfaceRight = smoothedRotation * Vector3.right;
        Vector3 surfaceForward = smoothedRotation * Vector3.forward;
        Quaternion adjustedRotation = smoothedRotation;

        RaycastHit hit;
        if (Physics.Raycast(adjustedPos + 500 * Vector3.up, Vector3.down, out hit, 1000, 1, QueryTriggerInteraction.Ignore))
        {
            adjustedPos = hit.point + hit.normal * groundUnitMover.height;
            surfaceNormal = hit.normal;
            surfaceRight = Vector3.Cross(surfaceNormal, smoothedRotation * Vector3.forward);
            surfaceForward = Vector3.Cross(surfaceRight, surfaceNormal);
            if (surfaceForward != Vector3.zero && surfaceNormal != Vector3.zero)
                adjustedRotation = Quaternion.LookRotation(surfaceForward, surfaceNormal);
        }
        adjustedRotation = adjustedRotation.normalized;
        //groundTraverse.Field("velocity").SetValue(targetVelocity + (targetPositionGlobal - smoothedPosition).toVector3 / smoothTime);
        
        rb.MovePosition(adjustedPos);
        rb.MoveRotation(adjustedRotation);//move rotation was throwing "Rotation quaternions must be unit length"

        if (isSoldier) {
            soldier.animator.SetFloat(walkingAnimation, (targetVelocity + (targetPositionGlobal - smoothedPosition).toVector3 / smoothTime).magnitude);
        }
    }

    public void GroundUpdate(Packet packet)
    {
        lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        targetPositionGlobal = lastMessage.position + lastMessage.velocity.toVector3 * Networker.pingToHost;
        targetVelocity = lastMessage.velocity.toVector3;
        //targetRotation = lastMessage.rotation;
        //targetRotation = targetRotation.normalized;
        //could not get the rotation to work for whatever reason, so ground moves face their velocity vector

        Debug.Log("Ground reciever rotation is: " + lastMessage.rotation.ToString());

        if ((VTMapManager.GlobalToWorldPoint(lastMessage.position) - groundUnitMover.transform.position).magnitude > 100)
        {
            Debug.Log("Ground mover is too far, teleporting.");
            groundUnitMover.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
            Quaternion qs = lastMessage.rotation;
            qs = qs.normalized;
            groundUnitMover.transform.rotation = qs;
            smoothedPosition = lastMessage.position;
            if (targetVelocity.sqrMagnitude > 1)
            {
                smoothedRotation = Quaternion.LookRotation(targetVelocity);
            }
            //smoothedRotation = lastMessage.rotation;
            //smoothedRotation = smoothedRotation.normalized;
        }
    }

    public void OnDestroy()
    {
        Networker.ShipUpdate -= GroundUpdate;
        Debug.Log("Destroyed GroundMoverUpdate");
        Debug.Log(gameObject.name);
    }
}
