using Picknick.Creature;
using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// Base for one ability. Only the ability matching the creature's badge is enabled.
    /// Runs before CreatureController so it can adjust movement in the same physics step.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(CreatureController))]
    public abstract class Ability : MonoBehaviour
    {
        public abstract AbilityType Type { get; }

        /// <summary>Body mass while this ability is active. Lighter creatures fly farther.</summary>
        public abstract float Mass { get; }

        protected CreatureController Creature { get; private set; }

        protected Vector2 AimDirection
        {
            get
            {
                Vector2 aim = Creature.Input.Aim;
                return aim.sqrMagnitude > 0.04f ? aim.normalized : new Vector2(Creature.FacingDirection, 0f);
            }
        }

        protected virtual void Awake()
        {
            Creature = GetComponent<CreatureController>();
        }

        protected virtual void OnEnable()
        {
            Creature.SetMass(Mass);
        }
    }
}
