using System.Collections.Generic;
using Picknick.Abilities;
using Picknick.CameraRig;
using Picknick.Creature;
using UnityEngine;

namespace Picknick.Sandbox
{
    /// <summary>
    /// Builds a graybox test level and four creatures at runtime, so the sandbox scene
    /// needs nothing but this component. Used to tune movement, stacking and the three
    /// abilities (plan steps 2 and 3).
    /// </summary>
    public class SandboxBootstrap : MonoBehaviour
    {
        public int creatureCount = 4;
        public CreatureTuning tuning;

        static readonly Color[] CreatureColors =
        {
            new Color(1.00f, 0.55f, 0.45f), // coral
            new Color(0.45f, 0.75f, 1.00f), // sky
            new Color(0.60f, 0.90f, 0.50f), // leaf
            new Color(1.00f, 0.85f, 0.35f), // butter
            new Color(0.80f, 0.60f, 1.00f),
            new Color(1.00f, 0.60f, 0.85f),
            new Color(0.50f, 0.95f, 0.90f),
            new Color(0.95f, 0.95f, 0.95f),
        };

        protected static readonly Color GroundColor = new Color(0.55f, 0.53f, 0.50f);
        protected static readonly Color CrateColor = new Color(0.80f, 0.62f, 0.40f);
        protected static readonly Color HeavyColor = new Color(0.35f, 0.30f, 0.28f);
        protected static readonly Color MetalColor = new Color(0.55f, 0.65f, 0.75f);

        // Starting badges for creatures 1-4, as in Room 1 of the plan.
        static readonly AbilityType[] StartingAbilities =
        {
            AbilityType.Strong, AbilityType.Feather, AbilityType.Magnet, AbilityType.Magnet,
        };

        Sprite square;
        LocalInputRouter router;

        protected IReadOnlyList<CreatureController> Creatures => router.Creatures;
        protected virtual Vector2 SpawnOrigin => new Vector2(0f, 1f);
        protected virtual float KillHeight => -6f;
        protected virtual Color BackgroundColor => new Color(0.98f, 0.93f, 0.84f);

        protected virtual string Hint =>
            "Magnet: hold to pull blue metal or a teammate to you. In the air, or on fixed metal, it pulls YOU.\n" +
            "Feather: hold jump in the air to glide. Ability grabs or drops a teammate.\n" +
            "Strong: push the dark heavy can. Ability picks up a teammate, press again to throw.";
        Transform keyboardMarker;

        void Awake()
        {
            square = CreateSquareSprite();
            router = gameObject.AddComponent<LocalInputRouter>();

            BuildLevel();
            SpawnCreatures();
            SetUpCamera();
        }

        void LateUpdate()
        {
            CreatureController selected = router.KeyboardCreature;
            if (selected != null && keyboardMarker != null)
                keyboardMarker.position = selected.transform.position + new Vector3(0f, 0.75f, 0f);
        }

        GUIStyle hintStyle;

        protected GUIStyle HintStyle => hintStyle;

        protected virtual void OnGUI()
        {
            hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.2f, 0.2f, 0.2f) },
                padding = new RectOffset(8, 8, 6, 6),
            };
            hintBackground ??= SolidTexture(new Color(1f, 0.97f, 0.9f, 0.9f));

            // Panel behind the help text so it stays readable over walls.
            var hint = new GUIContent(
                "Keyboard: A/D move, Space jump, mouse aims, E or left click = ability, Q = swap badge with the nearest teammate, Tab = switch creature\n" +
                "Gamepads: one per creature. Stick move, A jump, right stick aims, X or RT = ability, Y = swap\n" +
                Hint);
            float width = Mathf.Min(hintStyle.CalcSize(hint).x, Screen.width - 16f);
            var hintRect = new Rect(8, 8, width, hintStyle.CalcHeight(hint, width));
            GUI.DrawTexture(hintRect, hintBackground);
            GUI.Label(hintRect, hint, hintStyle);

            Camera cam = Camera.main;
            if (cam == null) return;
            labelStyle ??= new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, padding = new RectOffset() };
            sideLabelStyle ??= new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleRight };

            foreach (CreatureController creature in router.Creatures)
            {
                var abilities = creature.GetComponent<CreatureAbilities>();
                string label = abilities.Current.ToString();

                // Someone standing on (or carried above) us would cover a label above our head,
                // so put ours on the left side instead.
                bool covered = HasCreatureAbove(creature);
                Vector3 anchor = creature.transform.position + (covered ? new Vector3(-0.5f, 0f, 0f) : new Vector3(0f, 0.95f, 0f));
                Vector3 screen = cam.WorldToScreenPoint(anchor);
                if (screen.z < 0f) continue;

                float y = Screen.height - screen.y - 10f;
                if (covered)
                    GUI.Label(new Rect(screen.x - 84f, y, 80f, 20f), label, sideLabelStyle);
                else
                    GUI.Label(new Rect(screen.x - 40f, y, 80f, 20f), label, labelStyle);
            }
        }

        bool HasCreatureAbove(CreatureController creature)
        {
            Vector2 me = creature.transform.position;
            foreach (CreatureController other in router.Creatures)
            {
                if (other == creature) continue;
                Vector2 offset = (Vector2)other.transform.position - me;
                if (Mathf.Abs(offset.x) < 0.7f && offset.y > 0f && offset.y < 1.3f)
                    return true;
            }
            return false;
        }

        static Texture2D SolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        Texture2D hintBackground;
        GUIStyle sideLabelStyle;
        GUIStyle labelStyle;

        protected virtual void BuildLevel()
        {
            var level = new GameObject("Level").transform;

            // Floor with a pit between x=18 and x=22.
            Block(level, "Floor A", new Vector2(4f, -0.5f), new Vector2(28f, 1f), GroundColor);
            Block(level, "Floor B", new Vector2(33.5f, -0.5f), new Vector2(23f, 1f), GroundColor);
            Block(level, "Wall L", new Vector2(-10.5f, 5f), new Vector2(1f, 12f), GroundColor);
            Block(level, "Wall R", new Vector2(45.5f, 5f), new Vector2(1f, 12f), GroundColor);

            // Ledge, 2 units high: one creature can't reach it from another's head,
            // but two stacked can, or one standing on the crate.
            Block(level, "Ledge", new Vector2(13f, 1f), new Vector2(10f, 2f), GroundColor);
            // Far ledge across a 4-unit gap: just out of jump range. Feather glides it,
            // or a Magnet grapples the metal anchor hanging above the pit.
            Block(level, "Far Ledge", new Vector2(26f, 1f), new Vector2(8f, 2f), GroundColor);
            Block(level, "Pit Anchor", new Vector2(20f, 6f), new Vector2(0.6f, 0.6f), MetalColor).AddComponent<Magnetic>();

            // High shelf: reach it with a Strong throw, or grapple the anchor above it.
            Block(level, "High Shelf", new Vector2(36f, 5f), new Vector2(6f, 0.6f), GroundColor);
            Block(level, "Shelf Anchor", new Vector2(38f, 8f), new Vector2(0.6f, 0.6f), MetalColor).AddComponent<Magnetic>();

            Prop(level, "Crate", new Vector2(4f, 0.5f), new Vector2(1f, 1f), 1.5f, CrateColor);
            Prop(level, "Metal Crate", new Vector2(-2f, 0.5f), new Vector2(0.8f, 0.8f), 1f, MetalColor).AddComponent<Magnetic>();
            Prop(level, "Heavy Can", new Vector2(-6f, 0.6f), new Vector2(1.2f, 1.2f), 4f, HeavyColor).AddComponent<Heavy>();
        }

        void SpawnCreatures()
        {
            var root = new GameObject("Creatures").transform;
            for (int i = 0; i < creatureCount; i++)
            {
                var go = new GameObject($"Creature {i + 1}");
                go.transform.SetParent(root);
                go.transform.position = SpawnOrigin + new Vector2(i * 1.2f, 0f);

                go.AddComponent<Rigidbody2D>();
                go.AddComponent<BoxCollider2D>();
                CreatureController creature = go.AddComponent<CreatureController>();
                if (tuning != null)
                    creature.ApplyTuning(tuning);
                go.AddComponent<FallRespawn>().killHeight = KillHeight;
                go.AddComponent<Magnetic>();
                var abilities = go.AddComponent<CreatureAbilities>();

                Color color = CreatureColors[i % CreatureColors.Length];
                Visual(go.transform, "Body", Vector2.zero, new Vector2(0.8f, 0.8f), color, 0);
                Visual(go.transform, "Eye L", new Vector2(-0.15f, 0.12f), new Vector2(0.12f, 0.18f), Color.black, 1);
                Visual(go.transform, "Eye R", new Vector2(0.15f, 0.12f), new Vector2(0.12f, 0.18f), Color.black, 1);

                var badge = Visual(go.transform, "Badge", new Vector2(0f, -0.2f), new Vector2(0.36f, 0.16f), Color.white, 1)
                    .GetComponent<SpriteRenderer>();
                abilities.Changed += type => badge.color = BadgeColor(type);
                abilities.Set(StartingAbilities[i % StartingAbilities.Length]);

                router.Creatures.Add(creature);
            }

            keyboardMarker = Visual(null, "Keyboard Marker", Vector2.zero, new Vector2(0.25f, 0.12f), Color.white, 2).transform;
        }

        void SetUpCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.transform.position = new Vector3(0f, 2f, -10f);
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;

            SharedCamera shared = cam.GetComponent<SharedCamera>();
            if (shared == null)
                shared = cam.gameObject.AddComponent<SharedCamera>();
            foreach (CreatureController creature in router.Creatures)
                shared.Targets.Add(creature.transform);
        }

        static Color BadgeColor(AbilityType type) => type switch
        {
            AbilityType.Magnet => new Color(0.3f, 0.55f, 0.95f),
            AbilityType.Feather => Color.white,
            AbilityType.Strong => new Color(0.9f, 0.25f, 0.2f),
            _ => Color.gray,
        };

        protected GameObject Block(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            GameObject go = Visual(parent, name, center, size, color, -3);
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        protected GameObject Prop(Transform parent, string name, Vector2 center, Vector2 size, float mass, Color color)
        {
            GameObject go = Visual(parent, name, center, size, color, 0);
            go.AddComponent<BoxCollider2D>();
            var body = go.AddComponent<Rigidbody2D>();
            body.mass = mass;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            return go;
        }

        protected GameObject Visual(Transform parent, string name, Vector2 localPos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
