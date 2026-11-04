using System;
using UnityEngine;

namespace ZSP.Player
{
    public enum UpgradeType
    {
        Damage,
        Armor,
        Stealth,
        Reload
    }

    /// <summary>
    /// Simple credits/XP progression with four upgrade tracks. WeaponSystem and
    /// StealthSystem read the multipliers automatically when this component is
    /// present on the same player.
    /// </summary>
    public class ProgressionSystem : MonoBehaviour
    {
        [Header("Progress")]
        public int credits = 250;
        public int xp = 0;
        public int level = 1;
        public int maxTier = 5;
        public int baseUpgradeCost = 100;

        [Header("Upgrade Tiers")]
        [Range(0, 5)] public int damageTier = 0;
        [Range(0, 5)] public int armorTier = 0;
        [Range(0, 5)] public int stealthTier = 0;
        [Range(0, 5)] public int reloadTier = 0;

        public event Action Changed;

        public float DamageMultiplier => 1f + damageTier * 0.1f;
        public float ReloadMultiplier => Mathf.Max(0.5f, 1f - reloadTier * 0.08f);
        public float StealthMultiplier => Mathf.Max(0.4f, 1f - stealthTier * 0.1f);
        public int ArmorBonus => armorTier * 10;
        public int XpForNextLevel => level * 500;

        public void AddXp(int amount)
        {
            xp += Mathf.Max(0, amount);
            while (xp >= XpForNextLevel)
            {
                xp -= XpForNextLevel;
                level++;
            }
            Changed?.Invoke();
        }

        public void AddCredits(int amount)
        {
            credits = Mathf.Max(0, credits + amount);
            Changed?.Invoke();
        }

        public int Tier(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage: return damageTier;
                case UpgradeType.Armor: return armorTier;
                case UpgradeType.Stealth: return stealthTier;
                case UpgradeType.Reload: return reloadTier;
                default: return 0;
            }
        }

        public int UpgradeCost(UpgradeType type) => baseUpgradeCost + Tier(type) * 75;

        public bool TryUpgrade(UpgradeType type)
        {
            int tier = Tier(type);
            if (tier >= maxTier) return false;
            int cost = UpgradeCost(type);
            if (credits < cost) return false;

            credits -= cost;
            switch (type)
            {
                case UpgradeType.Damage: damageTier++; break;
                case UpgradeType.Armor: armorTier++; break;
                case UpgradeType.Stealth: stealthTier++; break;
                case UpgradeType.Reload: reloadTier++; break;
            }
            Changed?.Invoke();
            return true;
        }
    }
}
