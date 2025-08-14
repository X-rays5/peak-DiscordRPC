using System;
using DiscordRPC;
using Photon.Pun;

namespace PeakDiscordRPC;

public static class DiscordRichPresence
{
    private static DiscordRpcClient _client;
    private static readonly object Lock = new();

    private static readonly string AliveState = "Climbing to new heights";
    private static readonly string PassedOutState = "Hanging on for dear life";
    private static readonly string DeadState = "My climbing career is officially over. 💀";

    private static string _state;
    private static readonly string LargeImageKey = "peak-logo";
    private static readonly string LargeImageText = "PEAK";
    private static readonly string LargeImageUrl = "https://store.steampowered.com/app/3527290/PEAK/";

    private static readonly string PartyId = Guid.NewGuid().ToString();

    public static void Initialize()
    {
        lock (Lock)
        {
            if (_client != null) return;

            _client = new DiscordRpcClient("1404923560647987310");
            _client.OnReady += (sender, e) =>
            {
                Plugin.LOG.LogInfo($"Connected to Discord as {e.User.Username}");
            };

            _client.OnError += (sender, e) => { Plugin.LOG.LogError($"Discord RPC Error: {e.Message}"); };
        }
    }

    public static void PresenceUpdate()
    {
        lock (Lock)
        {
            if (_client == null) return;
            UpdatePresenceInternal();
        }
    }

    public static void SetState(string state)
    {
        lock (Lock)
        {
            _state = state;
            UpdatePresenceInternal();
        }
    }

    private static void UpdatePresenceInternal()
    {
        if (_client == null) return;

        var presence = new RichPresence
        {
            State = GetStateString(),
            Details = GetDetailsString(),
            Assets = new Assets
            {
                LargeImageKey = LargeImageKey,
                LargeImageText = LargeImageText,
                LargeImageUrl = LargeImageUrl,
                SmallImageKey = DifficultyImageKey(Ascents.currentAscent),
                SmallImageText = $"Ascent: {Ascents.currentAscent.ToString()}"
            },
            Party = new Party
            {
                ID = PhotonNetwork.OfflineMode ? string.Empty : PartyId,
                Privacy = Party.PrivacySetting.Private,
                Size = GetPartySize(),
                Max = GetPartyMaxSize(),
            }
        };

        _client.SetPresence(presence);
    }

    public static void Dispose()
    {
        lock (Lock)
        {
            if (_client == null) return;
            _client.Dispose();
            _client = null;
        }
    }

    private static string GetStateString()
    {
        var day = "";
        if (Plugin.isInGame && Character.localCharacter != null)
        {
            day = $" - Day: {DayNightManager.instance.dayCount}";
        }

        return $"{_state}{day}";
    }

    private static int GetPartySize()
    {
        if (PhotonNetwork.OfflineMode)
        {
            // In offline mode, we assume the player is alone
            return 1;
        }

        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 0)
        {
            return PhotonNetwork.CurrentRoom.PlayerCount;
        }

        // Default to 1 player if no room is available. The user must be offline?
        return 1;
    }

    private static int GetPartyMaxSize()
    {
        if (PhotonNetwork.OfflineMode)
        {
            return 1;
        }

        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.MaxPlayers > 0)
        {
            // For some reason, the max players count is one higher than the actual max players in the room
            return PhotonNetwork.CurrentRoom.MaxPlayers - 1;
        }

        // Default to 4 players if no room is available
        return 4;
    }

    private static string DifficultyImageKey(int ascentLevel)
    {
        if (ascentLevel >= 0 && ascentLevel <= 7)
        {
            return $"difficulty-ascent{ascentLevel}";
        }

        return "bing-bong";
    }

    private static string GetDetailsString()
    {
        if (!Plugin.isInGame || Character.localCharacter == null)
        {
            return "";
        }

        return Character.localCharacter.data.fullyPassedOut ? Character.localCharacter.data.dead ? DeadState : PassedOutState : AliveState;
    }
}