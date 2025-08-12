using System.Collections.Generic;
using DiscordRPC;
using DiscordRPC.Logging;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PeakDiscordRPC;

public static class DiscordRichPresence
{
    private static DiscordRpcClient _client;
    private static readonly object Lock = new();

    private static readonly string ALIVE_STATE = "Just barely making it... again.";
    private static readonly string PASSED_OUT_STATE = "Hanging on for dear life";
    private static readonly string DEAD_STATE = "My climbing career is officially over. 💀";

    private static string _state;
    private static string _largeImageKey = "peak-logo";
    private static string _largeImageText = "PEAK";
    private static string _largeImageUrl = "https://store.steampowered.com/app/3527290/PEAK/";

    private static readonly Queue<string> JoinSecretQueue = new();

    private static PhotonCallbacks _photonCallbacks;

    public static void Initialize(string clientId)
    {
        lock (Lock)
        {
            if (_client != null) return;

            Plugin.LOG.LogInfo("Initializing Discord Rich Presence...");
            _client = new DiscordRpcClient(clientId)
            {
                Logger = new ConsoleLogger()
            };

            _client.OnJoin += (sender, e) =>
            {
                // Check if we are already connected to the Photon master server.
                if (PhotonNetwork.IsConnectedAndReady)
                {
                    PhotonNetwork.JoinRoom(e.Secret);
                }
                else
                {
                    JoinSecretQueue.Enqueue(e.Secret);
                    PhotonNetwork.ConnectUsingSettings();
                }
            };

            _client.Initialize();
            Plugin.LOG.LogInfo("Discord Rich Presence initialized successfully.");

            // Create a new GameObject and add our MonoBehaviourPunCallbacks component to it.
            // This allows the callback methods to be triggered.
            var callbackHandler = new GameObject("DiscordPhotonCallbackHandler");
            // Prevent the GameObject from being destroyed when loading new scenes
            Object.DontDestroyOnLoad(callbackHandler);
            _photonCallbacks = callbackHandler.AddComponent<PhotonCallbacks>();
        }
    }

    public class PhotonCallbacks : MonoBehaviourPunCallbacks
    {
        public override void OnConnectedToMaster()
        {
            string secret = DiscordRichPresence.GetPendingJoinSecret();
            if (!string.IsNullOrEmpty(secret))
            {
                PhotonNetwork.JoinRoom(secret);
            }
        }

        public override void OnJoinedRoom() => UpdatePresenceInternal();
        public override void OnLeftRoom() => UpdatePresenceInternal();
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) => UpdatePresenceInternal();
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) => UpdatePresenceInternal();
        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) => UpdatePresenceInternal();
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) => UpdatePresenceInternal();
        public override void OnDisconnected(DisconnectCause cause) => UpdatePresenceInternal();
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

    public static void SetLargeImageKey(string key)
    {
        lock (Lock)
        {
            _largeImageKey = key;
            UpdatePresenceInternal();
        }
    }

    public static void SetLargeImageText(string text)
    {
        lock (Lock)
        {
            _largeImageText = text;
            UpdatePresenceInternal();
        }
    }

    private static void UpdatePresenceInternal()
    {
        if (_client == null) return;

        string day = "";
        if (DayNightManager.instance.dayCount > 0 && Character.localCharacter != null)
        {
            day = $" - Day: {DayNightManager.instance.dayCount}";
        }

        var presence = new RichPresence
        {
            State = $"{_state}{day}",
            Details = GetPlayerState(),
            Assets = new Assets
            {
                LargeImageKey = _largeImageKey,
                LargeImageText = _largeImageText,
                LargeImageUrl = _largeImageUrl,
                SmallImageKey = DifficultyImageKey(Ascents.currentAscent),
                SmallImageText = $"Ascent: {Ascents.currentAscent.ToString()}"
            },
            Party = new Party
            {
                ID = PhotonNetwork.OfflineMode ? string.Empty : PhotonNetwork.CurrentRoom?.Name ?? string.Empty,
                Privacy = Party.PrivacySetting.Private,
                Size = GetPartySize(),
                Max = GetPartyMaxSize(),
            }
        };

        _client.SetPresence(presence);
    }

    private static string GetPendingJoinSecret()
    {
        if (JoinSecretQueue.Count > 0)
        {
            return JoinSecretQueue.Dequeue();
        }

        return "";
    }

    public static void Dispose()
    {
        lock (Lock)
        {
            if (_client == null) return;
            _client.Dispose();
            _client = null;
        }

        if (_photonCallbacks != null)
        {
            UnityEngine.Object.Destroy(_photonCallbacks.gameObject);
            _photonCallbacks = null;
        }
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
        else
        {
            return 1;
        }
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
        else
        {
            return 4;
        }
    }

    private static string DifficultyImageKey(int ascentLevel)
    {
        if (ascentLevel >= 0 && ascentLevel <= 7)
        {
            return $"difficulty-ascent{ascentLevel}";
        }

        return "bing-bong";
    }

    private static string GetPlayerState()
    {
        if (Character.localCharacter == null)
        {
            return "";
        }

        return Character.localCharacter.data.fullyPassedOut ? Character.localCharacter.data.dead ? DEAD_STATE : PASSED_OUT_STATE : ALIVE_STATE;
    }
}