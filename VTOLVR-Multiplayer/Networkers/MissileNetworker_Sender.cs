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
    public RigidbodyNetworker_Sender rbSender;
    private bool hasFired = false;

    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        Networker.MissileChangeAuthority += MissileChangeAuthority;
        lastMessage = new Message_MissileUpdate(networkUID);
        lastLaunchMessage = new Message_MissileLaunch(networkUID, Quaternion.identity);
        lastDetonateMessage = new Message_MissileDetonate(networkUID);
        thisMissile = GetComponent<Missile>();
        thisMissile.OnMissileDetonated += OnDetonated;
    }

    private void FixedUpdate()
    {
        if (hasFired != thisMissile.fired)
        {
            Debug.Log("Missile fired " + thisMissile.name);
            hasFired = true;

            rbSender = gameObject.AddComponent<RigidbodyNetworker_Sender>();
            rbSender.networkUID = networkUID;

            lastLaunchMessage.ownerUID = ownerUID;
            lastLaunchMessage.networkUID = networkUID;
            switch (thisMissile.guidanceMode) {
                case Missile.GuidanceModes.Heat:
                    lastLaunchMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
                    ulong uid;
                    if (AIDictionaries.reverseAllActors.TryGetValue(thisMissile.heatSeeker.likelyTargetActor, out uid))
                    {
                        lastLaunchMessage.targetActorUID = AIDictionaries.reverseAllActors[thisMissile.heatSeeker.likelyTargetActor];
                    }
                    else {
                        lastLaunchMessage.targetActorUID = 0;
                        Debug.Log("IR MISSILE: Couldn't find UID ");
                    }
                    lastLaunchMessage.targetPosition = new Vector3D(thisMissile.heatSeeker.targetPosition);
                    break;
                default:
                    break;
            }
            if (thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
            {
                lastLaunchMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
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
        if (lastChangeMessage.newOwnerUID == 0) {
            Debug.Log("The host is now incharge of this missile.");
            if (Networker.isHost)
            {
                Debug.Log("We are the host! This is our missile!");
                localAuthority = true;
            }
            else {
                Debug.Log("We are not the host. This is not our missile.");
                localAuthority = false;
            }
        }
        else {
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
        else {
            Debug.Log("We should not be incharge of this missile");
            Destroy(rbSender);
            Destroy(this);

            MissileNetworker_Receiver mReceiver = gameObject.AddComponent<MissileNetworker_Receiver>();
            mReceiver.rbReceiver = gameObject.AddComponent<RigidbodyNetworker_Receiver>();
            Debug.Log("Switched missile to others authority!");
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

        Networker.RequestNetworkUID -= RequestUID;
        Networker.MissileChangeAuthority -= MissileChangeAuthority;
        thisMissile.OnMissileDetonated -= OnDetonated;
    }
}