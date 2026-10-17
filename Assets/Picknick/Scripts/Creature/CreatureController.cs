using System.Collections.Generic;
using UnityEngine;

namespace Picknick.Creature
{
    /// <summary>
    /// Run, jump and stand on things (including other creatures). Physics-driven so
    /// creatures can bump, stack and later be pulled, carried and thrown.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class CreatureController : MonoBehaviour
    {
        [SerializeField] CreatureTuning tuning;
        [SerializeField] LayerMask groundMask = ~0;

        Rigidbody2D body;
        BoxCollider2D box;
        CreatureInput input;
        float coyoteTimer;
        float jumpBufferTimer;
        readonly List<Collider2D> groundHits = new List<Collider2D>(8);

        public CreatureTuning Tuning => tuning;
        public Rigidbody2D Body => body;
        public bool IsGrounded { get; private set; }

        /// <summary>The rigidbody we are standing on (another creature, a crate), if any.</summary>
        public Rigidbody2D StandingOn { get; private set; }

        public int FacingDirection { get; private set; } = 1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            ApplyTuning(tuning);
        }

        /// <summary>Swap movement feel at runtime (abilities will use this to change mass).</summary>
        public void ApplyTuning(CreatureTuning value)
        {
            tuning = value != null ? value : ScriptableObject.CreateInstance<CreatureTuning>();

            body.mass = tuning.mass;
            body.gravityScale = tuning.gravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            box.size = tuning.size;
            // No friction, otherwise creatures stick to walls and to each other's sides.
            // Riding on a moving creature is handled explicitly via StandingOn.
            box.sharedMaterial = new PhysicsMaterial2D("Creature") { friction = 0f, bounciness = 0f };
        }

        public void SetInput(in CreatureInput next)
        {
            if (next.JumpPressed)
                jumpBufferTimer = tuning.jumpBufferTime;
            input = next;
            if (Mathf.Abs(next.Move) > 0.01f)
                FacingDirection = next.Move > 0f ? 1 : -1;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateGrounded();

            coyoteTimer = IsGrounded ? tuning.coyoteTime : coyoteTimer - dt;
            jumpBufferTimer -= dt;

            Vector2 velocity = body.linearVelocity;
            float platformVx = StandingOn != null ? StandingOn.linearVelocity.x : 0f;

            // In the air with no input we keep momentum, so throws and pulls aren't cancelled.
            bool steering = IsGrounded || Mathf.Abs(input.Move) > 0.01f;
            if (steering)
            {
                float target = input.Move * tuning.moveSpeed + platformVx;
                float accel = IsGrounded ? tuning.groundAcceleration : tuning.airAcceleration;
                velocity.x = Mathf.MoveTowards(velocity.x, target, accel * dt);
            }

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                velocity.y = JumpVelocity() + (StandingOn != null ? Mathf.Max(0f, StandingOn.linearVelocity.y) : 0f);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                IsGrounded = false;
            }

            body.linearVelocity = velocity;
            body.gravityScale = tuning.gravityScale * GravityMultiplier(velocity.y);
        }

        float GravityMultiplier(float vy)
        {
            if (vy < 0f) return tuning.fallGravityMultiplier;
            if (vy > 0f && !input.JumpHeld) return tuning.lowJumpGravityMultiplier;
            return 1f;
        }

        float JumpVelocity() =>
            Mathf.Sqrt(2f * tuning.jumpHeight * Mathf.Abs(Physics2D.gravity.y) * tuning.gravityScale);

        void UpdateGrounded()
        {
            Bounds bounds = box.bounds;
            Vector2 center = new Vector2(bounds.center.x, bounds.min.y - tuning.groundCheckDepth * 0.5f);
            Vector2 size = new Vector2(bounds.size.x * 0.9f, tuning.groundCheckDepth);

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundMask);
            Physics2D.OverlapBox(center, size, 0f, filter, groundHits);

            IsGrounded = false;
            StandingOn = null;
            foreach (Collider2D hit in groundHits)
            {
                if (hit.attachedRigidbody == body)
                    continue;

                // Don't count as grounded on the frame we leave the ground with a jump.
                float groundVy = hit.attachedRigidbody != null ? hit.attachedRigidbody.linearVelocity.y : 0f;
                if (body.linearVelocity.y - groundVy > 0.5f)
                    continue;

                IsGrounded = true;
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                    StandingOn = hit.attachedRigidbody;
                break;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (box == null) box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            float depth = tuning != null ? tuning.groundCheckDepth : 0.08f;
            Bounds bounds = box.bounds;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(
                new Vector3(bounds.center.x, bounds.min.y - depth * 0.5f),
                new Vector3(bounds.size.x * 0.9f, depth));
        }
    }
}
