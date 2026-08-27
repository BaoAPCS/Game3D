using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Labels the collider that receives melee hits for a character. Damage is
    /// still owned by CombatHealth on the character root; this component makes
    /// the hurtbox relationship explicit and inspectable without duplicating
    /// physics colliders.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHurtbox : MonoBehaviour
    {
        [SerializeField] private Collider volume;
        [SerializeField] private CombatHealth ownerHealth;

        public Collider Volume => volume;
        public CombatHealth OwnerHealth => ownerHealth;

        private void Awake()
        {
            ResolveReferences();
        }

        public void Configure(Collider targetVolume, CombatHealth health)
        {
            volume = targetVolume;
            ownerHealth = health;
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (volume == null)
            {
                volume = GetComponent<Collider>();
            }

            if (ownerHealth == null)
            {
                ownerHealth = GetComponentInParent<CombatHealth>();
            }

            if (volume == null)
            {
                Debug.LogError(
                    $"[CombatHurtbox] '{name}' does not have a Collider.",
                    this);
            }

            if (ownerHealth == null)
            {
                Debug.LogError(
                    $"[CombatHurtbox] '{name}' does not have CombatHealth in its parent hierarchy.",
                    this);
            }
        }
    }
}
