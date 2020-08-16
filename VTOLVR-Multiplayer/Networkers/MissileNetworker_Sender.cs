using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_MissileUpdate lastMessage;
    private Message_MissileLaunch lastLaunchMessage;
    private Message_MissileDetonate lastDetonateMessage;
    private Missile thisMissile;
    private bool hasFired = false;

    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        lastLaunchMessage = new Message_MissileLaunch(networkUID, Quaternion.identity);
        lastDetonateMessage = new Message_MissileDetonate(networkUID);
        thisMissile = GetComponent<Missile>();
    }

    private void FixedUpdate()
    {
        if (hasFired != thisMissile.fired)
        {
            Debug.Log("Missile fired " + thisMissile.name);
            hasFired = true;

            RigidbodyNetworker_Sender rbSender = gameObject.AddComponent<RigidbodyNetworker_Sender>();
            rbSender.networkUID = networkUID;
        }
        if (thisMissile.fired)
        {
            lastLaunchMessage.networkUID = networkUID;
            if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
            {
                lastLaunchMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
                lastLaunchMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
            }
            if (Networker.isHost)
            {
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastDetonateMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            else
            {
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastDetonateMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
        }
        /*if (thisMissile.guidanceMode == Missile.GuidanceModes.Radar)
        {
            if (thisMissile.isPitbull)
            {
                Debug.Log(gameObject.name + " is now pitbull.");
            }
        }*/
    }
    public void RequestUID(Packet packet)
    {
        Message_RequestNetworkUID lastMessage = ((PacketSingle)packet).message as Message_RequestNetworkUID;
        if (lastMessage.clientsUID != networkUID)
            return;
        networkUID = lastMessage.resultUID;
        Debug.Log($"Missile ({gameObject.name}) has received their UID from the host. \n Missiles UID = {networkUID}");
        Networker.RequestNetworkUID -= RequestUID;
    }
    /// <summary>
    /// OnDestory will most likley be called when the missile blows up.
    /// </summary>
    public void OnDestroy()
    {
        if (Networker.isHost)
        {
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastDetonateMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastDetonateMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        }
    }

    private void SendMessage(bool isDestoryed)
    {
        if (Networker.isHost)
        {
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID,lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}