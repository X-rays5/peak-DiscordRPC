using HarmonyLib;

namespace PeakDiscordRPC;

[HarmonyPatch(typeof(Character), "Update")]
public class CharacterUpdateHook
{
    private static float _lastUpdateTime;

    static void Postfix(Character __instance)
    {
        if (!__instance.IsLocal)
            return;

        if (UnityEngine.Time.time - _lastUpdateTime < 5f)
            return;

        _lastUpdateTime = UnityEngine.Time.time;
        DiscordRichPresence.PresenceUpdate();
    }
}