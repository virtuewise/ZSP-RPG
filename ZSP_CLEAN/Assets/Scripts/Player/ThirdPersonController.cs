using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// CharacterController-based third-person / FPS-capable controller.
    /// Supports walk, sprint, crouch, jump, stealth mode and exposes a noise
    /// level used by the StealthSystem.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraTransform;

        [Header("Speeds")]
        public float walkSpeed = 3.2f;
        public float runSpeed = 6.2f;
        public float crouchSpeed = 1.4f;

        [Header("Jump / Gravity")]
        public float jumpHeight = 1.1f;
        public float gravity = -22f;

        [Header("Turning")]
        public float turnSmoothTime = 0.08f;

        [Header("Crouch")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchLerpSpeed = 12f;

        [Header("Stealth")]
        public bool stealthMode = false;

        private CharacterController controller;
        private float verticalVelocity;
        private float turnSmoothVelocity;
        private bool crouching;
        private Vector3 horizontalVelocity;

        public bool IsCrouching => crouching;
        public bool IsSprinting { get; private set; }
        public Vector3 HorizontalVelocity => horizontalVelocity;

        /// <summary>0 = silent, 1 = sprinting. Used by enemy sensors.</summary>
        public float NoiseLevel
        {
            get
            {
                if (controller == null) return 0f;
                float speed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
                float noise = Mathf.Clamp01(speed / Mathf.Max(0.01f, runSpeed));
                if (crouching) noise *= 0.15f;
                if (stealthMode) noise *= 0.5f;
                return noise;
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standHeight = controller.height;
        }

        private void Update()
        {
            ReadMovement();
            ApplyGravityAndJump();
            ApplyCrouch();
        }

        private void ReadMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 input = new Vector3(h, 0f, v);
            if (input.sqrMagnitude > 1f) input.Normalize();

            Vector3 direction = input;
            if (cameraTransform != null)
            {
                Vector3 forward = cameraTransform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = cameraTransform.right;
                right.y = 0f;
                right.Normalize();

                direction = forward * v + right * h;
                if (direction.sqrMagnitude > 1f) direction.Normalize();
            }

            crouching = Input.GetKey(KeyCode.LeftControl) || stealthMode;
            IsSprinting = !crouching && Input.GetKey(KeyCode.LeftShift) && v > 0.1f;

            float speed = crouching ? crouchSpeed : (IsSprinting ? runSpeed : walkSpeed);

            horizontalVelocity = Vector3.zero;
            if (direction.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 move = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                horizontalVelocity = move * speed;
                controller.Move(horizontalVelocity * Time.deltaTime);
            }
        }

        private void ApplyGravityAndJump()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump") && controller.isGrounded && !crouching)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void ApplyCrouch()
        {
            float targetHeight = crouching ? crouchHeight : standHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        }
    }
}
