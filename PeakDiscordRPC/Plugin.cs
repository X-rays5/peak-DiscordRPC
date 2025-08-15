using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace PeakDiscordRPC;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private float _lastRPCUpdateTime;

    private static Plugin _instance;

    internal static ManualLogSource LOG => _instance.Logger;

    private Harmony _harmony;

    public static bool IsInGame = false;
    public static bool IsInMenu = false;

    private void Awake()
    {
        _instance = this;

        LOG.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} is starting...");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        LOG.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} has methods patched.");

        DiscordRichPresence.Initialize();
    }

    private void Update()
    {
        if (UnityEngine.Time.time - _lastRPCUpdateTime < 1f)
            return;

        _lastRPCUpdateTime = UnityEngine.Time.time;
        DiscordRichPresence.PresenceUpdate();
    }
}
