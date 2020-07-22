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
    private AIPilot aIPilot;
    private WheelsController wheelsController;
    private AeroController aeroController;
    private VRThrottle vRThrottle;
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
    private Tailhook tailhook;
    private CatapultHook launchBar;
    private RefuelPort refuelPort;
    private Traverse traverseThrottle;
    private Actor actor;
    private ulong sequenceNumber;
    private void Awake()
    {
        actor = gameObject.GetComponent<Actor>();
        lastFiringMessage = new Message_WeaponFiring(-1, false, networkUID);
        // lastStoppedFiringMessage = new Message_WeaponStoppedFiring(networkUID);
        lastCountermeasureMessage = new Message_FireCountermeasure(true, true, networkUID);
        lastDeathMessage = new Message_Death(networkUID);
        wheelsController = GetComponent<WheelsController>();
        aeroController = GetComponent<AeroController>();
        isPlayer = actor.isPlayer;
        sequenceNumber = 0;
        if (isPlayer)
        {
            lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, false, networkUID, sequenceNumber);
            vRThrottle = gameObject.GetComponentInChildren<VRThrottle>();
            if (vRThrottle == null)
                Debug.Log("Throttle was null on vehicle " + gameObject.name);
            else
                vRThrottle.OnSetThrottle.AddListener(SetThrottle);
        }
        else
        {
            lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, false, networkUID, sequenceNumber);
            aIPilot = gameObject.GetComponent<AIPilot>();
            if (aIPilot == null)
            { Debug.Log("Aipilot was null on vehicle " + gameObject.name); }
            traverseThrottle = Traverse.Create(aIPilot.autoPilot.engines[0]);
        }
        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Weapon Manager was null on vehicle " + gameObject.name);
        else
            traverse = Traverse.Create(weaponManager);

        cmManager = GetComponentInChildren<CountermeasureManager>();
        if (cmManager == null)
            Debug.LogError("CountermeasureManager was null on vehicle " + gameObject.name);
        else
            cmManager.OnFiredCM += FireCountermeasure;

        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on vehicle " + gameObject.name);

        if (weaponManager)
        {
            Networker.WeaponSet += WeaponSet;
        }
        Debug.Log("Done Plane Sender");
        tailhook = GetComponentInChildren<Tailhook>();
        launchBar = GetComponentInChildren<CatapultHook>();
        refuelPort = GetComponentInChildren<RefuelPort>();
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
                if (Networker.isHost)
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
                else
                    NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
            }
        }
    }

    private void LateUpdate()
    {
        lastMessage.flaps = aeroController.flaps;
        lastMessage.pitch = Mathf.Round(aeroController.input.x * 100000f) / 100000f;
        lastMessage.yaw = Mathf.Round(aeroController.input.y * 100000f) / 100000f;
        lastMessage.roll = Mathf.Round(aeroController.input.z * 100000f) / 100000f;
        lastMessage.brakes = aeroController.brake;
        lastMessage.landingGear = LandingGearState();
        lastMessage.networkUID = networkUID;
        lastMessage.sequenceNumber = ++sequenceNumber;
        if (!isPlayer)
        {
            SetThrottle((float)traverseThrottle.Field("throttle").GetValue());
        }
        if (tailhook != null) {
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
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        }
    }

    private bool LandingGearState()
    {
        return wheelsController.gearAnimator.GetCurrentState() == GearAnimator.GearStates.Extended;
    }

    public void SetThrottle(float t)
    {
        lastMessage.throttle = t;
    }

    public void WeaponSet(Packet packet)
    {
        //This message has only been sent to us so no need to check UID
        List<HPInfo> hpInfos = VTOLVR_Multiplayer.PlaneEquippableManager.generateHpInfoListFromWeaponManager(weaponManager,
            VTOLVR_Multiplayer.PlaneEquippableManager.HPInfoListGenerateNetworkType.sender);

        List<int> cm = VTOLVR_Multiplayer.PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager);

        float fuel = VTOLVR_Multiplayer.PlaneEquippableManager.generateLocalFuelValue();

        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID,
            new Message_WeaponSet_Result(hpInfos.ToArray(), cm.ToArray(), fuel, networkUID),
            Steamworks.EP2PSend.k_EP2PSendReliable);
    }
    public void FireCountermeasure() {
        lastCountermeasureMessage.UID = networkUID;
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastCountermeasureMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastCountermeasureMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
    }
    public void Death()
    {
        lastDeathMessage.UID = networkUID;
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastDeathMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastDeathMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
    public void OnDestroy()
    {
        Networker.WeaponSet -= WeaponSet;
    }
}