using System.Collections.Generic;
using Picknick.Abilities;
using Picknick.Creature;
using Picknick.Net;
using Picknick.Puzzle;
using Picknick.Sandbox;
using UnityEngine;

namespace Picknick.Rooms
{
    /// <summary>
    /// Room 1, "The Kitchen Counter", in graybox (see docs/prototype-plan.md, section 2).
    /// Left to right: A the can, B the sink gap, C the cupboard door, D the fridge finale.
    /// Everyone must end up in the mouse hole on top of the fridge.
    /// </summary>
    public class KitchenRoom : SandboxBootstrap
    {
        static readonly Color WoodColor = new Color(0.65f, 0.45f, 0.30f);
        static readonly Color CounterColor = new Color(0.84f, 0.68f, 0.48f);
        static readonly Color CabinetDoorColor = new Color(0.76f, 0.59f, 0.40f);
        static readonly Color StoneColor = new Color(0.35f, 0.35f, 0.38f);
        static readonly Color WallColor = new Color(0.86f, 0.78f, 0.62f);
        static readonly Color FloorColor = new Color(0.72f, 0.68f, 0.63f);
        static readonly Color FridgeColor = new Color(0.93f, 0.95f, 0.97f);
        static readonly Color SteelColor = new Color(0.62f, 0.66f, 0.7f);
        static readonly Color KeyColor = new Color(1f, 0.8f, 0.2f);

        ExitZone exit;
        readonly List<(Transform target, Vector2 offset, string text)> nameTags = new List<(Transform, Vector2, string)>();
        GUIStyle tagStyle;

        protected override Color BackgroundColor => new Color(0.99f, 0.95f, 0.82f);

        protected override Vector2 SpawnOrigin => new Vector2(1f, 1f);
        protected override float KillHeight => -4f;

        protected override string Hint =>
            "Room 1: The Kitchen Counter. Get all four creatures into the mouse hole on top of the fridge (far right).\n" +
            "HOW TO: " + SectionHint();

        /// <summary>A step-by-step tip for the section the keyboard creature is in.</summary>
        string SectionHint()
        {
            float x = KeyboardCreature != null ? KeyboardCreature.transform.position.x : 0f;
            if (x < 20f)
                return "Get everyone onto the counter. Strong (red badge): walk into a teammate, press E to pick them up, " +
                       "point the mouse at the counter and press E to throw. Last, Tab to a Magnet on the counter, stand a few steps " +
                       "back from the edge, point at Strong below and hold left click to heave it up.";
            if (x < 30f)
                return "Cross the sink. Feather (white badge): jump and HOLD Space to glide over. Magnets (blue badge): hold left click " +
                       "on the faucet to swing across, then from the far side hold left click on a teammate to pull them over.";
            if (x < 44f)
                return "Strong: stand right next to the cupboard door to hold it open while the others walk through. " +
                       "Then a Magnet on the far side holds left click on the spoon until it turns green: the door stays open for Strong.";
            return "Stand on the cookie box (or a friend) and hold left click on the key with a Magnet to knock it down. " +
                   "Magnets: grapple the fridge magnet to get on top, then pull the others up and pull the key to the hatch.";
        }

        protected override void BuildLevel()
        {
            var level = new GameObject("Kitchen").transform;

            // Room shell.
            Block(level, "Floor", new Vector2(4.5f, -0.5f), new Vector2(11f, 1f), FloorColor);
            Block(level, "Wall Left", new Vector2(-1.5f, 6.5f), new Vector2(1f, 15f), WallColor);
            Block(level, "Wall Right", new Vector2(58.5f, 7.5f), new Vector2(1f, 17f), WallColor);

            // A: "The Can". The counter is 3 high: push the can over, stack, or get thrown.
            Block(level, "Counter A", new Vector2(15f, 1f), new Vector2(10f, 4f), CounterColor);
            GameObject can = Prop(level, "Tin Can", new Vector2(6f, 0.6f), new Vector2(1.2f, 1.2f), 4f, HeavyColor);
            can.AddComponent<Heavy>();
            Decor(can.transform, "Can Label", new Vector2(6f, 0.6f), new Vector2(1.2f, 0.45f), new Color(0.85f, 0.25f, 0.2f), 1);
            Tag(can.transform, new Vector2(0f, 1f), "tin can");
            AddCheckpoint(level, "Checkpoint Start", new Vector2(3f, 1.5f), new Vector2(8f, 3f));
            AddCheckpoint(level, "Checkpoint Counter", new Vector2(14f, 4.5f), new Vector2(8f, 3f));

            // B: "The Sink Gap". 4 wide: just too far to jump. Glide, get carried,
            // get pulled across by a Magnet, or grapple the faucet.
            Block(level, "Counter B", new Vector2(37f, 1f), new Vector2(26f, 4f), CounterColor);
            Block(level, "Faucet", new Vector2(22.8f, 6.2f), new Vector2(0.6f, 0.6f), MetalColor).AddComponent<Magnetic>();
            AddCheckpoint(level, "Checkpoint Sink", new Vector2(27f, 4.5f), new Vector2(5f, 3f));

            // C: "The Cupboard Door". Strong holds it open from the left; someone on the
            // right must pull the spoon lever with a Magnet to latch it, or Strong is stuck.
            Block(level, "Cupboard Ceiling", new Vector2(37f, 6f), new Vector2(14f, 1f), WoodColor);
            var holdZone = AddZone(level, "Door Hold Zone", new Vector2(33.3f, 3.6f), new Vector2(1.4f, 1.2f));
            GameObject doorObject = Visual(level, "Cupboard Door", new Vector2(34.3f, 4.25f), new Vector2(0.6f, 2.5f), WoodColor, 0);
            doorObject.AddComponent<BoxCollider2D>();
            doorObject.AddComponent<Rigidbody2D>();
            var door = doorObject.AddComponent<HoldDoor>();
            doorObject.AddComponent<SyncedBody>();
            door.holdZone = holdZone;
            GameObject spoon = Visual(level, "Spoon Lever", new Vector2(39f, 5.1f), new Vector2(0.25f, 0.7f), MetalColor, 0);
            spoon.AddComponent<BoxCollider2D>();
            spoon.AddComponent<Magnetic>();
            spoon.AddComponent<MagnetLever>().door = door;
            AddCheckpoint(level, "Checkpoint Cupboard", new Vector2(40.5f, 4.2f), new Vector2(3f, 2.4f));

            // D: "The Fridge". Knock the key off its hook with a Magnet, get everyone up
            // the fridge (cookie box, fridge magnet, throws), bring the key to the hatch.
            Block(level, "Fridge", new Vector2(54f, 4f), new Vector2(8f, 10f), FridgeColor);
            Block(level, "Fridge Magnet", new Vector2(51f, 10.6f), new Vector2(0.6f, 0.6f), MetalColor).AddComponent<Magnetic>();
            Block(level, "Cabinet", new Vector2(55.5f, 12f), new Vector2(5f, 1f), WoodColor);
            GameObject cookies = Prop(level, "Cookie Box", new Vector2(48.5f, 3.75f), new Vector2(1.5f, 1.5f), 1.5f, CrateColor);
            Decor(cookies.transform, "Cookie", new Vector2(48.5f, 3.75f), new Vector2(0.7f, 0.7f), new Color(0.55f, 0.35f, 0.2f), 1);
            Tag(cookies.transform, new Vector2(0f, 1.05f), "cookie box");

            Visual(level, "Hook", new Vector2(45.5f, 11.2f), new Vector2(0.1f, 0.5f), Color.black, 0);
            GameObject key = Prop(level, "Key", new Vector2(45.5f, 10.8f), new Vector2(0.5f, 0.3f), 0.3f, KeyColor);
            key.AddComponent<Magnetic>();
            key.AddComponent<HookedItem>();
            key.AddComponent<FallRespawn>().checkpoint = new Vector2(46f, 4f);

            GameObject hatch = Block(level, "Hatch", new Vector2(53.7f, 10.25f), new Vector2(0.6f, 2.5f), WoodColor);
            var hatchLock = hatch.AddComponent<Hatch>();
            hatch.AddComponent<SyncedBody>();
            hatchLock.key = key.transform;
            hatchLock.keyhole = AddZone(level, "Keyhole", new Vector2(52.6f, 10f), new Vector2(1.6f, 2f));
            Visual(level, "Keyhole Mark", new Vector2(53.7f, 10.25f), new Vector2(0.2f, 0.35f), KeyColor, 1).transform.SetParent(hatch.transform, true);
            AddCheckpoint(level, "Checkpoint Fridge", new Vector2(51.8f, 10.2f), new Vector2(3.4f, 2.4f));

            Visual(level, "Mouse Hole", new Vector2(57.7f, 9.75f), new Vector2(0.6f, 1.5f), new Color(0.15f, 0.1f, 0.08f), 0);
            exit = AddZone(level, "Exit", new Vector2(56f, 10.25f), new Vector2(4f, 2.5f)).gameObject.AddComponent<ExitZone>();
            Tag(key.transform, new Vector2(0f, 0.45f), "key");

            Decorate(level);
        }

        /// <summary>Non-solid shapes and name tags so the graybox reads as a kitchen.</summary>
        void Decorate(Transform level)
        {
            // Window on the back wall.
            Decor(level, "Window Frame", new Vector2(4f, 7f), new Vector2(4.2f, 3.2f), Color.white, -5);
            Decor(level, "Window Glass", new Vector2(4f, 7f), new Vector2(3.8f, 2.8f), new Color(0.7f, 0.86f, 0.98f), -4);
            Decor(level, "Window Bar V", new Vector2(4f, 7f), new Vector2(0.15f, 2.8f), Color.white, -4);
            Decor(level, "Window Bar H", new Vector2(4f, 7f), new Vector2(3.8f, 0.15f), Color.white, -4);

            // Stone worktops and cabinet doors on the counters.
            Decor(level, "Worktop A", new Vector2(15f, 2.88f), new Vector2(10f, 0.25f), StoneColor, -2);
            Decor(level, "Worktop B", new Vector2(37f, 2.88f), new Vector2(26f, 0.25f), StoneColor, -2);
            for (float x = 11.5f; x < 20f; x += 3f) CabinetDoor(level, x);
            for (float x = 25.5f; x < 50f; x += 3f) CabinetDoor(level, x);
            Tag(level, new Vector2(15f, 1.2f), "counter");

            // Sink basin, water and faucet pipe (the faucet head is the metal block).
            Decor(level, "Sink Basin", new Vector2(22f, 1f), new Vector2(4f, 4f), SteelColor, -5);
            Decor(level, "Sink Water", new Vector2(22f, -0.6f), new Vector2(4f, 0.8f), new Color(0.45f, 0.7f, 0.95f), -4);
            Decor(level, "Faucet Pipe", new Vector2(24.3f, 4.6f), new Vector2(0.3f, 3.2f), SteelColor, -4);
            Decor(level, "Faucet Arm", new Vector2(23.55f, 6.2f), new Vector2(1.5f, 0.3f), SteelColor, -4);
            Tag(level, new Vector2(22f, 0.4f), "sink");
            Tag(level, new Vector2(22.8f, 6.8f), "faucet");

            // Inside the cupboard.
            Decor(level, "Cupboard Back", new Vector2(37f, 4.25f), new Vector2(14f, 2.5f), new Color(0.5f, 0.35f, 0.24f), -5);
            Tag(level, new Vector2(37f, 6.7f), "cupboard");
            Tag(level, new Vector2(39f, 4.4f), "spoon");

            // Fridge details and the cabinet above it.
            Decor(level, "Fridge Seam", new Vector2(54f, 6.5f), new Vector2(8f, 0.08f), SteelColor, -2);
            Decor(level, "Fridge Handle Low", new Vector2(50.4f, 4.5f), new Vector2(0.15f, 2.4f), SteelColor, -2);
            Decor(level, "Fridge Handle High", new Vector2(50.4f, 7.8f), new Vector2(0.15f, 1.2f), SteelColor, -2);
            Tag(level, new Vector2(54f, 5f), "fridge");
            Tag(level, new Vector2(56.5f, 11f), "mouse hole");
        }

        void CabinetDoor(Transform level, float x)
        {
            Decor(level, "Cabinet Door", new Vector2(x, 1.2f), new Vector2(2.6f, 2.8f), CabinetDoorColor, -2);
            Decor(level, "Cabinet Knob", new Vector2(x + 0.9f, 1.9f), new Vector2(0.15f, 0.15f), WoodColor, -2);
        }

        void Decor(Transform parent, string name, Vector2 center, Vector2 size, Color color, int order)
        {
            GameObject go = Visual(parent, name, Vector2.zero, size, color, order);
            go.transform.position = center;
            // Keep world size even under a scaled parent (props are scaled squares).
            Vector3 parentScale = parent.lossyScale;
            go.transform.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, 1f);
        }

        void Tag(Transform target, Vector2 offset, string text) => nameTags.Add((target, offset, text));

        protected override void OnGUI()
        {
            DrawNameTags();
            base.OnGUI();
            if (exit == null) return;

            if (exit.Cleared)
            {
                var style = new GUIStyle(HintStyle) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                int seconds = Mathf.FloorToInt(exit.ClearTime);
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60), $"Room cleared! {seconds / 60}:{seconds % 60:00}", style);
            }
            else if (exit.Inside > 0)
            {
                GUI.Label(new Rect(12, Screen.height - 30, 400, 24), $"In the mouse hole: {exit.Inside}/{exit.Total}", HintStyle);
            }
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
                if (target == null) continue;
                Vector3 screen = cam.WorldToScreenPoint((Vector2)target.position + offset);
                if (screen.z < 0f) continue;
                GUI.Label(new Rect(screen.x - 60f, Screen.height - screen.y - 10f, 120f, 20f), text, tagStyle);
            }
        }

        static Zone AddZone(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var zone = go.AddComponent<Zone>();
            zone.size = size;
            return zone;
        }

        static void AddCheckpoint(Transform parent, string name, Vector2 center, Vector2 size)
        {
            AddZone(parent, name, center, size).gameObject.AddComponent<Checkpoint>();
        }
    }
}
