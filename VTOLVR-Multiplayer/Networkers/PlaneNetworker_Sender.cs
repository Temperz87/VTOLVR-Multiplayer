using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
public class PlaneNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    //Classes we use to find the information out
    private bool isPlayer;
    //private AIPilot aIPilot;
    private WheelsController wheelsController;
    private AeroController aeroController;
    //private VRThrottle vRThrottle;
    private WeaponManager weaponManager;
    private CountermeasureManager cmManager;
    private FuelTank fuelTank;
    private Traverse traverse;
    private bool previousFiringState;
    private int lastIdx;
    private Message_PlaneUpdate lastMessage;
    private Message_WeaponFiring lastFiringMessage;
    private Message_FireCountermeasure lastCountermeasureMessage;
    private Message_Death lastDeathMessage;
    private ModuleEngine engine;
    private Tailhook tailhook;
    private CatapultHook launchBar;
    private RefuelPort refuelPort;
    //private Traverse traverseThrottle;
    private Actor actor;
    private InternalWeaponBay iwb = null;
    private ulong sequenceNumber;
    private HPEquipMissileLauncher lastml;

    private float tick = 0;
    public float tickRate = 2;

    private float tickPuppet= 0;
    public float tickRatePuppet = 20;
    bool sendRearmPacket = false;


    GameObject headL;
    GameObject head;
    GameObject hip;
    Transform Rhand;
    Transform Lhand;

 
    Message_IKPuppet ikMsg;
    private void Awake()
    {
        actor = gameObject.GetComponent<Actor>();
        lastFiringMessage = new Message_WeaponFiring(-1, false, false, networkUID);
        // lastStoppedFiringMessage = new Message_WeaponStoppedFiring(networkUID);
        lastCountermeasureMessage = new Message_FireCountermeasure(true, true, networkUID);
        lastDeathMessage = new Message_Death(networkUID, false,"");
        wheelsController = GetComponent<WheelsController>();
        aeroController = GetComponent<AeroController>();
        isPlayer = actor.isPlayer;
        sequenceNumber = 0;
        lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, false, networkUID, sequenceNumber);

        tick += UnityEngine.Random.Range(0.0f, 1.0f / tickRate);
        engine = gameObject.GetComponentInChildren<ModuleEngine>();
        if (engine == null)
        {
            Debug.Log("engine was null on vehicle " + gameObject.name);
        }

        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Weapon Manager was null on vehicle " + gameObject.name);
        else
        {
            traverse = Traverse.Create(weaponManager);
            Networker.WeaponSet += WeaponSet;
            weaponManager.OnWeaponEquipped += Rearm;
            //weaponManager.OnWeaponUnequippedHPIdx +=Rearm;

            //detect player rearm
           
         
            if (actor.isPlayer && weaponManager.GetIWBForEquip(3) != null)
            {
                iwb = weaponManager.GetIWBForEquip(3);
            }
        }

        cmManager = GetComponentInChildren<CountermeasureManager>();
        if (cmManager == null)
            Debug.LogError("CountermeasureManager was null on vehicle " + gameObject.name);
        else
            cmManager.OnFiredCM += FireCountermeasure;

        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on vehicle " + gameObject.name);


        Debug.Log("Done Plane Sender");
        tailhook = GetComponentInChildren<Tailhook>();
        launchBar = GetComponentInChildren<CatapultHook>();
        refuelPort = GetComponentInChildren<RefuelPort>();

        if(isPlayer)
        setupManSender();

        if (PlayerManager.selectedVehicle.Contains("45"))
            vehicleType = VTOLVehicles.F45A;

        if (PlayerManager.selectedVehicle.Contains("42"))
            vehicleType = VTOLVehicles.AV42C;

        if (PlayerManager.selectedVehicle.Contains("26"))
            vehicleType = VTOLVehicles.FA26B;

        if (vehicleType != VTOLVehicles.None)
        {
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (rend.material.name.Contains("Glass") || rend.material.name.Contains("glass"))
                {
                    Color meshColor = rend.sharedMaterial.color;

                    meshColor *= new Color(0.8f, 0.8f, 1.0f, 1.0f);
                    meshColor.a = 0.6f;
                    Shader newShader = Shader.Find("Transparent/Diffuse");

                    rend.material.color = meshColor;
                    rend.material.shader = newShader;
                }

                


            }
            setupManReciever();
        }
            
        ikMsg = new Message_IKPuppet(networkUID);
    }



    GameObject manPuppet;

    public Transform puppetRhand;
    public Transform puppetLhand;
    public Transform puppetHead;
    public Transform puppetHeadLook;
    public Transform puppethip;
    bool manSetup = false;


    FastIKFabric ikh;
            FastIKFabric ikrh;
            FastIKFabric iklh;
             FastIKLook ikheadlook;
    public VTOLVehicles vehicleType = VTOLVehicles.None;

    private void setupManReciever()
    {

     

        manPuppet = GameObject.Instantiate(CUSTOM_API.manprefab, gameObject.GetComponent<Rigidbody>().transform);

        manPuppet.transform.localScale = new Vector3(0.072f, 0.072f, 0.074f);

        manPuppet.transform.localEulerAngles = new Vector3(0.0f, 180.0f, 0.0f);

        if (vehicleType == VTOLVehicles.FA26B)
            manPuppet.transform.localPosition = new Vector3(0.03f, 1.04f, 5.31f);

        if (vehicleType == VTOLVehicles.F45A)
            manPuppet.transform.localPosition = new Vector3(-0.06f, 0.81f, 5.7f);

        if (vehicleType == VTOLVehicles.AV42C)
            manPuppet.transform.localPosition = new Vector3(-0.07f, 0.69f, -0.1f);



        Debug.Log("righthandControl");
        puppetRhand = CUSTOM_API.GetChildWithName(manPuppet, "righthandControl").transform;
        Debug.Log("lefthandControl");
        puppetLhand = CUSTOM_API.GetChildWithName(manPuppet, "lefthandControl").transform;
        Debug.Log("headControl");
        puppetHead = CUSTOM_API.GetChildWithName(manPuppet, "headControl").transform;
        Debug.Log("headLook");
        puppetHeadLook = CUSTOM_API.GetChildWithName(manPuppet, "lookControl").transform;
        puppetHeadLook.transform.position = puppetHeadLook.transform.position - new Vector3(0.0f, 0.15f, 0.0f);
        Debug.Log("Bone.008");
        puppethip = CUSTOM_API.GetChildWithName(manPuppet, "Bone.008").transform;

        Debug.Log("headik_end");
          ikh = CUSTOM_API.GetChildWithName(manPuppet, "Bone.007").AddComponent<FastIKFabric>();
        ikh.Target = puppetHead;
        ikh.ChainLength = 4;

        Debug.Log("righthandik_end");
          ikrh = CUSTOM_API.GetChildWithName(manPuppet, "righthandik_end").AddComponent<FastIKFabric>();
        ikrh.Target = puppetRhand;
        ikrh.ChainLength = 3;

        Debug.Log("lefthandik_end");
          iklh = CUSTOM_API.GetChildWithName(manPuppet, "lefthandik_end").AddComponent<FastIKFabric>();
        iklh.Target = puppetLhand;
        iklh.ChainLength = 3;
        Debug.Log("SetupNewDisplay");


        Debug.Log("headik");
          ikheadlook = CUSTOM_API.GetChildWithName(manPuppet, "headik").AddComponent<FastIKLook>();
        ikheadlook.Target = puppetHeadLook;
        manSetup = true;

    }

     
    private void setupManSender()
    {
        Rhand = CUSTOM_API.GetChildTransformWithName(gameObject, "Controller (right)");
        Debug.Log("Controller (left)");
        Lhand = CUSTOM_API.GetChildTransformWithName(gameObject, "Controller (left)");

        Debug.Log("neckBone_end");
        head = CUSTOM_API.GetChildWithName(gameObject, "neckBone_end");

        Debug.Log("hip.left");
        hip = CUSTOM_API.GetChildWithName(gameObject, "hip.left");
        headL = CUSTOM_API.GetChildWithName(gameObject, "Helmet");
        Debug.Log("hip.left");
        hip = CUSTOM_API.GetChildWithName(gameObject, "hip.left");
    }
    private void sendManData()
    {

        Vector3 rhandHostPos = Rhand.position - hip.transform.position;

        Vector3 lhandHostPos = Lhand.position - hip.transform.position;

        Vector3 headHostPos = head.transform.position - hip.transform.position;

        Vector3 headlookHostPos = (headL.transform.position + headL.transform.forward) - hip.transform.position;


        ikMsg.networkUID = networkUID;
        ikMsg.puppetRhand = new Vector3D(rhandHostPos);
        ikMsg.puppetLhand = new Vector3D(lhandHostPos);
        ikMsg.puppetHead = new Vector3D(headHostPos * 1.0001f);
        ikMsg.puppetHeadLook = new Vector3D(headlookHostPos);

        if (Networker.isHost)
        {
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(ikMsg, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, ikMsg, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }

   
    private void Update()
    {
        if (weaponManager != null)
        {
            if (weaponManager.isFiring != previousFiringState || lastIdx != (int)traverse.Field("weaponIdx").GetValue())
            {
                previousFiringState = weaponManager.isFiring;
                lastFiringMessage.weaponIdx = (int)traverse.Field("weaponIdx").GetValue();
                lastIdx = lastFiringMessage.weaponIdx;
                Debug.Log("combinedWeaponIdx = " + lastFiringMessage.weaponIdx);
                lastFiringMessage.UID = networkUID;
                // lastStoppedFiringMessage.UID = networkUID;
                lastFiringMessage.isFiring = weaponManager.isFiring;
                if (weaponManager.currentEquip is HPEquipGun || weaponManager.currentEquip is VTOLCannon)
                {
                    lastFiringMessage.noAmmo = weaponManager.currentEquip.GetCount() == 0;
                }
                else
                {
                    lastFiringMessage.noAmmo = false;
                }
                if (weaponManager.isFiring && weaponManager.currentEquip is HPEquipMissileLauncher)
                {
                    lastml = weaponManager.currentEquip as HPEquipMissileLauncher;
                    lastFiringMessage.missileIdx = (int)Traverse.Create(lastml.ml).Field("missileIdx").GetValue();
                }
                if (Networker.isHost)
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
                else
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            }
        }
    }
    private void FixedUpdate()
    {
        tickPuppet += Time.fixedDeltaTime;
        if (isPlayer)
            if (tickPuppet > 1.0f / tickRatePuppet)
            {
                tickPuppet = 0.0f;
                sendManData();
                if (manSetup != true)
                    return;
                if (manPuppet.transform.position.magnitude > 500)
                {
                    manPuppet.SetActive(false);
                }
                else
                {
                    manPuppet.SetActive(true);

                    ikh.ResolveIK();
                    ikrh.ResolveIK();
                    iklh.ResolveIK();


                }
            }
        //buffers multiple euip events into one packet
        if (sendRearmPacket)
        {
            Rearm();
        }
       
       

       
       

        
        puppetRhand.position = puppethip.transform.position + ikMsg.puppetRhand.toVector3;
        puppetLhand.position = puppethip.transform.position + ikMsg.puppetLhand.toVector3;
        puppetHead.position = puppethip.transform.position + ikMsg.puppetHead.toVector3;
        puppetHeadLook.position = puppethip.transform.position + ikMsg.puppetHeadLook.toVector3;
    }
    private void LateUpdate()
    {
       
        tick += Time.deltaTime;
        if (tick > 1.0f / tickRate)
        { 
        tick = 0;
        lastMessage.flaps = aeroController.flaps;
        lastMessage.pitch = Mathf.Round(aeroController.input.x * 100000f) / 100000f;
        lastMessage.yaw = Mathf.Round(aeroController.input.y * 100000f) / 100000f;
        lastMessage.roll = Mathf.Round(aeroController.input.z * 100000f) / 100000f;
        lastMessage.brakes = aeroController.brake;
        lastMessage.landingGear = LandingGearState();
        lastMessage.networkUID = networkUID;
        lastMessage.sequenceNumber = ++sequenceNumber;
        if (iwb != null)
        {
            lastMessage.doorState = iwb.doorState;
        }
        if (engine != null)
        {
            lastMessage.throttle = engine.finalThrottle;
        }
        if (tailhook != null)
        {
            lastMessage.tailHook = tailhook.isDeployed;
        }
        if (launchBar != null)
        {
            lastMessage.launchBar = launchBar.deployed;
        }
        if (refuelPort != null)
        {
            lastMessage.fuelPort = refuelPort.open;
        }

        if (Networker.isHost)
        {
                Networker.addToUnreliableSendBuffer(lastMessage);
        }
            //NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        }
    }

    private bool LandingGearState()
    {
        if(wheelsController !=null)
        { 
        return wheelsController.gearAnimator.GetCurrentState() == GearAnimator.GearStates.Extended;
        }
        return false;
    }

    public void SetThrottle(float t)
    {
        lastMessage.throttle = t;
    }

    public void WeaponSet(Packet packet)
    {
        if (weaponManager == null)
            return;
        //This message has only been sent to us so no need to check UID
        List<HPInfo> hpInfos = PlaneEquippableManager.generateHpInfoListFromWeaponManager(weaponManager,
            PlaneEquippableManager.HPInfoListGenerateNetworkType.sender);

        List<int> cm = PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager);

        float fuel = PlaneEquippableManager.generateLocalFuelValue();

        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID,
            new Message_WeaponSet_Result(hpInfos.ToArray(), cm.ToArray(), fuel, networkUID),
            Steamworks.EP2PSend.k_EP2PSendReliable);
    }

    public void FireCountermeasure()
    {
        lastCountermeasureMessage.UID = networkUID;
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastCountermeasureMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastCountermeasureMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }

    public void Rearm(HPEquippable hpEquip)
    {
        sendRearmPacket = true;
    }
 
    public void Rearm()
    {
        Debug.Log("Rearm!");

        GameObject vehicle = VTOLAPI.GetPlayersVehicleGameObject();
        WeaponManager wm = vehicle.GetComponentInChildren<WeaponManager>();
        CountermeasureManager cm = vehicle.GetComponentInChildren<CountermeasureManager>();

        Message_WeaponSet_Result rearm = new Message_WeaponSet_Result(
            PlaneEquippableManager.generateHpInfoListFromWeaponManager(wm, PlaneEquippableManager.HPInfoListGenerateNetworkType.generate, PlayerManager.localUID).ToArray(),
            PlaneEquippableManager.generateCounterMeasuresFromCmManager(cm).ToArray(),
            PlaneEquippableManager.generateLocalFuelValue(),
            PlayerManager.localUID);

        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(rearm, Steamworks.EP2PSend.k_EP2PSendReliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, rearm, Steamworks.EP2PSend.k_EP2PSendReliable);
        sendRearmPacket = false;
    }

    public void OnDestroy()
    {
        Networker.WeaponSet -= WeaponSet;
        //PlayerVehicleSetup pv = gameObject.GetComponent<PlayerVehicleSetup>();
        //if (pv != null)
            //pv.OnEndRearming.RemoveListener(Rearm);
    }
}
[HarmonyPatch(typeof(WeaponManager), "JettisonMarkedItems")]
public static class Patch1
{
    public static bool Prefix(WeaponManager __instance)
    {
        if (PlaneNetworker_Receiver.dontPrefixNextJettison)
        {
            PlaneNetworker_Receiver.dontPrefixNextJettison = false;
            return true;
        }
        List<int> toJettison = new List<int>();
        Traverse traverse;
        Message_JettisonUpdate lastMesage;
        if (__instance.actor == null)
        {
            return false;
        }
        else if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(__instance.actor, out ulong networkUID))
        {
            traverse = Traverse.Create(__instance);
            for (int i = 0; i < 30; i++)
            {
                HPEquippable equip = __instance.GetEquip(i);
                if (equip != null)
                {
                    if (equip.markedForJettison)
                        toJettison.Add(equip.hardpointIdx);
                }
            }
            if (toJettison.Count == 0)
            {
                Debug.Log("Tried to jettison nothing, not doing it");
                return true;
            }
            lastMesage = new Message_JettisonUpdate(toJettison.ToArray(), networkUID);
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMesage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMesage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
        else
        {
            Debug.LogError($"{networkUID} not found in AIDictionaries for jettison messsage to send.");
        }
        return true;
    }
}