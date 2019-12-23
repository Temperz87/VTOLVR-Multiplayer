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
    private AeroController aeroController;
    private ModuleEngine[] engines;
    private void Awake()
    {
        wheelsController = GetComponent<WheelsController>();
        aeroController = GetComponent<AeroController>();
        engines = GetComponentsInChildren<ModuleEngine>();
    }
    public void PlaneUpdate(Packet packet)
    {
        Debug.Log($"Plane Update\nOur Network ID = {networkUID} Packet Network ID = {packet.networkUID}");
        if (packet.networkUID != networkUID)
            return;
        lastMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;

        if (wheelsController.gearAnimator.GetCurrentState() == (lastMessage.landingGear ? GearAnimator.GearStates.Extended : GearAnimator.GearStates.Retracted))
        {
            wheelsController.SetGear(lastMessage.landingGear);
        }

        if (aeroController.flaps != lastMessage.flaps)
            aeroController.SetFlaps(lastMessage.flaps);
        if (aeroController.input != new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll))
            aeroController.input = new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll);
        if (aeroController.brake != lastMessage.breaks)
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