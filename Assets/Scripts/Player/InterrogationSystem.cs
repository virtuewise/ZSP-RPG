using System;
using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Raycast interrogation. Aim at an InterrogationTarget and press E.
    /// Trust rises with each successful interrogation and can unlock deeper intel.
    /// </summary>
    public class InterrogationSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera aimCamera;
        public LayerMask targetMask = ~0;

        [Header("Settings")]
        public float range = 3f;
        public KeyCode interactKey = KeyCode.E;

        public string LastIntel { get; private set; }
        public event Action<string> OnIntelReceived;

        private void Update()
        {
            if (Input.GetKeyDown(interactKey)) TryInterrogate();
        }

        public bool TryInterrogate()
        {
            Camera cam = aimCamera != null ? aimCamera : Camera.main;
            if (cam == null) return false;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, range, targetMask, QueryTriggerInteraction.Ignore))
                return false;

            InterrogationTarget target = hit.collider.GetComponentInParent<InterrogationTarget>();
            if (target == null) return false;

            LastIntel = target.GetIntel();
            OnIntelReceived?.Invoke(LastIntel);
            Debug.Log($"[ZSP] Interrogation: {LastIntel}");
            return true;
        }
    }

}
