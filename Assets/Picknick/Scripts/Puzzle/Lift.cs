using Picknick.Creature;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>A platform that rises while its button is pressed and sinks back when it isn't.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Lift : MonoBehaviour
    {
        public PressurePlate plate;
        public float rise = 6f;
        public float speed = 2f;

        Rigidbody2D body;
        Vector2 bottom;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            bottom = body.position;
        }

        void FixedUpdate()
        {
            if (!Authority.IsSimulating) return;

            // Move by velocity (not MovePosition) so riders see the lift's speed and stay grounded on it.
            float height = body.position.y - bottom.y;
            float target = plate != null && plate.Pressed ? rise : 0f;
            float vy = Mathf.Clamp((target - height) / Time.fixedDeltaTime, -speed, speed);
            body.linearVelocity = new Vector2(0f, vy);
        }
    }
}
