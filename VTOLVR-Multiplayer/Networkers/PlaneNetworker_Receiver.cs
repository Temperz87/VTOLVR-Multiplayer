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
    private FlapsLever flaps;
    private VRJoystick joystick;
    private FlightAssist flightAssist;

    private Traverse throttleTraverse;

    private int landingGearLastState = 0;
    private void Awake()
    {
        landingGear = GetComponentInChildren<LandingGearLever>();
        flaps = GetComponentInChildren<FlapsLever>();
        joystick = GetComponentInChildren<VRJoystick>();

        throttle = GetComponentInChildren<VRThrottle>();
        throttleTraverse = Traverse.Create(throttle);
        flightAssist = GetComponent<FlightAssist>();
        flightAssist.assistEnabled = true;
    }
    public void PlaneUpdate(Packet packet)
    {
        lastMessage = (Message_PlaneUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        Debug.Log("Received Plane Update\n" + lastMessage.ToString());

        if (landingGearLastState != (lastMessage.landingGear ? 0 : 1))
        {
            Debug.Log("Changing the landing gear state");
            landingGearLastState = lastMessage.landingGear ? 0 : 1;
            landingGear.SetState(landingGearLastState);
        }
        

        switch (lastMessage.flaps)
        {
            case 0:
                flaps.SetState(0);
                Debug.Log("Setting flaps to 0");
                break;
            case 0.5f:
                flaps.SetState(1);
                Debug.Log("Setting flaps to 1");
                break;
            case 1:
                flaps.SetState(2);
                Debug.Log("Setting flaps to 2");
                break;

        }

        joystick.OnSetStick.Invoke(new Vector3(lastMessage.pitch, lastMessage.yaw, lastMessage.roll));
        throttle.OnTriggerAxis.Invoke(lastMessage.breaks);

        
        throttle.RemoteSetThrottle(lastMessage.throttle);
    }
    public void OnDestory()
    {
        Networker.PlaneUpdate -= PlaneUpdate;
        Debug.Log("Destroyed Plane Update");
        Debug.Log(gameObject.name);
    }
}