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
    private WheelsController wheelsController;
    private AeroController aeroController;
    //private TiltController tiltController;
    private ModuleEngine[] engines;

    private void Awake()
    {
        wheelsController = GetComponent<WheelsController>();
        aeroController = GetComponent<AeroController>();
        //tiltController = GetComponent<TiltController>();
        engines = GetComponentsInChildren<ModuleEngine>();
        Networker.PlaneUpdate += PlaneUpdate;
    }
    public void PlaneUpdate(Packet packet)
    {
        lastMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;

        /*
        if (landingGearLastState != (lastMessage.landingGear ? 0 : 1))
        {
            Debug.Log("Changing the landing gear state");
            landingGearLastState = lastMessage.landingGear ? 0 : 1;
            landingGear.SetState(landingGearLastState);
        }
        */

        if (wheelsController.gearAnimator.GetCurrentState() == (lastMessage.landingGear ? GearAnimator.GearStates.Extended : GearAnimator.GearStates.Retracted))
        {
            wheelsController.SetGear(lastMessage.landingGear);
        }


        aeroController.flaps = lastMessage.flaps;

        aeroController.input = new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll);
        aeroController.SetBrakes(lastMessage.breaks);

        for (int i = 0; i < engines.Length; i++)
        {
            engines[i].SetThrottle(lastMessage.throttle);
        }
    }
    public void OnDestory()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
        Debug.Log("Destroyed Plane Update");
        Debug.Log(gameObject.name);
    }
}