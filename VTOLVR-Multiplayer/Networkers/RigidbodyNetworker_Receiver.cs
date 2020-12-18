using UnityEngine;

/// <summary>
/// Updates objects with a  rigidbody over the network using velocity and position.
/// </summary>
public class RigidbodyNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;

    private Vector3D globalTargetPosition;
    private Vector3 localTargetPosition;
    private Vector3 targetVelocity;
    private Quaternion targetRotation;
    private Vector3 targetRotationVelocity;
    private Rigidbody rb;
    private Actor actor;
    private KinematicPlane kplane;
    private float positionThreshold = 25.5f;
    public float smoothingTime = 0.1f;
    private float rotSmoothingTime = 0.1f;
    private float velSmoothingTime = 0.5f;//actor velocity for using with the gunsight, should stop the jitter
    private float latency = 0.0f;

    private PlayerManager.Player playerWeRepresent;

    private ulong mostCurrentUpdateNumber;

    private void Awake()
    {
        kplane = GetComponent<KinematicPlane>();
        actor = GetComponent<Actor>();

        if (kplane != null)
        {
            kplane.enabled = false;
            Debug.Log("Dissabled kplane on " + gameObject.name);
        }
        else
        {
            Debug.Log("Could not find kplane on " + gameObject.name);
        }

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        FloatingOriginTransform originTransform = GetComponent<FloatingOriginTransform>();
        if (originTransform == null)
            originTransform = gameObject.AddComponent<FloatingOriginTransform>();

        originTransform.SetRigidbody(rb);

        Networker.RigidbodyUpdate += RigidbodyUpdate;


        mostCurrentUpdateNumber = 0;
    }

    void LateUpdate()
    {
        ///stops baha touching our velocities
        actor.fixedVelocityUpdate = true;
        if (rb == null)
        {
            Debug.LogError("Rigid body is null on object " + gameObject.name);
        }
        if (rb.isKinematic == false)
        {
            rb.isKinematic = true;
            Debug.Log("Rigidbody was not kinematic on " + gameObject.name);
        }

        if (kplane != null) // yes this can be null on objects that arent airplanes
        {
            if (kplane.enabled == true)
            {
                kplane.enabled = false;
                Debug.Log("Disabled kplane again on " + gameObject.name);
            }
        }
        if (playerWeRepresent == null)
        {
            int playerID = PlayerManager.FindPlayerIDFromNetworkUID(networkUID);//get the ping of the player we represent
            if (playerID == -1)
            {//we are not a player, get the ping from the host
                playerID = PlayerManager.FindPlayerIDFromNetworkUID(PlayerManager.GetPlayerUIDFromCSteamID(Networker.hostID));//getting the host
            }
            if (playerID != -1)//couldnt find host latency, that sucks
            {
                playerWeRepresent = PlayerManager.players[playerID];
            }
        }
        if (playerWeRepresent != null)
        {
            //delta time needs to be added to latency as this runs after packet has arrived for a while
            latency = playerWeRepresent.ping;
        }

        globalTargetPosition += new Vector3D(targetVelocity * Time.fixedDeltaTime);
        localTargetPosition = VTMapManager.GlobalToWorldPoint(globalTargetPosition);

        Quaternion quatVel = Quaternion.Euler(targetRotationVelocity * Time.fixedDeltaTime);
        Quaternion currentRotation = transform.rotation;
        currentRotation *= quatVel;

        actor.SetCustomVelocity(Vector3.Lerp(actor.velocity, targetVelocity + (localTargetPosition - transform.position) / smoothingTime, Time.fixedDeltaTime / velSmoothingTime));
        rb.velocity = actor.velocity;
        
        Vector3D errorVec = (globalTargetPosition- VTMapManager.WorldToGlobalPoint(transform.position));
      
            rb.MovePosition(transform.position + targetVelocity * Time.fixedDeltaTime + ((errorVec.toVector3) * Time.fixedDeltaTime) / smoothingTime);
        Quaternion quat = Quaternion.Slerp(currentRotation, targetRotation, Time.fixedDeltaTime / rotSmoothingTime);
        rb.MoveRotation(quat.normalized);
    }

    public void RigidbodyUpdate(Packet packet)
    {
        Message_RigidbodyUpdate rigidbodyUpdate = (Message_RigidbodyUpdate)((PacketSingle)packet).message;
        //Debug.Log($"Rigidbody Update\nOur Network ID = {networkUID} Packet Network ID = {rigidbodyUpdate.networkUID}");
        if (rigidbodyUpdate.networkUID != networkUID)
            return;

        if (rigidbodyUpdate.sequenceNumber <= mostCurrentUpdateNumber)
         return;
        mostCurrentUpdateNumber = rigidbodyUpdate.sequenceNumber;

        globalTargetPosition = rigidbodyUpdate.position + rigidbodyUpdate.velocity.toVector3 * latency;
        localTargetPosition = VTMapManager.GlobalToWorldPoint(globalTargetPosition);
        targetVelocity = rigidbodyUpdate.velocity.toVector3;
        targetRotation = rigidbodyUpdate.rotation * Quaternion.Euler(rigidbodyUpdate.angularVelocity.toVector3 * latency);
        targetRotationVelocity = rigidbodyUpdate.angularVelocity.toVector3;

        Vector3D errorVec = (VTMapManager.WorldToGlobalPoint(transform.position) - globalTargetPosition);
        if (errorVec.magnitude > positionThreshold)
        {
            //Debug.Log("Outside of thresh hold, moving " + gameObject.name);
            transform.position = localTargetPosition;
            transform.rotation = rigidbodyUpdate.rotation;
        }
    }

 
    //sliders for testing different values for smoothing interpolation
    //uncomment if you wana tweak them in realtime
    //void OnGUI()
    //{
    //    smoothingTime = GUI.HorizontalSlider(new Rect(25, 25, 200, 30), smoothingTime, 0.1F, 10.0F);
    //    velocityMatchingForce = GUI.HorizontalSlider(new Rect(25, 50, 200, 30), velocityMatchingForce, 0.0F, 10.0F);
    //    GUI.TextField(new Rect(300, 25, 200, 30), "smoothing time: " + (Mathf.Round(smoothingTime*10)/10f).ToString());
    //    GUI.TextField(new Rect(300, 50, 200, 30), "velocityMatchingForce: " + (Mathf.Round(velocityMatchingForce*10)/10f).ToString());
    //}

    public void OnDisconnect(Packet packet)
    {
        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        if (message.UID != networkUID)
            return;
        Destroy(gameObject);
    }

    public void OnDestroy()
    {
        Networker.RigidbodyUpdate -= RigidbodyUpdate;

        Debug.Log("Destroyed Rigidbody Update");
        Debug.Log(gameObject.name);
    }
}
