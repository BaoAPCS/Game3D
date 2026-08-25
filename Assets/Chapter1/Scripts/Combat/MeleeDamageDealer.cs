using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Evaluates a bone-following box volume explicitly. This avoids Rigidbody
    /// and OnTriggerEnter requirements on animated character bones.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeleeDamageDealer : MonoBehaviour
    {
        private const int HitBufferSize = 32;

        [SerializeField] private MeleeHitboxRig hitboxRig;
        [SerializeField] private LayerMask targetLayerMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Collide;
        [SerializeField] private bool useSweptQuery = true;

        private readonly Collider[] overlapBuffer =
            new Collider[HitBufferSize];
        private readonly RaycastHit[] castBuffer =
            new RaycastHit[HitBufferSize];
        private readonly HashSet<IDamageable> damagedTargets =
            new HashSet<IDamageable>();

        private MeleeHitboxLimb activeLimb;
        private float activeDamage;
        private int activeAttackToken;
        private bool hitWindowActive;
        private bool hasPreviousPose;
        private MeleeHitboxPose previousPose;

        public bool IsHitWindowActive => hitWindowActive;
        public int DamagedTargetCount => damagedTargets.Count;
        public LayerMask TargetLayerMask => targetLayerMask;

        private void Awake()
        {
            ResolveRig();
        }

        private void OnDisable()
        {
            EndHitWindow();
        }

        public void Configure(
            MeleeHitboxRig rig,
            LayerMask layers,
            QueryTriggerInteraction queryTriggerInteraction =
                QueryTriggerInteraction.Collide)
        {
            hitboxRig = rig;
            targetLayerMask = layers;
            triggerInteraction = queryTriggerInteraction;
        }

        public bool BeginHitWindow(
            MeleeHitboxLimb limb,
            float damage,
            int attackToken)
        {
            EndHitWindow();
            ResolveRig();
            if (limb == MeleeHitboxLimb.None ||
                damage <= 0f ||
                hitboxRig == null ||
                !hitboxRig.TryGetPose(limb, out previousPose))
            {
                return false;
            }

            activeLimb = limb;
            activeDamage = damage;
            activeAttackToken = attackToken;
            damagedTargets.Clear();
            hitWindowActive = true;
            hasPreviousPose = true;
            return true;
        }

        public int PerformSingleHit(
            MeleeHitboxLimb limb,
            float damage,
            int attackToken)
        {
            if (!BeginHitWindow(limb, damage, attackToken))
            {
                return 0;
            }

            EvaluateHitWindow();
            int hitCount = damagedTargets.Count;
            EndHitWindow();
            return hitCount;
        }

        public int EvaluateHitWindow()
        {
            if (!hitWindowActive ||
                hitboxRig == null ||
                !hitboxRig.TryGetPose(activeLimb, out MeleeHitboxPose pose))
            {
                return damagedTargets.Count;
            }

            // Auto Sync Transforms is disabled in this project. Synchronize
            // animated/moved hurtboxes before querying the current visual pose.
            Physics.SyncTransforms();

            int overlapCount = Physics.OverlapBoxNonAlloc(
                pose.Center,
                pose.HalfExtents,
                overlapBuffer,
                pose.Rotation,
                targetLayerMask,
                triggerInteraction);
            for (int i = 0; i < overlapCount; i++)
            {
                TryDamageCollider(overlapBuffer[i]);
            }

            if (useSweptQuery && hasPreviousPose)
            {
                Vector3 displacement = pose.Center - previousPose.Center;
                float distance = displacement.magnitude;
                if (distance > 0.0001f)
                {
                    int castCount = Physics.BoxCastNonAlloc(
                        previousPose.Center,
                        previousPose.HalfExtents,
                        displacement / distance,
                        castBuffer,
                        previousPose.Rotation,
                        distance,
                        targetLayerMask,
                        triggerInteraction);
                    for (int i = 0; i < castCount; i++)
                    {
                        TryDamageCollider(castBuffer[i].collider);
                    }
                }
            }

            previousPose = pose;
            hasPreviousPose = true;
            return damagedTargets.Count;
        }

        public void EndHitWindow(int attackToken)
        {
            if (!hitWindowActive || attackToken != activeAttackToken)
            {
                return;
            }

            EndHitWindow();
        }

        public void EndHitWindow()
        {
            hitWindowActive = false;
            hasPreviousPose = false;
            activeLimb = MeleeHitboxLimb.None;
            activeDamage = 0f;
            activeAttackToken = 0;
            damagedTargets.Clear();
        }

        private void ResolveRig()
        {
            if (hitboxRig == null)
            {
                hitboxRig = GetComponent<MeleeHitboxRig>();
            }
        }

        private void TryDamageCollider(Collider hitCollider)
        {
            if (hitCollider == null ||
                hitCollider.transform.IsChildOf(transform))
            {
                return;
            }

            CombatHurtbox hurtbox =
                hitCollider.GetComponentInParent<CombatHurtbox>();
            IDamageable damageable = hurtbox != null &&
                hurtbox.OwnerHealth != null
                    ? hurtbox.OwnerHealth
                    : hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || damagedTargets.Contains(damageable))
            {
                return;
            }

            damagedTargets.Add(damageable);
            damageable.TakeDamage(activeDamage);
        }
    }
}
