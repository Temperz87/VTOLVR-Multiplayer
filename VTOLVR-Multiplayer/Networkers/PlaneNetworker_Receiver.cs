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
    private bool firstMessageReceived;
    public static bool dontPrefixNextJettison = false;
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
        Networker.JettisonUpdate += JettisonUpdate;
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

        if (!firstMessageReceived)
        {
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
        else
        {
            if (lastMessage.landingGear != newMessage.landingGear)
            {
                SetLandingGear(newMessage.landingGear);
            }
            if (lastMessage.tailHook != newMessage.tailHook)
            {
                SetTailHook(newMessage.tailHook);
            }
            if (lastMessage.launchBar != newMessage.launchBar)
            {
                SetLaunchBar(newMessage.launchBar);
            }
            if (lastMessage.fuelPort != newMessage.fuelPort)
            {
                SetFuelPort(newMessage.fuelPort);
            }
            if (lastMessage.pitch != newMessage.pitch || lastMessage.yaw != newMessage.yaw || lastMessage.roll != newMessage.roll)
            {
                SetOrientation(newMessage.pitch, newMessage.yaw, newMessage.roll);
            }
            if (lastMessage.flaps != newMessage.flaps)
            {
                SetFlaps(newMessage.flaps);
            }
            if (lastMessage.brakes != newMessage.brakes)
            {
                SetBrakes(newMessage.brakes);
            }
            if (lastMessage.throttle != newMessage.throttle)
            {
                SetThrottle(newMessage.throttle);
            }

        }
        lastMessage = newMessage;
    }
    private void SetLandingGear(bool state)
    {
        if (state)
            aiPilot.gearAnimator.Extend();
        else
            aiPilot.gearAnimator.Retract();
    }
    private void SetTailHook(bool state)
    {
        if (aiPilot.tailHook != null)
        {
            if (state)
                aiPilot.tailHook.ExtendHook();
            else
                aiPilot.tailHook.RetractHook();
        }
    }
    private void SetLaunchBar(bool state)
    {
        if (aiPilot.catHook != null)
        {
            if (state)
                aiPilot.catHook.SetState(1);
            else
                aiPilot.catHook.SetState(0);
        }
    }
    private void SetFuelPort(bool state)
    {
        if (aiPilot.refuelPort != null)
        {
            if (state)
                aiPilot.refuelPort.Open();
            else
                aiPilot.refuelPort.Close();
        }
    }
    private void SetOrientation(float pitch, float yaw, float roll)
    {
        for (int i = 0; i < autoPilot.outputs.Length; i++)
        {
            autoPilot.outputs[i].SetPitchYawRoll(new Vector3(pitch, yaw, roll));
            autoPilot.outputs[i].SetWheelSteer(yaw);
        }
    }
    private void SetFlaps(float flaps)
    {
        for (int i = 0; i < autoPilot.outputs.Length; i++)
        {
            autoPilot.outputs[i].SetFlaps(flaps);
        }
    }
    private void SetBrakes(float brakes)
    {
        for (int i = 0; i < autoPilot.outputs.Length; i++)
        {
            autoPilot.outputs[i].SetBrakes(brakes);
        }
    }
    private void SetThrottle(float throttle)
    {
        for (int i = 0; i < autoPilot.engines.Count; i++)
        {
            autoPilot.engines[i].SetThrottle(throttle);
        }
    }
    public void WeaponSet_Result(Packet packet)
    {
        Message_WeaponSet_Result message = (Message_WeaponSet_Result)((PacketSingle)packet).message;
        if (message.UID != networkUID)
            return;

        if (Networker.isHost && packet.networkUID != networkUID)
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
                        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(PlayerManager.GetPlayerCSteamID(message.UID),
                            new Message_RequestNetworkUID(clientsUID, message.hpLoadout[i].missileUIDS[j]),
                            EP2PSend.k_EP2PSendReliable);
                    }
                }
            }
        }

        PlaneEquippableManager.SetLoadout(gameObject, networkUID, message.normalizedFuel, message.hpLoadout, message.cmLoadout);

        if (Networker.isHost)
        {
            NetworkSenderThread.Instance.SendPacketAsHostToAllButOneSpecificClient(PlayerManager.GetPlayerCSteamID(message.UID),
                message,
                Steamworks.EP2PSend.k_EP2PSendReliable);
        }
    }

    private void JettisonUpdate(Packet packet)
    {
        Message_JettisonUpdate message = ((PacketSingle)packet).message as Message_JettisonUpdate;
        if (message.networkUID != networkUID)
            return;
        if (message.toJettison == null)
        {
            Debug.LogError("Why did we get a jettison message that want's to jettison nothing?");
            return;
        }
        foreach (var idx in message.toJettison)
        {
            HPEquippable equip = weaponManager.GetEquip(idx);
            if (equip != null)
                equip.markedForJettison = true;
        }
        dontPrefixNextJettison = true;
        weaponManager.JettisonMarkedItems();
    }
    public void WeaponFiring(Packet packet)
    {
        Message_WeaponFiring message = ((PacketSingle)packet).message as Message_WeaponFiring;
        if (message.UID != networkUID)
            return;
        idx = (int)traverse.Field("weaponIdx").GetValue();
        int i = 0;
        Debug.Log("Entering for loop");
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
            Debug.Log("couldn't change weapon idx to right weapon for aircraft " + gameObject.name);
        }
        if (message.isFiring != weaponManager.isFiring)
        {
            if (message.isFiring)
            {
                if (weaponManager.isMasterArmed == false)
                {
                    weaponManager.ToggleMasterArmed();
                }
                /*InternalWeaponBay bay =  weaponManager.GetIWBForEquip(weaponManager.currentEquip.hardpointIdx);

                    if(bay)
                    {
                        bay.RegisterOpenReq(weaponManager.currentEquip);
                    }*/

                if (weaponManager.currentEquip is HPEquipIRML || weaponManager.currentEquip is HPEquipRadarML || weaponManager.currentEquip is RocketLauncher)
                {
                    weaponManager.SingleFire();
                }
                else
                {
                    Debug.Log("try start fire for vehicle" + gameObject.name + " on current equip " + weaponManager.currentEquip);
                    weaponManager.StartFire();
                }
            }
            else
            {
                /*InternalWeaponBay bay = weaponManager.GetIWBForEquip(weaponManager.currentEquip.hardpointIdx);
                if (bay)
                {
                    bay.UnregisterOpenReq(weaponManager.currentEquip);
                }*/

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

        return PlaneEquippableManager.generateHpInfoListFromWeaponManager(weaponManager,
            PlaneEquippableManager.HPInfoListGenerateNetworkType.receiver).ToArray();
    }
    public int[] GetCMS()
    {
        //There is only ever 2 counter measures, thats why it's hard coded.
        return PlaneEquippableManager.generateCounterMeasuresFromCmManager(cmManager).ToArray();
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