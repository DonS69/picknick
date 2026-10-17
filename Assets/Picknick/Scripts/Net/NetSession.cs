using System;
using System.Collections.Generic;
using Picknick.Abilities;
using Picknick.Creature;
using Netcode.Transports;
using Picknick.Sandbox;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Picknick.Net
{
    /// <summary>
    /// Online play. One player hosts and simulates the whole room; everyone else joins,
    /// sends their input to the host and shows the snapshots the host sends back.
    /// Every peer builds the same room locally, so nothing has to be spawned over the network.
    /// Each player controls one creature; Tab switches to the next creature nobody controls.
    /// Two ways to connect: Steam (peer to peer by Steam ID, works over the internet) or
    /// LAN / this PC (IP address, also what Multiplayer Play Mode windows use).
    /// Lives on the GameFlow object, so a session survives moving from room to room. Each
    /// snapshot says which room the host is in; a guest in a different one loads it.
    /// </summary>
    public class NetSession : MonoBehaviour
    {
        const string InputMessage = "picknick.input";
        const string SwitchMessage = "picknick.switch";
        const string SnapshotMessage = "picknick.snapshot";
        const ulong NoOwner = ulong.MaxValue;
        const int MaxCreatures = 8;
        public const int MaxPlayers = 4;

        public string address = "127.0.0.1";
        public ushort port = 7777;
        [Tooltip("Physics steps between snapshots. At the default 50 steps/s, 2 means 25 snapshots/s.")]
        public int snapshotInterval = 2;

        LocalInputRouter router;
        List<CreatureController> creatures;
        bool roomReady;
        // Bumped by the host on every room load (restarts too), so guests know to reload.
        int roomLoad;
        int guestSceneIndex = -1;
        int guestRoomLoad = -1;
        NetworkManager network;
        UnityTransport transport;
        SteamNetworkingSocketsTransport steamTransport;
        SteamService steam;
        bool overSteam;
        readonly ulong[] owners = new ulong[MaxCreatures];
        readonly CreatureInput[] pending = new CreatureInput[MaxCreatures];
        int fixedSteps;
        bool leaveRequested;
        string status = "";
        GUIStyle panelStyle;

        public bool Online => network != null && network.IsListening;
        public bool IsHost => Online && network.IsServer;
        public bool CanChangeRoom => !Online || IsHost;
        public SteamService Steam => steam;
        public string Status => status;

        /// <summary>A guest lost the connection to the host (or never got in).</summary>
        public event Action LostHost;

        int CreatureCount => creatures != null ? Mathf.Min(creatures.Count, MaxCreatures) : 0;

        void Awake()
        {
            steam = gameObject.AddComponent<SteamService>();
            steam.JoinHost += hostId => { if (!Online) StartSteam(false, hostId); };
            ClearOwners();
        }

        // ---------- Rooms ----------

        /// <summary>Called by a room as it is built. Online, the session carries straight on in it.</summary>
        public void AttachRoom(LocalInputRouter inputRouter)
        {
            router = inputRouter;
            creatures = inputRouter.Creatures;
            roomReady = true;
            for (int i = CreatureCount; i < MaxCreatures; i++)
            {
                owners[i] = NoOwner;
                pending[i] = default;
            }
            if (!Online) return;

            router.enabled = false;
            if (IsHost)
            {
                foreach (ulong id in network.ConnectedClientsIds)
                    AssignFreeCreature(id);
            }
            else
            {
                foreach (CreatureController creature in creatures)
                    creature.SetInput(default);
            }
        }

        public void DetachRoom(LocalInputRouter inputRouter)
        {
            if (router != inputRouter) return;
            router = null;
            creatures = null;
            roomReady = false;
        }

        /// <summary>The host is about to load a room: guests will follow once snapshots say so.</summary>
        public void RoomLoading()
        {
            roomLoad++;
            roomReady = false;
        }

        // ---------- Called by the menu ----------

        public bool HostSteam()
        {
            StartSteam(true);
            return Online;
        }

        public void JoinSteam(string code) => StartSteam(false, ParseCode(code));

        public bool HostLan()
        {
            StartLan(true);
            return Online;
        }

        public void JoinLan() => StartLan(false);

        public string Address
        {
            get => address;
            set => address = value;
        }

        /// <summary>Stop playing online (hosting ends the session for everyone).</summary>
        public void Leave()
        {
            leaveRequested = false;
            status = "";
            GoOffline();
        }

        void Update()
        {
            if (leaveRequested)
            {
                leaveRequested = false;
                GoOffline();
            }
            if (!Online || !roomReady) return;

            int mine = CreatureOf(network.LocalClientId);
            if (mine >= 0) router.keyboardIndex = mine;
            CreatureInput local = mine >= 0 ? ReadLocalInput(creatures[mine]) : default;
            bool switchPressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;

            if (IsHost)
            {
                if (switchPressed) SwitchCreature(network.LocalClientId);
                for (int i = 0; i < creatures.Count; i++)
                {
                    creatures[i].SetInput(owners[i] == network.LocalClientId ? local : pending[i]);
                    ClearPressed(ref pending[i]);
                }
            }
            else
            {
                if (switchPressed) SendToHost(SwitchMessage, default, -1);
                if (mine >= 0) SendToHost(InputMessage, local, mine);
            }
        }

        void FixedUpdate()
        {
            if (!IsHost || !roomReady) return;
            if (++fixedSteps % Mathf.Max(1, snapshotInterval) == 0)
                SendSnapshot();
        }

        // ---------- Starting and stopping ----------

        void StartLan(bool host)
        {
            if (!PrepareNetworkManager()) return;
            network.NetworkConfig.NetworkTransport = transport;
            transport.SetConnectionData(address, port, host ? "0.0.0.0" : null);
            overSteam = false;
            StartOnline(host);
        }

        void StartSteam(bool host, ulong hostSteamId = 0)
        {
            if (!steam.Ready)
            {
                status = steam.Problem;
                return;
            }
            if (!host && hostSteamId == 0)
            {
                status = "Paste your friend's code first.";
                return;
            }
            if (!host && hostSteamId == steam.MySteamId)
            {
                status = "That's your own code. Steam can't connect you to yourself; use LAN to test on one PC.";
                return;
            }
            if (!PrepareNetworkManager()) return;
            network.NetworkConfig.NetworkTransport = steamTransport;
            steamTransport.ConnectToSteamID = hostSteamId;
            overSteam = true;
            StartOnline(host);
            if (host && Online)
            {
                steam.CreateLobby(MaxPlayers);
                status = "Hosting on Steam. Invite friends, or send them your code.";
            }
            else if (!host && Online)
            {
                status = "Joining your friend on Steam...";
            }
        }

        bool PrepareNetworkManager()
        {
            if (network == null)
                CreateNetworkManager();
            if (network.IsListening || network.ShutdownInProgress)
            {
                status = "Still closing the last session, try again in a moment.";
                return false;
            }
            return true;
        }

        void StartOnline(bool host)
        {
            ClearOwners();
            bool started = host ? network.StartHost() : network.StartClient();
            if (!started)
            {
                status = host ? "Could not host (is the port in use?)" : "Could not start joining";
                leaveRequested = true;
                return;
            }

            CustomMessagingManager messaging = network.CustomMessagingManager;
            if (host)
            {
                messaging.RegisterNamedMessageHandler(InputMessage, OnInput);
                messaging.RegisterNamedMessageHandler(SwitchMessage, OnSwitch);
                AssignFreeCreature(network.LocalClientId);
                status = "Hosting. Others can join at your IP address.";
            }
            else
            {
                messaging.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshot);
                Authority.IsSimulating = false;
                Physics2D.simulationMode = SimulationMode2D.Script;
                guestSceneIndex = -1;
                guestRoomLoad = -1;
                if (creatures != null)
                    foreach (CreatureController creature in creatures)
                        creature.SetInput(default);
                if (!overSteam) status = $"Joining {address}...";
            }

            if (router != null) router.enabled = false;
        }

        /// <summary>
        /// Made once and reused for every session. Destroying it ourselves right after a shutdown
        /// makes Netcode shut down twice and touch an already-closed transport.
        /// </summary>
        void CreateNetworkManager()
        {
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            transport = go.AddComponent<UnityTransport>();
            steamTransport = go.AddComponent<SteamNetworkingSocketsTransport>();
            network = go.AddComponent<NetworkManager>();
            network.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                // Every peer builds the room itself; don't let Netcode load or sync scenes.
                EnableSceneManagement = false,
            };
            network.OnClientConnectedCallback += OnClientConnected;
            network.OnClientDisconnectCallback += OnClientDisconnected;
        }

        void GoOffline()
        {
            // Netcode may already have shut itself down (e.g. after a failed join).
            if (network != null && network.IsListening && !network.ShutdownInProgress)
                network.Shutdown();
            steam.LeaveLobby();

            Authority.IsSimulating = true;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            foreach (SyncedBody body in SyncedBody.All)
                body.ClearTarget();
            ClearOwners();
            if (router != null) router.enabled = true;
        }

        void OnDestroy()
        {
            Authority.IsSimulating = true;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }

        void OnClientConnected(ulong clientId)
        {
            if (IsHost)
                AssignFreeCreature(clientId);
            else if (clientId == network.LocalClientId)
                status = "Connected. Tab switches to a free creature.";
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (IsHost && clientId != network.LocalClientId)
            {
                for (int i = 0; i < owners.Length; i++)
                    if (owners[i] == clientId) { owners[i] = NoOwner; pending[i] = default; }
                return;
            }

            // We lost the host (or never reached it).
            status = guestRoomLoad >= 0 ? "The host left the game." : "Couldn't reach the host. Check the code and try again.";
            leaveRequested = true;
            LostHost?.Invoke();
        }

        // ---------- Creatures and owners (host decides) ----------

        void ClearOwners()
        {
            for (int i = 0; i < owners.Length; i++)
            {
                owners[i] = NoOwner;
                pending[i] = default;
            }
        }

        int CreatureOf(ulong player)
        {
            for (int i = 0; i < CreatureCount; i++)
                if (owners[i] == player) return i;
            return -1;
        }

        void AssignFreeCreature(ulong player)
        {
            if (CreatureOf(player) >= 0) return;
            for (int i = 0; i < CreatureCount; i++)
            {
                if (owners[i] != NoOwner) continue;
                owners[i] = player;
                return;
            }
        }

        void SwitchCreature(ulong player)
        {
            int count = CreatureCount;
            if (count == 0) return;
            int current = CreatureOf(player);
            for (int step = 1; step <= count; step++)
            {
                int next = ((current < 0 ? -1 : current) + step + count) % count;
                if (owners[next] != NoOwner) continue;
                if (current >= 0)
                {
                    owners[current] = NoOwner;
                    pending[current] = default;
                }
                owners[next] = player;
                return;
            }
        }

        // ---------- Messages ----------

        const byte JumpHeld = 1, JumpPressed = 2, ActionHeld = 4, ActionPressed = 8, SwapPressed = 16;

        void SendToHost(string message, CreatureInput input, int creature)
        {
            var writer = new FastBufferWriter(32, Allocator.Temp);
            try
            {
                writer.WriteValueSafe(creature);
                writer.WriteValueSafe(input.Move);
                writer.WriteValueSafe(PackButtons(input));
                writer.WriteValueSafe(input.Aim);
                network.CustomMessagingManager.SendNamedMessage(message, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            }
            finally
            {
                writer.Dispose();
            }
        }

        void OnInput(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int creature);
            reader.ReadValueSafe(out float move);
            reader.ReadValueSafe(out byte buttons);
            reader.ReadValueSafe(out Vector2 aim);
            if (creature < 0 || creature >= CreatureCount || owners[creature] != sender)
                return;

            // Keep the latest held state, but never lose a press that arrives between frames.
            ref CreatureInput input = ref pending[creature];
            input.Move = move;
            input.Aim = aim;
            input.JumpHeld = (buttons & JumpHeld) != 0;
            input.ActionHeld = (buttons & ActionHeld) != 0;
            input.JumpPressed |= (buttons & JumpPressed) != 0;
            input.ActionPressed |= (buttons & ActionPressed) != 0;
            input.SwapPressed |= (buttons & SwapPressed) != 0;
        }

        void OnSwitch(ulong sender, FastBufferReader reader) => SwitchCreature(sender);

        void SendSnapshot()
        {
            var clients = new List<ulong>();
            foreach (ulong id in network.ConnectedClientsIds)
                if (id != network.LocalClientId) clients.Add(id);
            if (clients.Count == 0) return;

            int size = 24 + creatures.Count * 16 + SyncedBody.All.Count * 24;
            var writer = new FastBufferWriter(size, Allocator.Temp);
            try
            {
                writer.WriteValueSafe(SceneManager.GetActiveScene().buildIndex);
                writer.WriteValueSafe(roomLoad);
                writer.WriteValueSafe(creatures.Count);
                for (int i = 0; i < creatures.Count; i++)
                {
                    var abilities = creatures[i].GetComponent<CreatureAbilities>();
                    MagnetAbility magnet = abilities.Get<MagnetAbility>();
                    Magnetic target = magnet != null && magnet.enabled ? magnet.Target : null;
                    writer.WriteValueSafe(owners[i]);
                    writer.WriteValueSafe((byte)abilities.Current);
                    writer.WriteValueSafe((short)(target != null ? Magnetic.Registered.IndexOf(target) : -1));
                }

                writer.WriteValueSafe(SyncedBody.All.Count);
                foreach (SyncedBody body in SyncedBody.All)
                    body.Write(ref writer);

                network.CustomMessagingManager.SendNamedMessage(SnapshotMessage, clients, writer, NetworkDelivery.UnreliableSequenced);
            }
            finally
            {
                writer.Dispose();
            }
        }

        void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int sceneIndex);
            reader.ReadValueSafe(out int load);
            if (sceneIndex != guestSceneIndex || load != guestRoomLoad)
            {
                // The host is in another room (or restarted this one): build the same one here.
                guestSceneIndex = sceneIndex;
                guestRoomLoad = load;
                roomReady = false;
                status = "Connected. Tab switches to a free creature.";
                SceneManager.LoadScene(sceneIndex);
                return;
            }
            if (!roomReady) return;

            reader.ReadValueSafe(out int creatureCount);
            if (creatureCount != creatures.Count)
            {
                status = "The host's game is a different version. Make sure you both have the same build.";
                return;
            }

            for (int i = 0; i < creatureCount; i++)
            {
                reader.ReadValueSafe(out ulong owner);
                reader.ReadValueSafe(out byte ability);
                reader.ReadValueSafe(out short target);
                owners[i] = owner;

                var abilities = creatures[i].GetComponent<CreatureAbilities>();
                if (abilities.Current != (AbilityType)ability)
                    abilities.Set((AbilityType)ability);
                MagnetAbility magnet = abilities.Get<MagnetAbility>();
                if (magnet != null)
                    magnet.ShowTarget(target >= 0 && target < Magnetic.Registered.Count ? Magnetic.Registered[target] : null);
            }

            reader.ReadValueSafe(out int bodyCount);
            if (bodyCount != SyncedBody.All.Count) return;
            foreach (SyncedBody body in SyncedBody.All)
                body.Read(ref reader);
        }

        static byte PackButtons(CreatureInput input)
        {
            byte buttons = 0;
            if (input.JumpHeld) buttons |= JumpHeld;
            if (input.JumpPressed) buttons |= JumpPressed;
            if (input.ActionHeld) buttons |= ActionHeld;
            if (input.ActionPressed) buttons |= ActionPressed;
            if (input.SwapPressed) buttons |= SwapPressed;
            return buttons;
        }

        static void ClearPressed(ref CreatureInput input)
        {
            input.JumpPressed = false;
            input.ActionPressed = false;
            input.SwapPressed = false;
        }

        static CreatureInput ReadLocalInput(CreatureController creature)
        {
            CreatureInput input = default;
            if (Keyboard.current != null)
                LocalInputRouter.Merge(ref input, LocalInputRouter.ReadKeyboard(Keyboard.current, Mouse.current, creature.transform.position));
            if (Gamepad.current != null)
                LocalInputRouter.Merge(ref input, LocalInputRouter.ReadGamepad(Gamepad.current));
            return input;
        }

        // ---------- UI ----------

        /// <summary>Online, a small panel in the room shows who's here, your code and Leave.
        /// Hosting and joining happen in the main menu.</summary>
        void OnGUI()
        {
            if (!Online || router == null) return;

            panelStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            var area = new Rect(Screen.width - 370, Screen.height - 170, 360, 160);
            GUILayout.BeginArea(area, panelStyle);

            string how = overSteam ? "on Steam" : "on LAN";
            GUILayout.Label(IsHost ? $"Hosting {how}: {network.ConnectedClientsIds.Count} player(s)" : $"Online {how}");
            if (IsHost && overSteam)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Your code: {steam.MySteamId}");
                if (GUILayout.Button("Copy", GUILayout.Width(60))) GUIUtility.systemCopyBuffer = steam.MySteamId.ToString();
                GUILayout.EndHorizontal();
                GUI.enabled = steam.CanInvite;
                if (GUILayout.Button("Invite Steam friends")) steam.OpenInviteDialog();
                GUI.enabled = true;
            }
            int mine = CreatureOf(network.LocalClientId);
            GUILayout.Label(mine >= 0 ? $"You are creature {mine + 1}. Tab = switch to a free creature." : "Waiting for a creature...");
            if (status.Length > 0) GUILayout.Label(status);
            GUILayout.EndArea();
        }

        static ulong ParseCode(string code) =>
            ulong.TryParse(code.Trim(), out ulong id) ? id : 0;
    }
}
