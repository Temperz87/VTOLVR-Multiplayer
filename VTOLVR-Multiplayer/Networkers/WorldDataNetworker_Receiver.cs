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
    private float serverTimescale = 1f;

    private void Awake()
    {

        Networker.WorldDataUpdate += WorldDataUpdate;
        Networker.Disconnecting += OnDisconnect;

    }

    // If this sucks (it probably does) find a better solution (I.E. Harmony Patch SteamVR Pausing) :sick:
    public void Update()
    {
        // If the client's time scale is different than the servers timescale, force the clients to match
        if (Time.timeScale != serverTimescale)
        {
            Debug.Log($"Client timescale { Time.timeScale } mismatch with server { serverTimescale } - Forcing client update");
            Time.timeScale = serverTimescale;
        }
    }

    public void WorldDataUpdate(Packet packet)
    {

        Message_WorldData worldDataUpdate = (Message_WorldData)((PacketSingle)packet).message;
        Time.timeScale = worldDataUpdate.timeScale;
        serverTimescale = worldDataUpdate.timeScale;

        Debug.Log($"Set the timescale {worldDataUpdate.timeScale}");

    }

    public void OnDisconnect(Packet packet)
    {
        
        // TODO: This really should check if the disconnect packet is coming from the host, but idk how to do that.
        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        if (message.isHost)
        {
            Debug.Log("Received disconnect packet from host - setting timescale to 1.");
            Time.timeScale = 1f;
        }
        Destroy(gameObject);
    }

    public void OnDestroy()
    {
        Networker.WorldDataUpdate -= WorldDataUpdate;
        Networker.Disconnecting -= OnDisconnect;
        Debug.Log("Destroyed WorldData Update");
    }
}
