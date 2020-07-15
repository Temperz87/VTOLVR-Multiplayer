using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    // private Rigidbody rigidbody; doesn't exist in some missiles so we're not fucking with that shit
    private Message_MissileUpdate lastMessage;
    private Missile thisMissile;
    private bool receivedGlobalUID = false;
    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        thisMissile = GetComponent<Missile>();
        // rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (receivedGlobalUID && thisMissile != null && thisMissile.fired)
        {
            if (lastMessage == null)
            {
                Debug.LogError("lastMessage null");
            }
            lastMessage.networkUID = networkUID;
            /*if (rigidbody == null)
            {
                Debug.LogError("Rigidbody null");
            }
            if (rigidbody.position == null)
            {
                Debug.LogError("Rigidbody position null");
            }*/
            if (gameObject == null)
            {
                Debug.LogError("gameObject null in MIssileNetworked_Sender");
            }
            lastMessage.position = VTMapManager.WorldToGlobalPoint(gameObject.transform.position);
            Debug.Log("Missile_sender lastmessage.position");
            lastMessage.rotation = new Vector3D(gameObject.transform.rotation.eulerAngles);
            Debug.Log("Missile_sender lastmessage.rotation");
            lastMessage.guidanceMode = thisMissile.guidanceMode;
            if (thisMissile.guidanceMode == Missile.GuidanceModes.Radar)
            {
                if (thisMissile.radarLock != null && thisMissile.radarLock.actor != null)
                {
                    Debug.Log("Missile_sender lock data");
                    lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.radarLock.actor.transform.position);
                }
            }
            SendMessage(false);
            Debug.Log("Missile_sender Sendmessage");
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