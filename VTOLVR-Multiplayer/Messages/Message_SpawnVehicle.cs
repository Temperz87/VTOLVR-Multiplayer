using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Message_SpawnVehicle : Message
{
    public VTOLVehicles vehicle;
    public Vector3D position;
    public Vector3D rotation;
    public ulong csteamID;
    public ulong networkID;

    public Message_SpawnVehicle(VTOLVehicles vehicle, Vector3D position, Vector3D rotation, ulong csteamID, ulong networkID)
    {
        this.vehicle = vehicle;
        this.position = position;
        this.rotation = rotation;
        this.csteamID = csteamID;
        this.networkID = networkID;
        type = MessageType.SpawnVehicle;
    }
}