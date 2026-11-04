using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Short-range melee / takedown. A crouched (stealth) strike does bonus
    /// damage so the player can silently clear a room before opening fire.
    /// </summary>
    public class MeleeSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera aimCamera;
        public ThirdPersonController controller;
        public LayerMask hitMask = ~0;

        [Header("Melee")]
        public KeyCode meleeKey = KeyCode.F;
        public float range = 1.9f;
        public float radius = 0.55f;
        public float baseDamage = 45f;
        public float stealthMultiplier = 3f;
        public float cooldown = 0.8f;
        public float knockback = 6f;

        private float nextStrikeTime;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ThirdPersonController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(meleeKey) && Time.time >= nextStrikeTime)
                Strike();
        }

        private void Strike()
        {
            nextStrikeTime = Time.time + cooldown;

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;

            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, range, hitMask,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.root == transform.root) continue;

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    bool stealth = controller != null && controller.IsCrouching;
                    float damage = baseDamage * (stealth ? stealthMultiplier : 1f);
                    damageable.TakeDamage(Mathf.RoundToInt(damage), hit.point, direction);
                }

                Rigidbody body = hit.collider.attachedRigidbody;
                if (body != null && !body.isKinematic)
                    body.AddForceAtPosition(direction * knockback, hit.point, ForceMode.Impulse);
                break;
            }
        }
    }
}
