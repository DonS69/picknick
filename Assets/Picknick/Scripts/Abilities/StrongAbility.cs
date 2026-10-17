using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// Pushes Heavy objects (see Heavy). Action picks up a teammate; Action again throws
    /// them where you aim. Light teammates fly farther.
    /// </summary>
    public class StrongAbility : CarryAbility
    {
        public float throwSpeed = 12f;

        public override AbilityType Type => AbilityType.Strong;
        public override float Mass => 2f;

        protected override void Update()
        {
            base.Update();
            if (!Creature.Input.ActionPressed) return;
            if (Carried != null)
                Throw();
            else
                TryGrab();
        }

        void Throw()
        {
            Vector2 dir = Creature.Input.Aim.sqrMagnitude > 0.04f
                ? Creature.Input.Aim.normalized
                : new Vector2(Creature.FacingDirection, 0.7f).normalized;
            float speed = throwSpeed / Mathf.Sqrt(Mathf.Max(0.25f, Carried.Body.mass));
            Release(dir * speed + Creature.Body.linearVelocity * 0.5f, 0.35f);
        }
    }
}
