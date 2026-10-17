using Picknick.Creature;
using Picknick.Net;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>
    /// A floor button: pressed while a creature or any loose object (crate, bucket, clock)
    /// sits on it. It sinks and turns green when pressed. Online, guests read that from
    /// where the host put the button, since its position is synced.
    /// </summary>
    [RequireComponent(typeof(Zone), typeof(SpriteRenderer))]
    public class PressurePlate : MonoBehaviour
    {
        public float pressDepth = 0.08f;

        static readonly Color UpColor = new Color(0.9f, 0.35f, 0.3f);
        static readonly Color DownColor = new Color(0.45f, 0.85f, 0.45f);

        Zone zone;
        SpriteRenderer sprite;
        Vector3 restPosition;

        public bool Pressed { get; private set; }

        void Awake()
        {
            zone = GetComponent<Zone>();
            sprite = GetComponent<SpriteRenderer>();
            restPosition = transform.position;
        }

        void FixedUpdate()
        {
            if (!Authority.IsSimulating) return;
            Pressed = SomethingOnTop();
            transform.position = restPosition + Vector3.down * (Pressed ? pressDepth : 0f);
        }

        void Update()
        {
            if (!Authority.IsSimulating)
                Pressed = restPosition.y - transform.position.y > pressDepth * 0.5f;
            sprite.color = Pressed ? DownColor : UpColor;
        }

        bool SomethingOnTop()
        {
            // Creatures and props all carry a SyncedBody; walls and doors don't move freely.
            foreach (SyncedBody synced in SyncedBody.All)
            {
                Rigidbody2D body = synced.Body;
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic || !synced.gameObject.activeInHierarchy)
                    continue;
                if (zone.Contains(body.position))
                    return true;
            }
            return false;
        }
    }
}
