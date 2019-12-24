using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlaneNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_PlaneUpdate lastMessage;

    //Classes we use to set the information
    private WheelsController wheelsController;
    private ModuleEngine[] engines;
    private FlightAssist flightAssist;
    private void Awake()
    {
        wheelsController = GetComponent<WheelsController>();
        engines = GetComponentsInChildren<ModuleEngine>();
        flightAssist = GetComponent<FlightAssist>();
        flightAssist.assistEnabled = true;
    }
    public void PlaneUpdate(Packet packet)
    {
        lastMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;
        Debug.Log($"Plane Update\nOur Network ID = {networkUID} Packet Network ID = {lastMessage.networkUID}");
        if (lastMessage.networkUID != networkUID)
            return;
        Debug.Log("Received\n" + lastMessage.ToString());

        if (wheelsController.gearAnimator.GetCurrentState() == (lastMessage.landingGear ? GearAnimator.GearStates.Extended : GearAnimator.GearStates.Retracted))
        {
            wheelsController.SetGear(lastMessage.landingGear);
        }

        flightAssist.SetFlaps(lastMessage.flaps);
        flightAssist.SetPitchYawRoll(new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll));
        flightAssist.SetBrakes(lastMessage.breaks);

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