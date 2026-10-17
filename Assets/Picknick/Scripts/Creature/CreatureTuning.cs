using UnityEngine;

namespace Picknick.Creature
{
    /// <summary>
    /// Movement feel for a creature. Create one via Assets > Create > Picknick > Creature Tuning
    /// and tweak values live in Play mode. A creature without one uses these defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "Picknick/Creature Tuning", fileName = "CreatureTuning")]
    public class CreatureTuning : ScriptableObject
    {
        [Header("Body")]
        [Tooltip("Base mass. Abilities change this later (Feather 0.5, Strong 2).")]
        public float mass = 1f;
        public Vector2 size = new Vector2(0.8f, 0.8f);

        [Header("Run")]
        public float moveSpeed = 6f;
        public float groundAcceleration = 70f;
        public float airAcceleration = 35f;

        [Header("Jump")]
        [Tooltip("Apex height in world units. 1.1 is a bit more than one creature tall.")]
        public float jumpHeight = 1.1f;
        public float gravityScale = 3f;
        [Tooltip("Extra gravity while falling, so jumps feel snappy instead of floaty.")]
        public float fallGravityMultiplier = 1.6f;
        [Tooltip("Extra gravity when jump is released early, for short hops.")]
        public float lowJumpGravityMultiplier = 2f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;

        [Header("Ground check")]
        public float groundCheckDepth = 0.08f;
    }
}
