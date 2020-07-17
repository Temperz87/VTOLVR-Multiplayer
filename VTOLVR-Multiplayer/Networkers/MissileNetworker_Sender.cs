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
        Debug.Log("Missile networker awake");
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        thisMissile = GetComponent<Missile>();
        // rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (thisMissile == null)
        {
            Debug.LogError("thisMissile null.");
        }
        if (thisMissile != null && thisMissile.fired)
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
                Debug.LogError("gameObject null in MissileNetworker_Sender");
            }
            lastMessage.position = VTMapManager.WorldToGlobalPoint(gameObject.transform.position);
            lastMessage.rotation = new Vector3D(gameObject.transform.rotation.eulerAngles);
            lastMessage.guidanceMode = thisMissile.guidanceMode;
            if (thisMissile.guidanceMode == Missile.GuidanceModes.Radar)
            {
                if (thisMissile.radarLock != null && thisMissile.radarLock.actor != null)
                {
                    Debug.Log("Missile_sender lock data");
                    lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.radarLock.actor.transform.position);
                }
            }
            else if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
            {
                lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
            }
            SendMessage(false);
            Debug.Log("Missile_sender Send message");
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
        Debug.Log("Recieved global UID");
    }
    /// <summary>
    /// OnDestory will most likley be called when the missile blows up.
    /// </summary>
    public void OnDestroy()
    {
        lastMessage.hasExploded = true;
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