using UnityEngine;

namespace ZSP.Player
{
    /// <summary>
    /// Minimal damage receiver for enemies and destructible props. Attach it to
    /// any imported enemy model so WeaponSystem and MeleeSystem can damage it.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        public int maxHealth = 100;
        public int currentHealth;

        [Header("Death")]
        public bool destroyOnDeath = true;
        public float destroyDelay = 3f;
        public GameObject deathEffect;

        public System.Action<int, int> OnHealthChanged;

        private bool dead;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int amount, Vector3 point, Vector3 direction)
        {
            if (dead) return;

            currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, amount));
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
                Die();
        }

        private void Die()
        {
            dead = true;

            if (deathEffect != null)
                Instantiate(deathEffect, transform.position, Quaternion.identity);

            // Stop obvious behaviours without assuming a specific enemy script.
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != this) behaviour.enabled = false;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;

            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }
}
