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
    private bool hasReportedPitbull = false;
    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        thisMissile = GetComponent<Missile>();
        // rigidbody = GetComponent<Rigidbody>();
    }

    private void LateUpdate()
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
                    lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.radarLock.actor.transform.position);
                    foreach (var AI in AIManager.AIVehicles)
                    {
                        if (AI.actor == thisMissile.radarLock.actor)
                        {
                            lastMessage.radarLock = AI.vehicleUID;
                            Debug.Log($"Missile {gameObject.name} has found its lock {AI.actor.name} with an uID of {AI.vehicleUID} while trying to lock {thisMissile.radarLock.actor.name}");
                        }
                    }
                }
            }
            else if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
            {
                lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
            }
            SendMessage(false);
        }
        if (thisMissile.guidanceMode == Missile.GuidanceModes.Radar)
        {
            if (thisMissile.isPitbull)
            {
                if (!hasReportedPitbull) {
                    Debug.Log(gameObject.name + " is now pitbull.");
                }
            }
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
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID,lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }
}