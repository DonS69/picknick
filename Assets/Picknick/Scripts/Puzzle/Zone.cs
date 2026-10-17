using UnityEngine;

namespace Picknick.Puzzle
{
    /// <summary>
    /// An axis-aligned area. Puzzle pieces poll it instead of using trigger colliders,
    /// so carried (kinematic) creatures count the same as walking ones.
    /// </summary>
    public class Zone : MonoBehaviour
    {
        public Vector2 size = Vector2.one;

        public bool Contains(Vector2 point)
        {
            Vector2 center = transform.position;
            return Mathf.Abs(point.x - center.x) <= size.x * 0.5f
                && Mathf.Abs(point.y - center.y) <= size.y * 0.5f;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
