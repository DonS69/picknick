using Picknick.Abilities;
using Picknick.Creature;
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
        static readonly Color KeyColor = new Color(1f, 0.8f, 0.2f);

        ExitZone exit;

        protected override Vector2 SpawnOrigin => new Vector2(1f, 1f);
        protected override float KillHeight => -4f;

        protected override string Hint =>
            "Room 1: The Kitchen Counter. Get all four creatures into the mouse hole on top of the fridge (far right).\n" +
            "Dark can = Strong only. Blue = metal for Magnets. The cupboard door opens while Strong stands next to it.";

        protected override void BuildLevel()
        {
            var level = new GameObject("Kitchen").transform;

            // Room shell.
            Block(level, "Floor", new Vector2(4.5f, -0.5f), new Vector2(11f, 1f), GroundColor);
            Block(level, "Wall Left", new Vector2(-1.5f, 6.5f), new Vector2(1f, 15f), GroundColor);
            Block(level, "Wall Right", new Vector2(58.5f, 7.5f), new Vector2(1f, 17f), GroundColor);

            // A: "The Can". The counter is 3 high: push the can over, stack, or get thrown.
            Block(level, "Counter A", new Vector2(15f, 1f), new Vector2(10f, 4f), GroundColor);
            Prop(level, "Tin Can", new Vector2(6f, 0.6f), new Vector2(1.2f, 1.2f), 4f, HeavyColor).AddComponent<Heavy>();
            AddCheckpoint(level, "Checkpoint Start", new Vector2(3f, 1.5f), new Vector2(8f, 3f));
            AddCheckpoint(level, "Checkpoint Counter", new Vector2(14f, 4.5f), new Vector2(8f, 3f));

            // B: "The Sink Gap". 4 wide: just too far to jump. Glide, get carried,
            // get pulled across by a Magnet, or grapple the faucet.
            Block(level, "Counter B", new Vector2(37f, 1f), new Vector2(26f, 4f), GroundColor);
            Block(level, "Faucet", new Vector2(22.8f, 6.2f), new Vector2(0.6f, 0.6f), MetalColor).AddComponent<Magnetic>();
            AddCheckpoint(level, "Checkpoint Sink", new Vector2(27f, 4.5f), new Vector2(5f, 3f));

            // C: "The Cupboard Door". Strong holds it open from the left; someone on the
            // right must pull the spoon lever with a Magnet to latch it, or Strong is stuck.
            Block(level, "Cupboard Ceiling", new Vector2(37f, 6f), new Vector2(14f, 1f), GroundColor);
            var holdZone = AddZone(level, "Door Hold Zone", new Vector2(33.3f, 3.6f), new Vector2(1.4f, 1.2f));
            GameObject doorObject = Visual(level, "Cupboard Door", new Vector2(34.3f, 4.25f), new Vector2(0.6f, 2.5f), WoodColor, 0);
            doorObject.AddComponent<BoxCollider2D>();
            doorObject.AddComponent<Rigidbody2D>();
            var door = doorObject.AddComponent<HoldDoor>();
            door.holdZone = holdZone;
            GameObject spoon = Visual(level, "Spoon Lever", new Vector2(39f, 5.1f), new Vector2(0.25f, 0.7f), MetalColor, 0);
            spoon.AddComponent<BoxCollider2D>();
            spoon.AddComponent<Magnetic>();
            spoon.AddComponent<MagnetLever>().door = door;
            AddCheckpoint(level, "Checkpoint Cupboard", new Vector2(40.5f, 4.2f), new Vector2(3f, 2.4f));

            // D: "The Fridge". Knock the key off its hook with a Magnet, get everyone up
            // the fridge (cookie box, fridge magnet, throws), bring the key to the hatch.
            Block(level, "Fridge", new Vector2(54f, 4f), new Vector2(8f, 10f), new Color(0.85f, 0.88f, 0.9f));
            Block(level, "Fridge Magnet", new Vector2(51f, 10.6f), new Vector2(0.6f, 0.6f), MetalColor).AddComponent<Magnetic>();
            Block(level, "Cabinet", new Vector2(55.5f, 12f), new Vector2(5f, 1f), GroundColor);
            Prop(level, "Cookie Box", new Vector2(48.5f, 3.75f), new Vector2(1.5f, 1.5f), 1.5f, CrateColor);

            Visual(level, "Hook", new Vector2(45.5f, 11.2f), new Vector2(0.1f, 0.5f), Color.black, 0);
            GameObject key = Prop(level, "Key", new Vector2(45.5f, 10.8f), new Vector2(0.5f, 0.3f), 0.3f, KeyColor);
            key.AddComponent<Magnetic>();
            key.AddComponent<HookedItem>();
            key.AddComponent<FallRespawn>().checkpoint = new Vector2(46f, 4f);

            GameObject hatch = Block(level, "Hatch", new Vector2(53.7f, 10.25f), new Vector2(0.6f, 2.5f), WoodColor);
            var hatchLock = hatch.AddComponent<Hatch>();
            hatchLock.key = key.transform;
            hatchLock.keyhole = AddZone(level, "Keyhole", new Vector2(52.6f, 10f), new Vector2(1.6f, 2f));
            Visual(level, "Keyhole Mark", new Vector2(53.7f, 10.25f), new Vector2(0.2f, 0.35f), KeyColor, 1).transform.SetParent(hatch.transform, true);
            AddCheckpoint(level, "Checkpoint Fridge", new Vector2(51.8f, 10.2f), new Vector2(3.4f, 2.4f));

            Visual(level, "Mouse Hole", new Vector2(57.7f, 9.75f), new Vector2(0.6f, 1.5f), new Color(0.15f, 0.1f, 0.08f), 0);
            exit = AddZone(level, "Exit", new Vector2(56f, 10.25f), new Vector2(4f, 2.5f)).gameObject.AddComponent<ExitZone>();
        }

        protected override void OnGUI()
        {
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
