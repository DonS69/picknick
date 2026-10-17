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
            if (key == null || keyhole == null || !keyhole.Contains(key.position))
                return;

            Destroy(key.gameObject);
            gameObject.SetActive(false);
        }
    }
}
