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
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastMessage = new Message_RigidbodyUpdate(new Vector3D(rb.velocity), new Vector3D(rb.angularVelocity), new Vector3D(transform.position), new Vector3D(transform.rotation.eulerAngles), networkUID);
    }

    private void LateUpdate()
    {
        lastMessage.position = VTMapManager.WorldToGlobalPoint(transform.position);
        lastMessage.rotation = new Vector3D(transform.rotation.eulerAngles);
        if (Multiplayer.SoloTesting)
            lastMessage.position += new Vector3D(-30,0,0);
        lastMessage.velocity = new Vector3D(rb.velocity);
        lastMessage.angularVelocity = new Vector3D(rb.angularVelocity);
        lastMessage.networkUID = networkUID;
        if (Networker.isHost)
            Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        else
            Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
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
