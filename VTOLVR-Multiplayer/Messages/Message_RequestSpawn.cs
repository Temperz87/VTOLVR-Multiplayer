using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Message_RequestSpawn_Result : Message
{
    public V3 position;
    public V3 rotation;

    public Message_RequestSpawn_Result(V3 position, V3 rotation)
    {
        this.position = position;
        this.rotation = rotation;
        type = MessageType.RequestSpawn_Result;
    }
}