using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;

/// <summary>
/// Updates objects with a  rigidbody over the network using velocity and position.
/// </summary>
public class WorldDataNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;

    private void Awake()
    {

        Networker.WorldDataUpdate += WorldDataUpdate;

    }

    public void WorldDataUpdate(Packet packet)
    {

        Message_WorldData worldDataUpdate = (Message_WorldData)((PacketSingle)packet).message;


        Time.timeScale = worldDataUpdate.timeScale;

    }

    public void OnDisconnect(Packet packet)
    {
        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        Destroy(gameObject);
    }

    public void OnDestroy()
    {
        Networker.WorldDataUpdate -= WorldDataUpdate;
        Networker.Disconnecting -= OnDisconnect;
        Debug.Log("Destroyed WorldData Update");
    }
}
