using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class CombatAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController combatController;

        private void Awake()
        {
            ResolveController();
        }

        private void OnValidate()
        {
            ResolveController();
        }

        public void OpenComboWindow()
        {
            ResolveController();
            combatController?.OpenComboWindow();
        }

        public void PerformAttackHit()
        {
            ResolveController();
            combatController?.PerformAttackHit();
        }

        public void CloseComboWindow()
        {
            ResolveController();
            combatController?.CloseComboWindow();
        }

        public void EndAttack()
        {
            ResolveController();
            combatController?.EndAttack();
        }

        private void ResolveController()
        {
            if (combatController == null)
            {
                combatController = GetComponentInParent<PlayerCombatController>(true);
            }
        }
    }
}
