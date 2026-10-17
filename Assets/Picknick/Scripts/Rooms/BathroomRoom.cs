using Picknick.Abilities;
using Picknick.Creature;
using Picknick.Net;
using Picknick.Puzzle;
using UnityEngine;

namespace Picknick.Rooms
{
    /// <summary>
    /// Room 2, "The Bathroom". Left to right: A the soap and the gate (new: floor buttons),
    /// B the bathtub (don't touch the water), C the laundry lift up the washing machine.
    /// Everyone must end up in the air vent on top of the washing machine.
    /// </summary>
    public class BathroomRoom : RoomBase
    {
        static readonly Color TileColor = new Color(0.78f, 0.88f, 0.92f);
        static readonly Color FloorTileColor = new Color(0.62f, 0.7f, 0.76f);
        static readonly Color PorcelainColor = new Color(0.96f, 0.97f, 0.98f);
        static readonly Color WaterColor = new Color(0.45f, 0.72f, 0.95f, 0.85f);
        static readonly Color SoapColor = new Color(0.95f, 0.6f, 0.75f);
        static readonly Color MachineColor = new Color(0.9f, 0.92f, 0.94f);

        protected override Color BackgroundColor => new Color(0.88f, 0.95f, 0.98f);

        protected override string Title => "Room 2: The Bathroom";
        protected override string Goal => "Get all four creatures into the air vent on top of the washing machine (far right).";

        protected override string SectionHint(Vector2 position)
        {
            if (position.x < 14f)
                return "The gate opens while something sits on the red button. Strong (red badge): walk into the dark soap to push it " +
                       "onto the button. (A creature standing on the button works too, but then that creature is stuck behind.)";
            if (position.x < 30f)
                return "Hop onto the step, then the tub rim. Don't touch the water! Feather (white badge): jump and HOLD Space to glide over. " +
                       "Magnets (blue badge): hold left click on the shower head to swing across, then pull teammates over from the far rim.";
            return "The lift goes up while something sits on its button. Magnet: hold left click on the metal bucket to knock it off its hook, " +
                   "then push it onto the button so nobody has to stay behind. Ride the lift and climb into the vent.";
        }

        protected override void BuildLevel()
        {
            var level = new GameObject("Bathroom").transform;

            // Room shell.
            Block(level, "Floor", new Vector2(24.5f, -0.5f), new Vector2(52f, 1f), FloorTileColor);
            Block(level, "Wall Left", new Vector2(-1.5f, 6.5f), new Vector2(1f, 15f), TileColor);
            Block(level, "Wall Right", new Vector2(50.5f, 6.5f), new Vector2(1f, 15f), TileColor);

            // A: "The Soap". Only Strong can push the soap; on the button it holds the gate open.
            GameObject soap = Prop(level, "Soap", new Vector2(6.5f, 0.4f), new Vector2(1.4f, 0.8f), 4f, HeavyColor);
            soap.AddComponent<Heavy>();
            Decor(soap.transform, "Soap Label", new Vector2(6.5f, 0.4f), new Vector2(1.4f, 0.3f), SoapColor, 1);
            Tag(soap.transform, new Vector2(0f, 0.8f), "soap");
            PressurePlate gateButton = AddPlate(level, "Gate Button", new Vector2(10f, 0f), 2.4f);
            Tag(gateButton.transform, new Vector2(0f, 0.5f), "button");
            Block(level, "Towel Cabinet", new Vector2(13f, 8f), new Vector2(1.2f, 10f), WoodColor);
            HoldDoor gate = AddDoor(level, "Gate", new Vector2(13f, 1.5f), new Vector2(0.6f, 3f), WoodColor);
            gate.plates = new[] { gateButton };
            AddCheckpoint(level, "Checkpoint Start", new Vector2(3f, 1.5f), new Vector2(8f, 3f));
            AddCheckpoint(level, "Checkpoint Gate", new Vector2(17.5f, 1.5f), new Vector2(7f, 3f));

            // B: "The Bathtub". A step, the tub rims, and 5 units of water between them.
            Block(level, "Bath Step", new Vector2(20.25f, 0.4f), new Vector2(1.5f, 0.8f), WoodColor);
            Block(level, "Tub Rim Left", new Vector2(22.25f, 0.85f), new Vector2(1.5f, 1.7f), PorcelainColor);
            Block(level, "Tub Rim Right", new Vector2(28.75f, 0.85f), new Vector2(1.5f, 1.7f), PorcelainColor);
            Decor(level, "Tub Inside", new Vector2(25.5f, 0.85f), new Vector2(5f, 1.7f), new Color(0.85f, 0.88f, 0.9f), -5);
            Decor(level, "Bath Water", new Vector2(25.5f, 0.7f), new Vector2(5f, 1.4f), WaterColor, -2);
            AddKillZone(level, "Bath Water Zone", new Vector2(25.5f, 0.7f), new Vector2(5f, 1.6f));
            AddAnchor(level, "Shower Head", new Vector2(25.5f, 5.5f), new Vector2(0.8f, 0.4f));
            AddCheckpoint(level, "Checkpoint Tub", new Vector2(22.25f, 2.5f), new Vector2(1.5f, 1.6f));

            // C: "The Laundry Lift". The lift rises while its button is pressed. Whoever stands on the
            // button is left behind, unless the bucket hanging on the wall takes their place.
            PressurePlate liftButton = AddPlate(level, "Lift Button", new Vector2(34f, 0f), 2f);
            Tag(liftButton.transform, new Vector2(0f, 0.5f), "button");
            Visual(level, "Bucket Hook", new Vector2(37.5f, 4.75f), new Vector2(0.1f, 0.4f), Color.black, 0);
            GameObject bucket = Prop(level, "Bucket", new Vector2(37.5f, 4.2f), new Vector2(0.7f, 0.7f), 1f, MetalColor);
            bucket.AddComponent<Magnetic>();
            bucket.AddComponent<HookedItem>();
            bucket.AddComponent<FallRespawn>().checkpoint = new Vector2(36f, 1f);
            Tag(bucket.transform, new Vector2(0f, 0.6f), "bucket");

            GameObject liftObject = Visual(level, "Lift", new Vector2(41.9f, 0.2f), new Vector2(2.8f, 0.4f), WoodColor, 0);
            liftObject.AddComponent<BoxCollider2D>();
            liftObject.AddComponent<Rigidbody2D>();
            var lift = liftObject.AddComponent<Lift>();
            liftObject.AddComponent<SyncedBody>();
            lift.plate = liftButton;
            lift.rise = 6f;
            Tag(liftObject.transform, new Vector2(0f, 0.5f), "lift");

            Block(level, "Washing Machine", new Vector2(46.65f, 3.1f), new Vector2(6.7f, 6.2f), MachineColor);
            AddCheckpoint(level, "Checkpoint Laundry", new Vector2(34f, 1.5f), new Vector2(8f, 3f));
            AddCheckpoint(level, "Checkpoint Machine", new Vector2(46.5f, 7.4f), new Vector2(6f, 2.4f));

            Visual(level, "Vent", new Vector2(49.75f, 7.2f), new Vector2(0.5f, 1.6f), new Color(0.3f, 0.32f, 0.35f), 0);
            AddExit(level, new Vector2(48f, 7.4f), new Vector2(4f, 2.4f));

            Decorate(level);
        }

        /// <summary>Non-solid shapes and name tags so the graybox reads as a bathroom.</summary>
        void Decorate(Transform level)
        {
            // Tiles along the bottom of the wall.
            for (float x = -0.5f; x < 50f; x += 1.5f)
                Decor(level, "Wall Tile", new Vector2(x, 1.5f), new Vector2(1.4f, 1.4f), TileColor, -6);

            // Mirror and sink on the left wall.
            Decor(level, "Mirror Frame", new Vector2(4f, 6.5f), new Vector2(3.4f, 3.4f), WoodColor, -5);
            Decor(level, "Mirror", new Vector2(4f, 6.5f), new Vector2(3f, 3f), new Color(0.8f, 0.92f, 0.98f), -4);
            Tag(level, new Vector2(13f, 3.4f), "gate");

            // Shower pipe, tub feet and the curtain rail.
            Decor(level, "Shower Pipe", new Vector2(25.5f, 9.5f), new Vector2(0.25f, 7.6f), MetalColor, -4);
            Decor(level, "Curtain", new Vector2(29.8f, 6.5f), new Vector2(0.8f, 5f), new Color(0.6f, 0.85f, 0.75f, 0.7f), -4);
            Tag(level, new Vector2(25.5f, 6.1f), "shower head");
            Tag(level, new Vector2(25.5f, 0.4f), "bath");

            // Washing machine porthole and buttons.
            Decor(level, "Porthole Ring", new Vector2(46.65f, 3f), new Vector2(3.2f, 3.2f), MetalColor, -2);
            Decor(level, "Porthole Glass", new Vector2(46.65f, 3f), new Vector2(2.6f, 2.6f), new Color(0.55f, 0.7f, 0.85f), -1);
            Decor(level, "Machine Panel", new Vector2(46.65f, 5.6f), new Vector2(6.7f, 0.6f), MetalColor, -2);
            Tag(level, new Vector2(46.65f, 1f), "washing machine");
            Tag(level, new Vector2(48.5f, 8.4f), "air vent");
        }
    }
}
