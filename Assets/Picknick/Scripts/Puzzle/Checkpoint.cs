using Picknick.Abilities;
using Picknick.Creature;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>Creatures entering the zone respawn here from now on.</summary>
    [RequireComponent(typeof(Zone))]
    public class Checkpoint : MonoBehaviour
    {
        Zone zone;

        void Awake() => zone = GetComponent<Zone>();

        void FixedUpdate()
        {
            foreach (CreatureAbilities creature in CreatureAbilities.All)
            {
                if (zone.Contains(creature.transform.position) && creature.TryGetComponent(out FallRespawn respawn))
                    respawn.checkpoint = transform.position;
            }
        }
    }
}
