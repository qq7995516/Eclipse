using HarmonyLib;
using ProjectM;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class GameplayInputSystemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameplayInputSystem), nameof(GameplayInputSystem.HandleInput))]
    static unsafe void HandleInputPrefix(InputState inputState)
    {
        //Core.Log.LogInfo($"InputState: {inputState.ToString()}");
    }
}
