using Picknick.Abilities;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>A metal lever. Pull it with a Magnet for a moment and it latches its door open.</summary>
    [RequireComponent(typeof(Magnetic))]
    public class MagnetLever : MonoBehaviour
    {
        public HoldDoor door;
        public float pullTime = 0.4f;

        Magnetic magnetic;
        float pulled;

        public bool Latched { get; private set; }

        void Awake() => magnetic = GetComponent<Magnetic>();

        void Update()
        {
            if (Latched) return;

            pulled = MagnetAbility.IsTargeted(magnetic) ? pulled + Time.deltaTime : 0f;
            if (pulled < pullTime) return;

            Latched = true;
            if (door != null) door.Latched = true;
            transform.rotation = Quaternion.Euler(0f, 0f, -50f);
            if (TryGetComponent(out SpriteRenderer sprite))
                sprite.color = new Color(0.45f, 0.85f, 0.45f);
        }
    }
}
