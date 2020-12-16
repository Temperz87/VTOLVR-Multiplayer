using Harmony;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AIManager
{
    public static Queue<Packet> AIsToSpawnQueue = new Queue<Packet>();
    private static List<ulong> spawnedAI = new List<ulong>();
    public static List<AI> AIVehicles = new List<AI>(); //This is the list of all AI, and an easy way to access AI variables
    public struct AI
    {
        public GameObject vehicle;
        public string vehicleName;
        public Actor actor;
        public ulong vehicleUID;

        public AI(GameObject vehicle, string vehicleName, Actor actor, ulong vehicleUID)
        {
            this.vehicle = vehicle;
            this.vehicleName = vehicleName;
            this.actor = actor;
            this.vehicleUID = vehicleUID;
        }
    }

    static string[] carrierNames = { "HMS Marsh", "HNLMS KetKev", "USS Temperz", "ENS Surgeon", "USS Dib", "USS Nebriv", "USS Zaelix", "HMS Cheese" };

    /// <summary>
    /// This is used by the client and only the client to spawn ai vehicles.
    /// </summary>
    public static void SpawnAIVehicle(Packet packet) // This should never run on the host
    {
        if (Networker.isHost)
        {
            Debug.LogWarning("Host shouldn't be trying to spawn an ai vehicle.");
            return;
        }
        Message_SpawnAIVehicle message = (Message_SpawnAIVehicle)((PacketSingle)packet).message;

        if (!PlayerManager.gameLoaded)
        {
            Debug.LogWarning("Our game isn't loaded, adding spawn vehicle to queue");
            AIsToSpawnQueue.Enqueue(packet);
            return;
        }
        foreach (ulong id in spawnedAI)
        {
            if (id == message.rootActorNetworkID)
            {
                Debug.Log("Got a spawnAI message for a vehicle we have already added! Name == " + message.unitName + " Returning...");
                return;
            }
        }

        spawnedAI.Add(message.rootActorNetworkID);
        //Debug.Log("Got a new aiSpawn uID.");
        if (message.unitName == "Player")
        {
            Debug.LogWarning("Player shouldn't be sent to someones client....");
            return;
        }
        Debug.Log("Trying to spawn AI " + message.aiVehicleName);

        GameObject prefab = UnitCatalogue.GetUnitPrefab(message.unitName);
        if (prefab == null)
        {
            Debug.LogError(message.unitName + " was not found.");
            return;
        }
        GameObject newAI = GameObject.Instantiate(prefab, VTMapManager.GlobalToWorldPoint(message.position), message.rotation);
        //Debug.Log("Setting vehicle name");
        newAI.name = message.aiVehicleName;
        Actor actor = newAI.GetComponent<Actor>();
        if (actor == null)
            Debug.LogError("actor is null on object " + newAI.name);

        if (message.redfor)
            actor.team = Teams.Enemy;
        else
            actor.team = Teams.Allied;

        AirportManager airport = null;

        //if(message.hasAirport)

        airport = newAI.GetComponent<AirportManager>();

        UnitSpawn unitSP = newAI.GetComponent<UnitSpawn>();
        GameObject.Destroy(unitSP);

        newAI.AddComponent<UnitSpawn>();


        unitSP = newAI.GetComponent<UnitSpawn>();

        UnitSpawner UnitSpawner = new UnitSpawner();

        actor.unitSpawn = unitSP;
        actor.unitSpawn.unitSpawner = UnitSpawner;
        unitSP.actor = actor;
        Traverse.Create(actor.unitSpawn.unitSpawner).Field("_spawnedUnit").SetValue(unitSP);
        Traverse.Create(actor.unitSpawn.unitSpawner).Field("_spawned").SetValue(true);
        Traverse.Create(actor.unitSpawn.unitSpawner).Field("_unitInstanceID").SetValue(message.unitInstanceID); // To make objectives work.
        UnitSpawner.team = actor.team;
        UnitSpawner.unitName = actor.unitSpawn.unitName;

        if (!PlayerManager.teamLeftie)
        {
            UnitSpawner.team = actor.team;
        }
        else
        {
            if (actor.team == Teams.Enemy)
            {
                actor.team = Teams.Allied;
                foreach (Actor subActor in newAI.GetComponentsInChildren<Actor>())
                {
                    subActor.team = Teams.Allied;
                    TargetManager.instance.UnregisterActor(subActor);
                    TargetManager.instance.RegisterActor(subActor);
                }
            }
            else
            if (actor.team == Teams.Allied)
            {
                actor.team = Teams.Enemy;
                foreach (Actor subActor in newAI.GetComponentsInChildren<Actor>())
                {
                    subActor.team = Teams.Enemy;
                    TargetManager.instance.UnregisterActor(subActor);
                    TargetManager.instance.RegisterActor(subActor);
                }
            }
            UnitSpawner.team = actor.team;
        }
        if (airport != null)
        {
            airport.team = actor.team;
            SetUpCarrier(newAI, message.rootActorNetworkID, actor.team);
        }

        TargetManager.instance.UnregisterActor(actor);
        TargetManager.instance.RegisterActor(actor);
        VTScenario.current.units.AddSpawner(actor.unitSpawn.unitSpawner);

        if (message.hasGroup)
        {
            VTScenario.current.groups.AddUnitToGroup(UnitSpawner, message.unitGroup);
        }
        Debug.Log(actor.name + $" has had its unitInstanceID set at value {actor.unitSpawn.unitSpawner.unitInstanceID}.");
        VTScenario.current.units.AddSpawner(actor.unitSpawn.unitSpawner);
        Debug.Log($"Spawned new vehicle at {newAI.transform.position}");

        newAI.AddComponent<FloatingOriginTransform>();

        newAI.transform.position = VTMapManager.GlobalToWorldPoint(message.position);
        newAI.transform.rotation = message.rotation;

        Debug.Log("This unit should have " + message.networkIDs.Length + " actors! ");

        int currentSubActorID = 0;
        foreach (Actor child in newAI.GetComponentsInChildren<Actor>())
        {
            Debug.Log("setting up actor: " + currentSubActorID);
            UIDNetworker_Receiver uidReciever = child.gameObject.AddComponent<UIDNetworker_Receiver>();
            uidReciever.networkUID = message.networkIDs[currentSubActorID];

            if (child.gameObject.GetComponent<Health>() != null)
            {
                HealthNetworker_Receiver healthNetworker = child.gameObject.AddComponent<HealthNetworker_Receiver>();
                healthNetworker.networkUID = message.networkIDs[currentSubActorID];
                //HealthNetworker_Sender healthNetworkerS = newAI.AddComponent<HealthNetworker_Sender>();
                //healthNetworkerS.networkUID = message.networkID;
                // Debug.Log("added health Sender to ai");
                // Debug.Log("added health reciever to ai");
            }
            else
            {
                Debug.Log(message.aiVehicleName + " has no health?");
            }

            if (child.gameObject.GetComponent<ShipMover>() != null)
            {
                ShipNetworker_Receiver shipNetworker = child.gameObject.AddComponent<ShipNetworker_Receiver>();
                shipNetworker.networkUID = message.networkIDs[currentSubActorID];
            }
            else if (child.gameObject.GetComponent<GroundUnitMover>() != null)
            {
                if (child.gameObject.GetComponent<Rigidbody>() != null)
                {
                    GroundNetworker_Receiver groundNetworker = child.gameObject.AddComponent<GroundNetworker_Receiver>();
                    groundNetworker.networkUID = message.networkIDs[currentSubActorID];
                }
            }
            else if (child.gameObject.GetComponent<Rigidbody>() != null)
            {
                Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
                RigidbodyNetworker_Receiver rbNetworker = child.gameObject.AddComponent<RigidbodyNetworker_Receiver>();
                rbNetworker.networkUID = message.networkIDs[currentSubActorID];
                if(child.gameObject.GetComponent<RefuelPlane>() != null)
                rbNetworker.smoothingTime =0.5f;
            }
            if (child.role == Actor.Roles.Air)
            {
                PlaneNetworker_Receiver planeReceiver = child.gameObject.AddComponent<PlaneNetworker_Receiver>();
                planeReceiver.networkUID = message.networkIDs[currentSubActorID];
                AIPilot aIPilot = child.gameObject.GetComponent<AIPilot>();
                aIPilot.enabled = false;
                aIPilot.kPlane.SetToKinematic();
                aIPilot.kPlane.enabled = false;
                aIPilot.commandState = AIPilot.CommandStates.Navigation;
                aIPilot.kPlane.enabled = true;
                aIPilot.kPlane.SetVelocity(Vector3.zero);
                aIPilot.kPlane.SetToDynamic();

                RotationToggle wingRotator = aIPilot.wingRotator;
                if (wingRotator != null)
                {
                    WingFoldNetworker_Receiver wingFoldReceiver = child.gameObject.AddComponent<WingFoldNetworker_Receiver>();
                    wingFoldReceiver.networkUID = message.networkIDs[currentSubActorID];
                    wingFoldReceiver.wingController = wingRotator;
                }
                if (aIPilot.isVtol)
                {
                    //Debug.Log("Adding Tilt Controller to this vehicle " + message.networkID);
                    EngineTiltNetworker_Receiver tiltReceiver = child.gameObject.AddComponent<EngineTiltNetworker_Receiver>();
                    tiltReceiver.networkUID = message.networkIDs[currentSubActorID];
                }

                if (child.gameObject.GetComponentInChildren<ExteriorLightsController>() != null)
                {
                    ExtLight_Receiver extLight = child.gameObject.AddComponent<ExtLight_Receiver>();
                    extLight.networkUID = message.networkIDs[currentSubActorID];
                }

                Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();

                foreach (Collider collider in child.gameObject.GetComponentsInChildren<Collider>())
                {
                    if (collider)
                    {
                        collider.gameObject.layer = 9;
                    }
                }

                Debug.Log("Doing weapon manager shit on " + child.gameObject.name + ".");
                WeaponManager weaponManager = child.gameObject.GetComponent<WeaponManager>();
                if (weaponManager == null)
                    Debug.LogError(child.gameObject.name + " does not seem to have a weapon maanger on it.");
                else
                {
                    PlaneEquippableManager.SetLoadout(child.gameObject, message.networkIDs[currentSubActorID], message.normalizedFuel, message.hpLoadout, message.cmLoadout);
                }
            }

            AIUnitSpawn aIUnitSpawn = child.gameObject.GetComponent<AIUnitSpawn>();
            if (aIUnitSpawn == null)
                Debug.LogWarning("AI unit spawn is null on respawned unit " + aIUnitSpawn);
            // else
            // newAI.GetComponent<AIUnitSpawn>().SetEngageEnemies(message.Aggresive);
            VehicleMover vehicleMover = child.gameObject.GetComponent<VehicleMover>();
            if (vehicleMover != null)
            {
                vehicleMover.enabled = false;
                vehicleMover.behavior = GroundUnitMover.Behaviors.Parked;
            }
            else
            {
                GroundUnitMover ground = child.gameObject.GetComponent<GroundUnitMover>();
                if (ground != null)
                {
                    ground.enabled = false;
                    ground.behavior = GroundUnitMover.Behaviors.Parked;
                }
            }

            Debug.Log("Checking for gun turrets on child " + child.name);
            if (child.gameObject.GetComponentsInChildren<Actor>().Length <= 1 && child.gameObject.GetComponent<RefuelPlane>() == null)
            {//only run this code on units without subunits
                Debug.Log("This is a child, with " + child.gameObject.GetComponentsInChildren<Actor>().Length + " actors, so it could have guns!");
                ulong turretCount = 0;
                foreach (ModuleTurret moduleTurret in child.gameObject.GetComponentsInChildren<ModuleTurret>())
                {
                    TurretNetworker_Receiver tRec = child.gameObject.AddComponent<TurretNetworker_Receiver>();
                    tRec.networkUID = message.networkIDs[currentSubActorID];
                    tRec.turretID = turretCount;
                    Debug.Log("Added turret " + turretCount + " to actor " + message.networkIDs[currentSubActorID] + " uid");
                    turretCount++;
                }
                ulong gunCount = 0;
                foreach (GunTurretAI turretAI in child.gameObject.GetComponentsInChildren<GunTurretAI>())
                {
                    turretAI.engageEnemies = false;
                    AAANetworker_Reciever aaaRec = turretAI.gameObject.AddComponent<AAANetworker_Reciever>();
                    aaaRec.networkUID = message.networkIDs[currentSubActorID];
                    aaaRec.gunID = gunCount;
                    Debug.Log("Added gun " + gunCount + " to actor " + message.networkIDs[currentSubActorID] + " uid");
                    gunCount++;
                }

                foreach (RocketLauncherAI rocketAI in child.gameObject.GetComponentsInChildren<RocketLauncherAI>())
                {
                    rocketAI.enabled = false;
                    RocketLauncherNetworker_Reciever rocketRec = rocketAI.gameObject.AddComponent<RocketLauncherNetworker_Reciever>();
                    rocketRec.networkUID = message.networkIDs[currentSubActorID];
                    Debug.Log("Added rocket arty to actor " + message.networkIDs[currentSubActorID] + " uid");
                }
            }
            else
            {
                Debug.Log("This isnt a child leaf thing, it has " + child.gameObject.GetComponentsInChildren<Actor>().Length + " actors");
            }
            IRSamLauncher iLauncher = child.gameObject.GetComponent<IRSamLauncher>();
            if (iLauncher != null)
            {
                //iLauncher.ml.RemoveAllMissiles();
                iLauncher.ml.LoadAllMissiles();
                iLauncher.SetEngageEnemies(false);
                MissileNetworker_Receiver mlr;
                //iLauncher.ml.LoadCount(message.IRSamMissiles.Length);
                Debug.Log($"Adding IR id's on IR SAM, len = {message.IRSamMissiles.Length}.");
                for (int i = 0; i < message.IRSamMissiles.Length; i++)
                {
                    mlr = iLauncher.ml.missiles[i]?.gameObject.AddComponent<MissileNetworker_Receiver>();
                    mlr.thisML = iLauncher.ml;
                    mlr.networkUID = message.IRSamMissiles[i];
                }
                Debug.Log("Added IR id's.");
            }
            Soldier soldier = child.gameObject.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.SetEngageEnemies(false);
                if (soldier.soldierType == Soldier.SoldierTypes.IRMANPAD)
                {
                    soldier.SetEngageEnemies(false);
                    IRMissileLauncher ir = soldier.irMissileLauncher;
                    if (ir != null)
                    {
                        //ir.RemoveAllMissiles();
                        ir.LoadAllMissiles();
                        MissileNetworker_Receiver mlr;
                        //ir.LoadCount(message.IRSamMissiles.Length);
                        Debug.Log($"Adding IR id's on manpads, len = {message.IRSamMissiles.Length}.");
                        for (int i = 0; i < message.IRSamMissiles.Length; i++)
                        {
                            mlr = ir.missiles[i]?.gameObject.AddComponent<MissileNetworker_Receiver>();
                            mlr.thisML = ir;
                            mlr.networkUID = message.IRSamMissiles[i];
                        }
                        Debug.Log("Added IR id's on manpads.");
                    }
                    else
                    {
                        Debug.Log($"Manpad {message.networkIDs} forgot its rocket launcher pepega.");
                    }
                }
            }

            Debug.Log("Checking for SAM launchers");
            SAMLauncher launcher = child.gameObject.GetComponent<SAMLauncher>();
            if (launcher != null)
            {
                Debug.Log("I found a sam launcher!");
                SamNetworker_Reciever samNetworker = launcher.gameObject.AddComponent<SamNetworker_Reciever>();
                samNetworker.networkUID = message.networkIDs[currentSubActorID];
                samNetworker.radarUIDS = message.radarIDs;
                //Debug.Log($"Added samNetworker to uID {message.networkID}.");
                launcher.SetEngageEnemies(false);
                launcher.fireInterval = float.MaxValue;
                launcher.lockingRadars = null;
            }
            /*IRSamLauncher ml = actor.gameObject.GetComponentInChildren<IRSamLauncher>();
            if (ml != null)
            {
                ml.SetEngageEnemies(false);
                MissileNetworker_Receiver lastRec;
                for (int i = 0; i < ml.ml.missiles.Length; i++)
                {
                    lastRec = ml.ml.missiles[i].gameObject.AddComponent<MissileNetworker_Receiver>();
                    lastRec.networkUID = message.IRSamMissiles[i];
                    lastRec.thisML = ml.ml;
                }
            }*/
            //this code for ir missiles was here twice, so i dissable the seccond copy

            Debug.Log("Checking for locking radars");
            foreach (LockingRadar radar in child.GetComponentsInChildren<LockingRadar>())
            {
                if (radar.GetComponent<Actor>() == child)
                {
                    Debug.Log($"Adding radar receiver to object {child.name} as it is the same game object as this actor.");
                    LockingRadarNetworker_Receiver lastLockingReceiver = child.gameObject.AddComponent<LockingRadarNetworker_Receiver>();
                    lastLockingReceiver.networkUID = message.networkIDs[currentSubActorID];
                    Debug.Log("Added locking radar!");
                }
                else if (radar.GetComponentInParent<Actor>() == child)
                {
                    Debug.Log($"Adding radar receiver to object {child.name} as it is a child of this actor.");
                    LockingRadarNetworker_Receiver lastLockingReceiver = child.gameObject.AddComponent<LockingRadarNetworker_Receiver>();
                    lastLockingReceiver.networkUID = message.networkIDs[currentSubActorID];
                    Debug.Log("Added locking radar!");
                }
                else
                {
                    Debug.Log("This radar is not direct child of this actor, ignoring");
                }
            }
            AIVehicles.Add(new AI(child.gameObject, message.aiVehicleName, child, message.networkIDs[currentSubActorID]));
            Debug.Log("Spawned in AI " + child.gameObject.name);

            if (!VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(message.networkIDs[currentSubActorID]))
            {
                VTOLVR_Multiplayer.AIDictionaries.allActors.Add(message.networkIDs[currentSubActorID], child);
            }
            if (!VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.ContainsKey(actor))
            {
                VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Add(child, message.networkIDs[currentSubActorID]);
            }

            currentSubActorID++;
        }
    }
    /// <summary>
    /// Tell the connected clients about all the vehicles the host has. This code should never be run on a client.
    /// </summary>
    /// <param name="steamID">Pass 0 to tell it to every client.</param>
    public static void TellClientAboutAI(CSteamID steamID)
    {
        if (!Networker.isHost)
        {
            Debug.LogWarning("The client shouldn't be trying to tell everyone about AI");
            return;
        }
        Debug.Log("Trying sending AI's to client " + steamID);
        foreach (var actor in TargetManager.instance.allActors)
        {
            if (actor == null)
                continue;
            if (actor.parentActor != null)
            {
                continue;
            }
            Debug.Log("Trying sending new stage 1");
            if (!actor.isPlayer)
                if (actor.name.Contains("Client [") == false)
                {
                    Debug.Log("Trying sending new stage 2");
                    bool Aggresion = false;
                    if (actor.gameObject.GetComponent<UIDNetworker_Sender>() != null)
                    {
                        Debug.Log("Try sending ai " + actor.name + " to client.");
                        HPInfo[] hPInfos2 = null;
                        int[] cmLoadout = null;
                        UIDNetworker_Sender uidSender = actor.gameObject.GetComponent<UIDNetworker_Sender>();
                        List<ulong> subUIDs = new List<ulong>();
                        foreach (UIDNetworker_Sender subActor in actor.gameObject.GetComponentsInChildren<UIDNetworker_Sender>())
                        {
                            subUIDs.Add(subActor.networkUID);
                            Debug.Log("Found ID sender with ID " + subActor.networkUID);
                        }
                        if (steamID != new CSteamID(0))
                            foreach (HealthNetworker_Sender subActorHealth in actor.gameObject.GetComponentsInChildren<HealthNetworker_Sender>())
                                {
                                    if (subActorHealth.health.normalizedHealth < 0.0f || subActorHealth.health.normalizedHealth == 0.0f)
                                    subActorHealth.Death();
                                }

                        if (actor.role == Actor.Roles.Air)
                        {
                            WeaponManager wm = actor.gameObject.GetComponent<WeaponManager>();
                            if (wm != null)
                                hPInfos2 = PlaneEquippableManager.generateHpInfoListFromWeaponManager(actor.weaponManager, PlaneEquippableManager.HPInfoListGenerateNetworkType.sender, uidSender.networkUID).ToArray();
                        }
                        AIUnitSpawn aIUnitSpawn = actor.gameObject.GetComponent<AIUnitSpawn>();
                        if (aIUnitSpawn == null)
                        {
                            Debug.LogWarning("AI unit spawn is null on ai " + actor.name);
                        }
                        else
                        {
                            Aggresion = aIUnitSpawn.engageEnemies;
                        }
                        bool canBreak = false;
                        PhoneticLetters letters = new PhoneticLetters();
                        foreach (var Group in VTScenario.current.groups.GetExistingGroups(actor.team))
                        {
                            foreach (var ID in Group.unitIDs)
                            {
                                if (aIUnitSpawn != null)
                                    if (ID == actor.unitSpawn.unitID)
                                    {
                                        letters = Group.groupID;
                                        canBreak = true;
                                        break;
                                    }
                            }
                            if (canBreak)
                                break;
                        }

                        Debug.LogWarning("passed group stuff");
                        List<ulong> ids = new List<ulong>();
                        ulong lastID;
                        SAMLauncher launcher = actor.gameObject.GetComponentInChildren<SAMLauncher>();
                        if (launcher != null)
                        {
                            foreach (var radar in launcher.lockingRadars)
                            {
                                if (radar.myActor == null)
                                {
                                    Debug.LogError("Locking radar on one of the SAM's is literally null.");
                                }
                                if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(radar.myActor, out lastID))
                                {
                                    ids.Add(lastID);
                                    // Debug.Log("Aded a radar ID");
                                }
                                else
                                    Debug.LogError("Couldn't get a locking radar on one of the SAM's, probably a dictionary problem.");
                            }
                        }
                        ulong[] irIDS = new ulong[0];
                        IRSAMNetworker_Sender irs = actor.gameObject.GetComponentInChildren<IRSAMNetworker_Sender>();
                        if (irs != null)
                        {
                            irIDS = irs.irIDs;
                        }
                        bool redfor = false;

                        if (actor.team == Teams.Enemy)
                            redfor = true;

                        bool hasAirport = false;
                        AirportManager airport = actor.gameObject.GetComponent<AirportManager>();
                        if (airport != null)
                            hasAirport = true;
                        if (steamID != new CSteamID(0))
                        {
                            Debug.Log("Finally sending AI " + actor.name + " to client " + steamID);
                            Debug.Log("This unit is made from " + subUIDs.Count + " actors! ");
                            if (canBreak)
                            {
                                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(steamID, new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName), redfor, hasAirport,
                                    VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                    actor.gameObject.transform.rotation, uidSender.networkUID, subUIDs.ToArray(), hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, letters, ids.ToArray(), irIDS),
                                    EP2PSend.k_EP2PSendReliableWithBuffering);
                            }
                            else
                            {
                                // Debug.Log("It seems that " + actor.name + " is not in a unit group, sending anyways.");
                                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(steamID, new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName), redfor, hasAirport,
                                    VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                    actor.gameObject.transform.rotation, uidSender.networkUID, subUIDs.ToArray(), hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, ids.ToArray(), irIDS),
                                    EP2PSend.k_EP2PSendReliableWithBuffering);
                            }
                        }
                        else
                        {
                            Debug.Log("Finally sending AI " + actor.name + " to client all clients.");
                            Debug.Log("This unit is made from " + subUIDs.Count + " actors! ");
                            if (canBreak)
                            {
                                Networker.addToReliableSendBuffer(new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName), redfor, hasAirport,
                                    VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                    actor.gameObject.transform.rotation, uidSender.networkUID, subUIDs.ToArray(), hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, letters, ids.ToArray(), irIDS));
                            }
                            else
                            {
                                // Debug.Log("It seems that " + actor.name + " is not in a unit group, sending anyways.");
                                Networker.addToReliableSendBuffer(new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName), redfor, hasAirport,
                                VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                actor.gameObject.transform.rotation, uidSender.networkUID, subUIDs.ToArray(), hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, ids.ToArray(), irIDS));
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Could not find the UIDNetworker_Sender");
                    }
                }
        }
    }

    public static void setupAIAircraft(Actor actor)
    {
        if (actor.role == Actor.Roles.Missile || actor.isPlayer)
            return;
        if (actor.name.Contains("Rearm/Refuel"))
            return;
        foreach (AI ai in AIManager.AIVehicles)
        {
            if (ai.actor == actor)
                return;
        }
        if (actor.parentActor == null)
        {
            AIManager.AIVehicles.Add(new AIManager.AI(actor.gameObject, actor.unitSpawn.unitName, actor, Networker.networkUID + 1));
            foreach (Actor child in actor.gameObject.GetComponentsInChildren<Actor>())
            {
                ulong networkUID = Networker.GenerateNetworkUID();
                Debug.Log("Adding UID senders to " + child.name + $", their uID will be {networkUID}.");
                if (!VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(networkUID))
                {
                    VTOLVR_Multiplayer.AIDictionaries.allActors.Add(networkUID, child);
                }
                if (!VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.ContainsKey(child))
                {
                    VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Add(child, networkUID);
                }
                UIDNetworker_Sender uidSender = child.gameObject.AddComponent<UIDNetworker_Sender>();
                uidSender.networkUID = networkUID;
                Debug.Log("Added UID sender!");

                Debug.Log("Checking for locking radars");
                foreach (LockingRadar radar in child.GetComponentsInChildren<LockingRadar>())
                {
                    if (radar.GetComponent<Actor>() == child)
                    {
                        Debug.Log($"Adding radar sender to object {child.name} as it is the same game object as this actor.");
                        LockingRadarNetworker_Sender lastLockingSender = child.gameObject.AddComponent<LockingRadarNetworker_Sender>();
                        lastLockingSender.networkUID = networkUID;
                        Debug.Log("Added locking radar!");
                    }
                    else if (radar.GetComponentInParent<Actor>() == child)
                    {
                        Debug.Log($"Adding radar sender to object {child.name} as it is a child of this actor.");
                        LockingRadarNetworker_Sender lastLockingSender = child.gameObject.AddComponent<LockingRadarNetworker_Sender>();
                        lastLockingSender.networkUID = networkUID;
                        Debug.Log("Added locking radar!");
                    }
                    else
                    {
                        Debug.Log("This radar is not direct child of this actor, ignoring");
                    }
                }

                Debug.Log("Checking for health");
                if (child.gameObject.GetComponent<Health>() != null)
                {
                    Debug.Log("adding health sender to ai");
                    HealthNetworker_Sender healthNetworker = child.gameObject.AddComponent<HealthNetworker_Sender>();
                    healthNetworker.networkUID = networkUID;
                    Debug.Log("added health sender to ai!");
                }
                else
                {
                    Debug.Log(child.name + " has no health?");
                }

                Debug.Log("checking for movement type");
                if (child.gameObject.GetComponent<ShipMover>() != null)
                {
                    Debug.Log("I am a ship!");
                    ShipNetworker_Sender shipNetworker = child.gameObject.AddComponent<ShipNetworker_Sender>();
                    shipNetworker.networkUID = networkUID;
                }
                else if (child.gameObject.GetComponent<GroundUnitMover>() != null)
                {
                    Debug.Log("I am a ground mover!");
                    if (child.gameObject.GetComponent<Rigidbody>() != null)
                    {
                        GroundNetworker_Sender lastGroundSender = child.gameObject.AddComponent<GroundNetworker_Sender>();
                        lastGroundSender.networkUID = networkUID;
                    }
                }
                else if (child.gameObject.GetComponent<Rigidbody>() != null)
                {
                    Debug.Log("I am physicsy!");
                    RigidbodyNetworker_Sender lastRigidSender = child.gameObject.AddComponent<RigidbodyNetworker_Sender>();
                    lastRigidSender.networkUID = networkUID;
                    //reduced tick rate for ground Units
                    if (child.role == Actor.Roles.Ground)
                    {
                        lastRigidSender.tickRate = 0.01f;
                    }
                    if (child.role == Actor.Roles.GroundArmor)
                    {
                        lastRigidSender.tickRate = 1.0f;
                    }

                 
                }

                Debug.Log("checking if aircraft");
                if (!child.isPlayer && child.role == Actor.Roles.Air)
                {
                    if (child.weaponManager != null)
                        PlaneEquippableManager.generateHpInfoListFromWeaponManager(child.weaponManager, PlaneEquippableManager.HPInfoListGenerateNetworkType.generate, uidSender.networkUID);
                    PlaneNetworker_Sender lastPlaneSender = child.gameObject.AddComponent<PlaneNetworker_Sender>();
                    lastPlaneSender.networkUID = networkUID;
                }

                Debug.Log("checking ext lights");
                if (child.gameObject.GetComponentInChildren<ExteriorLightsController>() != null)
                {
                    //ExtNPCLight_Sender extLight = actor.gameObject.AddComponent<ExtNPCLight_Sender>();
                    //extLight.networkUID = networkUID;
                }

                Debug.Log("checking for guns");
                if (child.gameObject.GetComponentsInChildren<Actor>().Count() <= 1 && child.gameObject.GetComponent<RefuelPlane>() == null)
                {//only run this code on units without subunits
                    ulong turretCount = 0;
                    foreach (ModuleTurret moduleTurret in child.gameObject.GetComponentsInChildren<ModuleTurret>())
                    {
                        TurretNetworker_Sender tSender = moduleTurret.gameObject.AddComponent<TurretNetworker_Sender>();
                        tSender.networkUID = networkUID;
                        tSender.turretID = turretCount;
                        Debug.Log("Added turret " + turretCount + " to actor " + networkUID + " uid");
                        turretCount++;
                    }
                    ulong gunCount = 0;
                    foreach (GunTurretAI moduleTurret in child.gameObject.GetComponentsInChildren<GunTurretAI>())
                    {
                        AAANetworker_Sender gSender = moduleTurret.gameObject.AddComponent<AAANetworker_Sender>();
                        gSender.networkUID = networkUID;
                        gSender.gunID = gunCount;
                        Debug.Log("Added gun " + gunCount + " to actor " + networkUID + " uid");
                        gunCount++;
                    }
                    foreach (RocketLauncherAI rocketLauncer in child.gameObject.GetComponentsInChildren<RocketLauncherAI>())
                    {
                        RocketLauncherNetworker_Sender rSender = rocketLauncer.gameObject.AddComponent<RocketLauncherNetworker_Sender>();
                        rSender.networkUID = networkUID;
                        Debug.Log("Added rocket arty to actor " + networkUID + " uid");
                    }
                }

                Debug.Log("checking for IRSams");
                IRSamLauncher ml = child.gameObject.GetComponentInChildren<IRSamLauncher>();
                if (ml != null)
                {
                    List<ulong> samIDS = new List<ulong>();
                    MissileNetworker_Sender lastSender;
                    for (int i = 0; i < ml.ml.missiles.Length; i++)
                    {
                        lastSender = ml.ml.missiles[i].gameObject.AddComponent<MissileNetworker_Sender>();
                        lastSender.networkUID = Networker.GenerateNetworkUID();
                        samIDS.Add(lastSender.networkUID);
                    }
                    child.gameObject.AddComponent<IRSAMNetworker_Sender>().irIDs = samIDS.ToArray();
                }

                Debug.Log("checking for soldier");
                Soldier soldier = child.gameObject.GetComponentInChildren<Soldier>();
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
                        child.gameObject.AddComponent<IRSAMNetworker_Sender>().irIDs = samIDS.ToArray();
                    }
                }

                Debug.Log("checking for airport");
                AirportManager airport = child.gameObject.GetComponent<AirportManager>();
                if (airport != null)
                {
                    AIManager.SetUpCarrier(child.gameObject, networkUID, child.team);
                }
                //if (!child.unitSpawn.unitSpawner.spawned)
                //{
                //    Debug.Log("Actor " + child.name + " isn't spawned yet, still sending.");
                //}
            }
        }
        else
        {
            Debug.Log(actor.name + " has a parent, not giving an uID sender.");
        }
    }

    public static void SetUpCarrier(GameObject carrier, ulong id, Teams team)
    {
        AirportManager airport = carrier.GetComponent<AirportManager>();
        if (airport != null)
        {
            airport.airportName = carrierNames[(int)id % carrierNames.Length] + " " + id;
            if (Networker.isClient)
            {
                VTMapManager.fetch.airports.Add(airport);
            }

            GameObject carrierPrefab = UnitCatalogue.GetUnitPrefab("AlliedCarrier");
            if (airport.voiceProfile == null)
            {
                if (carrierPrefab != null)
                {
                    airport.voiceProfile = carrierPrefab.GetComponent<AirportManager>().voiceProfile;
                    Debug.Log("Set ATC voice!");
                }
                else
                {
                    Debug.Log("Could not find carrier...");
                }
            }

            if (airport.vtolOnlyLanding == false)
            {//this stops inapropriate code from running on either the LHA or the Cruiser, and causing problems
                if (airport.carrierOlsTransform == null)
                {
                    GameObject olsTransform = new GameObject();
                    olsTransform.transform.parent = carrier.transform;
                    olsTransform.transform.position = airport.runways[0].transform.position + airport.runways[0].transform.forward * 30;
                    olsTransform.transform.localRotation = Quaternion.Euler(-3.5f, 180f, 0f);
                    airport.carrierOlsTransform = olsTransform.transform;
                }

                if (airport.ols == null)
                {
                    if (carrierPrefab != null)
                    {
                        GameObject olsObject = GameObject.Instantiate(carrierPrefab.GetComponent<AirportManager>().ols.gameObject);
                        olsObject.transform.parent = carrier.transform;
                        olsObject.transform.localPosition = new Vector3(-25f, 19.7f, 45f);
                        olsObject.transform.localRotation = Quaternion.Euler(-3.5f, 180, 0);

                        OpticalLandingSystem ols = olsObject.GetComponent<OpticalLandingSystem>();
                        airport.ols = ols;
                        airport.runways[0].ols = ols;

                        Debug.Log("Stole the OLS!");
                    }
                    else
                    {
                        Debug.Log("Could not find carrier...");
                    }
                }

                Actor actor = carrier.GetComponent<Actor>();
                if (actor != null)
                {
                    actor.iconType = UnitIconManager.MapIconTypes.Carrier;
                    actor.useIconRotation = true;
                    actor.iconRotationReference = airport.runways[0].transform;
                }
            }
            else
            {
                Debug.Log("This is a cruiser or an LHA, no need to set up runways or landing systems");
            }

            if (airport.carrierCables.Length == 0)
            {
                airport.carrierCables = carrier.GetComponentsInChildren<CarrierCable>();
                Debug.Log("Assigned the carrier wires!");
            }

            int catCount = 1;
            foreach (CarrierCatapult catapult in carrier.GetComponentsInChildren<CarrierCatapult>())
            {
                if (catapult.catapultDesignation == 0)
                {
                    catapult.catapultDesignation = catCount;
                    catCount++;
                }
            }

            if (airport.surfaceColliders.Length == 0)
            {
                airport.surfaceColliders = carrier.GetComponentsInChildren<Collider>();
                Debug.Log("Assigned the surface colliders!");
            }

            airport.waveOffCheckDist = 400;
            airport.waveOffMinDist = 200;
            airport.waveOffAoA.max = 12;
            airport.waveOffAoA.min = 7;

            if (carrier.GetComponentInChildren<ReArmingPoint>() == null)
            {
                Debug.Log("Carrier had no rearming points, adding my own!");
                foreach (AirportManager.ParkingSpace parkingSpace in airport.parkingSpaces)
                {
                    GameObject rearmingGameObject = new GameObject();
                    ReArmingPoint rearmingPoint = rearmingGameObject.AddComponent<ReArmingPoint>();
                    rearmingGameObject.transform.parent = parkingSpace.transform;
                    rearmingGameObject.transform.localPosition = Vector3.zero;
                    rearmingGameObject.transform.localRotation = Quaternion.identity;
                    rearmingPoint.team = team;
                    rearmingPoint.radius = 18.93f;
                    rearmingPoint.canArm = true;
                    rearmingPoint.canRefuel = true;
                }
            }
            else
            {
                Debug.Log("Carrier already had rearming points");
            }
        }
    }

    public static string GetUnitNameFromCatalog(string unitname)
    {
        foreach (var key in UnitCatalogue.catalogue.Keys)
        {
            UnitCatalogue.UnitTeam team;
            UnitCatalogue.catalogue.TryGetValue(key, out team);
            foreach (UnitCatalogue.Unit unit in team.allUnits)
            {
                if (unit.name == unitname)
                {
                    return unit.prefabName;
                }
            }
        }
        Debug.Log("Could not find " + unitname + " in unit catalog");
        return "";
    }

    public static void CleanUpOnDisconnect()
    {
        VTOLVR_Multiplayer.AIDictionaries.allActors?.Clear();
        VTOLVR_Multiplayer.AIDictionaries.reverseAllActors?.Clear();
        AIsToSpawnQueue?.Clear();
        spawnedAI?.Clear();
        AIVehicles?.Clear();
    }
}
