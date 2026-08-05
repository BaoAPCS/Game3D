using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private Animator animator;
        [SerializeField] private string deathTrigger = "Die";
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private bool disableOnDeath = true;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
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
            Debug.Log(
                $"[EnemyHealth] {gameObject.name} took {damage:0.##} damage. HP: {currentHealth:0.##}/{maxHealth:0.##}.",
                this);

            if (Mathf.Approximately(currentHealth, 0f))
            {
                Die();
            }
        }

        public void ResetHealth()
        {
            IsDead = false;
            currentHealth = maxHealth;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            Debug.Log($"[EnemyHealth] {gameObject.name} died.", this);

            if (animator != null && !string.IsNullOrWhiteSpace(deathTrigger) && HasTrigger(animator, deathTrigger))
            {
                animator.SetTrigger(deathTrigger);
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else if (disableOnDeath)
            {
                gameObject.SetActive(false);
            }
        }

        private static bool HasTrigger(Animator targetAnimator, string triggerName)
        {
            if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = targetAnimator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
