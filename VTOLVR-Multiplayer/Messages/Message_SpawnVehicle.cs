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
    public V3 position;
    public V3 rotation;
    public ulong csteamID;
    public ulong networkID;

    public Message_SpawnVehicle(VTOLVehicles vehicle, V3 position, V3 rotation, ulong csteamID, ulong networkID)
    {
        this.vehicle = vehicle;
        this.position = position;
        this.rotation = rotation;
        this.csteamID = csteamID;
        this.networkID = networkID;
        type = MessageType.SpawnVehicle;
    }
}