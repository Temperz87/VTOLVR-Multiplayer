using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
public class PlaneNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;

    //Classes we use to find the information out
    private WheelsController wheelsController;
    private AeroController aeroController;
    private VRThrottle vRThrottle;

    private WeaponManager weaponManager;
    private CountermeasureManager cmManager;
    private FuelTank fuelTank;
    private Traverse traverse;

    private bool previousFiringState;

    private Message_PlaneUpdate lastMessage;
    private Message_WeaponFiring lastFiringMessage;
    private Message_WeaponStoppedFiring lastStoppedFiringMessage;

    private void Awake()
    {
        
        lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, networkUID);
        lastFiringMessage = new Message_WeaponFiring(-1, networkUID);
        lastStoppedFiringMessage = new Message_WeaponStoppedFiring(networkUID);


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
        fuelTank = GetComponent<FuelTank>();
        if (fuelTank == null)
            Debug.LogError("FuelTank was null on our vehicle");

        Networker.WeaponSet += WeaponSet;

        traverse = Traverse.Create(weaponManager);
        Debug.Log("Done Plane Sender");
    }

    private void Update()
    {
        if (weaponManager.isFiring != previousFiringState)
        {
            previousFiringState = weaponManager.isFiring;
            lastFiringMessage.weaponIdx = (int)traverse.Field("weaponIdx").GetValue();
            Debug.Log("combinedWeaponIdx = " + lastFiringMessage.weaponIdx);
            lastFiringMessage.UID = networkUID;
            lastStoppedFiringMessage.UID = networkUID;
            if (weaponManager.isFiring)
            {
                if (Networker.isHost)
                    Networker.SendGlobalP2P(lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                else
                    Networker.SendP2P(Networker.hostID,lastFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            else
            {
                if (Networker.isHost)
                    Networker.SendGlobalP2P(lastStoppedFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
                else
                    Networker.SendP2P(Networker.hostID, lastStoppedFiringMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
            }
            
        }
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

        if (Networker.isHost)
            Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
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
        List<HPInfo> hpInfos = new List<HPInfo>();
        List<int> cm = new List<int>();
        float fuel = 0.65f;

        HPEquippable lastEquippable = null;
        for (int i = 0; i < weaponManager.equipCount; i++)
        {
            lastEquippable = weaponManager.GetEquip(i);
            List<ulong> missileUIDS = new List<ulong>();
            if (lastEquippable.weaponType != HPEquippable.WeaponTypes.Gun &&
                lastEquippable.weaponType != HPEquippable.WeaponTypes.Rocket)
            {
                HPEquipMissileLauncher HPml = lastEquippable as HPEquipMissileLauncher;
                if (HPml.ml == null) //I think this could be null.
                    break;
                for (int j = 0; j < HPml.ml.missiles.Length; j++)
                {
                    //If they are null, they have been shot.
                    if (HPml.ml.missiles[i] == null)
                    {
                        missileUIDS.Add(0);
                        continue;
                    }

                    MissileNetworker_Sender sender = HPml.ml.missiles[i].gameObject.GetComponent<MissileNetworker_Sender>();
                    if (sender != null)
                        missileUIDS.Add(sender.networkUID);
                    else
                        Debug.LogError($"Failed to get NetworkUID for missile ({HPml.ml.missiles[i].gameObject.name})");
                }
            }

            hpInfos.Add(new HPInfo(
                    lastEquippable.gameObject.name.Replace("(Clone)", ""),
                    lastEquippable.weaponType,
                    missileUIDS.ToArray()));
        }

        for (int i = 0; i < cmManager.countermeasures.Count; i++)
        {
            cm.Add(cmManager.countermeasures[i].count);
        }

        fuel = fuelTank.fuel / fuelTank.totalFuel;

        Networker.SendP2P(Networker.hostID,
            new Message_WeaponSet_Result(hpInfos.ToArray(), cm.ToArray(), fuel, networkUID),
            Steamworks.EP2PSend.k_EP2PSendReliable);
    }
    public void OnDestory()
    {
        Networker.WeaponSet -= WeaponSet;
    }
}