using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    public ulong ownerUID;//0 is host owns the missile
    private Message_MissileUpdate lastMessage;
    private Message_MissileLaunch lastLaunchMessage;
    private Message_MissileDetonate lastDetonateMessage;
    private Missile thisMissile;
    private Traverse traverse;
    public RigidbodyNetworker_Sender rbSender;
    public bool hasFired = false;
    public ulong targetUID;
    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID, new Vector3D(), new Vector3D());
        lastLaunchMessage = new Message_MissileLaunch(networkUID);
        lastDetonateMessage = new Message_MissileDetonate(networkUID);
        thisMissile = GetComponent<Missile>();
        thisMissile.OnMissileDetonated += OnDetonated;

        if (thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
        {
            traverse = Traverse.Create(thisMissile.heatSeeker);
            if (targetUID != 0)
            {
                if (AIDictionaries.allActors.TryGetValue(targetUID, out Actor actor))
                {
                    Debug.Log("IR CLIENT MISSILE: Firing on " + actor);
                    thisMissile.heatSeeker.transform.rotation = lastLaunchMessage.seekerRotation;
                    traverse.Method("TrackHeat").GetValue();
                    thisMissile.heatSeeker.SetHardLock();
                }
                else
                {
                    Debug.LogWarning("IR client missile did not find its heat target.");
                }
                if (!thisMissile.hasTarget)
                {
                    Debug.LogError("This IR missile does not have a target.");
                }
            }
        }

        if (GetComponent<MissileNetworker_Receiver>() != null)
        {
            Destroy(GetComponent<MissileNetworker_Receiver>());
        }       
    }

    private void FixedUpdate()
    {
        if (hasFired != thisMissile.fired)
        {
            Debug.Log("Missile fired " + thisMissile.name);
            hasFired = true;

            rbSender = gameObject.AddComponent<RigidbodyNetworker_Sender>();
            rbSender.networkUID = networkUID;
            rbSender.ownerUID = ownerUID;

            lastLaunchMessage.ownerUID = ownerUID;
            lastLaunchMessage.networkUID = networkUID;
            lastLaunchMessage.guidanceType = thisMissile.guidanceMode;
            switch (thisMissile.guidanceMode)
            {
                case Missile.GuidanceModes.Heat:
                    lastLaunchMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
                    ulong uid;
                    if (thisMissile.heatSeeker.likelyTargetActor != null)
                    {
                        if (AIDictionaries.reverseAllActors.TryGetValue(thisMissile.heatSeeker.likelyTargetActor, out uid))
                        {
                            lastLaunchMessage.targetActorUID = uid;
                            Debug.Log("IR MISSILE: Firing on " + uid);
                        }
                        else
                        {
                            lastLaunchMessage.targetActorUID = 0;
                            Debug.Log("IR MISSILE: Couldn't find UID ");
                        }
                    }
                    else
                    {
                        Debug.Log("IR MISSILE: Target was null, could not find UID");
                    }
                    lastLaunchMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.heatSeeker.targetPosition);
                    break;
                case Missile.GuidanceModes.Radar:
                    ulong uid2;
                    if (thisMissile.radarLock == null)
                    {
                        Debug.LogError("RadarLock null");
                    }
                    else if (AIDictionaries.reverseAllActors.TryGetValue(thisMissile.radarLock.actor, out uid2))
                    {
                        lastLaunchMessage.targetActorUID = uid2;
                        Debug.Log("RADAR MISSILE: Firing on " + uid2);
                    }
                    else
                    {
                        lastLaunchMessage.targetActorUID = 0;
                        Debug.Log("RADAR MISSILE: Couldn't find UID ");
                    }
                    break;
                case Missile.GuidanceModes.Optical:
                    lastLaunchMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
                    break;
                default:
                    break;
            }
            if (Networker.isHost)
            {
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastLaunchMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            else
            {
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastLaunchMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
        }
        if (hasFired && thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
        {
            if (traverse != null)
            {
                lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.heatSeeker.targetPosition);
                lastMessage.lastTargetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.heatSeeker.targetPosition);
                if (Networker.isHost)
                {
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
                }
                else
                {
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
                }
            }
            else
            {
                Debug.Log("traverse was null lmao");
            }
        }

        if (GetComponent<MissileNetworker_Receiver>() != null)
        {
            Debug.Log("fml, there are both missile senders and recievers");
            Destroy(GetComponent<MissileNetworker_Receiver>());
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
    public void OnDetonated(Missile missile)
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

    void OnDestroy()
    {
        Networker.RequestNetworkUID -= RequestUID;
        thisMissile.OnMissileDetonated -= OnDetonated;
    }
}