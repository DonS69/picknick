using Picknick.CameraRig;
using Picknick.Creature;
using UnityEngine;

namespace Picknick.Sandbox
{
    /// <summary>
    /// Builds a graybox test level and four creatures at runtime, so the sandbox scene
    /// needs nothing but this component. Used to tune movement and stacking (plan step 2).
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

        static readonly Color GroundColor = new Color(0.55f, 0.53f, 0.50f);
        static readonly Color CrateColor = new Color(0.80f, 0.62f, 0.40f);
        static readonly Color HeavyColor = new Color(0.35f, 0.30f, 0.28f);

        Sprite square;
        LocalInputRouter router;
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

        void OnGUI()
        {
            hintStyle ??= new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.2f, 0.2f, 0.2f) } };
            GUI.Label(new Rect(12, 8, 700, 60),
                "Keyboard: A/D or arrows move, Space/W jump, Tab switches creature (white marker)\n" +
                "Gamepads: one per creature, stick/d-pad move, A jump\n" +
                "Goal: get all four onto the high ledge. Crate helps, stacking on heads works too.", hintStyle);
        }

        void BuildLevel()
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
            // Far ledge across a 4-unit gap: just out of jump range (a job for Feather later).
            Block(level, "Far Ledge", new Vector2(26f, 1f), new Vector2(8f, 2f), GroundColor);

            Prop(level, "Crate", new Vector2(4f, 0.5f), new Vector2(1f, 1f), 1.5f, CrateColor);
            Prop(level, "Heavy Can", new Vector2(-5f, 0.6f), new Vector2(1.2f, 1.2f), 12f, HeavyColor);
        }

        void SpawnCreatures()
        {
            var root = new GameObject("Creatures").transform;
            for (int i = 0; i < creatureCount; i++)
            {
                var go = new GameObject($"Creature {i + 1}");
                go.transform.SetParent(root);
                go.transform.position = new Vector2(i * 1.2f, 1f);

                go.AddComponent<Rigidbody2D>();
                go.AddComponent<BoxCollider2D>();
                CreatureController creature = go.AddComponent<CreatureController>();
                if (tuning != null)
                    creature.ApplyTuning(tuning);
                go.AddComponent<FallRespawn>().killHeight = -6f;

                Color color = CreatureColors[i % CreatureColors.Length];
                Visual(go.transform, "Body", Vector2.zero, new Vector2(0.8f, 0.8f), color, 0);
                Visual(go.transform, "Eye L", new Vector2(-0.15f, 0.12f), new Vector2(0.12f, 0.18f), Color.black, 1);
                Visual(go.transform, "Eye R", new Vector2(0.15f, 0.12f), new Vector2(0.12f, 0.18f), Color.black, 1);

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
            cam.backgroundColor = new Color(0.98f, 0.93f, 0.84f);

            SharedCamera shared = cam.GetComponent<SharedCamera>();
            if (shared == null)
                shared = cam.gameObject.AddComponent<SharedCamera>();
            foreach (CreatureController creature in router.Creatures)
                shared.Targets.Add(creature.transform);
        }

        void Block(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            GameObject go = Visual(parent, name, center, size, color, -1);
            go.AddComponent<BoxCollider2D>();
        }

        void Prop(Transform parent, string name, Vector2 center, Vector2 size, float mass, Color color)
        {
            GameObject go = Visual(parent, name, center, size, color, 0);
            go.AddComponent<BoxCollider2D>();
            var body = go.AddComponent<Rigidbody2D>();
            body.mass = mass;
            body.gravityScale = 3f;
            body.freezeRotation = true;
        }

        GameObject Visual(Transform parent, string name, Vector2 localPos, Vector2 size, Color color, int order)
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
