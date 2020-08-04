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

    private float positionThreshold = 0.5f;

    private void Start()
    {
        thisMissile = GetComponent<Missile>();
        // rigidbody = GetComponent<Rigidbody>();
        Networker.MissileUpdate += MissileUpdate;
        thisMissile.OnDetonate.AddListener(new UnityEngine.Events.UnityAction(() => { Debug.Log("Missile detonated: " + thisMissile.name); }));
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
                Debug.LogError($"Missile launcher is null on missile {thisMissile.actor.name}.");
            }
            if (lastMessage.guidanceMode == Missile.GuidanceModes.Radar)
            {
                // thisMissile.debugMissile = true;
                HPEquipMissileLauncher radarLauncher = thisML.gameObject.GetComponent<HPEquipMissileLauncher>();
                if (radarLauncher != null)
                {
                    Debug.Log("Guidance mode radar, firing it as a radar missile.");
                    radarLauncher.OnStartFire();
                }
            }
            else
            {
                if (lastMessage.guidanceMode == Missile.GuidanceModes.Optical)
                {
                    foreach (var collider in thisMissile.gameObject.GetComponentsInChildren<Collider>())
                    {
                        Debug.Log("Guidance mode Optical.");
                        // collider.gameObject.layer = 9;
                    }
                }
                Debug.Log("Try fire missile clientside");
                traverse.Field("missileIdx").SetValue(idx);
                thisML.FireMissile();
            }
        }

        if (lastMessage.hasExploded)
        {
            Debug.Log("Missile exploded.");
            thisMissile.Detonate();
            return;
        }

        // gameObject.transform.velocity = lastMessage.velocity.toVector3;
        gameObject.transform.rotation = lastMessage.rotation;
        if (Vector3.Distance(gameObject.transform.position, VTMapManager.GlobalToWorldPoint(lastMessage.position)) > positionThreshold)
        {
            // Debug.LogWarning($"Missile ({gameObject.name}) is outside the threshold. Teleporting to position.");
            gameObject.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
        }
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
 */