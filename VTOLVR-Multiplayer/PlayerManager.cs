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
using System.Text.RegularExpressions;
using UnityEngine.UI;

public static class PlayerManager
{
    public static List<Transform> spawnPoints { private set; get; }
    public static List<ReArmingPoint> reaArms;
    public static string selectedVehicle = "";
    private static float spawnSpacing = 20;
    private static int spawnsCount = 20;
    private static int spawnTicker = 1;
    public static bool buttonMade = false;
    public static bool OPFORbuttonMade = false;
    public static Text text;
    public static bool firstSpawnDone = false;
    public static bool firstKillSpawnDone = false;
    public static bool airSpawn = false;
    public static bool carrierStart = false;
    public static bool sendGPS = true;
    public static bool carrierFound = false;
    public static bool unSubscribe = true;
    public static float timeAlive = 0.0f;
    public static int kills = 0;
    /// <summary>
    /// This is the queue for people waiting to get a spawn point,
    /// incase the host hasn't loaded in, in time.
    /// </summary>
    private static Queue<Message_RequestSpawn> spawnRequestQueue = new Queue<Message_RequestSpawn>();
    private static Queue<Packet> playersToSpawnQueue = new Queue<Packet>();
    private static Queue<CSteamID> playersToSpawnIdQueue = new Queue<CSteamID>();
    public static bool gameLoaded;
    public static GameObject av42cPrefab, fa26bPrefab, f45Prefab;
    private static List<ulong> spawnedVehicles = new List<ulong>();
    public static ulong localUID;
    private static Packet storedSpawnMessage;
    public static GameObject worldData;
    public static List<Renderer> invisibleActorList = new List<Renderer>();
    public static Multiplayer multiplayerInstance = null;
    public static bool teamLeftie = false;
    public static int carrierStartTimer = 0;
    public static ReArmingPoint rearmPoint;
    public static float flyCounter = 0;
    public static Vector3 av42Offset = new Vector3(0, 0.972f, -5.126f);//the difference between the origin of the ai and player AV-42s
    public static GameObject FrequenceyButton;
    public static Hitbox lastBulletHit;
    public static Material playerCanopyMaterial;
    public class Player
    {
        public CSteamID cSteamID;
        public GameObject vehicle;
        public Actor actor;
        public VTOLVehicles vehicleType;
        public ulong vehicleUID;
        public bool leftie;
        public float ping;
        public float timeSinceLastResponse;
        public string nameTag;
        public long discordID;
        
        public Player(CSteamID cSteamID, GameObject vehicle, Actor aactor, VTOLVehicles vehicleType, ulong vehicleUID, bool leftTeam, string tagName, long idiscord)
        {
            this.cSteamID = cSteamID;
            this.vehicle = vehicle;
            this.actor = aactor;
            this.vehicleType = vehicleType;
            this.vehicleUID = vehicleUID;
            this.leftie = leftTeam;
            this.nameTag = tagName;
            timeSinceLastResponse = 0.0f;
            this.discordID = idiscord;
    }
    }
    public static List<Player> players = new List<Player>(); //This is the list of players
 
    /// <summary>
    /// This runs when the map has finished loading and hopefully 
    /// when the player first can interact with the vehicle.
    /// </summary>
    /// 

    public static IEnumerator MapLoaded()
    {
        Debug.Log("map loading started");
        if (!Networker.isHost)
        {
            ScreenFader.FadeOut(Color.black, 0.0f, fadeoutVolume: true);
        }

        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady || FlightSceneManager.instance.switchingScene || !PlayerSpawn.playerVehicleReady)
        {
            ScreenFader.FadeOut(Color.black, 0.0f, fadeoutVolume: true);
            yield return null;
        }
         
        Debug.Log("The map has loaded");
        gameLoaded = true;
        // As a client, when the map has loaded we are going to request a spawn point from the host
        SetPrefabs();
        CUSTOM_API.loadDisplayPrefab();
         
        carrierStart = FlightSceneManager.instance.playerActor.unitSpawn.unitSpawner.linkedToCarrier;
        if (carrierStart && !Networker.isHost)
        {
            Debug.LogError("mapload1");
            ScreenFader.FadeOut(Color.black, 0.0f, fadeoutVolume: true);
            TempPilotDetacher detacher = FlightSceneManager.instance.playerActor.gameObject.GetComponentInChildren<TempPilotDetacher>();
            //gears = GetComponentsInChildren<GearAnimator>();
            //shifter = GetComponentInChildren<FloatingOriginShifter>();
            Debug.LogError("mapload2");
            EjectionSeat ejection = FlightSceneManager.instance.playerActor.gameObject.GetComponentInChildren<EjectionSeat>();
            Debug.LogError("mapload3");
            UnityEngine.Object.Destroy(detacher.cameraRig);
            UnityEngine.Object.Destroy(detacher.gameObject);
            UnityEngine.Object.Destroy(ejection.gameObject);
            UnityEngine.Object.Destroy(BlackoutEffect.instance);
            UnityEngine.Object.Destroy(FlightSceneManager.instance.playerActor.gameObject.GetComponent<PlayerSpawn>());
            UnityEngine.Object.Destroy(FlightSceneManager.instance.playerActor.gameObject);
            Debug.LogError("mapload4");
            /* foreach (EngineEffects effect in effects)
             {
                 Destroy(effect);
             }*/
            //as much stuff as im destroying, some stuff is most likely getting through, future people, look into this
            Debug.LogError("mapload5");
            AudioController.instance.ClearAllOpenings();
            Debug.LogError("mapload6");
            UnitIconManager.instance.UnregisterAll();
            TargetManager.instance.detectedByAllies.Clear();
            TargetManager.instance.detectedByEnemies.Clear();

            foreach (var actor in TargetManager.instance.allActors)
            {
                if (actor != null)
                {
                    actor.discovered = false;
                    actor.drawIcon = true;
                    //actor.DiscoverActor();


                    actor.permanentDiscovery = false;

                    Traverse.Create(actor).Field("detectedByAllied").SetValue(false);
                    Traverse.Create(actor).Field("detectedByEnemy").SetValue(false);

                    if (actor.team == Teams.Allied)
                    {
                        actor.DetectActor(Teams.Allied);
                        actor.UpdateKnownPosition(actor.team);

                    }

                    //actor.DiscoverActor(); <----------------breaks and only works on every 2nd spawn
                    // UnitIconManager.instance.RegisterIcon(actor, 0.07f * actor.iconScale, actor.iconOffset);

                }
            }

            if (PlayerManager.selectedVehicle == "FA-26B")
                PlayerManager.selectedVehicle = "F/A-26B";
            PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(PlayerManager.selectedVehicle);
            string campID;
            if (PlayerManager.selectedVehicle == "AV-42C")
            {
                campID = "av42cQuickFlight";
            }
            else if (PlayerManager.selectedVehicle == "F/A-26B")
            {
                campID = "fa26bFreeFlight";
            }
            else
            {
                campID = "f45-quickFlight";
            }

            Campaign campref = VTResources.GetBuiltInCampaign(campID).ToIngameCampaign();
            PilotSaveManager.currentCampaign = campref;
            if (PilotSaveManager.currentVehicle == null)
            {
                Debug.LogError("current vehicle is null");
            }
            GameObject newPlayer = GameObject.Instantiate(PilotSaveManager.currentVehicle.vehiclePrefab);
            if (newPlayer == null)
            {
                Debug.LogError("new vehicle is null");
            }
            newPlayer.GetComponent<Actor>().designation = FlightSceneManager.instance.playerActor.designation;//reassigning designation

            FlightSceneManager.instance.playerActor = newPlayer.GetComponent<Actor>();
            FlightSceneManager.instance.playerActor.flightInfo.PauseGCalculations();
            FlightSceneManager.instance.playerActor.flightInfo.OverrideRecordedAcceleration(Vector3.zero);

            PilotSaveManager.currentScenario.totalBudget = 999999;
            PilotSaveManager.currentScenario.initialSpending = 0;
            PilotSaveManager.currentScenario.inFlightSpending = 0;
            PilotSaveManager.currentScenario.equipConfigurable = true;

            PlayerVehicleSetup pvSetup = newPlayer.GetComponent<PlayerVehicleSetup>();
            pvSetup.SetupForFlight();

            Rigidbody rb = newPlayer.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.velocity = new Vector3(0, 0, 0);
            GearAnimator gearAnim = newPlayer.GetComponent<GearAnimator>();
            if (gearAnim != null)
            {
                if (gearAnim.state != GearAnimator.GearStates.Extended)
                    gearAnim.ExtendImmediate();
            }
            ScreenFader.FadeOut(Color.black, 0.0f, fadeoutVolume: true);
        }

        ObjectiveNetworker_Reciever.loadObjectives();
        if (!Networker.isHost)
        {


            FlightSceneManager.instance.playerActor.gameObject.transform.parent = null;
            Debug.Log($"Sending spawn request to host, host id: {Networker.hostID}, client id: {SteamUser.GetSteamID().m_SteamID}");
            Debug.Log("Killing all units currently on the map.");
            List<Actor> allActors = new List<Actor>();
            foreach (var actor in TargetManager.instance.allActors)
            {
                if (!actor.isPlayer)
                {
                    if (!actor.name.Contains("Rearm/Refuel"))
                    {
                        allActors.Add(actor);
                    }
                }
            }
            foreach (var actor in allActors)
            {
                TargetManager.instance.UnregisterActor(actor);
                GameObject.Destroy(actor.gameObject);

            }
            VTScenario.current.units.units.Clear();
            VTScenario.current.units.alliedUnits.Clear();
            VTScenario.current.units.enemyUnits.Clear();
            VTScenario.current.groups.DestroyAll();

            UnitIconManager.instance.UnregisterAll();
            TargetManager.instance.detectedByAllies.Clear();
            TargetManager.instance.detectedByEnemies.Clear();
            if (teamLeftie)
                foreach (AirportManager airportManager in VTMapManager.fetch.airports)
                {
                    if (airportManager.team == Teams.Allied)
                    {
                        airportManager.team = Teams.Enemy;

                    }
                    else
                     if (airportManager.team == Teams.Enemy)
                    {
                        airportManager.team = Teams.Allied;
                    }
                }


            var rearmPoints = GameObject.FindObjectsOfType<ReArmingPoint>();
            //back up option below

            if (teamLeftie)
                foreach (ReArmingPoint rep in rearmPoints)
                {
                    if (rep.team == Teams.Allied)
                    {
                        rep.team = Teams.Enemy;
                        rep.canArm = true;
                        rep.canRefuel = true;

                    }
                    else
                    if (rep.team == Teams.Enemy)
                    {
                        rep.team = Teams.Allied;

                        rep.canArm = true;
                        rep.canRefuel = true;
                    }
                }

            SpawnPlayersInPlayerSpawnQueue();
            Message_RequestSpawn msg = new Message_RequestSpawn(teamLeftie, SteamUser.GetSteamID().m_SteamID);
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, msg, EP2PSend.k_EP2PSendReliable);

            /*foreach (var actor in TargetManager.instance.allActors)
            {
                VTScenario.current.units.AddSpawner(actor.unitSpawn.unitSpawner);
            }*/


        }
        else
        {
            Debug.Log("Starting map loaded host routines");
            Networker.hostLoaded = true;
            Networker.hostReady = true;
            foreach (var actor in TargetManager.instance.allActors)
            {
                AIManager.setupAIAircraft(actor);
            }
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_HostLoaded(true), EP2PSend.k_EP2PSendReliable);
            GameObject localVehicle = FlightSceneManager.instance.playerActor.gameObject;
            if (localVehicle != null)
            {
                GenerateSpawns(localVehicle.transform);
                localUID = Networker.GenerateNetworkUID();
                UIDNetworker_Sender hostSender = localVehicle.AddComponent<UIDNetworker_Sender>();
                hostSender.networkUID = localUID;
                Debug.Log($"The host's uID is {localUID}");


                Transform hostTrans = localVehicle.transform;
                ///uncomment to randomise host spawn//
                ///

                localVehicle.transform.position = hostTrans.position;

                SpawnLocalVehicleAndInformOtherClients(localVehicle, hostTrans.transform.position, hostTrans.transform.rotation, localUID, true, 0);
                ScreenFader.FadeIn(0.25f);
            }
            else
                Debug.Log("Local vehicle for host was null");
            if (spawnRequestQueue.Count != 0)
                SpawnRequestQueue();
            Networker.alreadyInGame = true;
        }

        while (AIManager.AIsToSpawnQueue.Count > 0)
        {
            AIManager.SpawnAIVehicle(AIManager.AIsToSpawnQueue.Dequeue());
        }
        SpawnPlayersInPlayerSpawnQueue();


        if (!Networker.isHost)
        {
            // If the player is not the host, they only need a receiver?
            Debug.Log($"Player not the host, adding world data receiver");
            worldData = new GameObject();
            worldData.AddComponent<WorldDataNetworker_Receiver>();
        }
        else
        {
            // If the player is the host, setup the sender so they can send world data
            Debug.Log($"Player is the host, setting up the world data sender");
            worldData = new GameObject();
            worldData.AddComponent<WorldDataNetworker_Sender>();
        }


       
    }

    public static void SpawnPlayersInPlayerSpawnQueue()
    {
        if (gameLoaded)
            while (playersToSpawnQueue.Count > 0)
            {
                SpawnPlayerVehicle(playersToSpawnQueue.Dequeue(), playersToSpawnIdQueue.Dequeue());
            }
    }

    /// <summary>
    /// This is a way to invoke SpawnRequstQueue() if the queue is loaded
    /// </summary>
    public static void SpawnRequestQueuePublic()
    {
        if (spawnRequestQueue.Count != 0)
        {
            SpawnRequestQueue();
        }
    }
    /// <summary>
    /// This gives all the people waiting their spawn points
    /// </summary>
    private static void SpawnRequestQueue() //Run by Host Only
    {
        Debug.Log($"Giving {spawnRequestQueue.Count} people their spawns");
        Transform lastSpawn;
        while (spawnRequestQueue.Count > 0)
        {
            Message_RequestSpawn lastMessage = spawnRequestQueue.Dequeue();
            lastSpawn = FindFreeSpawn(lastMessage.teaml);
            Debug.Log("The players spawn will be " + lastSpawn);


            if (Networker.isHost)
            {
                Debug.Log("Telling connected client about AI units");
                AIManager.TellClientAboutAI(new CSteamID(lastMessage.senderSteamID));
                ObjectiveNetworker_Reciever.sendObjectiveHistory(new CSteamID(lastMessage.senderSteamID));
            }
            GameObject localVehicle = FlightSceneManager.instance.playerActor.gameObject;
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(
                new CSteamID(lastMessage.senderSteamID),
                new Message_RequestSpawn_Result(new Vector3D(localVehicle.transform.position), localVehicle.transform.rotation, Networker.GenerateNetworkUID(), players.Count),
                EP2PSend.k_EP2PSendReliable);
        }
    }
    /// <summary>
    /// This is when a client has requested a spawn point from the host,
    /// the host gets a spawn point and sends back the position. This does
    /// not actually spawn the vehicle yet, just makes the request to the
    /// host. 
    /// </summary>
    /// <param name="packet">The Message</param>
    /// <param name="sender">The client who sent it</param>
    public static bool GetPlayerTeam(CSteamID sender)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].cSteamID.m_SteamID == sender.m_SteamID)
            {
                return players[i].leftie;
            }
        }
        return false;
    }

    public static void RequestSpawn(Packet packet, CSteamID sender) //Run by Host Only
    {
        Message_RequestSpawn lastMessage = (Message_RequestSpawn)((PacketSingle)packet).message;
        Debug.Log("A player has requested for a spawn point");
        if (!Networker.hostLoaded)
        {
            Debug.Log("The host isn't ready yet, adding to queue");
            spawnRequestQueue.Enqueue(lastMessage);
            return;
        }

        //Transform spawn = FindFreeSpawn(lastMessage.teaml);



        if (Networker.isHost)
        {
            Debug.Log("Telling connected client about AI units");
            AIManager.TellClientAboutAI(new CSteamID(lastMessage.senderSteamID));
            ObjectiveNetworker_Reciever.sendObjectiveHistory(new CSteamID(lastMessage.senderSteamID));
        }
        GameObject localVehicle = FlightSceneManager.instance.playerActor.gameObject;
        // Debug.Log("The players spawn will be " + spawn);
        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(sender, new Message_RequestSpawn_Result(new Vector3D(localVehicle.transform.position), localVehicle.transform.rotation, Networker.GenerateNetworkUID(), players.Count), EP2PSend.k_EP2PSendReliable);
    }
    /// <summary>
    /// When the client receives a P2P message of their spawn point, 
    /// this will move them to that location before sending their vehicle 
    /// to the host. This will call the function that spawns the local
    /// vehicle. 
    /// </summary>
    /// <param name="packet">The message sent over the network</param>
    public static void RequestSpawn_Result(Packet packet) //Run by Clients Only
    {
        Debug.Log("The host has sent back our spawn point");
        Message_RequestSpawn_Result result = (Message_RequestSpawn_Result)((PacketSingle)packet).message;
        Debug.Log($"We need to move to {result.position} : {result.rotation}");

        GameObject localVehicle = FlightSceneManager.instance.playerActor.gameObject;
        if (localVehicle == null)
        {
            Debug.LogError("The local vehicle was null");
            return;
        }
        localVehicle.transform.position = result.position.toVector3;
        localVehicle.transform.rotation = result.rotation;

        PlayerVehicle currentVehiclet = PilotSaveManager.currentVehicle;
        localVehicle.transform.TransformPoint(currentVehiclet.playerSpawnOffset);

        if (carrierStart)
            if (!carrierFound)
            {
                storedSpawnMessage = packet;
                return;
            }
        SpawnLocalVehicleAndInformOtherClients(localVehicle, localVehicle.transform.position, localVehicle.transform.rotation, result.vehicleUID, true, result.playerCount);
        localUID = result.vehicleUID;

        Time.timeScale = 1.0f;
    }
    /// <summary>
    /// Spawns a local vehicle, and sends the message to other clients to 
    /// spawn their representation of this vehicle
    /// </summary>
    /// <param name="localVehicle">The local clients gameobject</param>
    public static void Update()
    {
        CUSTOM_API.Update();
        if (!Networker.isHost)
            if (gameLoaded)
                if (!firstSpawnDone)
                {
                    if (carrierStart)
                    {
                        foreach (var actor in TargetManager.instance.allActors)
                        {
                            if (actor.role == Actor.Roles.Ship)
                            {

                                carrierFound = true;
                            }
                        }


                    }
                    ReArmingPoint[] rearmPoints = GameObject.FindObjectsOfType<ReArmingPoint>();
                    ReArmingPoint rearmPoint = null;

                    float lastRadius = 0;
                    if (PlayerManager.carrierStart)
                    {
                        foreach (ReArmingPoint rep in rearmPoints)
                        {
                            if (rep.team == Teams.Allied)
                            {
                                if (rep.radius < 19.0f)
                                {
                                    rearmPoint = rep;
                                }
                            }
                        }
                    }
                    if (carrierFound)
                    {
                        carrierStartTimer += 1;
                    }
                    if (rearmPoint != null && carrierFound && carrierStart && carrierStartTimer > 100)
                    {
                        if (storedSpawnMessage != null)
                        {
                            RequestSpawn_Result(storedSpawnMessage);
                        }

                    }




                }



        if (gameLoaded)
        {

            if(FrequenceyButton != null)
            {
                Vector3 up = FrequenceyButton.transform.up;
               up = up.normalized;

               float dot =  Vector3.Dot(up, new Vector3(0.0f, 1.0f, 0.0f));
                if(dot>0.2)
                {
                    FrequenceyButton.SetActive(false);
                }
                else
                {


                    FrequenceyButton.SetActive(true);

                }
            }

            foreach (Renderer rend in invisibleActorList)
            {

                
                    rend.enabled = true;
                

            }
            invisibleActorList.Clear();

            Vector3D plane = VTMapManager.WorldToGlobalPoint(FlightSceneManager.instance.playerActor.gameObject.transform.position);

            foreach (Player play in players)
            {
                if (play.vehicle != null)
                {

                    Vector3D plane2 = VTMapManager.WorldToGlobalPoint(play.vehicle.transform.position);
                    Vector3D dist = plane - plane2;

                    double dists = dist.magnitude;
                    if (dists < 5.0f && play.vehicleUID != localUID)
                    {
                        foreach (var comp in play.vehicle.GetComponentsInChildren<Renderer>())
                        {
                            if(comp.enabled == true)
                            {
                                comp.enabled = false;
                                invisibleActorList.Add(comp);
                            }
                        }
                       
                    }
                }
            } 




        }

        if (gameLoaded)
        {
            if (EndMission.instance.completeObject.active == false)
                EndMission.instance.HideEndMission();

            Actor player = FlightSceneManager.instance.playerActor;
            if (player)
            {

                // if( (bool)player.flightInfo && !player.flightInfo.isLanded)
                {
                    flyCounter += Time.fixedDeltaTime;

                    if (flyCounter > 10.0f && flyCounter < 13.0f)
                    {
                        //FlightLogger.Log("Plane Unparented");
                        //player.gameObject.GetComponent<Rigidbody>().transform.SetParent(null);
                        player.health.invincible = false;
                    }
                }
            }

            PlayerManager.SpawnPlayersInPlayerSpawnQueue();//addmitedly, this probably isnt the best place to put this, feel free to move it somewhere els
        }

    }

    public static void hackSaveUnlockAllWeapons()
    {
        string weaponList = "";
        VTOLVehicles currentVehicle = getPlayerVehicleType();
        //rbSender.SetSpawn(pos, rot);
        if (currentVehicle == VTOLVehicles.AV42C)
        {
            weaponList = "gau-8;m230;h70-x7;h70-4x4;h70-x19;mk82x1;mk82x2;mk82x3;mk82HDx1;mk82HDx2;mk82HDx3;agm89x1;gbu38x1;gbu38x2;gbu38x3;gbu39x3;gbu39x4u;cbu97x1;hellfirex4;maverickx1;maverickx3;cagm-6;sidewinderx1;sidewinderx2;sidewinderx3;iris-t-x1;iris-t-x2;iris-t-x3;sidearmx1;sidearmx2;sidearmx3;marmx1";
        }

        if (currentVehicle == VTOLVehicles.FA26B)
        {
            weaponList = "fa26_droptank;fa26_droptankXL;fa26-cft;fa26_gun;af_amraam;af_amraamRail;af_aim9;fa26_aim9x2;fa26_aim9x3;fa26_iris-t-x1;fa26_iris-t-x2;fa26_iris-t-x3;af_mk82;fa26_mk82x2;fa26_mk82x3;fa26_mk82HDx1;fa26_mk82HDx2;fa26_mk82HDx3;fa26_mk83x1;fa26_cbu97x1;h70-x7ld;h70-x7ld-under;h70-x14ld;h70-x14ld-under;fa26_cagm-6;fa26_gbu38x1;fa26_gbu38x2;fa26_gbu38x3;fa26_gbu39x4uFront;fa26_gbu39x4uRear;fa26_maverickx1;fa26_maverickx3;fa26_agm89x1;fa26_tgp;fa26_harmx1;fa26_harmx1dpMount;fa26_sidearmx1;fa26_sidearmx2;fa26_sidearmx3;af_amraamRailx2;fa26_gbu12x1;fa26_gbu12x2;fa26_gbu12x3;fa26_agm161";
        }
        if (currentVehicle == VTOLVehicles.F45A)
        {
            weaponList = "f45_gun;f45_sidewinderx2;f45_aim9x1;f45_amraamInternal;f45_amraamRail;f45_mk82x1;f45_mk82Internal;f45_mk82x4Internal;f45_gbu12x2Internal;f45_gbu12x1;f45-gbu39;f45_agm161;f45_agm161Internal;f45_droptank;f45_gbu38x1;f45_gbu38x2Internal;f45_gbu38x4Internal;f45_mk83x1;f45_mk83x1Internal;f45-agm145I;f45-agm145ISide;f45-agm145x3;f45-gbu53";
        }

        CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
        if (campaignSave == null)
        {
            Debug.LogError("Campaign save is null");
            return;
        }
        char[] delimiterChars = { ';' };
        string[] wepList = weaponList.Split(delimiterChars);
        campaignSave.availableWeapons.Clear();
        foreach (var w in wepList)
            campaignSave.availableWeapons.Add(w);
    }

    public static void StartConfig(LoadoutConfigurator lc)
    {
        // FloatingOriginShifter shift = FlightSceneManager.instance.playerActor.gameObject.GetComponentInChildren<FloatingOriginShifter>();
        //  shift.enabled = true;
        ModuleEngine[] engines = FlightSceneManager.instance.playerActor.gameObject.GetComponentsInChildren<ModuleEngine>();
        foreach (ModuleEngine eng in engines)
        {
            eng.FullyRepairEngine();
        }
        ScreenFader.FadeIn(0);
    }
    public static void StartRearm(ReArmingPoint rp)
    {
        if (PlayerManager.selectedVehicle == "FA-26B")
            PlayerManager.selectedVehicle = "F/A-26B";
        PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(PlayerManager.selectedVehicle);
        string campID;
        if (PlayerManager.selectedVehicle == "AV-42C")
        {
            campID = "av42cQuickFlight";
        }
        else if (PlayerManager.selectedVehicle == "F/A-26B")
        {
            campID = "fa26bFreeFlight";
        }
        else
        {
            campID = "f45-quickFlight";
        }

        Campaign campref = VTResources.GetBuiltInCampaign(campID).ToIngameCampaign();
        PilotSaveManager.currentCampaign = campref;
        Rigidbody rb = FlightSceneManager.instance.playerActor.gameObject.GetComponent<Rigidbody>();
        PlayerManager.rearmPoint = rp;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        unSubscribe = true;
        FlightSceneManager.instance.playerActor.flightInfo.PauseGCalculations();
        FlightSceneManager.instance.playerActor.flightInfo.OverrideRecordedAcceleration(Vector3.zero);
        //rb.detectCollisions = false;
        rearmPoint.OnEndRearm += finishRearm;
        Actor act = FlightSceneManager.instance.playerActor.gameObject.GetComponent<Actor>();


        act.health.invincible = true;
        flyCounter = 0;
        PlayerVehicleSetup pvSetup = act.gameObject.GetComponent<PlayerVehicleSetup>();
        pvSetup.OnBeginUsingConfigurator += StartConfig;

        GearAnimator gearAnim = FlightSceneManager.instance.playerActor.gameObject.GetComponentInChildren<GearAnimator>();
        if (gearAnim != null)
        {

            gearAnim.ExtendImmediate();
        }
        //hackSaveUnlockAllWeapons();

        // hackSaveUnlockAllWeapons();


        rearmPoint.BeginReArm();
    }
    public static void finishRearm()
    {
        Rigidbody rb = FlightSceneManager.instance.playerActor.gameObject.GetComponent<Rigidbody>();
        if (!carrierStart)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;

            VTOLQuickStart qstart = FlightSceneManager.instance.playerActor.GetComponentInChildren<VTOLQuickStart>();
            if (qstart != null)
                qstart.QuickStart();
        }
        else
        {
            rb.velocity = Vector3.zero;
            //FlightSceneManager.instance.playerActor.gameObject.transform.position = rearmPoint.transform.position + new Vector3(0.0f, 1.6f, 0.0f);
            rb.transform.position = FlightSceneManager.instance.playerActor.gameObject.transform.position;
            List<Actor> alist = new List<Actor>();
            Actor.GetActorsInRadius(FlightSceneManager.instance.playerActor.transform.position, 100.0f, Teams.Allied, TeamOptions.BothTeams, alist);
            foreach (Actor actor in alist)
            {
                MovingPlatform plat = actor.gameObject.GetComponentInChildren<MovingPlatform>();
                if (plat != null)
                {
                    Vector3 localPos = plat.transform.InverseTransformPoint(FlightSceneManager.instance.playerActor.gameObject.transform.position);
                    Vector3 localFwd = plat.transform.InverseTransformDirection(FlightSceneManager.instance.playerActor.gameObject.transform.forward);
                    Vector3 localUp = plat.transform.InverseTransformDirection(FlightSceneManager.instance.playerActor.gameObject.transform.up);

                    FlightSceneManager.instance.playerActor.gameObject.transform.position = plat.transform.TransformPoint(localPos);
                    Vector3 forward = plat.transform.TransformDirection(localFwd);
                    Vector3 upwards = plat.transform.TransformDirection(localUp);
                    FlightSceneManager.instance.playerActor.gameObject.transform.rotation = Quaternion.LookRotation(forward, upwards);
                    Debug.Log("attaching to carrier");
                    rb.velocity = plat.rb.GetRelativePointVelocity(rb.position);
                    rb.isKinematic = false;
                    FlightLogger.Log("Plane Parented");
                    //rb.transform.SetParent(plat.transform);
                }
            }

            Debug.Log("origin stuff to carrier");



        }

        Physics.SyncTransforms();
        //FloatingOrigin.instance.ShiftOrigin(rb.position,true);
        //rb.detectCollisions = true;
        Actor act = FlightSceneManager.instance.playerActor.gameObject.GetComponent<Actor>();
        act.health.invincible = true;
        if (unSubscribe)
        {
            rearmPoint.OnEndRearm -= finishRearm;
        }
        PlayerVehicleSetup pvSetup = act.gameObject.GetComponent<PlayerVehicleSetup>();
        pvSetup.OnBeginUsingConfigurator -= StartConfig;
        unSubscribe = false;

      PilotSaveManager.currentCampaign = Networker._instance.pilotSaveManagerControllerCampaign;
      PilotSaveManager.currentScenario = Networker._instance.pilotSaveManagerControllerCampaignScenario;

    }
    public static void SpawnLocalVehicleAndInformOtherClients(GameObject localVehicle, Vector3 pos, Quaternion rot, ulong UID, bool sendNewSpawnPacket = false, int playercount = 0) //Both
    {
        Debug.Log("Sending our location to spawn our vehicle");
        VTOLVehicles currentVehicle = getPlayerVehicleType();
        Actor actor = localVehicle.GetComponent<Actor>();
        Player localPlayer = new Player(SteamUser.GetSteamID(), localVehicle, actor, currentVehicle, UID, PlayerManager.teamLeftie, SteamFriends.GetPersonaName(),DiscordRadioManager.userID);
        AddToPlayerList(localPlayer);
        Rigidbody rb = localVehicle.GetComponent<Rigidbody>();
        
        ReArmingPoint[] rearmPoints = GameObject.FindObjectsOfType<ReArmingPoint>();
        rearmPoint = rearmPoints[UnityEngine.Random.Range(0, rearmPoints.Length - 1)];
        int rand = UnityEngine.Random.Range(0, rearmPoints.Length - 1);
        int counter = 0;

        float lastRadius = 0.0f;

        
        //rb.detectCollisions = false;
        if (PlayerManager.carrierStart)
        {
            foreach (ReArmingPoint rep in rearmPoints)
            {

                if (rep.team == Teams.Allied)
                {
                    if (rep.radius > 18.0f && rep.radius < 19.0f)
                    {
                        rearmPoint = rep;
                    }
                }
            }
        }
        else
            foreach (ReArmingPoint rep in rearmPoints)
            {
                if (rep.team == Teams.Allied && rep.CheckIsClear(actor))
                {

                    if (rep.radius > lastRadius)
                    {
                        rearmPoint = rep;
                        lastRadius = rep.radius;
                    }
                }
            }

        if (Networker.isHost && firstSpawnDone == false)
        {

            //StartRearm(rearmPoint);


        }
        else
        {
            if (teamLeftie)
            {

                StartRearm(rearmPoint);
            }
            else
            {
                if (firstSpawnDone == false)
                {
                    PlayerSpawn ps = GameObject.FindObjectOfType<PlayerSpawn>();
                    if (ps.initialSpeed < 5.0f || carrierStart)
                    {
                        StartRearm(rearmPoint);
                    }

                }
                else
                {
                    StartRearm(rearmPoint);
                }
            }

        }



        //prevent fall through ground
        if ((bool)VTMapGenerator.fetch)
        {
            VTMapGenerator.fetch.BakeColliderAtPosition(localVehicle.transform.position);
        }
        //rb.detectCollisions = true;
        SetupLocalAircraft(localVehicle, pos, rot, UID, sendNewSpawnPacket);


        firstSpawnDone = true;
    }

    public static void MissileDamage(Packet packet)
    {
        Message_MissileDamage lastMissileDamageMessage = ((PacketSingle)packet).message as Message_MissileDamage;

        //ignore damage message from same player
        //if (lastMissileDamageMessage.networkUID == PlayerManager.localUID)
        //return;

        ulong actorTodamage = lastMissileDamageMessage.actorTobeDamaged;
        ulong damageSource = lastMissileDamageMessage.damageSourceActor;
        Debug.Log("applying missile damage");
        if (VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(actorTodamage))
        {
            Actor source = null;
            if (VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(damageSource))
                source = VTOLVR_Multiplayer.AIDictionaries.allActors[damageSource];
            Actor act = VTOLVR_Multiplayer.AIDictionaries.allActors[actorTodamage];
            if (act != null)
            {
                bool storage = act.health.invincible;

                if (act.gameObject.name.Contains("lient"))
                {
                    if (actorTodamage == localUID)
                    {
                        act.health.invincible = false;
                    }
                }
                
                act.health.Damage(lastMissileDamageMessage.damage, act.position, Health.DamageTypes.Impact, source, "Missile Impact");
                act.health.invincible = storage;
            }
              

        }
    }

    public static VTOLVehicles getPlayerVehicleType()
    {
        if (PlayerManager.selectedVehicle == "AV-42C")
        {
            return VTOLVehicles.AV42C;
        }
        if (PlayerManager.selectedVehicle  == "FA-26B" || PlayerManager.selectedVehicle == "F/A-26B")
        {
            return VTOLVehicles.FA26B;
        }
        
         
          return VTOLVehicles.F45A;
        
    }
    public static void SetupLocalAircraft(GameObject localVehicle, Vector3 pos, Quaternion rot, ulong UID, bool sendNewSpawnPacket)
    {
        VTOLVehicles currentVehicle = getPlayerVehicleType();
        Actor actor = localVehicle.GetComponent<Actor>();

        if (VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(UID))
            VTOLVR_Multiplayer.AIDictionaries.allActors[UID] = actor;
        else
            VTOLVR_Multiplayer.AIDictionaries.allActors.Add(UID, actor);
        if (VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(UID))
            VTOLVR_Multiplayer.AIDictionaries.reverseAllActors[actor] = UID;
        else
            VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Add(actor, UID);

        RigidbodyNetworker_Sender rbSender = localVehicle.AddComponent<RigidbodyNetworker_Sender>();
        rbSender.networkUID = UID;
        rbSender.tickRate = 20;
        //rbSender.SetSpawn(pos, rot);
        if (currentVehicle == VTOLVehicles.AV42C)
        {
            rbSender.originOffset = av42Offset;
        }

        Debug.Log("Adding Plane Sender");
        PlaneNetworker_Sender planeSender = localVehicle.AddComponent<PlaneNetworker_Sender>();
        planeSender.networkUID = UID;

        if (currentVehicle == VTOLVehicles.AV42C || currentVehicle == VTOLVehicles.F45A)
        {
            Debug.Log("Added Tilt Updater to our vehicle");
            EngineTiltNetworker_Sender tiltSender = localVehicle.AddComponent<EngineTiltNetworker_Sender>();
            tiltSender.networkUID = UID;
        }

        if (actor != null)
        {
            if (actor.unitSpawn != null)
            {
                if (actor.unitSpawn.unitSpawner == null)
                {
                    Debug.Log("unit spawner was null, adding one");
                    actor.unitSpawn.unitSpawner = actor.gameObject.AddComponent<UnitSpawner>();
                }
            }
        }

        if (localVehicle.GetComponent<Health>() != null)
        {
            HealthNetworker_Sender healthNetworker = localVehicle.AddComponent<HealthNetworker_Sender>();
            PlayerNetworker_Sender playerNetworker = localVehicle.AddComponent<PlayerNetworker_Sender>();



            healthNetworker.networkUID = UID;
            playerNetworker.networkUID = UID;
            Debug.Log("added health sender to local player");
        }
        else
        {
            Debug.Log("local player has no health?");
        }

        if (localVehicle.GetComponentInChildren<WingFoldController>() != null)
        {
            WingFoldNetworker_Sender wingFold = localVehicle.AddComponent<WingFoldNetworker_Sender>();
            wingFold.wingController = localVehicle.GetComponentInChildren<WingFoldController>().toggler;
            wingFold.networkUID = UID;
        }

        if (localVehicle.GetComponentInChildren<StrobeLightController>() != null)
        {
            ExtLight_Sender extLight = localVehicle.AddComponent<ExtLight_Sender>();
            extLight.networkUID = UID;
        }

        if (localVehicle.GetComponentInChildren<LockingRadar>() != null)
        {
            Debug.Log($"Adding LockingRadarSender to player {localVehicle.name}");
            LockingRadarNetworker_Sender radarSender = localVehicle.AddComponent<LockingRadarNetworker_Sender>();
            radarSender.networkUID = UID;
        }
        if (currentVehicle == VTOLVehicles.AV42C)
            AvatarManager.SetupAircraftRoundels(localVehicle.transform, currentVehicle, GetPlayerCSteamID(localUID), av42Offset);
        else
            AvatarManager.SetupAircraftRoundels(localVehicle.transform, currentVehicle, GetPlayerCSteamID(localUID), Vector3.zero);

        //if(sendNewSpawnPacket)
        {
            List<HPInfo> hpInfos = PlaneEquippableManager.generateLocalHpInfoList(UID);
            CountermeasureManager cmManager = localVehicle.GetComponentInChildren<CountermeasureManager>();
            List<int> cm = PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager);
            float fuel = PlaneEquippableManager.generateLocalFuelValue();

            Debug.Log("Assembled our local vehicle");
            if (!Networker.isHost)
            {
                // Not host, so send host the spawn vehicle message
                Debug.Log($"Sending spawn vehicle message to: {Networker.hostID}");
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID,
                    new Message_SpawnPlayerVehicle(currentVehicle,
                        new Vector3D(pos),
                        rot,
                        SteamUser.GetSteamID().m_SteamID,
                        UID,
                        hpInfos.ToArray(),
                        cm.ToArray(),
                        fuel, PlayerManager.teamLeftie, SteamFriends.GetPersonaName(),DiscordRadioManager.userID),
                        EP2PSend.k_EP2PSendReliable);
            }
            else
            {
                 Debug.Log("I am host,sending respawn");
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_SpawnPlayerVehicle(currentVehicle,
                        new Vector3D(pos),
                        rot,
                        SteamUser.GetSteamID().m_SteamID,
                        UID,
                        hpInfos.ToArray(),
                        cm.ToArray(),
                        fuel, PlayerManager.teamLeftie, SteamFriends.GetPersonaName(), DiscordRadioManager.userID),
                        EP2PSend.k_EP2PSendReliable);
            }
        }
        WeaponManager localWManager = localVehicle.GetComponent<WeaponManager>();

        localWManager.gpsSystem.CreateGroup("MP");
        localWManager.gpsSystem.UpdateRemotelyModifiedGroups();
        if(currentVehicle == VTOLVehicles.FA26B)
        {

       
            //CUSTOM_API.SetupNewDisplay();
            CUSTOM_API.setupFA26(localVehicle);

        }else
        {

            FrequenceyButton = Multiplayer.CreateFreqButton();
            CUSTOM_API.setupLeg(localVehicle);
        }

     
        if (Multiplayer._instance.alpha)
        foreach(var wings in localVehicle.GetComponentsInChildren<Wing>())
        {
            wings.dragCoefficient *= 0.5f;
        }
        
        foreach (var engine in localVehicle.GetComponentsInChildren<ModuleEngine>())
        {
            engine.maxThrust *= Multiplayer._instance.thrust;
        }
    }
    /// <summary>
    /// When the user has received a message of spawn player vehicle, 
    /// this creates the player vehicle and removes any thing which shouldn't
    /// be on it. 
    /// </summary>
    /// <param name="packet">The message</param>
    public static void SpawnPlayerVehicle(Packet packet, CSteamID sender) //Both, but never spawns the local vehicle, only executes spawn vehicle messages from other clients
    {
        // We don't actually need the "sender" id, unless we're a client and want to check that the packet came from the host
        // which we're not doing right now.
        Message_SpawnPlayerVehicle message = (Message_SpawnPlayerVehicle)((PacketSingle)packet).message;

        if (message.networkID == PlayerManager.localUID)
        {
            return;
        }

        Debug.Log($"Recived a Spawn Vehicle Message from: {message.csteamID}");
        CSteamID spawnerSteamId = new CSteamID(message.csteamID);

        if (!gameLoaded)
        {
            Debug.LogWarning("Our game isn't loaded, adding spawn vehicle to queue");
            playersToSpawnQueue.Enqueue(packet);
            playersToSpawnIdQueue.Enqueue(sender);
            return;
        }
        //foreach (ulong id in spawnedVehicles)
        //{
        //    if (id == message.csteamID)
        //    {
        //        Debug.Log("Got a spawnedVehicle message for a vehicle we have already added! Returning....");
        //        return;
        //    }
        //}
        //spawnedVehicles.Add(message.csteamID);
        Debug.Log("Got a new spawnVehicle uID.");
        if (Networker.isHost)
        {
            //Debug.Log("Generating UIDS for any missiles the new vehicle has");
            for (int i = 0; i < message.hpLoadout.Length; i++)
            {
                for (int j = 0; j < message.hpLoadout[i].missileUIDS.Length; j++)
                {
                    if (message.hpLoadout[i].missileUIDS[j] != 0)
                    {
                        //Storing the old one
                        ulong clientsUID = message.hpLoadout[i].missileUIDS[j];
                        //Generating a new global UID for that missile
                        message.hpLoadout[i].missileUIDS[j] = Networker.GenerateNetworkUID();
                        //Sending it back to that client
                        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(spawnerSteamId,
                            new Message_RequestNetworkUID(clientsUID, message.hpLoadout[i].missileUIDS[j]),
                            EP2PSend.k_EP2PSendReliable);
                    }
                }
            }

            Debug.Log("Telling other clients about new player and new player about other clients. Player count = " + players.Count);
            for (int i = 0; i < players.Count; i++)
            {
              
                if (players[i].cSteamID == SteamUser.GetSteamID())
                {
                    //Debug.LogWarning("Skiping this one as it's the host");
                    //Send the host player to the new player.
                    //Debug.Log($"Running host code to tell new player about host vehicle.");

                    GameObject localVehicle = FlightSceneManager.instance.playerActor.gameObject;
                    WeaponManager localWeaponManager = localVehicle.GetComponent<WeaponManager>();

                    List<HPInfo> hpInfos = PlaneEquippableManager.generateHpInfoListFromWeaponManager(localWeaponManager,
                        PlaneEquippableManager.HPInfoListGenerateNetworkType.sender);
                    CountermeasureManager cmManager = localVehicle.GetComponentInChildren<CountermeasureManager>();
                    List<int> cm = PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager);
                    float fuel = PlaneEquippableManager.generateLocalFuelValue();

                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(spawnerSteamId,
                        new Message_SpawnPlayerVehicle(
                            players[i].vehicleType,
                            VTMapManager.WorldToGlobalPoint(players[i].vehicle.transform.position),
                            players[i].vehicle.transform.rotation,
                            players[i].cSteamID.m_SteamID,
                            players[i].vehicleUID,
                            hpInfos.ToArray(),
                            cm.ToArray(),
                            fuel, players[i].leftie, players[i].nameTag, players[i].discordID),
                        EP2PSend.k_EP2PSendReliable);

                    //Debug.Log($"We have told the new player about the host and NOT the other way around.");
                    //Debug.Log($"We don't need to resync the host weapons, that's guaranteed to already be up to date.");
                    continue;
                    }

                if (players[i].vehicle != null)
                {
                    PlaneNetworker_Receiver existingPlayersPR = players[i].vehicle.GetComponent<PlaneNetworker_Receiver>();
                    //We first send the new player to an existing spawned in player
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(players[i].cSteamID, message, EP2PSend.k_EP2PSendReliable);
                    //Then we send this current player to the new player.
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(spawnerSteamId,
                        new Message_SpawnPlayerVehicle(
                            players[i].vehicleType,
                            VTMapManager.WorldToGlobalPoint(players[i].vehicle.transform.position),
                             players[i].vehicle.transform.rotation,
                            players[i].cSteamID.m_SteamID,
                            players[i].vehicleUID,
                            existingPlayersPR.GenerateHPInfo(),
                            existingPlayersPR.GetCMS(),
                            existingPlayersPR.GetFuel(), players[i].leftie, players[i].nameTag, players[i].discordID),
                        EP2PSend.k_EP2PSendReliable);
                    //Debug.Log($"We have told {players[i].cSteamID.m_SteamID} about the new player ({message.csteamID}) and the other way round.");

                    //We ask the existing player what their load out just incase the host's player receiver was out of sync.
                    //NetworkSenderThread.Instance.SendPacketToSpecificPlayer(players[i].cSteamID,
                      //  new Message(MessageType.WeaponsSet),
                        //EP2PSend.k_EP2PSendReliable);
                    //Debug.Log($"We have asked {players[i].cSteamID.m_SteamID} what their current weapons are, and now waiting for a responce."); // marsh typo response lmao
                }
                else
                {
                    Debug.Log("players[" + i + "].vehicle is null");
                }
            }
        }

        if (Networker.isHost)
        {
            Debug.Log("Telling connected client about AI units");
            AIManager.TellClientAboutAI(spawnerSteamId);
        }
        AddToPlayerList(new Player(spawnerSteamId, null, null, message.vehicle, message.networkID, message.leftie, message.nameTag, message.discordID));
        DiscordRadioManager.addPlayer(message.nameTag, message.discordID);
        GameObject puppet = SpawnRepresentation(message.networkID, message.position, message.rotation, message.leftie, message.nameTag, message.vehicle);
        if (puppet != null)
        {
            PlaneEquippableManager.SetLoadout(puppet, message.networkID, message.normalizedFuel, message.hpLoadout, message.cmLoadout);
        }


    } 
    public static void addGPSTarget(Message_GPSData msg)
    {
        GameObject localVehicle = FlightSceneManager.instance.playerActor.gameObject;
        WeaponManager localWManager = localVehicle.GetComponent<WeaponManager>();

        if (msg.uid == localUID)
            return;
        if (teamLeftie == msg.teamLeft)
        {
            sendGPS = false;
            bool groupFound = false;
            foreach (var gp in localWManager.gpsSystem.groupNames)
            {
                if (gp == msg.GPName)
                {
                    groupFound = true;
                }

            }
            if (!groupFound)
                localWManager.gpsSystem.CreateGroup(msg.GPName);

            string oldGroup = localWManager.gpsSystem.currentGroup.groupName;
            localWManager.gpsSystem.SetCurrentGroup(msg.GPName);

            localWManager.gpsSystem.AddTarget(VTMapManager.GlobalToWorldPoint(msg.pos), msg.prefix);
            localWManager.gpsSystem.TargetsChanged();
            localWManager.gpsSystem.SetCurrentGroup(oldGroup);

        }
        sendGPS = true;
        //NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, msg, EP2PSend.k_EP2PSendReliable);
    }
    public static GameObject SpawnRepresentation(ulong networkID, Vector3D position, Quaternion rotation, bool isLeft, string nameTagString, VTOLVehicles vehicle)
    {
        if (networkID == localUID)
            return null;

        int playerID = FindPlayerIDFromNetworkUID(networkID);
        if (playerID == -1)
        {
            Debug.LogError("Spawn Representation couldn't find a player id.");
        }
        //Player player = ref players[playerID];

        if (players[playerID].vehicle != null)
            GameObject.Destroy(players[playerID].vehicle);

        GameObject newVehicle = null;
        switch (vehicle)
        {
            case VTOLVehicles.None:
                Debug.LogError("Vehcile Enum seems to be none, couldn't spawn player vehicle");
                return null;
            case VTOLVehicles.AV42C:
                if (null == av42cPrefab)
                {
                    SetPrefabs();
                }
                newVehicle = GameObject.Instantiate(av42cPrefab, VTMapManager.GlobalToWorldPoint(position), rotation);
                break;
            case VTOLVehicles.FA26B:
                if (null == fa26bPrefab)
                {
                    SetPrefabs();
                }
                newVehicle = GameObject.Instantiate(fa26bPrefab, VTMapManager.GlobalToWorldPoint(position), rotation);
                break;
            case VTOLVehicles.F45A:
                if (null == f45Prefab)
                {
                    SetPrefabs();
                }
                newVehicle = GameObject.Instantiate(f45Prefab, VTMapManager.GlobalToWorldPoint(position), rotation);
                break;
        }
        //Debug.Log("Setting vehicle name");
        newVehicle.name = $"Client [{players[playerID].cSteamID}]";
        Debug.Log($"Spawned new vehicle at {newVehicle.transform.position}");
        //if (Networker.isHost)
        {
            HealthNetworker_Receiver healthNetworker = newVehicle.AddComponent<HealthNetworker_Receiver>();
            healthNetworker.networkUID = networkID;
        }
        //else
        {
           // HealthNetworker_ReceiverHostEnforced healthNetworker = newVehicle.AddComponent<HealthNetworker_ReceiverHostEnforced>();
            //healthNetworker.networkUID = networkID;
        }
        RigidbodyNetworker_Receiver rbNetworker = newVehicle.AddComponent<RigidbodyNetworker_Receiver>();
        rbNetworker.networkUID = networkID;
        rbNetworker.smoothingTime = 1.25f;
        PlaneNetworker_Receiver planeReceiver = newVehicle.AddComponent<PlaneNetworker_Receiver>();
        planeReceiver.networkUID = networkID;
        planeReceiver.vehicleType = players[playerID].vehicleType;
        if (players[playerID].vehicleType == VTOLVehicles.AV42C || players[playerID].vehicleType == VTOLVehicles.F45A)
        {
            //Debug.Log("Adding Tilt Controller to this vehicle " + message.networkID);
            EngineTiltNetworker_Receiver tiltReceiver = newVehicle.AddComponent<EngineTiltNetworker_Receiver>();
            tiltReceiver.networkUID = networkID;
        }

        Rigidbody rb = newVehicle.GetComponent<Rigidbody>();
        AIPilot aIPilot = newVehicle.GetComponent<AIPilot>();

        RotationToggle wingRotator = aIPilot.wingRotator;
        if (wingRotator != null)
        {
            WingFoldNetworker_Receiver wingFoldReceiver = newVehicle.AddComponent<WingFoldNetworker_Receiver>();
            wingFoldReceiver.networkUID = networkID;
            wingFoldReceiver.wingController = wingRotator;
        }

        LockingRadar lockingRadar = newVehicle.GetComponentInChildren<LockingRadar>();
        if (lockingRadar != null)
        {
            Debug.Log($"Adding LockingRadarReciever to vehicle {newVehicle.name}");
            LockingRadarNetworker_Receiver lockingRadarReceiver = newVehicle.AddComponent<LockingRadarNetworker_Receiver>();
            lockingRadarReceiver.networkUID = networkID;
        }

        ExteriorLightsController extLight = newVehicle.GetComponentInChildren<ExteriorLightsController>();
        if (extLight != null)
        {
            ExtLight_Receiver extLightReceiver = newVehicle.AddComponent<ExtLight_Receiver>();
            extLightReceiver.lightsController = extLight;
            extLightReceiver.networkUID = networkID;
        }

        aIPilot.enabled = false;
        Debug.Log($"Changing {newVehicle.name}'s position and rotation\nPos:{rb.position} Rotation:{rb.rotation.eulerAngles}");
        aIPilot.kPlane.SetToKinematic();
        aIPilot.kPlane.enabled = false;
        rb.interpolation = RigidbodyInterpolation.None;

        aIPilot.kPlane.enabled = true;
        aIPilot.kPlane.SetVelocity(Vector3.zero);
        aIPilot.kPlane.SetToDynamic();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Debug.Log($"Finished changing {newVehicle.name}\n Pos:{rb.position} Rotation:{rb.rotation.eulerAngles}");

        AvatarManager.SetupAircraftRoundels(newVehicle.transform, players[playerID].vehicleType, players[playerID].cSteamID, Vector3.zero);

        if (!Multiplayer._instance.hidePlayerNameTags)
        {
            GameObject parent = new GameObject("Name Tag Holder");
            GameObject nameTag = new GameObject("Name Tag");
            parent.transform.SetParent(newVehicle.transform);
            parent.transform.localRotation = Quaternion.Euler(0, 180, 0);
            nameTag.transform.SetParent(parent.transform);
            nameTag.AddComponent<Nametag>().SetText(
                nameTagString,
                newVehicle.transform, VRHead.instance.transform);
        }
        else
        {
            Debug.Log("Player has disabled name tags.");
        }
        if (isLeft != PlayerManager.teamLeftie)
        {
            aIPilot.actor.team = Teams.Enemy;
        }
        TargetManager.instance.RegisterActor(aIPilot.actor);
        aIPilot.actor.hideDeathLog = true;
        players[playerID].leftie = isLeft;
        players[playerID].vehicle = newVehicle;
        players[playerID].actor = aIPilot.actor;

        if (!VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(networkID))
        {
            VTOLVR_Multiplayer.AIDictionaries.allActors[networkID] = aIPilot.actor;
            VTOLVR_Multiplayer.AIDictionaries.reverseAllActors[aIPilot.actor] = networkID;
        }
        else
        {
            VTOLVR_Multiplayer.AIDictionaries.allActors.Remove(networkID);
            VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Remove(aIPilot.actor);

            VTOLVR_Multiplayer.AIDictionaries.allActors[networkID] = aIPilot.actor;
            VTOLVR_Multiplayer.AIDictionaries.reverseAllActors[aIPilot.actor] = networkID;

        }

        return newVehicle;
    }

    public static int FindPlayerIDFromNetworkUID(ulong networkUID)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].vehicleUID == networkUID)
            {
                return i;
            }
        }
        //Debug.Log("Could not find player with that UID, this is a problem.");
        return -1;
    }

    /// <summary>
    /// Finds the prefabs which are used for spawning the other players on our client
    /// </summary>
    private static void SetPrefabs()
    {
        UnitCatalogue.UpdateCatalogue();
        av42cPrefab = UnitCatalogue.GetUnitPrefab("AV-42CAI");
        fa26bPrefab = UnitCatalogue.GetUnitPrefab("FA-26B AI");
        f45Prefab = UnitCatalogue.GetUnitPrefab("F-45A AI");

        if (!av42cPrefab)
            Debug.LogError("Couldn't find the prefab for the AV-42C");
        if (!fa26bPrefab)
            Debug.LogError("Couldn't find the prefab for the F/A-26B");
        if (!f45Prefab)
            Debug.LogError("Couldn't find the prefab for the F-45A");

    }



    /// <summary>
    /// Creates the spawn points for the other players.
    /// </summary>
    /// <param name="startPosition">The location of where the first spawn should be</param>
    public static void GenerateSpawns(Transform startPosition)
    {
        Debug.Log("Generating Spawns!");
        Actor curPlayer = FlightSceneManager.instance.playerActor;
        GameObject lastSpawn;
        spawnPoints = new List<Transform>();
        //int spawnCounter = 0;
        //If the player starts on the ground
        Debug.Log($"The player's velocity is {curPlayer.velocity.magnitude}");

        /*bool carrier = curPlayer.unitSpawn.unitSpawner.linkedToCarrier;

        if (curPlayer.velocity.magnitude < .5f || carrier)
        {
            var rearmPoints = GameObject.FindObjectsOfType<ReArmingPoint>();
            //back up option below

            foreach (ReArmingPoint rep in rearmPoints)
            {

                if (rep.team == Teams.Allied)
                    if (spawnPoints.Count < spawnsCount)
                    {
                        lastSpawn = new GameObject("MP Spawn " + rep.GetInstanceID());
                        lastSpawn.AddComponent<FloatingOriginTransform>();
                        lastSpawn.transform.position = rep.transform.position;
                        lastSpawn.transform.rotation = rep.transform.rotation;
                        spawnPoints.Add(lastSpawn.transform);

                        Debug.Log($"Created ground Spawn at {lastSpawn.transform.position}");
                    }

            }


        }

        float height = 0;

        Debug.Log($"Creating remaining spawn points ({spawnsCount - spawnPoints.Count}) next to player.");
        int remainingSpawns = spawnsCount - spawnPoints.Count;
        if (remainingSpawns > 0)
            for (int i = 0; i < remainingSpawns; i++)
            {
                lastSpawn = new GameObject("MP Spawn " + i);
                lastSpawn.AddComponent<FloatingOriginTransform>();
                lastSpawn.transform.position = startPosition.position + startPosition.TransformVector(new Vector3(spawnSpacing * (i + 1), height, 0));
                lastSpawn.transform.rotation = startPosition.rotation;
                spawnPoints.Add(lastSpawn.transform);
                Debug.Log($"Created MP Spawn {i} at {lastSpawn.transform.position}");
                Debug.Log($"{remainingSpawns}");
            }

        Debug.Log("Done creating spawns");
        */
    }
    /// <summary>
    /// Returns a spawn point which isn't blocked by another player
    /// </summary>
    /// <returns>A free spawn point</returns>
    public static Transform FindFreeSpawn(bool leftie)
    {
        if (leftie) //leftie
        {
            Debug.Log($"Spawing a leftie");
            var rearmPoints = GameObject.FindObjectsOfType<ReArmingPoint>();

            ReArmingPoint rearmPoint = GameObject.FindObjectOfType<ReArmingPoint>();
            List<ReArmingPoint> EnemyPoints = new List<ReArmingPoint>();
            foreach (ReArmingPoint rep in rearmPoints)
            {
                if (rep.team == Teams.Enemy)
                {
                    EnemyPoints.Add(rep);

                }
            }


            if (EnemyPoints.Count() > 0)
            {
                Debug.Log($"found leftie spawn");
                rearmPoint = EnemyPoints[UnityEngine.Random.Range(0, EnemyPoints.Count - 1)];
                return rearmPoint.transform;
            }
        }
        // Later on this will check the spawns if there is anyone sitting still at this spawn

        spawnTicker += 1;
        if (spawnTicker > spawnsCount - 1)
            spawnTicker = 0;
        return spawnPoints[spawnTicker];
    }

    public static ulong GetPlayerUIDFromCSteamID(CSteamID cSteamID)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].cSteamID == cSteamID)
            {
                return players[i].vehicleUID;
            }
        }
        return 0;
    }

    public static CSteamID GetPlayerCSteamID(ulong uid)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].vehicleUID == uid)
            {
                return players[i].cSteamID;
            }
        }
        return new CSteamID();
    }
    public static int GetPlayerIDFromCSteamID(CSteamID cSteamID)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].cSteamID == cSteamID)
            {
                return i;
            }
        }
        return -1;
    }
    public static void AddToPlayerList(Player player)
    {
        foreach (var playerInList in players)
        {
            if (player.cSteamID == playerInList.cSteamID)
            {
                if (playerInList.vehicle != null)
                    GameObject.Destroy(playerInList.vehicle);
                players.Remove(playerInList);
                break;
            }
        }
        players.Add(player);
    }
    public static string GetPlayerNameFromActor(Actor act)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].actor == act)
            {
                return players[i].nameTag;
            }
        }
        return "";
    }

    public static void CleanUpPlayerManagerStaticVariables()
    {
        spawnPoints?.Clear();
        spawnRequestQueue?.Clear();
        playersToSpawnQueue?.Clear();
        playersToSpawnIdQueue?.Clear();
        gameLoaded = false;
        spawnedVehicles?.Clear();
        localUID = 0;
        worldData = null;
        players?.Clear();
        buttonMade = false;
        text = null;
        ObjectiveNetworker_Reciever.cleanUp();
        PlaneNetworker_Receiver.dontPrefixNextJettison = false;
        firstSpawnDone = false;
        firstKillSpawnDone = false;
        timeAlive = 0.0f;
        carrierStart = false;
        airSpawn = false;
        carrierFound = false;
        sendGPS = true;
        carrierStartTimer = 0;
        flyCounter = 0;
        kills = 0;
        OPFORbuttonMade = false;
    }

    public static void OnDisconnect()
    {
        CleanUpPlayerManagerStaticVariables();
        Networker._instance?.PlayerManagerReportsDisconnect();
    }
}
