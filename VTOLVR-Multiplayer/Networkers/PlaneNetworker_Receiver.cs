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
    private void Awake()
    {
        aiPilot = GetComponent<AIPilot>();
        autoPilot = aiPilot.autoPilot;
        Networker.PlaneUpdate += PlaneUpdate;
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
    public void OnDisconnect(Packet packet)
    {
        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        if (message.UID != networkUID)
            return;

        Networker.Disconnecting -= OnDisconnect;
        Destroy(gameObject);
    }
    public void OnDestory()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
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