using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;


[HarmonyPatch(typeof(AIUnitSpawn), "SpawnUnit")]
class Patch8
{
    static void Postfix(AIUnitSpawn __instance)
    {
        if (Networker.isHost)
        {
            Actor actor = __instance.actor;
            if (actor == null)
            {
                Debug.LogError("Actor was null on AIUNITSPAWN unit");
            }
            if (actor.role == Actor.Roles.Missile || actor.isPlayer)
                return;
            if (actor.parentActor == null)
            {
                ulong networkUID = Networker.GenerateNetworkUID();
                Debug.Log("Adding UID senders to " + actor.name + $", their uID will be {networkUID}.");
                AIManager.AIVehicles.Add(new AIManager.AI(actor.gameObject, actor.unitSpawn.unitName, actor, networkUID));
                if (!VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(networkUID))
                {
                    VTOLVR_Multiplayer.AIDictionaries.allActors.Add(networkUID, actor);
                }
                if (!VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.ContainsKey(actor))
                {
                    VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Add(actor, networkUID);
                }
                UIDNetworker_Sender uidSender = actor.gameObject.AddComponent<UIDNetworker_Sender>();
                uidSender.networkUID = networkUID;
                if (actor.hasRadar)
                {
                    Debug.Log($"Adding radar sender to object {actor.name}.");
                    LockingRadarNetworker_Sender lastLockingSender = actor.gameObject.AddComponent<LockingRadarNetworker_Sender>();
                    lastLockingSender.networkUID = networkUID;
                }
                if (actor.gameObject.GetComponent<Health>() != null)
                {
                    HealthNetworker_Sender healthNetworker = actor.gameObject.AddComponent<HealthNetworker_Sender>();
                    healthNetworker.networkUID = networkUID;
                    Debug.Log("added health sender to ai");
                }
                else
                {
                    Debug.Log(actor.name + " has no health?");
                }
                if (actor.gameObject.GetComponent<ShipMover>() != null)
                {
                    ShipNetworker_Sender shipNetworker = actor.gameObject.AddComponent<ShipNetworker_Sender>();
                    shipNetworker.networkUID = networkUID;
                }
                else if (actor.gameObject.GetComponent<Rigidbody>() != null)
                {
                    RigidbodyNetworker_Sender lastRigidSender = actor.gameObject.AddComponent<RigidbodyNetworker_Sender>();
                    lastRigidSender.networkUID = networkUID;
                }
                if (!actor.isPlayer && actor.role == Actor.Roles.Air)
                {
                    PlaneNetworker_Sender lastPlaneSender = actor.gameObject.AddComponent<PlaneNetworker_Sender>();
                    lastPlaneSender.networkUID = networkUID;
                }
                if (actor.gameObject.GetComponent<AirportManager>() != null)
                {
                    actor.gameObject.GetComponent<AirportManager>().airportName = "USS TEMPERZ " + networkUID;
                }
                if (!actor.unitSpawn.unitSpawner.spawned)
                {
                    Debug.Log("Actor " + actor.name + " isn't spawned yet, still sending.");
                }
                AIManager.TellClientAboutAI(new Steamworks.CSteamID(0));
            }
            else
                Debug.Log(actor.name + " has a parent, not giving an uID sender.");
        }
    }
}



