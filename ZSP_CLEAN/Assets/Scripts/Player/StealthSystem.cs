using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Simple, extendable stealth model: enemy sensors accumulate detection
    /// based on distance, field of view, line of sight, player noise and crouch.
    /// </summary>
    public class StealthSystem : MonoBehaviour
    {
        [Header("References")]
        public Transform playerRoot;
        public ThirdPersonController controller;
        public ProgressionSystem progression;

        [Header("Detection")]
        public float detectionRadius = 22f;
        public float baseDetectionRate = 0.65f;
        public float decayRate = 0.35f;
        public float crouchMultiplier = 0.45f;
        public LayerMask occlusionMask = ~0;

        [Range(0f, 1f)] public float detection;
        public bool IsDetected => detection >= 1f;
        public float Detection => detection;

        private void Awake()
        {
            if (playerRoot == null) playerRoot = transform;
            if (controller == null) controller = GetComponent<ThirdPersonController>();
        }

        private void Update()
        {
            float strongest = 0f;

            Collider[] hits = Physics.OverlapSphere(playerRoot.position, detectionRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider c in hits)
            {
                EnemySensor sensor = c.GetComponentInParent<EnemySensor>();
                if (sensor == null) continue;

                Vector3 eye = sensor.EyePosition;
                Vector3 toPlayer = (playerRoot.position + Vector3.up * 1.2f) - eye;
                float distance = toPlayer.magnitude;
                if (distance > detectionRadius) continue;

                Vector3 dir = toPlayer.normalized;
                float angle = Vector3.Angle(sensor.Forward, dir);
                if (angle > sensor.fieldOfView * 0.5f) continue;

                if (Physics.Raycast(eye, dir, out RaycastHit hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.root != playerRoot.root) continue;
                }

                float distanceFactor = 1f - Mathf.Clamp01(distance / detectionRadius);
                float noise = controller != null ? controller.NoiseLevel : 0f;
                float crouch = controller != null && controller.IsCrouching ? crouchMultiplier : 1f;

                float rate = baseDetectionRate * (0.35f + distanceFactor + noise) * crouch;
                if (progression != null) rate *= progression.StealthMultiplier;
                strongest = Mathf.Max(strongest, rate);
            }

            if (strongest > 0f)
                detection = Mathf.Clamp01(detection + strongest * Time.deltaTime);
            else
                detection = Mathf.Clamp01(detection - decayRate * Time.deltaTime);
        }

        public void ResetDetection() => detection = 0f;
    }

    /// <summary>Attach to an enemy (or a child eye object).</summary>
    public class EnemySensor : MonoBehaviour
    {
        public Transform eye;
        public float fieldOfView = 100f;

        public Vector3 EyePosition => eye != null ? eye.position : transform.position + Vector3.up * 1.6f;
        public Vector3 Forward => eye != null ? eye.forward : transform.forward;
    }
}
