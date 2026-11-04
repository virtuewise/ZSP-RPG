using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Orbit third-person camera with collision, aim zoom and shoulder offset.
    /// Attach to a Camera, assign the player root as Target.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.55f, 0f);

        [Header("Orbit")]
        public float distance = 4.2f;
        public float aimDistance = 1.8f;
        public float sensitivity = 2.2f;
        public float minPitch = -35f;
        public float maxPitch = 70f;
        public float followLerp = 18f;
        public float shoulderOffset = 0.35f;

        [Header("Collision")]
        public float collisionRadius = 0.22f;
        public LayerMask collisionMask = ~0;

        [Header("Input")]
        public KeyCode aimKey = KeyCode.Mouse1;

        private float yaw;
        private float pitch;
        private bool aiming;
        private readonly RaycastHit[] collisionHits = new RaycastHit[16];

        public bool IsAiming => aiming;

        private void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            aiming = Input.GetKey(aimKey);

            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 right = rotation * Vector3.right * (aiming ? shoulderOffset * 0.5f : shoulderOffset);
            pivot += right;

            float desiredDistance = aiming ? aimDistance : distance;
            Vector3 backward = rotation * Vector3.back;
            Vector3 desired = pivot + backward * desiredDistance;

            int hitCount = Physics.SphereCastNonAlloc(pivot, collisionRadius, backward, collisionHits,
                desiredDistance, collisionMask, QueryTriggerInteraction.Ignore);
            float nearestObstacle = desiredDistance;
            for (int i = 0; i < hitCount; i++)
            {
                var collider = collisionHits[i].collider;
                if (collider == null || collider.transform.root == target.root) continue;
                if (collisionHits[i].distance < nearestObstacle)
                    nearestObstacle = collisionHits[i].distance;
            }
            if (nearestObstacle < desiredDistance)
                desired = pivot + backward * Mathf.Max(0.4f, nearestObstacle - 0.05f);

            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followLerp);
            transform.rotation = rotation;
        }
    }
}
