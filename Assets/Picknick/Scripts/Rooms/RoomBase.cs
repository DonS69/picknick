using System.Collections.Generic;
using Picknick.Abilities;
using Picknick.Game;
using Picknick.Net;
using Picknick.Puzzle;
using Picknick.Sandbox;
using UnityEngine;

namespace Picknick.Rooms
{
    /// <summary>
    /// What every room shares: the goal line and per-section tips, name tags, the exit,
    /// the "Room cleared" screen with Next room, and helpers for building puzzle pieces.
    /// </summary>
    public abstract class RoomBase : SandboxBootstrap
    {
        protected static readonly Color WoodColor = new Color(0.65f, 0.45f, 0.30f);
        protected static readonly Color ButtonColor = new Color(0.9f, 0.35f, 0.3f);

        protected ExitZone exit;
        readonly List<(Transform target, Vector2 offset, string text)> nameTags = new List<(Transform, Vector2, string)>();
        GUIStyle tagStyle;
        GUIStyle clearedStyle;

        protected override Vector2 SpawnOrigin => new Vector2(1f, 1f);
        protected override float KillHeight => -4f;

        /// <summary>For example "Room 1: The Kitchen Counter".</summary>
        protected abstract string Title { get; }

        /// <summary>What "done" looks like, for example where the exit is.</summary>
        protected abstract string Goal { get; }

        /// <summary>A step-by-step tip for the part of the room the keyboard creature is in.</summary>
        protected abstract string SectionHint(Vector2 position);

        protected override string Hint =>
            $"{Title}. {Goal}\nHOW TO: " + SectionHint(KeyboardCreature != null ? (Vector2)KeyboardCreature.transform.position : Vector2.zero);

        protected override void OnGUI()
        {
            DrawNameTags();
            base.OnGUI();
            if (exit == null) return;

            if (exit.Cleared)
                DrawCleared();
            else if (exit.Inside > 0)
                GUI.Label(new Rect(12, Screen.height - 30, 400, 24), $"At the exit: {exit.Inside}/{exit.Total}", HintStyle);
        }

        void DrawCleared()
        {
            clearedStyle ??= new GUIStyle(HintStyle) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            int seconds = Mathf.FloorToInt(exit.ClearTime);
            bool last = !GameFlow.HasNextRoom;
            string text = last ? $"You finished every room! {seconds / 60}:{seconds % 60:00}" : $"Room cleared! {seconds / 60}:{seconds % 60:00}";
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60), text, clearedStyle);

            GameFlow flow = GameFlow.Ensure();
            var buttons = new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.35f + 70f, 220f, 90f);
            GUILayout.BeginArea(buttons);
            if (flow.CanChangeRoom)
            {
                if (GUILayout.Button(last ? "Back to the menu" : "Next room", GUILayout.Height(36))) flow.NextRoom();
                if (GUILayout.Button("Play this room again", GUILayout.Height(28))) flow.RestartRoom();
            }
            else
            {
                GUILayout.Label("Waiting for the host to pick the next room.", HintStyle);
            }
            GUILayout.EndArea();
        }

        void DrawNameTags()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            tagStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.25f, 0.2f, 0.15f, 0.75f) },
            };

            foreach (var (target, offset, text) in nameTags)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                Vector3 screen = cam.WorldToScreenPoint((Vector2)target.position + offset);
                if (screen.z < 0f) continue;
                GUI.Label(new Rect(screen.x - 60f, Screen.height - screen.y - 10f, 120f, 20f), text, tagStyle);
            }
        }

        // ---------- Building helpers ----------

        /// <summary>A non-solid shape, sized in world units even under a scaled parent.</summary>
        protected void Decor(Transform parent, string name, Vector2 center, Vector2 size, Color color, int order)
        {
            GameObject go = Visual(parent, name, Vector2.zero, size, color, order);
            go.transform.position = center;
            // Keep world size even under a scaled parent (props are scaled squares).
            Vector3 parentScale = parent.lossyScale;
            go.transform.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, 1f);
        }

        /// <summary>A small italic label that follows target.</summary>
        protected void Tag(Transform target, Vector2 offset, string text) => nameTags.Add((target, offset, text));

        protected static Zone AddZone(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var zone = go.AddComponent<Zone>();
            zone.size = size;
            return zone;
        }

        protected static void AddCheckpoint(Transform parent, string name, Vector2 center, Vector2 size)
        {
            AddZone(parent, name, center, size).gameObject.AddComponent<Checkpoint>();
        }

        protected void AddExit(Transform parent, Vector2 center, Vector2 size)
        {
            exit = AddZone(parent, "Exit", center, size).gameObject.AddComponent<ExitZone>();
        }

        /// <summary>Water and the like: creatures (and items) that fall in pop back to their checkpoint.</summary>
        protected static void AddKillZone(Transform parent, string name, Vector2 center, Vector2 size)
        {
            AddZone(parent, name, center, size).gameObject.AddComponent<KillZone>();
        }

        /// <summary>A floor button. topCenter is the middle of the floor surface it sits on.</summary>
        protected PressurePlate AddPlate(Transform parent, string name, Vector2 topCenter, float width)
        {
            GameObject go = Visual(parent, name, topCenter + new Vector2(0f, 0.06f), new Vector2(width, 0.12f), ButtonColor, -1);
            go.AddComponent<SyncedBody>();
            var zone = go.AddComponent<Zone>();
            zone.size = new Vector2(width, 1.2f);
            return go.AddComponent<PressurePlate>();
        }

        /// <summary>A door that slides up into the wall above it while held, pressed or latched open.</summary>
        protected HoldDoor AddDoor(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            GameObject go = Visual(parent, name, center, size, color, 0);
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Rigidbody2D>();
            var door = go.AddComponent<HoldDoor>();
            go.AddComponent<SyncedBody>();
            door.openHeight = size.y + 0.1f;
            return door;
        }

        /// <summary>Pull it with a Magnet for a moment and it latches the door open for good.</summary>
        protected GameObject AddLever(Transform parent, string name, Vector2 center, HoldDoor door)
        {
            GameObject go = Visual(parent, name, center, new Vector2(0.25f, 0.7f), MetalColor, 0);
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Magnetic>();
            go.AddComponent<MagnetLever>().door = door;
            return go;
        }

        /// <summary>Fixed metal: Magnets fly to it like a grappling hook.</summary>
        protected GameObject AddAnchor(Transform parent, string name, Vector2 center, Vector2 size)
        {
            GameObject go = Block(parent, name, center, size, MetalColor);
            go.AddComponent<Magnetic>();
            return go;
        }
    }
}
