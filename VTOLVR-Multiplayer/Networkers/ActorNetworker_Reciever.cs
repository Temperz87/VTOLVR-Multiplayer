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
    public string actorName;
    public Actor actor;
    private Message_ActorSync lastMessage;
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
        ActorNetworker_Sender.allActors = JsonConvert.DeserializeObject<Dictionary<string, ulong>>(lastMessage.allActors);
        ActorNetworker_Reciever lastReciever;
        ulong lastUID;
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
        }
    }
}
