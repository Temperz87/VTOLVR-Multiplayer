using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using Steamworks;
using Valve.Newtonsoft.Json;

class ActorNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    public Actor actor;
    public static Dictionary<ulong, string> allActors = new Dictionary<ulong, string>();
    private void Awake()
    {
        networkUID = Networker.GenerateNetworkUID();
        actor = base.GetComponent<Actor>();
        Debug.Log("Adding " + actor.name + " to the dictionary.");
        allActors.Add(networkUID, actor.name);
    }
    public static void SendDictionary(CSteamID uID) // Host Only
    {
        if (!Networker.isHost)
        {
            Debug.LogWarning("We aren't the host so we shouldn't be trying to send a dictionary.");
            return;
        }
        string serializedDic = JsonConvert.SerializeObject(allActors, Formatting.Indented);
        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(uID, new Message_ActorSync(serializedDic), EP2PSend.k_EP2PSendReliable);
    }
}

