using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;

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
    private Rigidbody rb;
    private float positionThreshold = 100f;
    private float smoothingTime = 1f;
    private float latency = 0.0f;
    private float velocityMatchingForce = 10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        FloatingOriginTransform originTransform = GetComponent<FloatingOriginTransform>();
        if (originTransform == null)
            originTransform = gameObject.AddComponent<FloatingOriginTransform>();
        originTransform.SetRigidbody(rb);
        Networker.RigidbodyUpdate += RigidbodyUpdate;

        rb.isKinematic = true;
    }

    void FixedUpdate() {
        globalTargetPosition += new Vector3D(targetVelocity * Time.fixedDeltaTime);
        localTargetPosition = VTMapManager.GlobalToWorldPoint(globalTargetPosition);

        rb.MovePosition(transform.position + targetVelocity * Time.fixedDeltaTime + ((localTargetPosition - transform.position) * Time.fixedDeltaTime) / smoothingTime);
        //rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime / smoothingTime));
    }

    public void RigidbodyUpdate(Packet packet)
    {
        Message_RigidbodyUpdate rigidbodyUpdate = (Message_RigidbodyUpdate)((PacketSingle)packet).message;
        //Debug.Log($"Rigidbody Update\nOur Network ID = {networkUID} Packet Network ID = {rigidbodyUpdate.networkUID}");
        if (rigidbodyUpdate.networkUID != networkUID)
            return;

        globalTargetPosition = rigidbodyUpdate.position + rigidbodyUpdate.velocity * latency;
        localTargetPosition = VTMapManager.GlobalToWorldPoint(globalTargetPosition);
        targetVelocity = rigidbodyUpdate.velocity.toVector3;
        targetRotation = rigidbodyUpdate.rotation;

        if (Vector3.Distance(transform.position, localTargetPosition) > positionThreshold)
        {
            //Debug.Log("Outside of thresh hold, moving " + gameObject.name);
            transform.position = localTargetPosition;
            //rb.velocity = rigidbodyUpdate.velocity.toVector3;
            
            transform.rotation = rigidbodyUpdate.rotation;
            //rb.angularVelocity = rigidbodyUpdate.angularVelocity.toVector3;
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
        Networker.Disconnecting -= OnDisconnect;
        Debug.Log("Destroyed Rigidbody Update");
        Debug.Log(gameObject.name);
    }
}
