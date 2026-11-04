using System;
using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Minimal health component used by the vertical slice and the SIS integration.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        public int maxHealth = 10;
        public int health = 10;

        public event Action<int> OnHealthChanged;
        public event Action OnDeath;

        public bool IsAlive => health > 0;

        private void Awake()
        {
            health = Mathf.Clamp(health, 0, maxHealth);
        }

        public void Damage(int amount)
        {
            if (!IsAlive) return;
            health = Mathf.Max(0, health - Mathf.Max(0, amount));
            OnHealthChanged?.Invoke(health);
            if (health <= 0) OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;
            health = Mathf.Min(maxHealth, health + Mathf.Max(0, amount));
            OnHealthChanged?.Invoke(health);
        }
    }
}
