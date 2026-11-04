using UnityEngine;

namespace ZSP.Vehicles
{
    /// <summary>
    /// WheelCollider vehicle controller for jeeps, trucks and technicals.
    /// Assign front/rear wheel colliders in the inspector.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Wheels")]
        public WheelCollider frontLeft;
        public WheelCollider frontRight;
        public WheelCollider rearLeft;
        public WheelCollider rearRight;

        [Header("Drive")]
        public float motorForce = 1800f;
        public float steerAngle = 32f;
        public float brakeForce = 3500f;
        public float maxSpeed = 26f;          // m/s
        public float reverseSpeed = 8f;

        [Header("Stability")]
        public Transform centerOfMass;
        public float downforce = 120f;

        private Rigidbody body;
        private float currentSteer;

        public float SpeedKph => body != null ? body.velocity.magnitude * 3.6f : 0f;
        public bool IsBraking { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (centerOfMass != null) body.centerOfMass = centerOfMass.localPosition;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");
            IsBraking = Input.GetKey(KeyCode.Space);

            float speed = body.velocity.magnitude;
            float motor = throttle * motorForce;

            if (speed > maxSpeed && throttle > 0f) motor = 0f;
            if (throttle < 0f && speed > reverseSpeed) motor = 0f;

            frontLeft.motorTorque = motor;
            frontRight.motorTorque = motor;
            rearLeft.motorTorque = motor * 0.9f;
            rearRight.motorTorque = motor * 0.9f;

            currentSteer = Mathf.Lerp(currentSteer, steer * steerAngle, Time.fixedDeltaTime * 8f);
            frontLeft.steerAngle = currentSteer;
            frontRight.steerAngle = currentSteer;

            float brake = IsBraking ? brakeForce : 0f;
            frontLeft.brakeTorque = brake;
            frontRight.brakeTorque = brake;
            rearLeft.brakeTorque = brake;
            rearRight.brakeTorque = brake;

            body.AddForce(-transform.up * downforce * Mathf.Clamp01(speed / maxSpeed));
        }
    }
}
