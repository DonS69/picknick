using System.Collections.Generic;
using Picknick.Creature;
using Unity.Netcode;
using UnityEngine;

namespace Picknick.Net
{
    /// <summary>
    /// An object whose position and on/off state the host sends to clients.
    /// Every peer builds the same room in the same order, so list index identifies the object.
    /// </summary>
    public class SyncedBody : MonoBehaviour
    {
        public static readonly List<SyncedBody> All = new List<SyncedBody>();

        Rigidbody2D body;
        Vector2 targetPosition;
        Vector2 targetVelocity;
        float targetAngle;
        float receivedAt;
        bool hasTarget;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            All.Add(this);
        }

        void OnDestroy() => All.Remove(this);

        public void Write(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(gameObject.activeSelf);
            writer.WriteValueSafe(body != null ? body.position : (Vector2)transform.position);
            writer.WriteValueSafe(body != null ? body.linearVelocity : Vector2.zero);
            writer.WriteValueSafe(transform.eulerAngles.z);
        }

        public void Read(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out bool active);
            reader.ReadValueSafe(out targetPosition);
            reader.ReadValueSafe(out targetVelocity);
            reader.ReadValueSafe(out targetAngle);
            receivedAt = Time.time;

            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
            if (!hasTarget)
            {
                transform.position = targetPosition;
                hasTarget = true;
            }
        }

        /// <summary>Forget the last snapshot (when going back offline).</summary>
        public void ClearTarget() => hasTarget = false;

        void Update()
        {
            if (!hasTarget || Authority.IsSimulating) return;

            // Extrapolate a little from the last snapshot, then ease toward it.
            Vector2 predicted = targetPosition + targetVelocity * Mathf.Min(Time.time - receivedAt, 0.1f);
            float blend = 1f - Mathf.Exp(-25f * Time.deltaTime);
            transform.position = Vector2.Lerp(transform.position, predicted, blend);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, blend));
        }
    }
}
