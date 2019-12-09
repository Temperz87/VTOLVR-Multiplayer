using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public class RigidbodyNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Rigidbody rb;
    private Message_RigidbodyUpdate lastMessage;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Added RB");
        FloatingOriginTransform originTransform = GetComponent<FloatingOriginTransform>();
        if (originTransform == null)
            originTransform = gameObject.AddComponent<FloatingOriginTransform>();
        Debug.Log("Set or got floating origin transform");
        originTransform.SetRigidbody(rb);
        Debug.Log("Setting last message");
        lastMessage = new Message_RigidbodyUpdate(rb.velocity, rb.angularVelocity, transform.position, networkUID);
    }

    private void LateUpdate()
    {
        lastMessage.position = Networker.GetWorldCentre() - transform.position;
        lastMessage.velocity = rb.velocity;
        lastMessage.angularVelocity = rb.angularVelocity;
        if (Networker.isHost)
            Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        else
            Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
    }
}
