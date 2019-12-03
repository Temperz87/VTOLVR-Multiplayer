using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public class Message_SpawnVehicle : Message
{
    public VTOLVehicles vehicle;
    public Vector3 position;
    public Quaternion rotation;
    public ulong csteamID;
    public ulong networkID;

    public Message_SpawnVehicle(VTOLVehicles vehicle, Vector3 position, Quaternion rotation, ulong csteamID, ulong networkID)
    {
        this.vehicle = vehicle;
        this.position = position;
        this.rotation = rotation;
        this.csteamID = csteamID;
        this.networkID = networkID;
        type = MessageType.SpawnVehicle;
    }
}