using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [Serializable]
    public sealed class ComboAttack
    {
        public string attackName = "Attack";
        public string animationTrigger = "PunchLeft";
        [Min(0f)] public float damage = 10f;
        [Min(0f)] public float attackRange = 1.2f;
        [Min(0.01f)] public float attackRadius = 0.35f;
        [Min(0f)] public float hitTime = 0.22f;
        [Min(0.01f)] public float attackDuration = 0.65f;
        [Min(0f)] public float comboInputStartTime = 0.25f;
        [Min(0f)] public float comboInputEndTime = 0.58f;
        [Min(0f)] public float recoveryTime = 0.08f;

        public void Sanitize()
        {
            damage = Mathf.Max(0f, damage);
            attackRange = Mathf.Max(0f, attackRange);
            attackRadius = Mathf.Max(0.01f, attackRadius);
            hitTime = Mathf.Max(0f, hitTime);
            attackDuration = Mathf.Max(0.01f, attackDuration);
            hitTime = Mathf.Min(hitTime, attackDuration);
            comboInputStartTime = Mathf.Min(Mathf.Max(0f, comboInputStartTime), attackDuration);
            comboInputEndTime = Mathf.Min(Mathf.Max(comboInputStartTime, comboInputEndTime), attackDuration);
            recoveryTime = Mathf.Max(0f, recoveryTime);
        }
    }
}
