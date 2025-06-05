using Eclipse.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InitializationPatches
{
    static readonly bool _shouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Familiars || Plugin.Quests;
    static bool _setCanvas = false;

    [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(GameDataManager __instance)
    {
        if (!__instance.GameDataInitialized || !__instance.World.IsCreated) return;

        try
        {
            if (_shouldInitialize && !Core._initialized)
            {
                Core.Initialize(__instance);

                if (Core._initialized)
                {
                    Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] ���ڿͻ����ϳ�ʼ����");
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] �ڿͻ��˳�ʼ��ʧ�ܣ����ڴ� try-catch �˳�... {ex}");
        }
    }

    [HarmonyPatch(typeof(UICanvasSystem), nameof(UICanvasSystem.UpdateHideIfDisabled))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(UICanvasBase canvas)
    {
        if (!_setCanvas && Core._initialized)
        {
            _setCanvas = true;
            Core.SetCanvas(canvas);
        }
    }

    [HarmonyPatch(typeof(ClientBootstrapSystem), nameof(ClientBootstrapSystem.OnDestroy))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientBootstrapSystem __instance)
    {
        CanvasService._killSwitch = true;

        CanvasService._shiftRoutine.Stop();
        CanvasService._canvasRoutine.Stop();

        CanvasService._shiftRoutine = null;
        CanvasService._canvasRoutine = null;

        CanvasService._active = false;
        CanvasService._shiftActive = false;
        CanvasService._ready = false;

        ClientChatSystemPatch._userRegistered = false;
        ClientChatSystemPatch._pending = false;

        CanvasService._version = string.Empty;

        _setCanvas = false;

        CanvasService._abilityTooltipData = null;
        CanvasService._dailyQuestIcon = null;
        CanvasService._weeklyQuestIcon = null;

        // �˴���Ҫ������ಿ�ֵ������߼���Ŀǰֻ�о���ͼ
        CanvasService.ResetState();

        Core.Reset();
    }
}