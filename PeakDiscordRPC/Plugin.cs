using BepInEx;
using BepInEx.Logging;
using DiscordRPC;
using HarmonyLib;

namespace PeakDiscordRPC;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;

    internal static ManualLogSource LOG => Instance.Logger;

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;

        LOG.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} is starting...");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        LOG.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} has methods patched.");

        DiscordRichPresence.Initialize("1404923560647987310");
    }
}
