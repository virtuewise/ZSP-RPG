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
            Debug.Log($"[ZSP] Interrogation: {LastIntel}");
            return true;
        }
    }

    public class InterrogationTarget : MonoBehaviour
    {
        [TextArea] public string[] intel = { "I only move cargo. I never saw the manifest." };
        [Range(0f, 1f)] public float trust = 0.35f;
        public float trustPerAnswer = 0.15f;

        private int index;

        public string GetIntel()
        {
            if (intel == null || intel.Length == 0) return "No useful information.";

            // Higher trust reveals later (more valuable) entries first.
            int start = Mathf.Clamp(Mathf.FloorToInt(trust * intel.Length), 0, intel.Length - 1);
            string result = intel[Mathf.Clamp(index + start, 0, intel.Length - 1)];
            index++;
            trust = Mathf.Clamp01(trust + trustPerAnswer);
            return result;
        }
    }
}
