using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;


[HarmonyPatch(typeof(VTEventTarget), "Invoke")]
class Patch2
{
    static void Postfix(VTEventTarget __instance)
    {



        String actionIdentifier = __instance.eventName + __instance.methodName + __instance.targetID;
        int hash = actionIdentifier.GetHashCode();

        Message_ScenarioAction ScanarioActionOutMessage  = new Message_ScenarioAction(PlayerManager.localUID,hash);
        if (Networker.isHost)
        {
            Debug.Log("Host sent  Event action" + __instance.eventName + " of type " + __instance.methodName + " for target " + __instance.targetID);
       
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(ScanarioActionOutMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
        else
        {

            Debug.Log("Client sent  Event action" + __instance.eventName + " of type " + __instance.methodName + " for target " + __instance.targetID);
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, ScanarioActionOutMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        }  
  
           
    }


 
    
}

//patch to grab all the events being loaded on creation this replaces original method
[HarmonyPatch(typeof(VTEventInfo), "LoadFromInfoNode")]
class Patch3
{
    static void Prefix(VTEventInfo __instance, ConfigNode infoNode)
    {

        Debug.Log("bahacode scenario dictionary");
        __instance.eventName = infoNode.GetValue("eventName");
        __instance.actions = new List<VTEventTarget>();
        foreach (ConfigNode node in infoNode.GetNodes("EventTarget"))
        {
            VTEventTarget vTEventTarget = new VTEventTarget();
            vTEventTarget.LoadFromNode(node);
            __instance.actions.Add(vTEventTarget);
            Debug.Log("Compiling scenario dictonary my codd2");
            String actionIdentifier = vTEventTarget.eventName + vTEventTarget.methodName + vTEventTarget.targetID;

            Debug.Log(actionIdentifier);
            int hash = actionIdentifier.GetHashCode();
            Debug.Log("Compiling scenario dictonary  adding to my dictionary");
            PlayerManager.scenarioActionsList.Add(hash,vTEventTarget);
        }

        return;//dont run bahas code
    }
}
