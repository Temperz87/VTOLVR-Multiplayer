using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using Steamworks;
using Valve.Newtonsoft.Json;

class ActorNetworker_Reciever : MonoBehaviour // Client Only
{
    public ulong networkUID;
    public Actor actor;
    private void Awake()
    {
        actor = base.GetComponent<Actor>();
    }
    public static void syncActors(Packet packet)
    {
        Debug.Log("Syncing actors.");
        if (Networker.isHost)
        {
            Debug.LogError("Should not have gotten an actor sync packet as the host");
            return;
        }
        Message_ActorSync lastMessage = (Message_ActorSync)((PacketSingle)packet).message;
        ActorNetworker_Sender.allActors = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(lastMessage.allActors);
        ActorNetworker_Reciever lastReciever;
        string lastString;
        foreach (var actor in TargetManager.instance.allActors)
        {
            foreach (var uID in ActorNetworker_Sender.allActors.Keys)
            {
                ActorNetworker_Sender.allActors.TryGetValue(uID, out lastString);
                if (actor.name == lastString)
                {
                    lastReciever = actor.gameObject.AddComponent<ActorNetworker_Reciever>();
                    lastReciever.networkUID = uID;
                    break;
                }
            }
        }
        /*
        foreach (var actor in TargetManager.instance.allActors)
        {
            if (ActorNetworker_Sender.allActors.ContainsKey(actor.name))
            {
                Debug.Log(actor.name + " is adding a new actor reciever.");
                ActorNetworker_Sender.allActors.TryGetValue(actor.name, out lastUID);
                lastReciever = actor.gameObject.AddComponent<ActorNetworker_Reciever>();
                lastReciever.networkUID = lastUID;
            }
            else
            {
                Debug.LogError(actor.name + " was not found in the dictionary.");
            }
        }*/
    }
}
