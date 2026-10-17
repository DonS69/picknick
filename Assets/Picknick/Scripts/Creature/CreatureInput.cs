using UnityEngine;

namespace Picknick.Creature
{
    /// <summary>
    /// One frame of intent for a creature. Local devices fill this today; later the
    /// network layer fills it for the owning client, so the controller never reads devices.
    /// </summary>
    public struct CreatureInput
    {
        public float Move;
        public bool JumpHeld;
        public bool JumpPressed;
        public bool ActionHeld;
        public bool ActionPressed;
        public Vector2 Aim;
    }
}
