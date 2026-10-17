using System;
using System.Collections.Generic;
using Picknick.Creature;
using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// The creature's ability badge. Exactly one ability is active. Press Swap next to a
    /// teammate to trade badges, so the room's set of abilities stays fixed.
    /// </summary>
    [RequireComponent(typeof(CreatureController))]
    public class CreatureAbilities : MonoBehaviour
    {
        public static readonly List<CreatureAbilities> All = new List<CreatureAbilities>();

        public float swapRange = 1.6f;

        Ability[] abilities;
        readonly List<(Collider2D other, float until)> ignoredCollisions = new List<(Collider2D, float)>();

        public CreatureController Creature { get; private set; }
        public AbilityType Current { get; private set; }
        public event Action<AbilityType> Changed;

        void Awake()
        {
            Creature = GetComponent<CreatureController>();
            abilities = new Ability[]
            {
                GetOrAdd<MagnetAbility>(),
                GetOrAdd<FeatherAbility>(),
                GetOrAdd<StrongAbility>(),
            };
            Set(AbilityType.Magnet);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Set(AbilityType type)
        {
            // Disable first so a carrying ability drops its passenger before mass changes.
            foreach (Ability ability in abilities)
                if (ability.Type != type) ability.enabled = false;
            foreach (Ability ability in abilities)
                if (ability.Type == type) ability.enabled = true;

            Current = type;
            Changed?.Invoke(type);
        }

        public T Get<T>() where T : Ability
        {
            foreach (Ability ability in abilities)
                if (ability is T typed) return typed;
            return null;
        }

        /// <summary>Ignore collisions with another creature for a while (after being carried).</summary>
        public void IgnoreCollisionFor(Collider2D mine, Collider2D other, float seconds)
        {
            Physics2D.IgnoreCollision(mine, other, true);
            ignoredCollisions.Add((other, Time.time + seconds));
        }

        void Update()
        {
            if (Creature.Input.SwapPressed && !Creature.IsCarried)
                TrySwap();

            Collider2D mine = Creature.GetComponent<Collider2D>();
            for (int i = ignoredCollisions.Count - 1; i >= 0; i--)
            {
                var (other, until) = ignoredCollisions[i];
                if (other == null)
                {
                    ignoredCollisions.RemoveAt(i);
                }
                else if (Time.time >= until && !Creature.IsCarried)
                {
                    Physics2D.IgnoreCollision(mine, other, false);
                    ignoredCollisions.RemoveAt(i);
                }
            }
        }

        void TrySwap()
        {
            CreatureAbilities nearest = null;
            float best = swapRange;
            foreach (CreatureAbilities other in All)
            {
                if (other == this || other.Creature.IsCarried) continue;
                float d = Vector2.Distance(other.transform.position, transform.position);
                if (d <= best) { best = d; nearest = other; }
            }
            if (nearest == null || nearest.Current == Current) return;

            AbilityType mine = Current;
            Set(nearest.Current);
            nearest.Set(mine);
        }

        T GetOrAdd<T>() where T : Ability
        {
            T existing = GetComponent<T>();
            return existing != null ? existing : gameObject.AddComponent<T>();
        }
    }
}
