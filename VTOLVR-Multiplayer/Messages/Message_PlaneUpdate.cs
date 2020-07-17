using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[Serializable]
public class Message_PlaneUpdate : Message
{
    public bool landingGear;
    public float flaps; //0 = 0, 0.5 = 1, 1 = 1
    public float pitch, roll, yaw;
    public float breaks, throttle;
    public bool tailHook, fuelPort, launchBar;
    public ulong networkUID;
    //public bool hasRadar;
    // public bool locked;
    // public int radarLock;
    public Message_PlaneUpdate(bool landingGear, float flaps, float pitch, float roll, float yaw, float breaks, float throttle, bool tailHook, bool launchBar, bool fuelPort, ulong networkUID)
    {
        this.landingGear = landingGear;
        this.flaps = flaps;
        this.pitch = pitch;
        this.roll = roll;
        this.yaw = yaw;
        this.breaks = breaks;
        this.throttle = throttle;
        this.tailHook = tailHook;
        this.fuelPort = fuelPort;
        this.networkUID = networkUID;
        this.launchBar = launchBar;
        // this.hasRadar = hasRadar;
        // this.locked = locked;
        // this.radarLock = radarLock;
        type = MessageType.PlaneUpdate;
    }

    public override string ToString()
    {
        return $"Landing Gear = {landingGear} Flaps = {flaps} Pitch = {pitch} Roll = {roll} Yaw = {yaw} Breaks = {breaks} " +
            $"Throttle = {throttle} tailHook = {tailHook} Fuel Port = {fuelPort} NetworkID = {networkUID}";
    }
}