using System;
using DiscordRPC;
using Photon.Pun;
using UnityEngine;

namespace PeakDiscordRPC;

public static class DiscordRichPresence
{
    private static DiscordRpcClient _client;
    private static readonly object Lock = new();

    private const string AliveState = "Climbing to new heights";
    private const string PassedOutState = "Hanging on for dear life";
    private const string DeadState = "My climbing career is officially over. 💀";

    private static string _state;
    private const string LargeImageKey = "peak-logo";
    private const string LargeImageText = "PEAK";
    private const string LargeImageUrl = "https://store.steampowered.com/app/3527290/PEAK/";

    private static readonly string PartyId = Guid.NewGuid().ToString();

    public static void Initialize()
    {
        lock (Lock)
        {
            if (_client != null) return;

            Plugin.LOG.LogInfo("Initializing DiscordRPC client.");
            _client = new DiscordRpcClient("1404923560647987310");
            _client.OnReady += (_, e) =>
            {
                Plugin.LOG.LogInfo($"Connected to Discord as {e.User.Username}");
                PresenceUpdate();
            };

            _client.OnError += (_, e) =>
            {
                Plugin.LOG.LogError($"Discord RPC Error: {e.Message}");
            };

            _client.Initialize();
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

    public static void Dispose()
    {
        lock (Lock)
        {
            if (_client == null) return;
            _client.Dispose();
            _client = null;
        }
    }

    private static void UpdatePresenceInternal()
    {
        if (_client == null) return;

        var presence = new RichPresence
        {
            State = GetStateString(),
            Details = GetDetailsString(),
            Timestamps = new Timestamps
            {
                StartUnixMilliseconds = GetGameStartTimeStamp()
            },
            Assets = new Assets
            {
                LargeImageKey = LargeImageKey,
                LargeImageText = LargeImageText,
                LargeImageUrl = LargeImageUrl,
                SmallImageKey = DifficultyImageKey(Ascents.currentAscent),
                SmallImageText = DifficultyImageText(Ascents.currentAscent)
            },
            Party = new Party
            {
                ID = GetPartyId(),
                Privacy = Party.PrivacySetting.Private,
                Size = GetPartySize(),
                Max = GetPartyMaxSize(),
            },
            Type = ActivityType.Playing,
        };

        _client.SetPresence(presence);
    }

    private static string GetStateString()
    {
        var day = "";
        if (Plugin.IsInGame && Character.localCharacter)
        {
            day = $" - Day: {DayNightManager.instance.dayCount}";
        }

        return $"{_state}{day}";
    }

    private static string GetDetailsString()
    {
        if (!Plugin.IsInGame || !Character.localCharacter)
        {
            return "";
        }

        if (Character.localCharacter.data.dead) {
            return DeadState;
        } else if (Character.localCharacter.data.fullyPassedOut) {
            return PassedOutState;
        }

        return AliveState;
    }

    private static ulong GetGameStartTimeStamp()
    {
        if (!Plugin.IsInGame || !RunManager.Instance)
        {
            return (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Time.realtimeSinceStartup);
        }

        var time = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - RunManager.Instance.timeSinceRunStarted);
        return time;
    }

    private static string DifficultyImageKey(int ascentLevel)
    {
        if (!Plugin.IsInGame)
            return "bing-bong";

        return ascentLevel is >= 0 and <= 7 ? $"difficulty-ascent{ascentLevel}" : "bing-bong";
    }

    private static string DifficultyImageText(int ascentLevel)
    {
        if (!Plugin.IsInGame)
        {
            return "Not in a game";
        }

        return ascentLevel switch
        {
            0 => "Peak Difficulty",
            -1 => "Tenderfoot",
            1 => "Ascent Level 1",
            2 => "Ascent Level 2",
            3 => "Ascent Level 3",
            4 => "Ascent Level 4",
            5 => "Ascent Level 5",
            6 => "Ascent Level 6",
            7 => "Ascent Level 7",
            _ => string.Empty
        };
    }

    private static string GetPartyId()
    {
        if (Plugin.IsInMenu)
        {
            return string.Empty;
        }

        return PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : PartyId;
    }

    private static int GetPartySize()
    {
        if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null)
        {
            // In offline mode, we assume the player is alone
            return 1;
        }

        return PhotonNetwork.CurrentRoom.PlayerCount > 0 ? PhotonNetwork.CurrentRoom.PlayerCount : 1; // Default to 1 player if no room is available. The user must be offline?
    }

    private static int GetPartyMaxSize()
    {
        if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null)
        {
            return 1;
        }

        if (PhotonNetwork.CurrentRoom.MaxPlayers > 0)
        {
            // For some reason, the max players count is one higher than the actual max players in the room
            return PhotonNetwork.CurrentRoom.MaxPlayers - 1;
        }

        // Default to 4 players if no room is available
        return 4;
    }
}
