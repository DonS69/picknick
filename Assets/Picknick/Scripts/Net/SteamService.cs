using System;
using Steamworks;
using UnityEngine;

namespace Picknick.Net
{
    /// <summary>
    /// Starts Steam (test App ID 480 from steam_appid.txt until Picknick has its own) and
    /// handles the lobby used for friend invites. The connection itself goes peer to peer
    /// through Steam's relays, straight to the host's Steam ID.
    /// </summary>
    public class SteamService : MonoBehaviour
    {
        Callback<LobbyCreated_t> lobbyCreated;
        Callback<GameLobbyJoinRequested_t> joinRequested;
        Callback<LobbyEnter_t> lobbyEntered;
        CSteamID lobby = CSteamID.Nil;

        public bool Ready { get; private set; }
        public string Problem { get; private set; } = "";
        public ulong MySteamId => Ready ? SteamUser.GetSteamID().m_SteamID : 0;
        public string MyName => Ready ? SteamFriends.GetPersonaName() : "";

        /// <summary>Raised when we entered a friend's lobby: connect to this host Steam ID.</summary>
        public event Action<ulong> JoinHost;

        void Awake()
        {
            try
            {
                if (!SteamAPI.Init())
                {
                    Problem = "Steam isn't running (start Steam and log in, then press Play again).";
                    return;
                }
            }
            catch (DllNotFoundException)
            {
                Problem = "Steam library missing from the project.";
                return;
            }

            Ready = true;
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        }

        void Update()
        {
            if (Ready) SteamAPI.RunCallbacks();
        }

        void OnDestroy()
        {
            if (!Ready) return;
            LeaveLobby();
            lobbyCreated?.Dispose();
            joinRequested?.Dispose();
            lobbyEntered?.Dispose();
            SteamAPI.Shutdown();
            Ready = false;
        }

        /// <summary>Host side: a friends-only lobby so friends can be invited from the Steam overlay.</summary>
        public void CreateLobby(int maxPlayers)
        {
            if (Ready) SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
        }

        public bool CanInvite => Ready && lobby != CSteamID.Nil;

        public void OpenInviteDialog()
        {
            if (CanInvite) SteamFriends.ActivateGameOverlayInviteDialog(lobby);
        }

        public void LeaveLobby()
        {
            if (!Ready || lobby == CSteamID.Nil) return;
            SteamMatchmaking.LeaveLobby(lobby);
            lobby = CSteamID.Nil;
        }

        void OnLobbyCreated(LobbyCreated_t result)
        {
            if (result.m_eResult != EResult.k_EResultOK)
            {
                Problem = $"Couldn't create a Steam lobby ({result.m_eResult}). Friends can still join with your code.";
                return;
            }
            lobby = new CSteamID(result.m_ulSteamIDLobby);
        }

        // A friend accepted an invite (or clicked "Join game") while Picknick is running.
        void OnJoinRequested(GameLobbyJoinRequested_t request)
        {
            SteamMatchmaking.JoinLobby(request.m_steamIDLobby);
        }

        void OnLobbyEntered(LobbyEnter_t entered)
        {
            var enteredLobby = new CSteamID(entered.m_ulSteamIDLobby);
            CSteamID owner = SteamMatchmaking.GetLobbyOwner(enteredLobby);
            lobby = enteredLobby;
            // The host also "enters" the lobby it just created; only guests connect.
            if (owner.m_SteamID != MySteamId)
                JoinHost?.Invoke(owner.m_SteamID);
        }
    }
}
