using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// Hold Jump in the air to glide. Action grabs or drops a teammate; carrying makes
    /// you sink faster, so the distance you can ferry someone is limited.
    /// </summary>
    public class FeatherAbility : CarryAbility
    {
        public float glideFallSpeed = 1.5f;
        public float carryingFallSpeed = 4.5f;

        public override AbilityType Type => AbilityType.Feather;
        public override float Mass => 0.5f;

        protected override void Update()
        {
            base.Update();
            if (!Creature.Input.ActionPressed) return;
            if (Carried != null)
                Release(Creature.Body.linearVelocity, 0.1f);
            else
                TryGrab();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            bool gliding = !Creature.IsGrounded && !Creature.IsCarried && Creature.Input.JumpHeld;
            if (gliding)
                Creature.FallSpeedLimit = Carried != null ? carryingFallSpeed : glideFallSpeed;
        }
    }
}
