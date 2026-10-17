using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>Blocks the way until the key is brought into the keyhole zone.</summary>
    public class Hatch : MonoBehaviour
    {
        public Transform key;
        public Zone keyhole;

        void FixedUpdate()
        {
            if (key == null || keyhole == null || !key.gameObject.activeSelf || !keyhole.Contains(key.position))
                return;

            // Deactivate rather than destroy, so online peers keep the same list of synced objects.
            key.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
