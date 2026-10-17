using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// Hold Action to pull the magnetic thing you aim at: metal or a teammate.
    /// Standing on the ground you plant your feet and the target comes to you (heavy ones slowly).
    /// In the air, or aiming at fixed metal, you fly to it instead: a free grappling hook.
    /// Mass decides speed: a Feather zips over, a Strong teammate is a slow heave.
    /// Works while being carried, so a Feather can ferry a Magnet to grab something mid-air.
    /// </summary>
    public class MagnetAbility : Ability
    {
        public float range = 7f;
        public float coneAngle = 30f;
        [Tooltip("Strong enough to beat gravity, so grappling upwards works.")]
        public float pullForce = 120f;
        public float maxPullSpeed = 12f;
        public float stopDistance = 1.1f;

        Magnetic target;
        LineRenderer beam;

        public override AbilityType Type => AbilityType.Magnet;

        /// <summary>What we're pulling right now, if anything.</summary>
        public Magnetic Target => target;

        /// <summary>True if any active Magnet is pulling this object.</summary>
        public static bool IsTargeted(Magnetic magnetic)
        {
            foreach (CreatureAbilities creature in CreatureAbilities.All)
            {
                MagnetAbility magnet = creature.Get<MagnetAbility>();
                if (magnet != null && magnet.isActiveAndEnabled && magnet.target == magnetic)
                    return true;
            }
            return false;
        }
        public override float Mass => 1f;

        protected override void Awake()
        {
            base.Awake();
            var beamObject = new GameObject("Magnet Beam");
            beamObject.transform.SetParent(transform, false);
            beam = beamObject.AddComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.widthMultiplier = 0.06f;
            beam.material = new Material(Shader.Find("Sprites/Default"));
            beam.startColor = new Color(0.4f, 0.8f, 1f, 0.9f);
            beam.endColor = new Color(0.4f, 0.8f, 1f, 0.4f);
            beam.sortingOrder = 3;
            beam.enabled = false;
        }

        void OnDisable()
        {
            target = null;
            if (beam != null) beam.enabled = false;
        }

        void Update()
        {
            if (!Creature.Input.ActionHeld)
                target = null;
            else if (target == null)
                target = FindTarget();
        }

        void LateUpdate()
        {
            beam.enabled = target != null;
            if (target == null) return;
            beam.SetPosition(0, transform.position);
            beam.SetPosition(1, target.transform.position);
        }

        void FixedUpdate()
        {
            if (target == null || !target.isActiveAndEnabled) { target = null; return; }

            Rigidbody2D me = Creature.Body;
            Vector2 toTarget = target.Position - me.position;
            float distance = toTarget.magnitude;
            if (distance > range * 1.25f) { target = null; return; }
            if (distance < stopDistance) return;

            Vector2 dir = toTarget / distance;
            bool targetMoves = target.IsMovable;
            Vector2 targetVelocity = targetMoves ? target.Body.linearVelocity : Vector2.zero;
            float closingSpeed = Vector2.Dot(me.linearVelocity - targetVelocity, dir);
            if (closingSpeed > maxPullSpeed) return;

            // Planted on the ground we hold firm; in the air (or on fixed metal) we get pulled too.
            // While carried (say, by a Feather) we can still pull things, but can't be pulled away.
            bool pullSelf = !Creature.IsCarried && (!targetMoves || !Creature.IsGrounded);

            if (targetMoves)
            {
                if (target.Creature != null)
                    target.Creature.AddExternalForce(-dir * pullForce);
                else
                    target.Body.AddForce(-dir * pullForce);
            }
            if (pullSelf)
                Creature.AddExternalForce(dir * pullForce);
        }

        Magnetic FindTarget()
        {
            Vector2 origin = transform.position;
            Vector2 aim = AimDirection;
            Magnetic best = null;
            float bestScore = float.MaxValue;

            foreach (Magnetic candidate in Magnetic.All)
            {
                if (candidate.gameObject == gameObject) continue;
                Vector2 offset = candidate.Position - origin;
                float distance = offset.magnitude;
                if (distance > range || distance < 0.01f) continue;
                float angle = Vector2.Angle(aim, offset);
                if (angle > coneAngle) continue;
                if (!InSight(origin, candidate)) continue;

                float score = distance * (1f + angle / coneAngle);
                if (score < bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }

        bool InSight(Vector2 origin, Magnetic candidate)
        {
            foreach (RaycastHit2D hit in Physics2D.LinecastAll(origin, candidate.Position))
            {
                if (hit.collider.isTrigger) continue;
                if (hit.collider.attachedRigidbody == Creature.Body) continue;
                // First thing in the way decides: it's either the target or a wall.
                return hit.collider.GetComponentInParent<Magnetic>() == candidate;
            }
            return true;
        }
    }
}
