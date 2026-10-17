using Picknick.Abilities;
using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>The room is cleared only when every creature is inside at the same time.</summary>
    [RequireComponent(typeof(Zone))]
    public class ExitZone : MonoBehaviour
    {
        Zone zone;

        public int Inside { get; private set; }
        public int Total => CreatureAbilities.All.Count;
        public bool Cleared { get; private set; }
        public float ClearTime { get; private set; }

        void Awake() => zone = GetComponent<Zone>();

        void FixedUpdate()
        {
            if (Cleared) return;

            int inside = 0;
            foreach (CreatureAbilities creature in CreatureAbilities.All)
                if (zone.Contains(creature.transform.position)) inside++;
            Inside = inside;

            if (inside > 0 && inside == Total)
            {
                Cleared = true;
                ClearTime = Time.timeSinceLevelLoad;
            }
        }
    }
}
