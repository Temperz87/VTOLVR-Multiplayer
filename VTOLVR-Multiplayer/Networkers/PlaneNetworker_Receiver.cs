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

    //Classes we use to set the information
    private AIPilot aiPilot;
    private AutoPilot autoPilot;

    private WeaponManager weaponManager;
    private CountermeasureManager cmManager;
    private FuelTank fuelTank;
    private Traverse traverse;
    // private RadarLockData radarLockData;
    private void Awake()
    {
        aiPilot = GetComponent<AIPilot>();
        autoPilot = aiPilot.autoPilot;
        Networker.PlaneUpdate += PlaneUpdate;
        Networker.WeaponSet_Result += WeaponSet_Result;
        Networker.Disconnecting += OnDisconnect;
        Networker.WeaponFiring += WeaponFiring;
        Networker.WeaponStoppedFiring += WeaponStoppedFiring;
        Networker.FireCountermeasure += FireCountermeasure;

        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Weapon Manager was null on " + gameObject.name);
        cmManager = GetComponentInChildren<CountermeasureManager>();
        if (cmManager == null)
            Debug.LogError("CountermeasureManager was null on " + gameObject.name);
        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on " + gameObject.name);

        traverse = Traverse.Create(weaponManager);
    }
    public void PlaneUpdate(Packet packet)
    {
        lastMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        if (lastMessage.landingGear)
            aiPilot.gearAnimator.Extend();
        else
            aiPilot.gearAnimator.Retract();
        if (lastMessage.tailHook && !aiPilot.tailHook.isDeployed)
            aiPilot.tailHook.ExtendHook();
        else if (!lastMessage.tailHook && aiPilot.tailHook.isDeployed)
            aiPilot.tailHook.RetractHook();
        if (lastMessage.launchBar && !aiPilot.catHook.deployed)
            aiPilot.catHook.SetState(1);
        else if (!lastMessage.launchBar && aiPilot.catHook.deployed)
            aiPilot.catHook.SetState(0);
        for (int i = 0; i < autoPilot.outputs.Length; i++)
        {
            autoPilot.outputs[i].SetPitchYawRoll(new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll));
            autoPilot.outputs[i].SetBrakes(lastMessage.breaks);            
            autoPilot.outputs[i].SetFlaps(lastMessage.flaps);
            autoPilot.outputs[i].SetWheelSteer(lastMessage.yaw);
        }
        for (int i = 0; i < autoPilot.engines.Count; i++)
        {
            autoPilot.engines[i].SetThrottle(lastMessage.throttle);
        }
        /*if (lastMessage.hasRadar)
        {
            if (lastMessage.radarLock != radarLockData.actor.actorID)
            {
                if (!lastMessage.locked)
                {
                    aiPilot.wm.lockingRadar.Unlock();
                }
                else
                {
                    aiPilot.wm.lockingRadar.ForceLock(lastMessage.radarLock.actor, out radarLockData);
                }
            }
        }*/
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
        if (message.weaponIdx != (int)traverse.Field("weaponIdx").GetValue())
        {
            traverse.Field("weaponIdx").SetValue(message.weaponIdx);
        }
        if (message.isFiring != weaponManager.isFiring)
        {
            weaponManager.StartFire();
        }
        /*To switch weapon, we go back one previous one on the index and then toggle it to go forward one.
        //So that everything gets called which is in the normal game.
        List<string> uniqueWeapons = traverse.Field("uniqueWeapons").GetValue() as List<string>;
        traverse.Field("weaponIdx").SetValue((message.weaponIdx - 1) % uniqueWeapons.Count);
        weaponManager.CycleActiveWeapons(); //Should move it one forward to the same weapon
        weaponManager.SetMasterArmed(true);
        weaponManager.StartFire();*/
    }
    public void WeaponStoppedFiring(Packet packet)
    {
        Message_WeaponStoppedFiring message = ((PacketSingle)packet).message as Message_WeaponStoppedFiring;
        if (message.UID != networkUID)
            return;
        weaponManager.EndFire();
    }

    public void FireCountermeasure(Packet packet) // chez
    {
        Debug.Log("Recieving CM Messsage");
        Message_FireCountermeasure message = ((PacketSingle)packet).message as Message_FireCountermeasure;
        //if (message.UID != networkUID)
        //    return;
        Debug.Log("FIRING CMS!");
        aiPilot.aiSpawn.CountermeasureProgram(message.flares, message.chaff, 1, 0.1f);
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

        
        Destroy(gameObject);
    }
    public void OnDestroy()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
        Networker.Disconnecting -= OnDisconnect;
        Networker.WeaponSet_Result -= WeaponSet_Result;
        Networker.WeaponFiring -= WeaponFiring;
        Networker.WeaponStoppedFiring -= WeaponStoppedFiring;
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