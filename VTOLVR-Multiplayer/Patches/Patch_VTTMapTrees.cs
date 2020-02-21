using HarmonyLib;

[HarmonyPatch(typeof(VTTMapTrees.TreeJob), "CreateTree")]
public static class Patch_TreeMaster
{ 
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return false;
    }
}