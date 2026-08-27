using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Passive combat health shared by Nam and Henry. Encounter-specific
    /// reactions, victory, defeat, and UI subscribe to its events separately.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool startAtMaximumHealth = true;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }

        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            currentHealth = startAtMaximumHealth
                ? maxHealth
                : Mathf.Clamp(currentHealth, 0f, maxHealth);
            IsDead = Mathf.Approximately(currentHealth, 0f);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || damage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            HealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log(
                $"[CombatHealth] {name} nhận {damage:0.##} sát thương. HP: {currentHealth:0.##}/{maxHealth:0.##}.",
                this);

            if (!Mathf.Approximately(currentHealth, 0f))
            {
                return;
            }

            IsDead = true;
            Died?.Invoke();
        }

        public void ResetHealth()
        {
            IsDead = false;
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(float value, bool refill)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill)
            {
                ResetHealth();
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            IsDead = Mathf.Approximately(currentHealth, 0f);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
