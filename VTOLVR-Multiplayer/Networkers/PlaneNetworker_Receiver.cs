using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using Steamworks;

public class PlaneNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_PlaneUpdate lastMessage;
    bool firstMessageReceived;

    //Classes we use to set the information
    private AIPilot aiPilot;
    private AutoPilot autoPilot;
    private WeaponManager weaponManager;
    private CountermeasureManager cmManager;
    private FuelTank fuelTank;
    private Traverse traverse;
    private int idx;
    // private RadarLockData radarLockData;
    private ulong mostCurrentUpdateNumber;
    private void Awake()
    {
        firstMessageReceived = false;
        aiPilot = GetComponent<AIPilot>();
        autoPilot = aiPilot.autoPilot;
        aiPilot.enabled = false;
        Networker.PlaneUpdate += PlaneUpdate;
        Networker.WeaponSet_Result += WeaponSet_Result;
        Networker.Disconnecting += OnDisconnect;
        Networker.WeaponFiring += WeaponFiring;
        // Networker.WeaponStoppedFiring += WeaponStoppedFiring;
        Networker.FireCountermeasure += FireCountermeasure;
        weaponManager = GetComponent<WeaponManager>();
        mostCurrentUpdateNumber = 0;
        if (weaponManager == null)
            Debug.LogError("Weapon Manager was null on " + gameObject.name);
        else
            traverse = Traverse.Create(weaponManager);
        cmManager = GetComponentInChildren<CountermeasureManager>();
        if (cmManager == null)
            Debug.LogError("CountermeasureManager was null on " + gameObject.name);
        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on " + gameObject.name);
    }
    public void PlaneUpdate(Packet packet)
    {
        Message_PlaneUpdate newMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;

        if (newMessage.networkUID != networkUID)
            return;
        // If already received this message or a newer one, don't need to update
        if (newMessage.sequenceNumber <= mostCurrentUpdateNumber)
            return;

        mostCurrentUpdateNumber = newMessage.sequenceNumber;

        if (!firstMessageReceived) {
            firstMessageReceived = true;
            SetLandingGear(newMessage.landingGear);
            SetTailHook(newMessage.tailHook);
            SetLaunchBar(newMessage.launchBar);
            SetFuelPort(newMessage.fuelPort);
            SetOrientation(newMessage.pitch, newMessage.yaw, newMessage.roll);
            SetFlaps(newMessage.flaps);
            SetBrakes(newMessage.brakes);
            SetThrottle(newMessage.throttle);
        }
        else {
            if (lastMessage.landingGear != newMessage.landingGear) {
                SetLandingGear(newMessage.landingGear);
            }
            if (lastMessage.tailHook != newMessage.tailHook) {
                SetTailHook(newMessage.tailHook);
            }
            if (lastMessage.launchBar != newMessage.launchBar) {
                SetLaunchBar(newMessage.launchBar);
            }
            if (lastMessage.fuelPort != newMessage.fuelPort) {
                SetFuelPort(newMessage.fuelPort);
            }
            if (lastMessage.pitch != newMessage.pitch || lastMessage.yaw != newMessage.yaw || lastMessage.roll != newMessage.roll) {
                SetOrientation(newMessage.pitch, newMessage.yaw, newMessage.roll);
            }
            if (lastMessage.flaps != newMessage.flaps) {
                SetFlaps(newMessage.flaps);
            }
            if (lastMessage.brakes != newMessage.brakes) {
                SetBrakes(newMessage.brakes);
            }
            if (lastMessage.throttle != newMessage.throttle) {
                SetThrottle(newMessage.throttle);
            }

        }
        lastMessage = newMessage;
    }
    private void SetLandingGear(bool state) {
        if (state)
            aiPilot.gearAnimator.Extend();
        else
            aiPilot.gearAnimator.Retract();
    }
    private void SetTailHook(bool state) {
        if (aiPilot.tailHook != null) {
            if (state)
                aiPilot.tailHook.ExtendHook();
            else
                aiPilot.tailHook.RetractHook();
        }
    }
    private void SetLaunchBar(bool state) {
        if (aiPilot.catHook != null) {
            if (state)
                aiPilot.catHook.SetState(1);
            else
                aiPilot.catHook.SetState(0);
        }
    }
    private void SetFuelPort(bool state) {
        if (aiPilot.refuelPort != null) {
            if (state)
                aiPilot.refuelPort.Open();
            else
                aiPilot.refuelPort.Close();
        }
    }
    private void SetOrientation(float pitch, float yaw, float roll) {
        for (int i = 0; i < autoPilot.outputs.Length; i++) {
            autoPilot.outputs[i].SetPitchYawRoll(new Vector3(pitch, yaw, roll));
            autoPilot.outputs[i].SetWheelSteer(yaw);
        }
    }
    private void SetFlaps(float flaps) {
        for (int i = 0; i < autoPilot.outputs.Length; i++) {
            autoPilot.outputs[i].SetFlaps(flaps);
        }
    }
    private void SetBrakes(float brakes) {
        for (int i = 0; i < autoPilot.outputs.Length; i++) {
            autoPilot.outputs[i].SetBrakes(brakes);
        }
    }
    private void SetThrottle(float throttle) {
        for (int i = 0; i < autoPilot.engines.Count; i++) {
            autoPilot.engines[i].SetThrottle(throttle);
        }
    }

    public void WeaponSet_Result(Packet packet)
    {
        Message_WeaponSet_Result message = (Message_WeaponSet_Result)((PacketSingle)packet).message;
        if (message.UID != networkUID)
            return;

        List<string> hpLoadoutNames = new List<string>();
        for (int i = 0; i < message.hpLoadout.Length; i++)
        {
            hpLoadoutNames.Add(message.hpLoadout[i].hpName);
        }


        Loadout loadout = new Loadout();
        loadout.hpLoadout = hpLoadoutNames.ToArray();
        loadout.cmLoadout = message.cmLoadout;
        loadout.normalizedFuel = message.normalizedFuel;
        if (weaponManager == null)
        {
            Debug.LogError("Weapon set was called this vehicle which has a null weapon manager " + gameObject.name);
        }
        weaponManager.EquipWeapons(loadout);

        for (int i = 0; i < cmManager.countermeasures.Count; i++)
        {
            //There should only ever be two counter measures.
            //So the second array in message should be fine.
            cmManager.countermeasures[i].count = message.cmLoadout[i];
        }

        fuelTank.startingFuel = loadout.normalizedFuel;
        fuelTank.SetNormFuel(loadout.normalizedFuel);

    }
    public void WeaponFiring(Packet packet)
    {
        Message_WeaponFiring message = ((PacketSingle)packet).message as Message_WeaponFiring;
        if (message.UID != networkUID)
            return;
        idx = (int)traverse.Field("weaponIdx").GetValue();
        int i = 0;
        while (message.weaponIdx != idx && i < 60)
        {
            if (weaponManager.isMasterArmed == false)
            {
                weaponManager.ToggleMasterArmed();
            }
            weaponManager.CycleActiveWeapons(false);
            idx = (int)traverse.Field("weaponIdx").GetValue();
            // Debug.Log(idx + " " + message.weaponIdx);
            i++;
        }
        if (i > 59)
        {
            Debug.Log("couldn't change weapon idx to right weapon for aircraft " + gameObject.name) ;
        }
        if (message.isFiring != weaponManager.isFiring)
        {
            if (message.isFiring)
            {
                if (weaponManager.isMasterArmed == false)
                {
                    weaponManager.ToggleMasterArmed();
                }
                if (weaponManager.currentEquip is HPEquipIRML || weaponManager.currentEquip is RocketLauncher)
                {
                    weaponManager.SingleFire();
                }
                else if (weaponManager.currentEquip is HPEquipRadarML missileLauncher)
                {
                    //weaponManager.SingleFire();
                    LockingRadarNetworker_Receiver plane_radar = weaponManager.gameObject.GetComponent<LockingRadarNetworker_Receiver>();

                    if (!plane_radar.lockingRadar.IsLocked() && plane_radar.lastLock != 0) {
                        foreach (var AI in AIManager.AIVehicles) {
                            if (AI.vehicleUID == plane_radar.lastLock) {
                                plane_radar.lockingRadar.ForceLock(AI.actor, out plane_radar.radarLockData);
                                if (plane_radar.lockingRadar.IsLocked()) {
                                    plane_radar.lastLock = AI.vehicleUID;
                                }
                                break;
                            }
                        }
                    }

                    if (weaponManager.currentEquip.armable) {
                        if (!weaponManager.currentEquip.armed) {
                            Debug.Log("Radar missile is not armed");
                        }
                    }
                    
                    if (!weaponManager.currentEquip.itemActivated) {
                        Debug.Log("Radar missile not activated");
                    }

                    if (false == missileLauncher.LaunchAuthorized()) {
                        string notAuthorizedToLaunch = "Radar missile not authorized to launch, reason: ";
                        bool additionalReason = false;
                        if (missileLauncher.ml.missileCount == 0) {
                            notAuthorizedToLaunch += "missileCount is zero";
                            additionalReason = true;
                        }
                        if (weaponManager.lockingRadar == null) {
                            if (additionalReason) {
                                notAuthorizedToLaunch += ", ";
                            }
                            notAuthorizedToLaunch += "lockingRadar is null";
                            additionalReason = true;
                        }
                        if (weaponManager.lockingRadar != null && !weaponManager.lockingRadar.IsLocked()) {
                            if (additionalReason) {
                                notAuthorizedToLaunch += ", ";
                            }
                            notAuthorizedToLaunch += "radar is not locked";
                            additionalReason = true;
                        }
                        if (!missileLauncher.dlz.inRangeMax) {
                            if (additionalReason) {
                                notAuthorizedToLaunch += ", ";
                            }
                            notAuthorizedToLaunch += "target is not in range";
                        }
                        Debug.Log(notAuthorizedToLaunch);
                        weaponManager.SingleFire();
                    }
                    else {
                        Debug.Log("Trying to fire radar missile from weapon firing message");
                        weaponManager.SingleFire();
                    }
                }
                else
                {
                    Debug.Log("try start fire for vehicle" + gameObject.name +" on current equip " + weaponManager.currentEquip);
                    weaponManager.StartFire();
                }
            }
            else
            {
                if (!(weaponManager.currentEquip is HPEquipIRML || weaponManager.currentEquip is RocketLauncher)) // I removed one for radar code because it's shooting missiles twice
                    weaponManager.EndFire();
            }
        }
    }
    public void FireCountermeasure(Packet packet) // chez
    {
        Message_FireCountermeasure message = ((PacketSingle)packet).message as Message_FireCountermeasure;
        if (message.UID != networkUID)
            return;
        aiPilot.aiSpawn.CountermeasureProgram(message.flares, message.chaff, 2, 0.1f);
    }

    public HPInfo[] GenerateHPInfo()
    {
        if (!Networker.isHost)
        {
            Debug.LogError("Generate HPInfo was ran from a player which isn't the host.");
            return null;
        }

        return VTOLVR_Multiplayer.PlaneEquippableManager.generateHpInfoListFromWeaponManager(weaponManager, 
            VTOLVR_Multiplayer.PlaneEquippableManager.HPInfoListGenerateNetworkType.receiver).ToArray();
    }
    public int[] GetCMS()
    {
        //There is only ever 2 counter measures, thats why it's hard coded.
        return VTOLVR_Multiplayer.PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager).ToArray();
    }
    public float GetFuel()
    {
        return fuelTank.fuel;
    }
    public void OnDisconnect(Packet packet)
    {
        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        if (message.UID != networkUID)
            return;

        firstMessageReceived = false;
        Destroy(gameObject);
    }
    public void OnDestroy()
    {
        firstMessageReceived = false;
        Networker.PlaneUpdate -= PlaneUpdate;
        Networker.Disconnecting -= OnDisconnect;
        Networker.WeaponSet_Result -= WeaponSet_Result;
        Networker.WeaponFiring -= WeaponFiring;
        // Networker.WeaponStoppedFiring -= WeaponStoppedFiring;
        Networker.FireCountermeasure -= FireCountermeasure;
        Debug.Log("Destroyed Plane Update");
        Debug.Log(gameObject.name);
    }
}

[HarmonyPatch(typeof(AutoPilot))]
[HarmonyPatch("UpdateAutopilot")]
public class Patch0
{
    public static bool Prefix(AutoPilot __instance, float deltaTime)
    {
        bool result = !__instance.gameObject.name.Contains("Client [");
        return result;
    }
}