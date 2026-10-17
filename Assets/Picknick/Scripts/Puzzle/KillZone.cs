using Picknick.Creature;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>Water and the like: anything with FallRespawn that ends up inside goes back to its checkpoint.</summary>
    [RequireComponent(typeof(Zone))]
    public class KillZone : MonoBehaviour
    {
        Zone zone;

        void Awake() => zone = GetComponent<Zone>();

        void FixedUpdate()
        {
            if (!Authority.IsSimulating) return;
            foreach (FallRespawn respawn in FallRespawn.All)
                if (zone.Contains(respawn.transform.position))
                    respawn.Respawn();
        }
    }
}
