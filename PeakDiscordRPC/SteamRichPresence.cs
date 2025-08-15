using HarmonyLib;
using PeakDiscordRPC;

namespace mod;

[HarmonyPatch(typeof(RichPresenceService), "SetState")]
public class SteamRichPresence
{

    static bool Prefix(RichPresenceState state)
    {
        Plugin.IsInGame = CheckIsRunBiome(state);
        Plugin.IsInMenu = state == RichPresenceState.Status_MainMenu;
        DiscordRichPresence.SetState(GetStateString(state));

        return true;
    }

    static bool CheckIsRunBiome(RichPresenceState state)
    {
        switch (state)
        {
            case RichPresenceState.Status_Shore:
            case RichPresenceState.Status_Tropics:
            case RichPresenceState.Status_Alpine:
            case RichPresenceState.Status_Mesa:
            case RichPresenceState.Status_Caldera:
            case RichPresenceState.Status_Kiln:
            case RichPresenceState.Status_Peak:
                return true;
            default:
                return false;
        }
    }

    static string GetStateString(RichPresenceState state)
    {
        return state switch
        {
            RichPresenceState.Status_MainMenu => "In the Main Menu",
            RichPresenceState.Status_Airport => "Waiting for a flight",
            RichPresenceState.Status_Shore => "Exploring the Shore",
            RichPresenceState.Status_Tropics => "Exploring the Tropics",
            RichPresenceState.Status_Alpine => "Exploring the Alpine",
            RichPresenceState.Status_Mesa => "Exploring the Mesa",
            RichPresenceState.Status_Caldera => "Exploring the Caldera",
            RichPresenceState.Status_Kiln => "Exploring the Kiln",
            RichPresenceState.Status_Peak => "On the Peak",
            _ => "Lost..."
        };
    }
}