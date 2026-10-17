using Picknick.Abilities;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>
    /// A heavy door that opens only while a Strong creature stands in its hold zone,
    /// and slams shut the moment they step out. Latching (e.g. by a lever) keeps it open.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class HoldDoor : MonoBehaviour
    {
        public Zone holdZone;
        public float openHeight = 2.6f;
        public float openSpeed = 3f;
        public float slamSpeed = 15f;

        Rigidbody2D body;
        Vector2 closedPosition;
        float openAmount;

        public bool Latched { get; set; }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            closedPosition = body.position;
        }

        void FixedUpdate()
        {
            bool open = Latched || StrongHolding();
            float speed = open ? openSpeed : slamSpeed;
            openAmount = Mathf.MoveTowards(openAmount, open ? 1f : 0f, speed / openHeight * Time.fixedDeltaTime);
            body.MovePosition(closedPosition + Vector2.up * (openAmount * openHeight));
        }

        bool StrongHolding()
        {
            if (holdZone == null) return false;
            foreach (CreatureAbilities creature in CreatureAbilities.All)
                if (creature.Current == AbilityType.Strong && holdZone.Contains(creature.transform.position))
                    return true;
            return false;
        }
    }
}
