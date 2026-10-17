using System.Collections.Generic;
using Picknick.Creature;
using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>Anything a Magnet can grab: metal objects and every creature.</summary>
    public class Magnetic : MonoBehaviour
    {
        public static readonly List<Magnetic> All = new List<Magnetic>();

        public Rigidbody2D Body { get; private set; }
        public CreatureController Creature { get; private set; }
        public Vector2 Position => transform.position;

        /// <summary>Fixed metal (no rigidbody, or not dynamic) never moves: the Magnet flies to it.</summary>
        public bool IsMovable =>
            Body != null && Body.bodyType == RigidbodyType2D.Dynamic && (Creature == null || !Creature.IsCarried);

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Creature = GetComponent<CreatureController>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
