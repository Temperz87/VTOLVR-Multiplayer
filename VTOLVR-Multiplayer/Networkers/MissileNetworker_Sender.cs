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
    private Message_MissileChangeAuthority lastChangeMessage;
    private Missile thisMissile;
    private Traverse traverse;
    public RigidbodyNetworker_Sender rbSender;
    public bool hasFired = false;
    public ulong targetUID;
    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        Networker.MissileChangeAuthority += MissileChangeAuthority;
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
                    lastLaunchMessage.targetPosition = new Vector3D(thisMissile.heatSeeker.targetPosition);
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

    public void MissileChangeAuthority(Packet packet)
    {
        lastChangeMessage = ((PacketSingle)packet).message as Message_MissileChangeAuthority;
        if (lastChangeMessage.networkUID != networkUID)
            return;

        Debug.Log("Missile changing authority!");
        bool localAuthority;
        if (lastChangeMessage.newOwnerUID == 0)
        {
            Debug.Log("The host is now incharge of this missile.");
            if (Networker.isHost)
            {
                Debug.Log("We are the host! This is our missile!");
                localAuthority = true;
            }
            else
            {
                Debug.Log("We are not the host. This is not our missile.");
                localAuthority = false;
            }
        }
        else
        {
            Debug.Log("A client is now incharge of this missile.");
            if (PlayerManager.localUID == lastChangeMessage.newOwnerUID)
            {
                Debug.Log("We are that client! This is our missile!");
                localAuthority = true;
            }
            else
            {
                Debug.Log("We are not that client. This is not our missile.");
                localAuthority = false;
            }
        }

        if (localAuthority)
        {
            Debug.Log("We are already incharge of this missile, nothing needs to change.");
        }
        else
        {
            Debug.Log("We should not be incharge of this missile");
            Destroy(rbSender);
            Destroy(this);

            MissileNetworker_Receiver mReceiver = gameObject.AddComponent<MissileNetworker_Receiver>();
            mReceiver.networkUID = networkUID;
            mReceiver.ownerUID = lastChangeMessage.newOwnerUID;
            mReceiver.hasFired = true;
            mReceiver.rbReceiver = gameObject.AddComponent<RigidbodyNetworker_Receiver>();
            mReceiver.rbReceiver.networkUID = networkUID;
            mReceiver.rbReceiver.ownerUID = lastChangeMessage.newOwnerUID;
            Debug.Log("Switched missile to others authority!");
            Debug.Log("Missile is owned by " + mReceiver.ownerUID + " and has UID " + mReceiver.rbReceiver.networkUID);
        }
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
        Networker.MissileChangeAuthority -= MissileChangeAuthority;
        thisMissile.OnMissileDetonated -= OnDetonated;
    }
}