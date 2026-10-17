using Picknick.Abilities;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>Hangs still (like a key on a hook) until a Magnet pulls it, then falls freely.</summary>
    [RequireComponent(typeof(Magnetic), typeof(Rigidbody2D))]
    public class HookedItem : MonoBehaviour
    {
        Magnetic magnetic;
        Rigidbody2D body;

        void Awake()
        {
            magnetic = GetComponent<Magnetic>();
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        void Update()
        {
            if (body.bodyType == RigidbodyType2D.Kinematic && MagnetAbility.IsTargeted(magnetic))
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                enabled = false;
            }
        }
    }
}
