using System.Collections.Generic;
using UnityEngine;

namespace Picknick.Abilities
{
    /// <summary>
    /// Too heavy to push unless a creature with Strong is touching it.
    /// It can still fall; only sideways movement is locked.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Heavy : MonoBehaviour
    {
        Rigidbody2D body;
        readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>(16);

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            bool strongTouching = false;
            body.GetContacts(contacts);
            foreach (ContactPoint2D contact in contacts)
            {
                Collider2D other = contact.collider.attachedRigidbody == body ? contact.otherCollider : contact.collider;
                var abilities = other != null ? other.GetComponentInParent<CreatureAbilities>() : null;
                if (abilities != null && abilities.Current == AbilityType.Strong)
                {
                    strongTouching = true;
                    break;
                }
            }

            body.constraints = strongTouching
                ? RigidbodyConstraints2D.FreezeRotation
                : RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        }
    }
}
