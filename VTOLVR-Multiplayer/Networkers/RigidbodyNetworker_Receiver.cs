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

    private Vector3 targetPosition;
    private Rigidbody rb;
    private float positionThreshhold = 10;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        FloatingOriginTransform originTransform = GetComponent<FloatingOriginTransform>();
        if (originTransform == null)
            originTransform = gameObject.AddComponent<FloatingOriginTransform>();
        originTransform.SetRigidbody(rb);
        Networker.RigidbodyUpdate += RigidbodyUpdate;
    }

    public void RigidbodyUpdate(Packet packet)
    {
        Message_RigidbodyUpdate rigidbodyUpdate = (Message_RigidbodyUpdate)((PacketSingle)packet).message;
        //Debug.Log($"Rigidbody Update\nOur Network ID = {networkUID} Packet Network ID = {rigidbodyUpdate.networkUID}");
        if (rigidbodyUpdate.networkUID != networkUID)
            return;
        targetPosition = VTMapManager.GlobalToWorldPoint(rigidbodyUpdate.position);
        rb.velocity = rigidbodyUpdate.velocity.toVector3;
        rb.angularVelocity = rigidbodyUpdate.angularVelocity.toVector3;
        transform.rotation = Quaternion.Euler(rigidbodyUpdate.rotation.toVector3); //Angular Velocity doesn't seem to be working so I'm just setting the rotation.

        if (Vector3.Distance(transform.position, targetPosition) > positionThreshhold)
        {
            Debug.Log("Outside of thresh hold, moving " + gameObject.name);
            transform.position = targetPosition;
        }
        else
        {
            //Debug.Log($"Updating Position of UID {networkUID} to {transform.position} : {rb.velocity}" + 
            //   $"\nNetwork Message was {rigidbodyUpdate.position} : {rigidbodyUpdate.velocity}");
        }
    }

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
