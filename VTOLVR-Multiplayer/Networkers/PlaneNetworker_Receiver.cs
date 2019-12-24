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
    private LandingGearLever landingGear;
    private VRThrottle throttle;
    private FlightAssist flightAssist;
    private Traverse traverse;

    private int landingGearLastState = 0;
    private void Awake()
    {
        landingGear = GetComponentInChildren<LandingGearLever>();
        throttle = GetComponentInChildren<VRThrottle>();
        traverse = Traverse.Create(throttle);
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

        if (landingGearLastState != (lastMessage.landingGear ? 0 : 1))
        {
            landingGearLastState = lastMessage.landingGear ? 0 : 1;
            landingGear.SetState(landingGearLastState);
        }

        flightAssist.SetFlaps(lastMessage.flaps);
        flightAssist.SetPitchYawRoll(new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll));
        flightAssist.SetBrakes(lastMessage.breaks);

        traverse.Method("UpdateThrottle", new object[] { lastMessage.throttle}).GetValue();
    }
    public void OnDestory()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
        Debug.Log("Destroyed Plane Update");
        Debug.Log(gameObject.name);
    }
}