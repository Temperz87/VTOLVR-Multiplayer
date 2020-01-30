using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
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
    private void Awake()
    {
        aiPilot = GetComponent<AIPilot>();
        autoPilot = aiPilot.autoPilot;
        Networker.PlaneUpdate += PlaneUpdate;
        Networker.WeaponSet_Result += WeaponSet_Result;
        Networker.Disconnecting += OnDisconnect;

        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError("Weapon Manager was null on " + gameObject.name);
        cmManager = GetComponentInChildren<CountermeasureManager>();
        if (cmManager == null)
            Debug.LogError("CountermeasureManager was null on " + gameObject.name);
        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on " + gameObject.name);
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
    }
    public void WeaponSet_Result(Packet packet)
    {
        Message_WeaponSet_Result message = (Message_WeaponSet_Result)((PacketSingle)packet).message;
        if (message.UID != networkUID)
            return;
        Loadout loadout = new Loadout();
        loadout.hpLoadout = message.hpLoadout;
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
    public void OnDisconnect(Packet packet)
    {
        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        if (message.UID != networkUID)
            return;

        
        Destroy(gameObject);
    }
    public void OnDestory()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
        Networker.Disconnecting -= OnDisconnect;
        Networker.WeaponSet_Result -= WeaponSet_Result;
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