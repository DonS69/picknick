using System.Collections.Generic;
using UnityEngine;

namespace Picknick.Creature
{
    /// <summary>
    /// Falling is cheap: drop below the kill height and you pop back at your checkpoint.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallRespawn : MonoBehaviour
    {
        public static readonly List<FallRespawn> All = new List<FallRespawn>();

        public float killHeight = -10f;
        public Vector2 checkpoint;

        Rigidbody2D body;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            checkpoint = transform.position;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

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
