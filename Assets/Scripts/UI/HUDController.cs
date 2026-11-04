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
        public PlayerHealth playerHealth;

        [Header("UI")]
        public Text ammoText;
        public Text detectionText;
        public Text objectiveText;
        public Image detectionFill;
        public Text healthText;

        [Header("Colours")]
        public Color calmColor = new Color(0.2f, 0.8f, 0.3f);
        public Color alertColor = new Color(0.9f, 0.2f, 0.15f);

        public string CurrentObjective { get; private set; } = "Find the first lead.";

        private void Start()
        {
            if (weapons != null)
            {
                weapons.OnAmmoChanged += UpdateAmmo;
                UpdateAmmo(weapons.CurrentAmmo, weapons.CurrentMagazineSize);
            }
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealth;
                UpdateHealth(playerHealth.health);
            }
            SetObjective(CurrentObjective);
        }

        private void OnDestroy()
        {
            if (weapons != null) weapons.OnAmmoChanged -= UpdateAmmo;
            if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealth;
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
            if (ammoText != null) ammoText.text = $"AMMO {current} / {max}";
        }

        private void UpdateHealth(int current)
        {
            if (healthText != null && playerHealth != null)
                healthText.text = $"HEALTH {current} / {playerHealth.maxHealth}";
        }

        public void SetObjective(string objective)
        {
            CurrentObjective = objective;
            if (objectiveText != null) objectiveText.text = objective;
        }
    }
}
