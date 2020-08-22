using Harmony;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    public ulong ownerUID;//0 is host owns the missile
    private Missile thisMissile;
    public MissileLauncher thisML;
    public RigidbodyNetworker_Receiver rbReceiver;
    public int idx;
    private Message_MissileUpdate lastMessage;
    private Message_MissileLaunch lastLaunchMessage;
    private Message_MissileDetonate lastDetonateMessage;
    private Message_MissileChangeAuthority lastChangeMessage;
    private Traverse traverse;
    private Traverse traverse2;
    private RadarLockData lockData;
    // private Rigidbody rigidbody; see missileSender for why i not using rigidbody
    public bool hasFired = false;

    float originalProxFuse;

    void Awake() {
        if (GetComponent<MissileNetworker_Sender>() != null)
        {
            Destroy(GetComponent<MissileNetworker_Sender>());
        }
    }

    private void Start()
    {
        thisMissile = GetComponent<Missile>();
        originalProxFuse = thisMissile.proxyDetonateRange;
        thisMissile.proxyDetonateRange = 0;
        traverse = Traverse.Create(thisML);
        if (thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
        {
            traverse2 = Traverse.Create(thisMissile.heatSeeker);
        }
        thisMissile.OnDetonate.AddListener(new UnityEngine.Events.UnityAction(() => { Debug.Log("Missile detonated: " + thisMissile.name); }));
        if (thisMissile.guidanceMode == Missile.GuidanceModes.Bomb || thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
        {
            foreach (var collider in thisMissile.GetComponentsInChildren<Collider>())
            {
                collider.gameObject.layer = 9;
            }
        }
        
        Networker.MissileUpdate += MissileUpdate;
        Networker.MissileLaunch += MissileLaunch;
        Networker.MissileDetonate += MissileDestroyed;
        Networker.MissileChangeAuthority += MissileChangeAuthority;

        thisMissile.explodeRadius *= Multiplayer._instance.missileRadius; thisMissile.explodeDamage *= Multiplayer._instance.missileDamage;
    }

    public void MissileLaunch(Packet packet)
    {
        lastLaunchMessage = ((PacketSingle)packet).message as Message_MissileLaunch;
        if (lastLaunchMessage.networkUID != networkUID)
            return;

        Debug.Log(thisMissile.gameObject.name + " missile fired on one end but not another, firing here.");
        if (thisML == null)
        {
            Debug.LogError($"Missile launcher is null on missile {thisMissile.actor.name}, someone forgot to assign it.");
        }
        if (thisMissile.guidanceMode == Missile.GuidanceModes.Radar)
        {
            // thisMissile.debugMissile = true;
            RadarMissileLauncher radarLauncher = thisML as RadarMissileLauncher;
            if (radarLauncher != null)
            {
                Debug.Log("Guidance mode radar, firing it as a radar missile.");
                if (!radarLauncher.TryFireMissile())
                {
                    Debug.LogError("Could not fire radar missile.");
                }
                else
                {
                    rbReceiver = gameObject.AddComponent<RigidbodyNetworker_Receiver>();
                    rbReceiver.networkUID = networkUID;
                }
            }
        }
        else
        {
            if (thisMissile.guidanceMode == Missile.GuidanceModes.Heat)
            {
                Debug.Log("Guidance mode Heat.");
                thisMissile.heatSeeker.transform.rotation = lastLaunchMessage.seekerRotation;
                thisMissile.heatSeeker.SetHardLock();
            }

            if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
            {
                Debug.Log("Guidance mode Optical.");

                GameObject emptyGO = new GameObject();
                Transform newTransform = emptyGO.transform;

                newTransform.position = VTMapManager.GlobalToWorldPoint(lastLaunchMessage.targetPosition);
                thisMissile.SetOpticalTarget(newTransform);
                //thisMissile.heatSeeker.SetHardLock();
            }
            Debug.Log("Try fire missile clientside");
            traverse.Field("missileIdx").SetValue(idx);
            thisML.FireMissile();
            RigidbodyNetworker_Receiver rbReceiver = gameObject.AddComponent<RigidbodyNetworker_Receiver>();
            rbReceiver.networkUID = networkUID;
        }
        if (hasFired != thisMissile.fired)
        {
            Debug.Log("Missile fired " + thisMissile.name);
            hasFired = true;
        }
    }

    public void MissileUpdate(Packet packet)
    {
        lastMessage = ((PacketSingle)packet).message as Message_MissileUpdate;
        if (lastMessage.networkUID != networkUID)
            return;
        traverse2.Field("visibilityCheckFrame").SetValue(0);
        traverse.Field("targetPosition").SetValue(VTMapManager.GlobalToWorldPoint(lastMessage.targetPosition));
        traverse.Field("lastTargetPosition").SetValue(VTMapManager.GlobalToWorldPoint(lastMessage.lastTargetPosition));
    }

    public void MissileDestroyed(Packet packet)
    {
        lastDetonateMessage = ((PacketSingle)packet).message as Message_MissileDetonate;
        if (lastDetonateMessage.networkUID != networkUID)
            return;

        Debug.Log("Missile exploded.");
        thisMissile.Detonate();
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
            Debug.Log("We should be incharge of this missile");
            Destroy(rbReceiver);
            Destroy(this);

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            thisMissile.proxyDetonateRange = originalProxFuse;

            MissileNetworker_Sender mSender = gameObject.AddComponent<MissileNetworker_Sender>();
            mSender.networkUID = networkUID;
            mSender.ownerUID = lastChangeMessage.newOwnerUID;
            mSender.hasFired = true;
            mSender.rbSender = gameObject.AddComponent<RigidbodyNetworker_Sender>();
            mSender.rbSender.networkUID = networkUID;
            mSender.rbSender.ownerUID = lastChangeMessage.newOwnerUID;
            Debug.Log("Switched missile to our authority!");
            Debug.Log("Missile is owned by " + mSender.ownerUID + " and has UID " + mSender.rbSender.networkUID);
        }
        else
        {
            Debug.Log("We are already not incharge of this missile, nothing needs to change.");
        }
    }

    void FixedUpdate() {
        if (GetComponent<MissileNetworker_Sender>() != null)
        {
            Debug.Log("fml, there are both missile senders and recievers");
        }
    }

    public void OnDestroy()
    {
        Networker.MissileUpdate -= MissileUpdate;
        Networker.MissileLaunch -= MissileLaunch;
        Networker.MissileDetonate -= MissileDestroyed;
        Networker.MissileChangeAuthority -= MissileChangeAuthority;
    }
}

/* Possible Issue
 * 
 * A missile on a client may explode early because of the random chance it loses locking bceause of 
 * counter measures. 
 * 
 * Missile.cs Line 633
 * This bool function could return false on a clients game and true on the owners game.
 * No error should occour on either game however the missile would now be out of sync, causing bigger issues.
 * 
 * The reason I have't tried fixing this issue is because it's right in the middle of UpdateTargetData() so
 * it would require a lot of rewriting game code just to add a network check for it.
 * 
 * As of writing this note CMS are not networked so it wouldn't effect it, but later on it will.
 * . Marsh.Mello . 21/02/2020 
 * Temperz87 says you suck.
 */
