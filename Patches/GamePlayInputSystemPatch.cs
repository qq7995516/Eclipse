using Eclipse.Services;
using HarmonyLib;
using ProjectM;
using UnityEngine;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class GameplayInputSystemPatch
{
    public static Vector3 bottomLeft;
    public static Vector3 topRight;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameplayInputSystem), nameof(GameplayInputSystem.HandleInput))]
    static void HandleInputPrefix(InputState inputState)
    {
        if (!Core.hasInitialized) return;
        if (!CanvasService.Active) return;

        if (Input.GetMouseButtonDown(0))
        {
            //Core.Log.LogInfo($"Mouse 0 Down {Input.mousePosition.x},{Input.mousePosition.y},{Input.mousePosition.z}");
            //Core.Log.LogInfo($"{bottomLeft.x},{bottomLeft.y},{bottomLeft.z} | {topRight.x},{topRight.y},{topRight.z}");

            //Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (IsMouseInside(Input.mousePosition))
            {
                //Core.Log.LogInfo($"Mouse 0 Down Inside {worldMousePosition.x},{worldMousePosition.y},{worldMousePosition.z}");
                ToggleUIObjects();
            }
        }
    }
    static void ToggleUIObjects()
    {
        CanvasService.UIActive = !CanvasService.UIActive;
        foreach (GameObject gameObject in CanvasService.ActiveObjects)
        {
            gameObject.active = CanvasService.UIActive;
        }
    }
    static bool IsMouseInside(Vector3 position)
    {
        return position.x >= bottomLeft.x && position.x <= topRight.x &&
               position.y >= bottomLeft.y && position.y <= topRight.y;
    }
}
