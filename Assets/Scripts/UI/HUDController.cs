using UnityEngine;
using UnityEngine.UI;
using ZSP.Player;

namespace ZSP.UI
{
    /// <summary>
    /// Minimal HUD driven by the new player systems. Assign the UI Text fields
    /// and the player components in the inspector.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Player Systems")]
        public ThirdPersonController controller;
        public WeaponSystem weapons;
        public StealthSystem stealth;

        [Header("UI")]
        public Text ammoText;
        public Text detectionText;
        public Text objectiveText;
        public Image detectionFill;

        [Header("Colours")]
        public Color calmColor = new Color(0.2f, 0.8f, 0.3f);
        public Color alertColor = new Color(0.9f, 0.2f, 0.15f);

        public string CurrentObjective { get; private set; } = "Find the first lead.";

        private void Start()
        {
            if (weapons != null) weapons.OnAmmoChanged += UpdateAmmo;
            SetObjective(CurrentObjective);
        }

        private void OnDestroy()
        {
            if (weapons != null) weapons.OnAmmoChanged -= UpdateAmmo;
        }

        private void Update()
        {
            if (stealth != null)
            {
                float d = stealth.Detection;
                if (detectionText != null)
                    detectionText.text = stealth.IsDetected ? "DETECTED" : $"SUSPICION {Mathf.RoundToInt(d * 100f)}%";

                if (detectionFill != null)
                {
                    detectionFill.fillAmount = d;
                    detectionFill.color = Color.Lerp(calmColor, alertColor, d);
                }
            }
        }

        private void UpdateAmmo(int current, int max)
        {
            if (ammoText != null) ammoText.text = $"{current} / {max}";
        }

        public void SetObjective(string objective)
        {
            CurrentObjective = objective;
            if (objectiveText != null) objectiveText.text = objective;
        }
    }
}
