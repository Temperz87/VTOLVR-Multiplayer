using System.Collections;
using UnityEngine;
using Harmony;
using TMPro;
using UnityEngine.UI;

class PlayerNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_Respawn lastMessage;
    public Health health;
    public Actor actor;

    public TempPilotDetacher detacher;
    //public GearAnimator[] gears;
    //public FloatingOriginShifter shifter;
    public EjectionSeat ejection;
    public EngineEffects[] effects;

    Coroutine repspawnTimer;

    Transform target;
    Transform ejectorParent;
    Transform canopyParent;
    Vector3 ejectorSeatPos;
    Quaternion ejectorSeatRot;
    Vector3 canopyPos;
    Quaternion canopyRot;

    GameObject hud;
    GameObject hudWaypoint;

    public float respawnTimer = 10.0f;
    void Awake()
    {
        lastMessage = new Message_Respawn(networkUID, new Vector3D(), new Quaternion(), false, Steamworks.SteamFriends.GetPersonaName(), VTOLAPI.GetPlayersVehicleEnum());
        actor = GetComponent<Actor>();
        health = actor.health;
        

        if (health == null)
            Debug.LogError("health was null on player " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);

        detacher = GetComponentInChildren<TempPilotDetacher>();
        //gears = GetComponentsInChildren<GearAnimator>();
        //shifter = GetComponentInChildren<FloatingOriginShifter>();
        ejection = GetComponentInChildren<EjectionSeat>();
        ejection.OnEject.AddListener(Eject);
        detacher.OnDetachPilot.AddListener(Eject);
        //ejectorSeatPos = ejection.transform.localPosition;
        //ejectorSeatRot = ejection.transform.localRotation;

        //target = detacher.cameraRig.transform.parent;
        //ejectorParent = ejection.gameObject.transform.parent;
        //if (ejection.canopyObject != null) {
        //    canopyParent = ejection.canopyObject.transform.parent;
        //    canopyPos = ejection.canopyObject.transform.localPosition;
        //    canopyRot = ejection.canopyObject.transform.localRotation;
        //}

        effects = GetComponentsInChildren<EngineEffects>();
    }

    IEnumerator RespawnTimer()
    {
        Debug.Log("Starting respawn timer.");
        GameObject button = null;
        if (!Networker.equipLocked)
            button = Multiplayer.CreateVehicleButton();
        yield return new WaitForSeconds(respawnTimer);
        if(button != null)
            Destroy(button);

        Debug.Log("Finished respawn timer.");

        ReArmingPoint[] rearmPoints = GameObject.FindObjectsOfType<ReArmingPoint>();
        ReArmingPoint rearmPoint = rearmPoints[Random.Range(0, rearmPoints.Length - 1)];

        float lastRadius = 0;
        if (PlayerManager.carrierStart)
        {
            foreach (ReArmingPoint rep in rearmPoints)
            {
                if (rep.team == Teams.Allied)
                {
                    if (rep.radius > 17.8f && rep.radius < 19.0f)
                    {
                        rearmPoint = rep;
                    }
                }
            }
        }
        else
            foreach (ReArmingPoint rep in rearmPoints)
            {
                Debug.Log("finding rearm pt");
                if (rep.team == Teams.Allied && rep.CheckIsClear(actor))
                {

                    if (rep.radius > lastRadius)
                    {
                        rearmPoint = rep;
                        lastRadius = rep.radius;
                    }
                }
            }



        //UnEject();
        //PutPlayerBackInAircraft();
        //RepairAircraft();

        //foreach (GearAnimator gear in gears) {
        //    gear.ExtendImmediate();
        //}

        //GetComponent<Rigidbody>().velocity = Vector3.zero;
        //transform.position = rearmPoint.transform.position + Vector3.up * 10;
        //transform.rotation = rearmPoint.transform.rotation;

        Destroy(FlightSceneManager.instance.playerActor.gameObject);
        Destroy(detacher.cameraRig);
        Destroy(detacher.gameObject);
        Destroy(ejection.gameObject);
        Destroy(BlackoutEffect.instance);
        Destroy(GetComponent<PlayerSpawn>());

        foreach (EngineEffects effect in effects)
        {
            Destroy(effect);
        }
        //as much stuff as im destroying, some stuff is most likely getting through, future people, look into this

        AudioController.instance.ClearAllOpenings();

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
        GameObject newPlayer = Instantiate(PilotSaveManager.currentVehicle.vehiclePrefab);
        if (newPlayer == null)
        {
            Debug.LogError("new vehicle is null");
        }
        newPlayer.GetComponent<Actor>().designation = FlightSceneManager.instance.playerActor.designation;//reassigning designation

        FlightSceneManager.instance.playerActor = newPlayer.GetComponent<Actor>();
        FlightSceneManager.instance.playerActor.flightInfo.PauseGCalculations();
        FlightSceneManager.instance.playerActor.flightInfo.OverrideRecordedAcceleration(Vector3.zero);

        rearmPoint.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.Success);
        PilotSaveManager.currentScenario.totalBudget = 999999;
        PilotSaveManager.currentScenario.initialSpending = 0;
        PilotSaveManager.currentScenario.inFlightSpending = 0;
        PilotSaveManager.currentScenario.equipConfigurable = true;

        PlayerVehicleSetup pvSetup = newPlayer.GetComponent<PlayerVehicleSetup>();
        pvSetup.SetupForFlight();

        Rigidbody rb = newPlayer.GetComponent<Rigidbody>();
        GearAnimator gearAnim = newPlayer.GetComponent<GearAnimator>();
        if (gearAnim != null)
        {
            if (gearAnim.state != GearAnimator.GearStates.Extended)
                gearAnim.ExtendImmediate();
        }


        //  PlayerManager.StartRearm(rearmPoint);
        //rb.velocity = Vector3.zero;
        //rb.detectCollisions = true;
        PlayerManager.SpawnLocalVehicleAndInformOtherClients(newPlayer, newPlayer.transform.position, newPlayer.transform.rotation, networkUID, false);

        //PlayerManager.SetupLocalAircraft(newPlayer, newPlayer.transform.position, newPlayer.transform.rotation, networkUID);

       /* lastMessage.UID = networkUID;
        lastMessage.isLeftie = PlayerManager.teamLeftie;
        lastMessage.tagName = Steamworks.SteamFriends.GetPersonaName();
        lastMessage.vehicle = VTOLAPI.GetPlayersVehicleEnum();
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
   */
    }

    void UnEject()
    {
        if (ejection.canopyObject)
        {
            ejection.canopyObject.GetComponentInChildren<Collider>().enabled = false;
            ejection.canopyObject.GetComponentInChildren<Collider>().gameObject.layer = 9;//set to debris layer
        }
        ejection.gameObject.GetComponentInChildren<Collider>().enabled = false;
        ejection.gameObject.GetComponentInChildren<Collider>().gameObject.layer = 9;//set to debris layer

        if (ejection.canopyObject)
        {
            Destroy(ejection.canopyObject.GetComponent<Rigidbody>());
            Destroy(ejection.canopyObject.GetComponent<FloatingOriginTransform>());

            ejection.canopyObject.transform.parent = canopyParent;
            ejection.canopyObject.transform.localPosition = canopyPos;
            ejection.canopyObject.transform.localRotation = canopyRot;
        }

        BlackoutEffect componentInChildren = VRHead.instance.GetComponentInChildren<BlackoutEffect>();
        if (componentInChildren)
        {
            componentInChildren.rb = GetComponent<Rigidbody>();
            componentInChildren.useFlightInfo = true;
        }
        ejection.gameObject.transform.parent = ejectorParent;
        ejection.transform.localPosition = ejectorSeatPos;
        ejection.transform.localRotation = ejectorSeatRot;

        Destroy(ejection.gameObject.GetComponent<FloatingOriginShifter>());
        Destroy(ejection.gameObject.GetComponent<FloatingOriginTransform>());
        ejection.seatRB.isKinematic = true;
        ejection.seatRB.interpolation = RigidbodyInterpolation.None;
        ejection.seatRB.collisionDetectionMode = CollisionDetectionMode.Discrete;

        ModuleParachute parachute = ejection.GetComponentInChildren<ModuleParachute>();
        parachute.CutParachute();

        Traverse.Create(ejection).Field("ejected").SetValue(false);//does nothing, cannot eject a seccond time
        //i dont think ejecting is necessary for now, but someone prob ought look into that

        //shifter.enabled = true;
        //AudioController.instance.AddExteriorOpening("eject", 0f);
    }

    void PutPlayerBackInAircraft()
    {
        detacher.cameraRig.transform.parent = target;
        detacher.cameraRig.transform.position = target.position;
        detacher.cameraRig.transform.rotation = target.rotation;

        detacher.pilotModel.SetActive(false);

        Destroy(detacher.cameraRig.GetComponent<FloatingOriginShifter>());
        Destroy(detacher.cameraRig.GetComponent<FloatingOriginTransform>());
        foreach (VRHandController vrhandController2 in VRHandController.controllers)
        {
            if (vrhandController2)
            {
                Destroy(vrhandController2.gameObject.GetComponent<VRTeleporter>());
            }
        }

        //shifter.enabled = true;
    }

    void RepairAircraft()
    {
        FlightAssist flightAssist = GetComponentInChildren<FlightAssist>();
        if (flightAssist != null)
        {
            flightAssist.assistEnabled = true;
        }
        else
        {
            Debug.Log("Could not fix flight assists");
        }

        RCSController rcsController = GetComponentInChildren<RCSController>();
        if (rcsController != null)
        {
            Traverse.Create(rcsController).Field("alive").SetValue(true);
        }
        else
        {
            Debug.Log("Could not fix rcs controller");
        }

        Battery battery = GetComponentInChildren<Battery>();
        if (battery != null)
        {
            Traverse.Create(battery).Field("isAlive").SetValue(true);
            battery.Connect();
        }
        else
        {
            Debug.Log("Could not fix battery");
        }

        GameObject hud = GameObject.Find("CollimatedHud");
        if (hud != null)
        {
            hud.SetActive(true);
        }
        else
        {
            Debug.Log("Could not fix hud");
        }

        GameObject hudWaypoint = GameObject.Find("WaypointLead");
        if (hudWaypoint != null)
        {
            hudWaypoint.SetActive(true);
        }
        else
        {
            Debug.Log("Could not fix hudWaypoint");
        }

        VRJoystick joystick = GetComponentInChildren<VRJoystick>();
        if (joystick != null)
        {
            joystick.sendEvents = true;
        }
        else
        {
            Debug.Log("Could not fix joystick");
        }

        VRInteractable[] levers = GetComponentsInChildren<VRInteractable>();
        foreach (VRInteractable lever in levers)
        {
            lever.enabled = true;
        }
        Debug.Log("Fixed " + levers.Length + " levers");
    }

    void Eject()
    {
        if (FlightSceneManager.instance.playerActor == null)
            return;
        FlightSceneManager.instance.playerActor.health.invincible = false;

        Actor killer = null;
        Actor fkiller = null;
        foreach (var heal in GetComponentsInChildren<Health>())
        {
            killer = Traverse.Create(heal).Field("lastSourceActor").GetValue<Actor>();
            if (killer != null)
            {

                fkiller = killer;
            }
        }
      


      
        string message = "";

        if (FlightSceneManager.instance.playerActor.health.killMessage != null)
        {
            message = FlightSceneManager.instance.playerActor.health.killMessage + " and cowardly ejected";

        }
        else
        {
            message = "cowardly ejection";
        }
        FlightSceneManager.instance.playerActor.health.Damage(10000000.0f, FlightSceneManager.instance.playerActor.gameObject.transform.position, Health.DamageTypes.Impact, fkiller, message);
        // health.invincible = false;
        //health.Kill();

    }
   
    void Death()
    {
        foreach (Collider collider in FlightSceneManager.instance.playerActor.gameObject.GetComponentsInChildren<Collider>())
        {
            if (collider)
            {
                Hitbox hitbox = collider.GetComponent<Hitbox>();

                if (hitbox != null)
                { 
                    collider.gameObject.layer = 9;
                }
            }
        }
        repspawnTimer = StartCoroutine("RespawnTimer");
    }
}
class PlaneButton : MonoBehaviour
{
    TextMeshPro textMesh;
    GameObject obj;
    public string text ="";
    void buttonFunc(string intext)
    {
        PlayerManager.selectedVehicle = intext;
    }
    void Awake()
    {
        foreach (var controller in GameObject.FindObjectsOfType<VRHandController>())
        {
            if (controller.isLeft)
            {
                obj = new GameObject();
        textMesh = obj.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.enableWordWrapping = false;
        
        obj.transform.SetParent(controller.transform);
        obj.transform.localPosition = new Vector3(0, 0.2f, 0);
        obj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
        }
    }

    private void Start()
    {
        textMesh.SetText(text);
    }
    private void LateUpdate()
    {
        
        foreach (var controller in GameObject.FindObjectsOfType<VRHandController>())
        {
            if (!controller.isLeft)
            {

                Vector3 dist = controller.transform.position - obj.transform.position;

                if(dist.magnitude<0.05f)
                {
                    buttonFunc("F-45A");
                    obj.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
                }
                else
                {
                    obj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                }

            }
        }
    }

}
