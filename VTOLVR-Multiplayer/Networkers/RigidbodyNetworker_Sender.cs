using System;
using System.Collections;
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
    public Vector3 spawnPos, spawnRot;
    private Vector3 lastPos;
    private Actor actor;
    private float Threshold;
    private bool sentFirstMessage;
    private void Awake()
    {
        actor = gameObject.GetComponent<Actor>();

        if (actor.role == Actor.Roles.Air)
        {
            Threshold = 1f;
        }
        else if (actor.role == Actor.Roles.Ground)
        {
            Threshold = 5f;
        }
        else
        {
            Threshold = 5f;
        }
        // Debug.Log($"Threshold for {actor.name} is now {Threshold}.");
        lastPos = gameObject.transform.position;
        rb = GetComponent<Rigidbody>();
        lastMessage = new Message_RigidbodyUpdate(new Vector3D(rb.velocity), new Vector3D(rb.angularVelocity), new Vector3D(transform.position), new Vector3D(transform.rotation.eulerAngles), networkUID);
    }

    private void FixedUpdate()
    {

        // If the player is landed change the threshold to be lower
        if (actor.role == Actor.Roles.Air)
        {
            if (actor.flightInfo != null)
            {
                if (actor.flightInfo.isLanded)
                {
                    Threshold = .2f;
                }
                else
                {
                    Threshold = 1f;
                }
            }
        }
        
        if (Vector3.Distance(lastPos, gameObject.transform.position) > Threshold || sentFirstMessage == false)
        {
            lastMessage.position = VTMapManager.WorldToGlobalPoint(transform.position);
            lastMessage.rotation = new Vector3D(transform.rotation.eulerAngles);
            if (Multiplayer.SoloTesting)
                lastMessage.position += new Vector3D(-30, 0, 0);
            lastMessage.velocity = new Vector3D(rb.velocity);
            lastMessage.angularVelocity = new Vector3D(rb.angularVelocity);
            lastMessage.networkUID = networkUID;
            sentFirstMessage = true;
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
            // Temperz STOP KILLING PERFORMANCE AND HARD DRIVES!
            //Debug.Log($"{actor.name} is not outside of the threshold {Threshold}, the distance is {Vector3.Distance(lastPos, gameObject.transform.position)} not updating it.");
    }

    public void SetSpawn()
    {
        StartCoroutine(SetSpawnEnumerator());
    }

    private IEnumerator SetSpawnEnumerator()
    {
        yield return new WaitForSeconds(0.5f);
        rb.position = spawnPos;
        rb.rotation = Quaternion.Euler(spawnRot);
        Debug.Log($"Our position is now {rb.position}");
    }
}
