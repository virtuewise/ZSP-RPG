using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Simple data-driven NPC target for the vertical-slice interrogation interaction.
    /// </summary>
    public class InterrogationTarget : MonoBehaviour
    {
        [TextArea] public string[] intel =
        {
            "I only move cargo. I never saw the manifest.",
            "The manifest came through the Mbare relay.",
            "Meridian has a watcher near the safehouse."
        };
        [Range(0f, 1f)] public float trust = 0.35f;
        public float trustPerAnswer = 0.15f;

        private int index;

        public string GetIntel()
        {
            if (intel == null || intel.Length == 0) return "No useful information.";
            int start = Mathf.Clamp(Mathf.FloorToInt(trust * intel.Length), 0, intel.Length - 1);
            string result = intel[Mathf.Clamp(index + start, 0, intel.Length - 1)];
            index++;
            trust = Mathf.Clamp01(trust + trustPerAnswer);
            return result;
        }
    }
}
