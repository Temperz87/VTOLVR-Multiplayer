using Harmony;

[HarmonyPatch(typeof(PlayerVehicleSetup), "SavePersistentData")]
class Patch_PlayerVehicleSetup_SavePersistentData
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(TempPilotDetacher), "OnExitScene")]
class Patch_TempPilotDetacher_OnExitScene
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        return false;
    }
}