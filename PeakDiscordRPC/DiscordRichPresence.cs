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
            _client.OnReady += (sender, e) =>
            {
                Plugin.LOG.LogInfo($"Connected to Discord as {e.User.Username}");
                PresenceUpdate();
            };

            _client.OnError += (sender, e) => { Plugin.LOG.LogError($"Discord RPC Error: {e.Message}"); };

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
        if (Plugin.isInGame && Character.localCharacter != null)
        {
            day = $" - Day: {DayNightManager.instance.dayCount}";
        }

        return $"{_state}{day}";
    }

    private static string GetDetailsString()
    {
        if (!Plugin.isInGame || Character.localCharacter == null)
        {
            return "";
        }

        if (Character.localCharacter.data.fullyPassedOut) {
            return PassedOutState;
        } else if (Character.localCharacter.data.dead) {
            return DeadState;
        }

        return AliveState;
    }

    private static ulong GetGameStartTimeStamp()
    {
        if (!Plugin.isInGame || RunManager.Instance == null)
        {
            return (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Time.realtimeSinceStartup);
        }

        var time = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - RunManager.Instance.timeSinceRunStarted);
        return time;
    }

    private static string DifficultyImageKey(int ascentLevel)
    {
        if (!Plugin.isInGame)
        {
            return "bing-bong"; // Default image key when not in game
        }

        if (ascentLevel >= 0 && ascentLevel <= 7)
        {
            // Ascent levels are 0-7, so we can use them directly to form the image key
            return $"difficulty-ascent{ascentLevel}";
        }

        return "bing-bong";
    }

    private static string DifficultyImageText(int ascentLevel)
    {
        if (!Plugin.isInGame)
        {
            return "Not in a game";
        }

        if (ascentLevel >= 1 && ascentLevel <= 7)
        {
            return $"Ascent Level: {ascentLevel}";
        }

        if (ascentLevel == 0)
        {
            return "Difficulty: Peak";
        }

        if (ascentLevel == -1)
        {
            return "Difficulty: Tenderfoot";
        }

        return string.Empty;
    }

    private static string GetPartyId()
    {
        if (PhotonNetwork.OfflineMode)
        {
            return string.Empty;
        }

        if (PhotonNetwork.CurrentRoom != null)
        {
            return PartyId;
        }

        return string.Empty;
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
}