using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Missile thisMissile;
    private Message_MissileUpdate lastMessage;
    private Rigidbody rigidbody;

    private float positionThreshold = 10;

    private void Start()
    {
        thisMissile = GetComponent<Missile>();
        rigidbody = GetComponent<Rigidbody>();
        Networker.MissileUpdate += MissileUpdate;
    }

    public void MissileUpdate(Packet packet)
    {
        lastMessage = ((PacketSingle)packet).message as Message_MissileUpdate;
        if (lastMessage.networkUID != networkUID)
            return;

        if (!thisMissile.fired)
        {
            RadarLockData lockData = new RadarLockData();
            lockData.actor = GetActorAtPosition(lastMessage.targetPosition);
            lockData.locked = true;
            lockData.lockingRadar = GetComponentInChildren<LockingRadar>();     //Unsure if these are on a child or not
            lockData.radarSymbol = GetComponentInChildren<Radar>().radarSymbol; //I'm just guessing they are
            thisMissile.SetRadarLock(lockData);
            thisMissile.Fire();
        }

        if (lastMessage.hasMissed)
        {
            thisMissile.Detonate();
            return;
        }

        rigidbody.velocity = lastMessage.velocity.toVector3;
        rigidbody.rotation = Quaternion.Euler(lastMessage.rotation.toVector3);
        if (Vector3.Distance(rigidbody.position, lastMessage.position.toVector3) > positionThreshold)
        {
            Debug.LogWarning($"Missile ({gameObject.name}) is outside the threshold. Teleporting to position.");
            rigidbody.position = lastMessage.position.toVector3;
        }
    }

    private Actor GetActorAtPosition(Vector3D globalPos)
    {
        Vector3 worldPos = VTMapManager.GlobalToWorldPoint(globalPos);
        Actor result = null;
        float num = 250000f;
        for (int i = 0; i < TargetManager.instance.allActors.Count; i++)
        {
            float sqrMagnitude = (TargetManager.instance.allActors[i].position - worldPos).sqrMagnitude;
            if (sqrMagnitude < num)
            {
                num = sqrMagnitude;
                result = TargetManager.instance.allActors[i];
            }
        }
        return result;
    }

    public void OnDestory()
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