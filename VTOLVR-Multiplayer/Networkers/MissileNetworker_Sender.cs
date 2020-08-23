using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VTOLVR_Multiplayer;

public class MissileNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_MissileUpdate lastMessage;
    private Missile thisMissile;
    private bool hasFired = false;
    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        thisMissile = GetComponent<Missile>();

        thisMissile.OnMissileDetonated += OnDetonated;
    }

    private void FixedUpdate()
    {
        if (thisMissile == null)
        {
            Debug.LogError("thisMissile null.");
        }
        if (hasFired != thisMissile.fired)
        {
            Debug.Log("Missile fired " + thisMissile.name);
            hasFired = true;

            RigidbodyNetworker_Sender rbSender = gameObject.AddComponent<RigidbodyNetworker_Sender>();
            rbSender.networkUID = networkUID;
        }
        if (thisMissile != null && thisMissile.fired)
        {
            if (lastMessage == null)
            {
                Debug.LogError("lastMessage null");
            }
            lastMessage.networkUID = networkUID;
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
                            // Debug.Log($"Missile {gameObject.name} has found its lock {AI.actor.name} with an uID of {AI.vehicleUID} while trying to lock {thisMissile.radarLock.actor.name}");
                        }
                    }
                }
            }
            else if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
            {
                lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
                //lastMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
            }
            else if (thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
            {
                //lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
                lastMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
            }
            SendMessage(false);
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

    public void OnDetonated(Missile missile)
    {

        foreach (Actor act in TargetManager.instance.allActors)
        {
            if (act != missile.actor)
            {


                Vector3 apos = act.transform.position;
                Vector3 misslepos = missile.transform.position;

                Vector3 line = apos - misslepos;
                float dist = (float)line.magnitude;
                Debug.Log("Actor Dist is " + dist);
                if (dist < missile.explodeRadius)
                {
                    Debug.Log("APassed damage radius checkS");
                    if (AIDictionaries.reverseAllActors.ContainsKey(act))
                    {
                        Message_MissileDamage dmgMessage = new Message_MissileDamage(PlayerManager.localUID);
                        dmgMessage.actorTobeDamaged = AIDictionaries.reverseAllActors[act];
                        dmgMessage.damage = missile.explodeDamage;

                        Debug.Log("sending missile damage");
                        if (Networker.isHost)
                        {
                            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(dmgMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                        }
                        else
                        {
                            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, dmgMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                        }

                    }


                }
            }

        }

    }
}
