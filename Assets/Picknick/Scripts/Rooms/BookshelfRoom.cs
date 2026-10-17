using Picknick.Abilities;
using Picknick.Puzzle;
using UnityEngine;

namespace Picknick.Rooms
{
    /// <summary>
    /// Room 3, "The Bookshelf" (living room). A: climb the bookshelf to its top board.
    /// B: cross over the fish tank to the mantelpiece. C: two buttons hold the mirror gate
    /// open, and a lever behind it latches it so the button-sitters can follow.
    /// Everyone must end up in the mouse hole at the end of the mantelpiece.
    /// </summary>
    public class BookshelfRoom : RoomBase
    {
        static readonly Color WallpaperColor = new Color(0.93f, 0.85f, 0.78f);
        static readonly Color FloorColor = new Color(0.55f, 0.4f, 0.3f);
        static readonly Color ShelfColor = new Color(0.58f, 0.38f, 0.24f);
        static readonly Color MantelColor = new Color(0.8f, 0.76f, 0.7f);
        static readonly Color GlassColor = new Color(0.75f, 0.9f, 0.95f, 0.5f);
        static readonly Color WaterColor = new Color(0.35f, 0.65f, 0.9f, 0.8f);

        static readonly Color[] BookColors =
        {
            new Color(0.75f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 0.75f), new Color(0.35f, 0.65f, 0.4f),
            new Color(0.85f, 0.7f, 0.3f), new Color(0.55f, 0.4f, 0.7f),
        };

        protected override Color BackgroundColor => new Color(0.97f, 0.9f, 0.82f);

        protected override string Title => "Room 3: The Bookshelf";
        protected override string Goal => "Get all four creatures into the mouse hole at the end of the mantelpiece (far right).";

        protected override string SectionHint(Vector2 position)
        {
            if (position.y < 5.5f && position.x < 24f)
                return "Climb the bookshelf: jump on a teammate's head to reach the lower shelf. To reach the top board, Magnets (blue badge) " +
                       "hold left click on the coat hook to fly up, and Strong (red badge) picks up Feather with E and throws them up (aim, E). " +
                       "From the top, a Magnet at the very edge holds left click on whoever is left. Strong is heavy: two Magnets pull it faster.";
            if (position.x < 35f)
                return "Cross over the fish tank. Feather (white badge): jump and HOLD Space to glide. Magnets: hold left click on the lamp " +
                       "to swing over, then pull teammates across from the mantelpiece. Two Magnets together pull Strong easily.";
            return "The mirror gate opens only while BOTH red buttons are pressed. Strong: push the clock onto one, someone sits on the other. " +
                   "Then a Magnet behind the gate holds left click on the lever until it turns green: the gate stays open for everyone.";
        }

        protected override void BuildLevel()
        {
            var level = new GameObject("Living Room").transform;

            // Room shell.
            Block(level, "Floor", new Vector2(26.5f, -0.5f), new Vector2(56f, 1f), FloorColor);
            Block(level, "Wall Left", new Vector2(-1.5f, 9f), new Vector2(1f, 20f), WallpaperColor);
            Block(level, "Wall Right", new Vector2(54.5f, 9f), new Vector2(1f, 20f), WallpaperColor);

            // A: "The Bookshelf". The lower shelf (2.5 high) takes a head-jump. The top board
            // (another 3.5 up) takes a Magnet on the coat hook, or a Feather thrown by Strong.
            Block(level, "Lower Shelf", new Vector2(15f, 1.25f), new Vector2(18f, 2.5f), ShelfColor);
            Block(level, "Top Board", new Vector2(22f, 5.8f), new Vector2(16f, 0.4f), ShelfColor);
            GameObject hook = AddAnchor(level, "Coat Hook", new Vector2(13.2f, 8f), new Vector2(0.4f, 0.4f));
            Tag(hook.transform, new Vector2(0f, 0.5f), "coat hook");
            AddCheckpoint(level, "Checkpoint Start", new Vector2(3f, 1.5f), new Vector2(8f, 3f));
            AddCheckpoint(level, "Checkpoint Lower Shelf", new Vector2(10f, 3.6f), new Vector2(8f, 2.2f));
            AddCheckpoint(level, "Checkpoint Top Board", new Vector2(20f, 7.2f), new Vector2(12f, 2.4f));

            // B: "The Fish Tank". A 5-wide gap from the top board to the mantelpiece, water below.
            Decor(level, "Tank Glass", new Vector2(29.75f, 2.25f), new Vector2(10.5f, 4.5f), GlassColor, -5);
            Decor(level, "Tank Water", new Vector2(29.75f, 1.5f), new Vector2(10.3f, 3f), WaterColor, -4);
            AddKillZone(level, "Tank Water Zone", new Vector2(29.5f, 1.5f), new Vector2(11f, 3f));
            GameObject lamp = AddAnchor(level, "Lamp", new Vector2(34f, 8.5f), new Vector2(1.2f, 0.5f));
            Tag(lamp.transform, new Vector2(0f, 0.6f), "lamp");
            Block(level, "Mantelpiece", new Vector2(44.75f, 3f), new Vector2(19.5f, 6f), MantelColor);

            // C: "The Mirror Gate". Both buttons down opens it; the lever behind it latches it open.
            GameObject clock = Prop(level, "Clock", new Vector2(37f, 6.4f), new Vector2(1f, 0.8f), 4f, HeavyColor);
            clock.AddComponent<Heavy>();
            Decor(clock.transform, "Clock Face", new Vector2(37f, 6.4f), new Vector2(0.6f, 0.6f), Color.white, 1);
            Tag(clock.transform, new Vector2(0f, 0.75f), "clock");
            PressurePlate left = AddPlate(level, "Button Left", new Vector2(39f, 6f), 1.8f);
            PressurePlate right = AddPlate(level, "Button Right", new Vector2(42f, 6f), 1.8f);
            Tag(left.transform, new Vector2(0f, 0.5f), "button");
            Tag(right.transform, new Vector2(0f, 0.5f), "button");
            Block(level, "Mirror Wall", new Vector2(45f, 14f), new Vector2(1.2f, 10f), WoodColor);
            HoldDoor gate = AddDoor(level, "Mirror Gate", new Vector2(45f, 7.5f), new Vector2(0.6f, 3f), WoodColor);
            gate.plates = new[] { left, right };
            AddLever(level, "Lever", new Vector2(47.5f, 8.3f), gate);
            AddCheckpoint(level, "Checkpoint Mantel", new Vector2(38f, 7.2f), new Vector2(6f, 2.4f));
            AddCheckpoint(level, "Checkpoint Mirror", new Vector2(48f, 7.2f), new Vector2(5f, 2.4f));

            Visual(level, "Mouse Hole", new Vector2(53.7f, 6.75f), new Vector2(0.6f, 1.5f), new Color(0.15f, 0.1f, 0.08f), 0);
            AddExit(level, new Vector2(51.5f, 7.25f), new Vector2(5f, 2.5f));

            Decorate(level);
        }

        /// <summary>Non-solid shapes and name tags so the graybox reads as a living room.</summary>
        void Decorate(Transform level)
        {
            // Books standing on the lower shelf (behind the creatures) and on the top board.
            for (int i = 0; i < 14; i++)
            {
                float height = 1.2f + (i * 7 % 5) * 0.2f;
                Decor(level, "Book", new Vector2(6.6f + i * 0.55f, 2.5f + height * 0.5f), new Vector2(0.45f, height), BookColors[i % BookColors.Length], -6);
            }
            for (int i = 0; i < 16; i++)
            {
                float height = 1.0f + (i * 3 % 4) * 0.25f;
                Decor(level, "Book", new Vector2(16.5f + i * 0.6f, 6f + height * 0.5f), new Vector2(0.5f, height), BookColors[(i + 2) % BookColors.Length], -6);
            }
            Tag(level, new Vector2(10f, 1.2f), "bookshelf");

            // A rug, the fish and the lamp's cord.
            Decor(level, "Rug", new Vector2(3f, 0.05f), new Vector2(6f, 0.1f), new Color(0.75f, 0.3f, 0.3f), -2);
            Decor(level, "Fish", new Vector2(28f, 1.6f), new Vector2(0.6f, 0.35f), new Color(1f, 0.55f, 0.15f), -3);
            Decor(level, "Fish 2", new Vector2(31.5f, 0.9f), new Vector2(0.5f, 0.3f), new Color(1f, 0.75f, 0.2f), -3);
            Decor(level, "Lamp Cord", new Vector2(34f, 14f), new Vector2(0.1f, 10.5f), Color.black, -5);
            Tag(level, new Vector2(29.75f, 3.3f), "fish tank");

            // Fireplace under the mantelpiece and the mirror above it.
            Decor(level, "Fireplace", new Vector2(44.75f, 2f), new Vector2(6f, 4f), new Color(0.25f, 0.2f, 0.18f), -2);
            Decor(level, "Fire", new Vector2(44.75f, 0.8f), new Vector2(3f, 1.2f), new Color(1f, 0.55f, 0.2f), -1);
            Decor(level, "Mirror Glass", new Vector2(45f, 12f), new Vector2(3.5f, 4f), new Color(0.8f, 0.92f, 0.98f), -4);
            Tag(level, new Vector2(44.75f, 4.5f), "fireplace");
            Tag(level, new Vector2(45f, 9.6f), "mirror gate");
            Tag(level, new Vector2(47.5f, 9f), "lever");
            Tag(level, new Vector2(52f, 8.2f), "mouse hole");
        }
    }
}
