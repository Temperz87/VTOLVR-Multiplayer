using Harmony;
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
    public Actor ownerActor;
     private float tick;
     private float tickRate = 1.0f/20.0f;

    private void Awake()
    {
        Networker.RequestNetworkUID += RequestUID;
        lastMessage = new Message_MissileUpdate(networkUID);
        thisMissile = GetComponent<Missile>();
        ownerActor = GetComponentInParent<Actor>();
        thisMissile.OnMissileDetonated += OnDetonated;
        thisMissile.explodeRadius *= 1.8f; thisMissile.explodeDamage *= 0.7f;
    }
    /*private bool sendRateLimiter()
    {
        tick += Time.fixedDeltaTime;

        if (tick > tickRate)
        {
            tick = 0.0f;
            return true;
        }
        return false;
    }*/
    private void FixedUpdate()
    {
        if (thisMissile == null)
        {
            Debug.LogError("thisMissile null.");
            return;
        }
        tick += Time.fixedDeltaTime;
        if (tick > 1.0f / tickRate)
        {
            tick = 0.0f;
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

                        if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.ContainsKey(thisMissile.radarLock.actor))
                        {
                            lastMessage.radarLock = VTOLVR_Multiplayer.AIDictionaries.reverseAllActors[thisMissile.radarLock.actor];
                        }

                    }
                }
                else if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
                {

                    if (thisMissile.opticalTargetActor != null)
                        lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
                    else
                    {
                        lastMessage.targetPosition = Traverse.Create(thisMissile).Field("staticOpticalTargetLock").GetValue<FixedPoint>().globalPoint;

                    }


                    //lastMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
                }
                else if (thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
                {
                    //lastMessage.targetPosition = VTMapManager.WorldToGlobalPoint(thisMissile.opticalTargetActor.transform.position);
                    lastMessage.seekerRotation = thisMissile.heatSeeker.transform.rotation;
                }
                SendMessage(false);
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
        lastMessage.hasExploded = true;
        SendMessage(true);
    }

    private void SendMessage(bool isDestoryed)
    {
        //if(sendRateLimiter())
        //{
        if (Networker.isHost)
        {
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, isDestoryed ? Steamworks.EP2PSend.k_EP2PSendReliable : Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        //}
    }

    public void OnDetonated(Missile missile)
    {
        List<Actor> alist = new List<Actor>();
        Actor.GetActorsInRadius(missile.transform.position, missile.explodeRadius, Teams.Allied, TeamOptions.BothTeams, alist);
        foreach (Actor act in alist)
        {
            if (act != missile.actor)
            {
                Debug.Log("APassed damage radius checkS");
                if (AIDictionaries.reverseAllActors.ContainsKey(act))
                {
                    Message_MissileDamage dmgMessage = new Message_MissileDamage(PlayerManager.localUID);
                    dmgMessage.actorTobeDamaged = AIDictionaries.reverseAllActors[act];
                    dmgMessage.damage = missile.explodeDamage;

                    if (ownerActor != null)
                        if (AIDictionaries.reverseAllActors.ContainsKey(ownerActor))
                        {
                            dmgMessage.damageSourceActor = AIDictionaries.reverseAllActors[ownerActor];
                        }

                    Debug.Log("sending missile damage");
                    if (Networker.isHost)
                    {
                        NetworkSenderThread.Instance.SendPacketAsHostToAllClients(dmgMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                        //NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, dmgMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
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
