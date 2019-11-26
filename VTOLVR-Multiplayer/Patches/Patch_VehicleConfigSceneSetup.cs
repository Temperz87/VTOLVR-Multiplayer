using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

namespace VTOLVR_Multiplayer
{
    [HarmonyPatch(typeof(VehicleConfigSceneSetup))]
    [HarmonyPatch("LaunchMission")]
    public class Patch_VehicleConfigSceneSetup_LaunchMission
    {
        public static bool Prefix(VehicleConfigSceneSetup __instance)
        {
            if (Networker.isHost)
            {
                if (Networker.EveryoneElseReady())
                {
                    Networker.SendGlobalP2P(new Message(MessageType.Ready_Result), Steamworks.EP2PSend.k_EP2PSendReliable);
                    return true;
                }
                else
                {
                    __instance.configScenarioUI.DenyLaunch("Not Everyone Ready");
                    return false;
                }
            }
            else if (!Networker.hostReady)
            {
                __instance.configScenarioUI.DenyLaunch("Waiting for Host");
                Networker.SendP2P(Networker.hostID, new Message(MessageType.Ready), Steamworks.EP2PSend.k_EP2PSendReliable);
                return false;
            }
            return true;
        }
    }
}
