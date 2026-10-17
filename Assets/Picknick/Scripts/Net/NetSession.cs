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

namespace Picknick.Net
{
    /// <summary>
    /// Online play. One player hosts and simulates the whole room; everyone else joins,
    /// sends their input to the host and shows the snapshots the host sends back.
    /// Every peer builds the same room locally, so nothing has to be spawned over the network.
    /// Each player controls one creature; Tab switches to the next creature nobody controls.
    /// Two ways to connect: Steam (peer to peer by Steam ID, works over the internet) or
    /// LAN / this PC (IP address, also what Multiplayer Play Mode windows use).
    /// </summary>
    public class NetSession : MonoBehaviour
    {
        const string InputMessage = "picknick.input";
        const string SwitchMessage = "picknick.switch";
        const string SnapshotMessage = "picknick.snapshot";
        const ulong NoOwner = ulong.MaxValue;

        public string address = "127.0.0.1";
        public ushort port = 7777;
        [Tooltip("Physics steps between snapshots. At the default 50 steps/s, 2 means 25 snapshots/s.")]
        public int snapshotInterval = 2;

        LocalInputRouter router;
        List<CreatureController> creatures;
        NetworkManager network;
        UnityTransport transport;
        SteamNetworkingSocketsTransport steamTransport;
        SteamService steam;
        bool overSteam;
        string friendCode = "";
        ulong[] owners;
        CreatureInput[] pending;
        int fixedSteps;
        bool leaveRequested;
        string status = "";
        GUIStyle panelStyle;

        bool Online => network != null && network.IsListening;
        bool IsHost => Online && network.IsServer;

        public void Init(LocalInputRouter inputRouter)
        {
            router = inputRouter;
            creatures = inputRouter.Creatures;
        }

        void Awake()
        {
            steam = gameObject.AddComponent<SteamService>();
            steam.JoinHost += hostId => { if (!Online) StartSteam(false, hostId); };
        }

        void Start()
        {
            owners = new ulong[creatures.Count];
            pending = new CreatureInput[creatures.Count];
            ClearOwners();
        }

        void Update()
        {
            if (leaveRequested)
            {
                leaveRequested = false;
                GoOffline();
            }
            if (!Online) return;

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
            if (!IsHost) return;
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
                steam.CreateLobby(owners.Length);
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
                foreach (CreatureController creature in creatures)
                    creature.SetInput(default);
                status = $"Joining {address}...";
            }

            router.enabled = false;
        }

        /// <summary>
        /// Made once and reused for every session. Destroying it ourselves right after a shutdown
        /// makes Netcode shut down twice and touch an already-closed transport.
        /// </summary>
        void CreateNetworkManager()
        {
            var go = new GameObject("NetworkManager");
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
            router.enabled = true;
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
            status = "Disconnected from the host.";
            leaveRequested = true;
        }

        // ---------- Creatures and owners (host decides) ----------

        void ClearOwners()
        {
            if (owners == null) return;
            for (int i = 0; i < owners.Length; i++)
            {
                owners[i] = NoOwner;
                pending[i] = default;
            }
        }

        int CreatureOf(ulong player)
        {
            if (owners == null) return -1;
            for (int i = 0; i < owners.Length; i++)
                if (owners[i] == player) return i;
            return -1;
        }

        void AssignFreeCreature(ulong player)
        {
            if (CreatureOf(player) >= 0) return;
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] != NoOwner) continue;
                owners[i] = player;
                return;
            }
        }

        void SwitchCreature(ulong player)
        {
            int current = CreatureOf(player);
            for (int step = 1; step <= owners.Length; step++)
            {
                int next = ((current < 0 ? -1 : current) + step + owners.Length) % owners.Length;
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
            if (creature < 0 || creature >= owners.Length || owners[creature] != sender)
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

            int size = 16 + creatures.Count * 16 + SyncedBody.All.Count * 24;
            var writer = new FastBufferWriter(size, Allocator.Temp);
            try
            {
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
            reader.ReadValueSafe(out int creatureCount);
            if (creatureCount != creatures.Count)
            {
                status = "The host is playing a different room. Open the same scene on both.";
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

        void OnGUI()
        {
            panelStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            var area = new Rect(Screen.width - 370, Screen.height - 230, 360, 220);
            GUILayout.BeginArea(area, panelStyle);

            if (!Online)
            {
                if (steam.Ready)
                {
                    GUILayout.Label($"Steam: {steam.MyName}");
                    if (GUILayout.Button("Host on Steam")) StartSteam(true);
                    GUILayout.BeginHorizontal();
                    friendCode = GUILayout.TextField(friendCode, GUILayout.Width(200));
                    if (GUILayout.Button("Join friend's code")) StartSteam(false, ParseCode(friendCode));
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label(steam.Problem);
                }

                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.Label("LAN / this PC", GUILayout.Width(90));
                address = GUILayout.TextField(address);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host LAN")) StartLan(true);
                if (GUILayout.Button("Join LAN")) StartLan(false);
                GUILayout.EndHorizontal();
            }
            else
            {
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
                if (GUILayout.Button("Leave")) leaveRequested = true;
            }

            if (status.Length > 0) GUILayout.Label(status);
            GUILayout.EndArea();
        }

        static ulong ParseCode(string code) =>
            ulong.TryParse(code.Trim(), out ulong id) ? id : 0;
    }
}
