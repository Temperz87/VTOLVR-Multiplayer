using Harmony;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Missile thisMissile;
    public MissileLauncher thisML;
    public int idx;
    private Message_MissileUpdate lastMessage;
    private Traverse traverse;
    // private Rigidbody rigidbody; see missileSender for why i not using rigidbody
    private bool hasFired = false;

    private void Start()
    {
        thisMissile = GetComponent<Missile>();
        // rigidbody = GetComponent<Rigidbody>();
        Networker.MissileUpdate += MissileUpdate;
        thisMissile.OnDetonate.AddListener(new UnityEngine.Events.UnityAction(() => { Debug.Log("Missile detonated: " + thisMissile.name); }));
        if (thisMissile.guidanceMode == Missile.GuidanceModes.Bomb)
        {
            foreach (var collider in thisMissile.GetComponentsInChildren<Collider>())
            {
                collider.gameObject.layer = 9;
            }
        }

        if (thisMissile.guidanceMode == Missile.GuidanceModes.Optical)
        {
            foreach (var collider in thisMissile.GetComponentsInChildren<Collider>())
            {
                collider.gameObject.layer = 9;
            }
        }

        thisMissile.explodeRadius *= 1.8f; thisMissile.explodeDamage *= 0.75f;
    }

    public void MissileUpdate(Packet packet)
    {
        if (!thisMissile.gameObject.activeSelf)
        {
            Debug.LogError(thisMissile.gameObject.name + " isn't active in hiearchy, changing it to active.");
            thisMissile.gameObject.SetActive(true);
        }
        if (traverse == null)
        {
            traverse = Traverse.Create(thisML);
        }
        lastMessage = ((PacketSingle)packet).message as Message_MissileUpdate;
        if (lastMessage.networkUID != networkUID)
        {
            return;
        }
        if (!thisMissile.fired)
        {
            Debug.Log(thisMissile.gameObject.name + " missile fired on one end but not another, firing here.");
            if (thisML == null)
            {
                Debug.LogError($"Missile launcher is null on missile {thisMissile.actor.name}, someone forgot to assign it.");
            }
            if (lastMessage.guidanceMode == Missile.GuidanceModes.Radar)
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
                        RigidbodyNetworker_Receiver rbReceiver = gameObject.AddComponent<RigidbodyNetworker_Receiver>();
                        rbReceiver.networkUID = networkUID;
                    }
                }
            }
            else
            {
                if (lastMessage.guidanceMode == Missile.GuidanceModes.Heat)
                {
                    Debug.Log("Guidance mode Heat.");
                    thisMissile.heatSeeker.transform.rotation = lastMessage.seekerRotation;
                    thisMissile.heatSeeker.SetHardLock();
                }

                if (lastMessage.guidanceMode == Missile.GuidanceModes.Optical)
                {
                    Debug.Log("Guidance mode Optical.");

                    GameObject emptyGO = new GameObject();
                    Transform newTransform = emptyGO.transform;

                    newTransform.position=VTMapManager.GlobalToWorldPoint(lastMessage.targetPosition);
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

        //explode missle after it has done its RB physics fixed timestep
        if (lastMessage.hasExploded)
        {
            Debug.Log("Missile exploded.");
            if (thisMissile != null)
                thisMissile.Detonate();

        }
    }
    private void LateUpdate()
    {
        
    }
    public void OnDestroy()
    {
        Networker.MissileUpdate -= MissileUpdate;
    }
}

/* Possiable Issue
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
