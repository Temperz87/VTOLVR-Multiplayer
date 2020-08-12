using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Harmony;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

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

    static string[] carrierNames = {"HMS Marsh", "HNLMS KetKev", "USS Temperz", "ENS Surgeon", "USS Dib", "USS Nebriv", "USS Zaelix", "HMS Cheese"};

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
            if (id == message.networkID)
            {
                Debug.Log("Got a spawnAI message for a vehicle we have already added! Returning....");
                return;
            }
        }

        spawnedAI.Add(message.networkID);
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
        GameObject newAI = GameObject.Instantiate(prefab, VTMapManager.GlobalToWorldPoint(message.position), Quaternion.Euler(message.rotation.toVector3));
        //Debug.Log("Setting vehicle name");
        newAI.name = message.aiVehicleName;
        Actor actor = newAI.GetComponent<Actor>();
        if (actor == null)
            Debug.LogError("actor is null on object " + newAI.name);
        UnitSpawner unitSpawn = actor.unitSpawn.unitSpawner = new UnitSpawner();

        unitSpawn.team = actor.team;
        unitSpawn.unitName = actor.unitSpawn.unitName;

        if (!PlayerManager.teamLeftie)
        {
            unitSpawn.team = actor.team;
        }
        else
        {
            if (actor.team == Teams.Enemy)
            {
                foreach (Actor subActor in newAI.GetComponentsInChildren<Actor>())
                    subActor.team = Teams.Allied;
            }
            else
            if (actor.team == Teams.Allied)
            {
                foreach (Actor subActor in newAI.GetComponentsInChildren<Actor>())
                    subActor.team = Teams.Enemy;
            }
            unitSpawn.team = actor.team;

            AirportManager airport = newAI.GetComponent<AirportManager>();
            if (airport != null)
            {
                airport.team = actor.team;
                SetUpCarrier(newAI, message.networkID, actor.team);
            }
        }
        TargetManager.instance.UnregisterActor(actor);
        TargetManager.instance.RegisterActor(actor);

        Traverse.Create(actor.unitSpawn.unitSpawner).Field("_unitInstanceID").SetValue(message.unitInstanceID); // To make objectives work.
        if (message.hasGroup)
        {
            VTScenario.current.groups.AddUnitToGroup(unitSpawn, message.unitGroup);
        }
        Debug.Log(actor.name + $" has had its unitInstanceID set at value {actor.unitSpawn.unitSpawner.unitInstanceID}.");
        VTScenario.current.units.AddSpawner(actor.unitSpawn.unitSpawner);
        Debug.Log($"Spawned new vehicle at {newAI.transform.position}");

        newAI.AddComponent<FloatingOriginTransform>();

        UIDNetworker_Receiver uidReciever = newAI.AddComponent<UIDNetworker_Receiver>();
        uidReciever.networkUID = message.networkID;

        if (newAI.GetComponent<Health>() != null)
        {
            HealthNetworker_Receiver healthNetworker = newAI.AddComponent<HealthNetworker_Receiver>();
            healthNetworker.networkUID = message.networkID;
            //HealthNetworker_Sender healthNetworkerS = newAI.AddComponent<HealthNetworker_Sender>();
            //healthNetworkerS.networkUID = message.networkID;
            // Debug.Log("added health Sender to ai");
            // Debug.Log("added health reciever to ai");
        }
        else
        {
            Debug.Log(message.aiVehicleName + " has no health?");
        }
        if (newAI.GetComponent<ShipMover>() != null)
        {
            ShipNetworker_Receiver shipNetworker = newAI.AddComponent<ShipNetworker_Receiver>();
            shipNetworker.networkUID = message.networkID;
        }
        else if (newAI.GetComponent<Rigidbody>() != null)
        {
            RigidbodyNetworker_Receiver rbNetworker = newAI.AddComponent<RigidbodyNetworker_Receiver>();
            rbNetworker.networkUID = message.networkID;
        }

        if (actor.role == Actor.Roles.Air)
        {
            PlaneNetworker_Receiver planeReceiver = newAI.AddComponent<PlaneNetworker_Receiver>();
            planeReceiver.networkUID = message.networkID;
            AIPilot aIPilot = newAI.GetComponent<AIPilot>();
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
                WingFoldNetworker_Receiver wingFoldReceiver = newAI.AddComponent<WingFoldNetworker_Receiver>();
                wingFoldReceiver.networkUID = message.networkID;
                wingFoldReceiver.wingController = wingRotator;
            }
            if (aIPilot.isVtol)
            {
                //Debug.Log("Adding Tilt Controller to this vehicle " + message.networkID);
                EngineTiltNetworker_Receiver tiltReceiver = newAI.AddComponent<EngineTiltNetworker_Receiver>();
                tiltReceiver.networkUID = message.networkID;
            }

            Rigidbody rb = newAI.GetComponent<Rigidbody>();

            foreach (Collider collider in newAI.GetComponentsInChildren<Collider>())
            {
                if (collider)
                {
                    collider.gameObject.layer = 9;
                }
            }
            Debug.Log($"Changing {newAI.name}'s position and rotation\nPos:{rb.position} Rotation:{rb.rotation.eulerAngles}");
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position = message.position.toVector3;
            rb.rotation = Quaternion.Euler(message.rotation.toVector3);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Debug.Log($"Finished changing {newAI.name}\n Pos:{rb.position} Rotation:{rb.rotation.eulerAngles}");
            //Debug.Log("Doing weapon manager shit on " + newAI.name + ".");
            WeaponManager weaponManager = newAI.GetComponent<WeaponManager>();
            if (weaponManager == null)
                Debug.LogError(newAI.name + " does not seem to have a weapon maanger on it.");
            else
            {
                string[] hpLoadoutNames = new string[30];
                //Debug.Log("foreach var equip in message.hpLoadout");
                int debugInteger = 0;
                foreach (var equip in message.hpLoadout)
                {
                    Debug.Log(debugInteger);
                    hpLoadoutNames[equip.hpIdx] = equip.hpName;
                    debugInteger++;
                }
                //Debug.Log("Setting Loadout on this new vehicle spawned");
                for (int i = 0; i < hpLoadoutNames.Length; i++)
                {
                    //Debug.Log("HP " + i + " Name: " + hpLoadoutNames[i]);
                }
                //Debug.Log("Now doing loadout shit.");
                Loadout loadout = new Loadout();
                loadout.normalizedFuel = message.normalizedFuel;
                loadout.hpLoadout = hpLoadoutNames;
                loadout.cmLoadout = message.cmLoadout;
                weaponManager.EquipWeapons(loadout);
                weaponManager.RefreshWeapon();
                //Debug.Log("Refreshed this weapon manager's weapons.");
                MissileNetworker_Receiver lastReciever;
                for (int i = 0; i < 30; i++)
                {
                    int uIDidx = 0;
                    HPEquippable equip = weaponManager.GetEquip(i);
                    if (equip is HPEquipMissileLauncher)
                    {
                        //Debug.Log(equip.name + " is a missile launcher");
                        HPEquipMissileLauncher hpML = equip as HPEquipMissileLauncher;
                        //Debug.Log("This missile launcher has " + hpML.ml.missiles.Length + " missiles.");
                        for (int j = 0; j < hpML.ml.missiles.Length; j++)
                        {
                            //Debug.Log("Adding missile reciever");
                            lastReciever = hpML.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Receiver>();
                            foreach (var thingy in message.hpLoadout) // it's a loop... because fuck you!
                            {
                                //Debug.Log("Try adding missile reciever uID");
                                if (equip.hardpointIdx == thingy.hpIdx)
                                {
                                    if (uIDidx < thingy.missileUIDS.Length)
                                    {
                                        lastReciever.networkUID = thingy.missileUIDS[uIDidx];
                                        lastReciever.thisML = hpML.ml;
                                        lastReciever.idx = j;
                                        uIDidx++;
                                    }
                                }

                                else if (equip is HPEquipGunTurret)
                                {   //dear god when this gets cleaned up move to PlaneEquippableManager - Cheese whenever he wrote that comment
                                    TurretNetworker_Receiver reciever = equip.gameObject.AddComponent<TurretNetworker_Receiver>();
                                    reciever.networkUID = message.networkID;
                                    reciever.turret = equip.GetComponent<ModuleTurret>();
                                    equip.enabled = false;
                                }
                            }
                        }
                    }
                }
                FuelTank fuelTank = newAI.GetComponent<FuelTank>();
                if (fuelTank == null)
                    Debug.LogError("Failed to get fuel tank on " + newAI.name);
                fuelTank.startingFuel = loadout.normalizedFuel * fuelTank.maxFuel;
                fuelTank.SetNormFuel(loadout.normalizedFuel);
            }
        }
        else if (actor.role == Actor.Roles.Ground)
        {
            AIUnitSpawn aIUnitSpawn = newAI.GetComponent<AIUnitSpawn>();
            if (aIUnitSpawn == null)
                Debug.LogWarning("AI unit spawn is null on respawned unit " + aIUnitSpawn);
            // else
            // newAI.GetComponent<AIUnitSpawn>().SetEngageEnemies(message.Aggresive);
            VehicleMover vehicleMover = newAI.GetComponent<VehicleMover>();
            if (vehicleMover != null)
            {
                vehicleMover.enabled = false;
            }
            else
            {
                GroundUnitMover ground = newAI.GetComponent<GroundUnitMover>();
                if (ground != null)
                {
                    ground.enabled = false;
                }
            }
            ModuleTurret turret = newAI.GetComponentInChildren<ModuleTurret>();
            if (turret != null)
            {

                TurretNetworker_Receiver tRec = newAI.AddComponent<TurretNetworker_Receiver>();
                tRec.networkUID = message.networkID;
            }
            GunTurretAI gunTurret = newAI.GetComponentInChildren<GunTurretAI>();
            if (gunTurret != null)
            {
                gunTurret.SetEngageEnemies(false);
                AAANetworker_Reciever aaaRec = newAI.AddComponent<AAANetworker_Reciever>();
                Debug.Log($"Added aaaRec to uID {message.networkID}");
                aaaRec.networkUID = message.networkID;
            }
            IRSamLauncher iLauncher = newAI.GetComponent<IRSamLauncher>();
            if (iLauncher != null)
            {
                iLauncher.ml.RemoveAllMissiles();
                iLauncher.SetEngageEnemies(false);
                MissileNetworker_Receiver mlr;
                Debug.Log($"Adding IR id's on IR SAM, len = {message.IRSamMissiles.Length}.");
                for (int i = 0; i < message.IRSamMissiles.Length; i++)
                {
                    iLauncher.ml.LoadCount(1);
                    mlr = iLauncher.ml.missiles[i]?.gameObject.AddComponent<MissileNetworker_Receiver>();
                    mlr.thisML = iLauncher.ml;
                    mlr.networkUID = message.IRSamMissiles[i];
                }
                Debug.Log("Added IR id's");
            }
            Soldier soldier = newAI.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.SetEngageEnemies(false);
                if (soldier.soldierType == Soldier.SoldierTypes.IRMANPAD)
                {
                    soldier.SetEngageEnemies(false);
                    IRMissileLauncher ir = soldier.irMissileLauncher;
                    if (ir != null)
                    {
                        ir.RemoveAllMissiles();
                        MissileNetworker_Receiver mlr;
                        Debug.Log($"Adding IR id's on manpads, len = {message.IRSamMissiles.Length}.");
                        for (int i = 0; i < message.IRSamMissiles.Length; i++)
                        {
                            ir.LoadCount(1);
                            mlr = ir.missiles[i]?.gameObject.AddComponent<MissileNetworker_Receiver>();
                            mlr.thisML = ir;
                            mlr.networkUID = message.IRSamMissiles[i];
                        }
                        Debug.Log("Added IR id's on manpads/");
                    }
                    else
                    {
                        Debug.Log($"Manpad {message.networkID} forgot its rocket launcher pepega.");
                    }
                }
            }
            SAMLauncher launcher = newAI.GetComponent<SAMLauncher>();
            if (launcher != null)
            {
                SamNetworker_Reciever samNetworker = launcher.gameObject.AddComponent<SamNetworker_Reciever>();
                samNetworker.networkUID = message.networkID;
                samNetworker.radarUIDS = message.radarIDs;
                //Debug.Log($"Added samNetworker to uID {message.networkID}.");
                launcher.SetEngageEnemies(false);
                launcher.fireInterval = float.MaxValue;
                launcher.lockingRadars = null;
            }
            IRSamLauncher ml = actor.gameObject.GetComponentInChildren<IRSamLauncher>();
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
            }
        }
        if (actor.gameObject.GetComponentInChildren<LockingRadar>() != null)
        {
            // Debug.Log($"Adding radar reciever to object {actor.name}.");
            LockingRadarNetworker_Receiver lr = actor.gameObject.AddComponent<LockingRadarNetworker_Receiver>();
            lr.networkUID = message.networkID;
        }
        AIVehicles.Add(new AI(newAI, message.aiVehicleName, actor, message.networkID));
        Debug.Log("Spawned in AI " + newAI.name);

        if (!VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(message.networkID))
        {
            VTOLVR_Multiplayer.AIDictionaries.allActors.Add(message.networkID, actor);
        }
        if (!VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.ContainsKey(actor))
        {
            VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Add(actor, message.networkID);
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
            List<HPInfo> hPInfos = new List<HPInfo>();
            if (actor == null)
                continue;
            if (!actor.isPlayer)
            {
                bool Aggresion = false;
                Debug.Log("Try sending ai " + actor.name + " to client.");
                if (actor.gameObject.GetComponent<UIDNetworker_Sender>() != null)
                {
                    UIDNetworker_Sender uidSender = actor.gameObject.GetComponent<UIDNetworker_Sender>();

                    HPInfo[] hPInfos2 = new HPInfo[0];
                    int[] cmLoadout = new int[0];

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
                        foreach (var    ID in Group.unitIDs)
                        {
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
                    if (steamID != (CSteamID)0)
                    {
                        Debug.Log("Finally sending AI " + actor.name + " to client " + steamID);
                        if (canBreak)
                        {
                            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(steamID, new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName),
                                VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                new Vector3D(actor.gameObject.transform.rotation.eulerAngles), uidSender.networkUID, hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, letters, ids.ToArray(), irIDS),
                                EP2PSend.k_EP2PSendReliable);
                        }
                        else
                        {
                            // Debug.Log("It seems that " + actor.name + " is not in a unit group, sending anyways.");
                            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(steamID, new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName),
                                VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                new Vector3D(actor.gameObject.transform.rotation.eulerAngles), uidSender.networkUID, hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, ids.ToArray(), irIDS),
                                EP2PSend.k_EP2PSendReliable);
                        }
                    }
                    else
                    {
                        Debug.Log("Finally sending AI " + actor.name + " to client all clients.");
                        if (canBreak)
                        {
                            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName),
                                VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                new Vector3D(actor.gameObject.transform.rotation.eulerAngles), uidSender.networkUID, hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, letters, ids.ToArray(), irIDS),
                                EP2PSend.k_EP2PSendReliable);
                        }
                        else
                        {
                            // Debug.Log("It seems that " + actor.name + " is not in a unit group, sending anyways.");
                            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_SpawnAIVehicle(actor.name, GetUnitNameFromCatalog(actor.unitSpawn.unitName),
                                VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position),
                                new Vector3D(actor.gameObject.transform.rotation.eulerAngles), uidSender.networkUID, hPInfos2, cmLoadout, 0.65f, Aggresion, actor.unitSpawn.unitSpawner.unitInstanceID, ids.ToArray(), irIDS),
                                EP2PSend.k_EP2PSendReliable);
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
   
    public static void SetUpCarrier(GameObject carrier, ulong id, Teams team) {
        AirportManager airport = carrier.GetComponent<AirportManager>();
        if (airport != null)
        {
            airport.airportName = carrierNames[(int)id % carrierNames.Length] + " " + id;
            if (Networker.isClient) {
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
                    Debug.Log("Added a rearming point at " + rearmingGameObject.transform.position.ToString() + "!");
                    Debug.Log("There are now " + ReArmingPoint.reArmingPoints.Count + " rearm points!");
                }
            }
            else {
                Debug.Log("Carrier already had rearming points");
            }
        }
    }

    public static string GetUnitNameFromCatalog(string unitname) {
        foreach (var key in UnitCatalogue.catalogue.Keys) {
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
