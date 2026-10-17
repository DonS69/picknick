using UnityEngine;

namespace Picknick.Creature
{
    /// <summary>
    /// Falling is cheap: drop below the kill height and you pop back at your checkpoint.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallRespawn : MonoBehaviour
    {
        public float killHeight = -10f;
        public Vector2 checkpoint;

        Rigidbody2D body;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            checkpoint = transform.position;
        }

        void FixedUpdate()
        {
            if (body.position.y < killHeight)
                Respawn();
        }

        public void Respawn()
        {
            body.position = checkpoint;
            body.linearVelocity = Vector2.zero;
        }
    }
}
