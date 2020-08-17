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
            if (actor.parentActor == null)
            {
                ulong networkUID = Networker.GenerateNetworkUID();
                Debug.Log("Adding UID senders to " + actor.name + $", their uID will be {networkUID}.");
                AIManager.AIVehicles.Add(new AIManager.AI(actor.gameObject, actor.unitSpawn.unitName, actor, networkUID));
                if (!AIDictionaries.allActors.ContainsKey(networkUID))
                {
                    AIDictionaries.allActors.Add(networkUID, actor);
                }
                if (!AIDictionaries.reverseAllActors.ContainsKey(actor))
                {
                    AIDictionaries.reverseAllActors.Add(actor, networkUID);
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

                if (actor.gameObject.GetComponentInChildren<ExteriorLightsController>() != null)
                {
                    ExtNPCLight_Sender extLight = actor.gameObject.AddComponent<ExtNPCLight_Sender>();
                    extLight.networkUID = networkUID;
                }

                if (((AIUnitSpawn)actor.unitSpawn).subUnits.Count() == 0)
                {//only run this code on units without subunits
                    ulong turretCount = 0;
                    foreach (ModuleTurret moduleTurret in actor.gameObject.GetComponentsInChildren<ModuleTurret>())
                    {
                        TurretNetworker_Sender tSender = moduleTurret.gameObject.AddComponent<TurretNetworker_Sender>();
                        tSender.networkUID = networkUID;
                        tSender.turretID = turretCount;
                        Debug.Log("Added turret " + turretCount + " to actor " + networkUID + " uid");
                        turretCount++;
                    }
                    ulong gunCount = 0;
                    foreach (GunTurretAI moduleTurret in actor.gameObject.GetComponentsInChildren<GunTurretAI>())
                    {
                        AAANetworker_Sender gSender = moduleTurret.gameObject.AddComponent<AAANetworker_Sender>();
                        gSender.networkUID = networkUID;
                        gSender.gunID = gunCount;
                        Debug.Log("Added gun " + gunCount + " to actor " + networkUID + " uid");
                        gunCount++;
                    }
                }
                IRSamLauncher ml = actor.gameObject.GetComponentInChildren<IRSamLauncher>();
                if (ml != null)
                {
                    List<ulong> samIDS = new List<ulong>();
                    MissileNetworker_Sender lastSender;
                    for (int i = 0; i < ml.ml.missiles.Length; i++)
                    {
                        lastSender = ml.ml.missiles[i].gameObject.AddComponent<MissileNetworker_Sender>();
                        lastSender.networkUID = Networker.GenerateNetworkUID();
                        lastSender.ownerUID = 0;
                        samIDS.Add(lastSender.networkUID);
                    }
                    actor.gameObject.AddComponent<IRSAMNetworker_Sender>().irIDs = samIDS.ToArray();
                }
                Soldier soldier = actor.gameObject.GetComponentInChildren<Soldier>();
                if (soldier != null)
                {
                    if (soldier.soldierType == Soldier.SoldierTypes.IRMANPAD)
                    {
                        List<ulong> samIDS = new List<ulong>();
                        MissileNetworker_Sender lastSender;
                        for (int i = 0; i < soldier.irMissileLauncher.missiles.Length; i++)
                        {
                            lastSender = soldier.irMissileLauncher.missiles[i].gameObject.AddComponent<MissileNetworker_Sender>();
                            lastSender.networkUID = Networker.GenerateNetworkUID();
                            samIDS.Add(lastSender.networkUID);
                        }
                        actor.gameObject.AddComponent<IRSAMNetworker_Sender>().irIDs = samIDS.ToArray();
                    }
                }
                AirportManager airport = actor.gameObject.GetComponent<AirportManager>();
                if (airport != null)
                {
                    AIManager.SetUpCarrier(actor.gameObject, networkUID, actor.team);
                }
                if (!actor.unitSpawn.unitSpawner.spawned)
                {
                    Debug.Log("Actor " + actor.name + " isn't spawned yet, still sending.");
                }
                AIManager.TellClientAboutAI(new Steamworks.CSteamID(0));
            }
            else
            {
                Debug.Log(actor.name + " has a parent, not giving an uID sender.");
                Debug.Log("This is a subunit, disabling AI to avoid desync");
                if (actor.gameObject.GetComponentInChildren<GunTurretAI>() != null)
                {
                    Debug.Log("Gunturret AI disabled");
                    GameObject.Destroy(actor.gameObject.GetComponentInChildren<GunTurretAI>());
                }
                if (actor.gameObject.GetComponentInChildren<SAMLauncher>() != null)
                {
                    Debug.Log("SAM Launcher disabled");
                    actor.gameObject.GetComponentInChildren<SAMLauncher>().enabled = false;
                }
            }
        }
    }
}



