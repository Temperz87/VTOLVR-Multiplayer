using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

    private Message_PlaneUpdate lastMessage;

    private void Awake()
    {
        
        lastMessage = new Message_PlaneUpdate(false, 0, 0, 0, 0, 0, 0, false, false, networkUID);

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
        Debug.Log("Done Plane Sender");
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
        List<string> hps = new List<string>();
        List<int> cm = new List<int>();
        float fuel = 0.65f;

        for (int i = 0; i < weaponManager.equipCount; i++)
        {
            //I am hoping the HPS is '<name>(Clone' which is the default unity spawn name. 
            //So then the <name> should match what it is saved as in the resources folder.
            hps.Add(weaponManager.GetEquip(i).gameObject.name.Replace("(Clone)",""));
        }

        for (int i = 0; i < cmManager.countermeasures.Count; i++)
        {
            cm.Add(cmManager.countermeasures[i].count);
        }

        fuel = fuelTank.fuel / fuelTank.totalFuel;

        Networker.SendP2P(Networker.hostID,
            new Message_WeaponSet_Result(hps.ToArray(), cm.ToArray(), fuel, networkUID),
            Steamworks.EP2PSend.k_EP2PSendReliable);
    }
    public void OnDestory()
    {
        Networker.WeaponSet -= WeaponSet;
    }
}