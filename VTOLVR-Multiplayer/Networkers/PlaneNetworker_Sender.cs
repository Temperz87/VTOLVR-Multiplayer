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

        
            
        ikMsg = new Message_IKPuppet(networkUID);
    }



    GameObject manPuppet;

    public Transform puppetRhand;
    public Transform puppetLhand;
    public Transform puppetHead;
    public Transform puppetHeadLook;
    public Transform puppethip;
    bool manSetup = false;
     
    

     
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
                //Debug.Log("combinedWeaponIdx = " + lastFiringMessage.weaponIdx);
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
       
            if (tickPuppet > 1.0f / tickRatePuppet)
            {
                tickPuppet = 0.0f;
                if(isPlayer)
                    sendManData();


        }
        //buffers multiple euip events into one packet
        if (sendRearmPacket)
        {
            Rearm();
        }
      


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