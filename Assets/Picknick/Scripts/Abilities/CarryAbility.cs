using Picknick.Creature;
using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// Shared by Feather and Strong: grab a nearby teammate and hold them above your head.
    /// The passenger can wriggle free by pressing Jump.
    /// </summary>
    public abstract class CarryAbility : Ability
    {
        public float grabRange = 1.3f;
        public Vector2 carryOffset = new Vector2(0f, 0.82f);

        protected CreatureController Carried { get; private set; }

        protected void TryGrab()
        {
            if (Creature.IsCarried) return;

            CreatureController nearest = null;
            float best = grabRange;
            foreach (CreatureAbilities other in CreatureAbilities.All)
            {
                CreatureController candidate = other.Creature;
                if (candidate == Creature || candidate.IsCarried || IsCarrying(candidate)) continue;
                float d = Vector2.Distance(candidate.transform.position, transform.position);
                if (d <= best) { best = d; nearest = candidate; }
            }
            if (nearest == null) return;

            Carried = nearest;
            Carried.IsCarried = true;
            Carried.Body.bodyType = RigidbodyType2D.Kinematic;
            Carried.Body.linearVelocity = Vector2.zero;
            Physics2D.IgnoreCollision(Creature.GetComponent<Collider2D>(), Carried.GetComponent<Collider2D>(), true);
        }

        protected void Release(Vector2 velocity, float noSteerTime)
        {
            if (Carried == null) return;
            CreatureController passenger = Carried;
            Carried = null;

            passenger.IsCarried = false;
            passenger.Body.bodyType = RigidbodyType2D.Dynamic;
            passenger.Launch(velocity, noSteerTime);

            // Keep ignoring each other briefly so we don't explode apart from overlapping.
            var abilities = passenger.GetComponent<CreatureAbilities>();
            if (abilities != null)
                abilities.IgnoreCollisionFor(passenger.GetComponent<Collider2D>(), Creature.GetComponent<Collider2D>(), 0.3f);
        }

        protected virtual void Update()
        {
            if (Carried != null && Carried.Input.JumpPressed)
                Release(Creature.Body.linearVelocity + Vector2.up * 6f, 0.1f);
        }

        protected virtual void FixedUpdate()
        {
            if (Carried == null) return;
            Carried.Body.MovePosition(Creature.Body.position + carryOffset);
        }

        protected virtual void OnDisable()
        {
            if (Creature != null)
                Release(Creature.Body.linearVelocity, 0f);
        }

        static bool IsCarrying(CreatureController creature)
        {
            foreach (CarryAbility carry in creature.GetComponents<CarryAbility>())
                if (carry.enabled && carry.Carried != null) return true;
            return false;
        }
    }
}
