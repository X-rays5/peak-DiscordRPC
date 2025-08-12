using HarmonyLib;

namespace PeakDiscordRPC;

[HarmonyPatch(typeof(Character), "Update")]
public class CharacterUpdateHook
{
    static void Postfix(Character __instance)
    {
        if (!__instance.IsLocal)
            return;

        DiscordRichPresence.PresenceUpdate();
    }
}