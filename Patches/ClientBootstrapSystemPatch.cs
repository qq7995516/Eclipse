namespace Eclipse.Patches;

/*
[HarmonyPatch]
internal static class ClientBootstrapSystemPatch
{
    [HarmonyPatch(typeof(ClientBootstrapSystem), nameof(ClientBootstrapSystem.BeginSetupClientWorld))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientBootstrapSystem __instance)
    {
        try
        {
            PrefabFactory.Manufacture();
        }
        catch (Exception ex)
        {
            Plugin.LogInstance.LogInfo($"LoadPersistenceSystemV2 SetLoadState: {ex}");
        }
    }
}
*/

