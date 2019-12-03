using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Message_RequestSpawn_Result : Message
{
    public Vector3 position;
    public Quaternion rotation;

    public Message_RequestSpawn_Result(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
        type = MessageType.RequestSpawn_Result;
    }
}