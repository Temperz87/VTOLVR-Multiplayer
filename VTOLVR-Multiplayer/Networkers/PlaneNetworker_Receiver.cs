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
    private ModuleEngine[] engines;
    private AIPilot aiPilot;

    private void Awake()
    {
        aeroController = GetComponent<AeroController>();
        aeroController.battery = GetComponentInChildren<Battery>();
        aiPilot = GetComponent<AIPilot>();
        Debug.Log($"There are {aiPilot.autoPilot.outputs.Length} outputs");
        for (int i = 0; i < aiPilot.autoPilot.outputs.Length; i++)
        {
            Debug.Log($"Output {i} name = {aiPilot.autoPilot.outputs[i].gameObject.name}");
        }
        Networker.PlaneUpdate += PlaneUpdate;
    }
    public void PlaneUpdate(Packet packet)
    {
        lastMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        aiPilot.commandState = AIPilot.CommandStates.Override;
        //aiPilot.autoPilot.steerMode = AutoPilot.SteerModes.Aim;

        if (lastMessage.landingGear)
            aiPilot.gearAnimator.Extend();
        else
            aiPilot.gearAnimator.Retract();

        aiPilot.autoPilot.SetFlaps(lastMessage.flaps);

        aiPilot.autoPilot.OverrideSetBrakes(lastMessage.breaks);
        aiPilot.autoPilot.OverrideSetThrottle(lastMessage.throttle);

        aeroController.input = new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll);
    }
    public void OnDestory()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
        Debug.Log("Destroyed Plane Update");
        Debug.Log(gameObject.name);
    }
}