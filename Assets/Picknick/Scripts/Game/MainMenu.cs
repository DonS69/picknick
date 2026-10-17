using UnityEngine;

namespace Picknick.Game
{
    /// <summary>
    /// The start screen: pick a room, then play on this PC, host for friends on Steam,
    /// or join a friend with their code. LAN is tucked at the bottom for testing.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        const float DesignHeight = 720f;

        static readonly Color Background = new Color(0.99f, 0.93f, 0.8f);
        static readonly Color Ink = new Color(0.25f, 0.2f, 0.15f);
        static readonly Color[] CreatureColors =
        {
            new Color(1.00f, 0.55f, 0.45f), new Color(0.45f, 0.75f, 1.00f),
            new Color(0.60f, 0.90f, 0.50f), new Color(1.00f, 0.85f, 0.35f),
        };

        int room;
        string friendCode = "";
        Texture2D white;
        GUIStyle titleStyle, subtitleStyle, textStyle, buttonStyle, bigButtonStyle, fieldStyle;

        void Awake()
        {
            GameFlow.Ensure();
            white = Texture2D.whiteTexture;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Background;
            }
        }

        void OnGUI()
        {
            GameFlow flow = GameFlow.Ensure();
            BuildStyles();

            // Lay out for a 720-pixel-tall screen and scale to the real one.
            float scale = Screen.height / DesignHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            DrawCreatures(width);

            var panel = new Rect(width * 0.5f - 230f, 40f, 460f, 640f);
            GUILayout.BeginArea(panel);
            GUILayout.Label("Picknick", titleStyle);
            GUILayout.Label("Tiny creatures, giant house. Everyone has to make it out.", subtitleStyle);
            GUILayout.Space(16);

            if (flow.Net.Online)
                DrawJoining(flow);
            else
                DrawMain(flow);

            if (flow.Net.Status.Length > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label(flow.Net.Status, textStyle);
            }
            GUILayout.EndArea();
        }

        void DrawMain(GameFlow flow)
        {
            GUILayout.Label("Choose a room", textStyle);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < GameFlow.Rooms.Length; i++)
            {
                GUI.backgroundColor = room == i ? new Color(0.55f, 1f, 0.55f) : Color.white;
                if (GUILayout.Button($"{i + 1}. {GameFlow.RoomTitles[i]}", buttonStyle, GUILayout.Height(44)))
                    room = i;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            if (GUILayout.Button("Play on this PC", bigButtonStyle, GUILayout.Height(52)))
                flow.PlayOffline(room);
            GUILayout.Label("Keyboard plus one gamepad per creature. Tab switches creature.", textStyle);
            GUILayout.Space(10);

            if (flow.Net.Steam.Ready)
            {
                GUILayout.Label($"Online with Steam friends (signed in as {flow.Net.Steam.MyName})", textStyle);
                if (GUILayout.Button("Host on Steam", bigButtonStyle, GUILayout.Height(52)))
                    flow.HostSteam(room);
                GUILayout.BeginHorizontal();
                friendCode = GUILayout.TextField(friendCode, fieldStyle, GUILayout.Height(36), GUILayout.Width(250));
                if (GUILayout.Button("Join friend's code", buttonStyle, GUILayout.Height(36)))
                    flow.Net.JoinSteam(friendCode);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Online play needs Steam: " + flow.Net.Steam.Problem, textStyle);
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("LAN / this PC", textStyle, GUILayout.Width(110));
            flow.Net.Address = GUILayout.TextField(flow.Net.Address, fieldStyle, GUILayout.Height(30), GUILayout.Width(140));
            if (GUILayout.Button("Host", buttonStyle, GUILayout.Height(30))) flow.HostLan(room);
            if (GUILayout.Button("Join", buttonStyle, GUILayout.Height(30))) flow.Net.JoinLan();
            GUILayout.EndHorizontal();

            GUILayout.Space(14);
            if (GUILayout.Button("Quit", buttonStyle, GUILayout.Height(36)))
                GameFlow.Quit();
        }

        /// <summary>Connecting as a guest: the host's room loads as soon as we're in.</summary>
        void DrawJoining(GameFlow flow)
        {
            GUILayout.Label(flow.Net.IsHost ? "Starting..." : "Joining your friend...", textStyle);
            if (GUILayout.Button("Cancel", buttonStyle, GUILayout.Height(40)))
                flow.Net.Leave();
        }

        /// <summary>Four creatures hopping along the bottom of the screen.</summary>
        void DrawCreatures(float width)
        {
            for (int i = 0; i < CreatureColors.Length; i++)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 3f + i * 0.9f)) * 40f;
                float x = width * 0.5f + (i - 1.5f) * 110f - 30f;
                float y = DesignHeight - 90f - hop;
                GUI.color = CreatureColors[i];
                GUI.DrawTexture(new Rect(x, y, 60f, 60f), white);
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(x + 15f, y + 14f, 9f, 14f), white);
                GUI.DrawTexture(new Rect(x + 36f, y + 14f, 9f, 14f), white);
            }
            GUI.color = Color.white;
        }

        void BuildStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = Ink;
            subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            subtitleStyle.normal.textColor = Ink;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            textStyle.normal.textColor = Ink;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            bigButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        }
    }
}
