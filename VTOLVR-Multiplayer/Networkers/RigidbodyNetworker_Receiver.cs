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
    }

    public void RigidbodyUpdate(Packet packet)
    {
        if (packet.networkUID != networkUID)
            return;
        Message_RigidbodyUpdate rigidbodyUpdate = (Message_RigidbodyUpdate)((PacketSingle)packet).message;
        targetPosition = VTMapManager.GlobalToWorldPoint(rigidbodyUpdate.position);
        rb.velocity = rigidbodyUpdate.velocity.toVector3;
        rb.angularVelocity = rigidbodyUpdate.angularVelocity.toVector3;

        if (Vector3.Distance(transform.position, targetPosition) > positionThreshhold)
        {
            Debug.Log("Outside of thresh hold, moving " + gameObject.name);
            transform.position = targetPosition;
        }
        else
        {
            Debug.Log($"Updating Position of UID {networkUID} to {transform.position} : {rb.velocity}" + 
                $"\nNetwork Message was {rigidbodyUpdate.position} : {rigidbodyUpdate.velocity}");
        }
    }

    public void OnDestroy()
    {
        Networker.RigidbodyUpdate -= RigidbodyUpdate;
        Debug.Log("Destroyed Rigidbody Update");
        Debug.Log(gameObject.name);
    }
}
