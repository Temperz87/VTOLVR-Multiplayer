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
    private WheelsController wheelsController;
    private AeroController aeroController;
    private VRThrottle vRThrottle;
    private Health health;
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
    private bool lastFiring = false;
    private string radarLock;
    private void Awake()
    {
        if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.AV42C)
        {
            lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, false, networkUID, false, false, radarLock);
        }
        else
        {
            lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, false, networkUID, true, false, radarLock);
        }
        lastFiringMessage = new Message_WeaponFiring(-1, false, networkUID);
        // lastStoppedFiringMessage = new Message_WeaponStoppedFiring(networkUID);
        lastCountermeasureMessage = new Message_FireCountermeasure(true, true, networkUID);
        lastDeathMessage = new Message_Death(networkUID);
        wheelsController = GetComponent<WheelsController>();
        aeroController = GetComponent<AeroController>();
        vRThrottle = gameObject.GetComponentInChildren<VRThrottle>();
        if (vRThrottle == null)
            Debug.Log("Cound't find throttle");
        else
            vRThrottle.OnSetThrottle.AddListener(SetThrottle);

        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Weapon Manager was null on our vehicle");

        cmManager = GetComponentInChildren<CountermeasureManager>();
        if (cmManager == null)
            Debug.LogError("CountermeasureManager was null on our vehicle");
        else
            cmManager.OnFiredCM += FireCountermeasure;

        health = GetComponent<Health>();
        if (health == null)
            Debug.LogError("health was null on our vehicle");
        else
            health.OnDeath.AddListener(Death);

        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on our vehicle");

        Networker.WeaponSet += WeaponSet;

        traverse = Traverse.Create(weaponManager);
        Debug.Log("Done Plane Sender");
        tailhook = GetComponentInChildren<Tailhook>();
        launchBar = GetComponentInChildren<CatapultHook>();
        refuelPort = GetComponentInChildren<RefuelPort>();
    }

    private void Update()
    {
        if (weaponManager.isFiring != previousFiringState | lastIdx != (int)traverse.Field("weaponIdx").GetValue())
        {
            previousFiringState = weaponManager.isFiring;
            lastFiringMessage.weaponIdx = (int)traverse.Field("weaponIdx").GetValue();
            lastIdx = lastFiringMessage.weaponIdx;
            Debug.Log("combinedWeaponIdx = " + lastFiringMessage.weaponIdx);
            lastFiringMessage.UID = networkUID;
            // lastStoppedFiringMessage.UID = networkUID;
            lastFiringMessage.isFiring = weaponManager.isFiring;
            if (Networker.isHost)
                Networker.SendGlobalP2P(lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
            else
                Networker.SendP2P(Networker.hostID, lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
        /*if (weaponManager.isFiring != previousFiringState)
        {
            previousFiringState = weaponManager.isFiring;
            lastFiringMessage.weaponIdx = (int)traverse.Field("weaponIdx").GetValue();
            Debug.Log("combinedWeaponIdx = " + lastFiringMessage.weaponIdx);
            lastFiringMessage.UID = networkUID;
            lastStoppedFiringMessage.UID = networkUID;
            if (weaponManager.isFiring | )
            {
                if (Networker.isHost)
                    Networker.SendGlobalP2P(lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                else
                    Networker.SendP2P(Networker.hostID,lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            else
            {
                if (Networker.isHost)
                {
                    Debug.Log("Sending packet as host");
                    Networker.SendGlobalP2P(lastStoppedFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                }
                else
                {
                    Debug.Log("Sending packet as client");
                    Networker.SendP2P(Networker.hostID, lastStoppedFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                }
            }
        }*/
    }

    private void LateUpdate()
    {
        lastMessage.flaps = aeroController.flaps;
        lastMessage.pitch = Mathf.Round(aeroController.input.x * 100000f) / 100000f;
        lastMessage.yaw = Mathf.Round(aeroController.input.y * 100000f) / 100000f;
        lastMessage.roll = Mathf.Round(aeroController.input.z * 100000f) / 100000f;
        lastMessage.breaks = aeroController.brake;

        lastMessage.landingGear = LandingGearState();
        lastMessage.networkUID = networkUID;
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
            Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
        {
            Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
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

        Networker.SendP2P(Networker.hostID,
            new Message_WeaponSet_Result(hpInfos.ToArray(), cm.ToArray(), fuel, networkUID),
            Steamworks.EP2PSend.k_EP2PSendReliable);
    }
    public void FireCountermeasure() {
        lastCountermeasureMessage.UID = networkUID;
        if (Networker.isHost)
            Networker.SendGlobalP2P(lastCountermeasureMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        else
            Networker.SendP2P(Networker.hostID, lastCountermeasureMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
    }
    public void Death()
    {
        lastDeathMessage.UID = networkUID;
        if (Networker.isHost)
            Networker.SendGlobalP2P(lastDeathMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        else
            Networker.SendP2P(Networker.hostID, lastDeathMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
    }
    public void OnDestroy()
    {
        Networker.WeaponSet -= WeaponSet;
    }
}