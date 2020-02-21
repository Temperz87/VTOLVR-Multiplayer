using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Rigidbody rigidbody;
    private Message_MissileUpdate lastMessage;
    private Missile thisMissile;
    private bool receivedGlobalUID = false;
    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        thisMissile = GetComponent<Missile>();
        rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (receivedGlobalUID && thisMissile != null && thisMissile.fired)
        {
            lastMessage.networkUID = networkUID;
            lastMessage.position = VTMapManager.WorldToGlobalPoint(rigidbody.position);
            lastMessage.rotation = new Vector3D(rigidbody.rotation.eulerAngles);
            if (thisMissile.radarLock != null && thisMissile.radarLock.actor != null)
            {
                lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.radarLock.actor.transform.position);
            }
            SendMessage(false);
        }
    }
    public void RequestUID(Packet packet)
    {
        Message_RequestNetworkUID lastMessage = ((PacketSingle)packet).message as Message_RequestNetworkUID;
        if (lastMessage.clientsUID != networkUID)
            return;
        networkUID = lastMessage.resultUID;
        Debug.Log($"Missile ({gameObject.name}) has received their UID from the host. \n Missiles UID = {networkUID}");
        Networker.RequestNetworkUID -= RequestUID;
        receivedGlobalUID = true;
    }
    /// <summary>
    /// OnDestory will mostlikley be called when the missile blows up.
    /// </summary>
    public void OnDestory()
    {
        lastMessage.hasMissed = true;
        SendMessage(true);
    }

    private void SendMessage(bool isDestoryed)
    {
        if (Networker.isHost)
        {
            Networker.SendGlobalP2P(lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        else
        {
            Networker.SendP2P(Networker.hostID,lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}