using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class CombatHitboxEditModeTests
    {
        private const string PlayerPrefabPath =
            "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string HenryMmaClipPath =
            "Assets/Chapter1/Resources/Henry/Henry_Mma_Kick.anim";
        private const string HenryRoundhouseClipPath =
            "Assets/Chapter1/Resources/Henry/Henry_Roundhouse_Kick.anim";

        [Test]
        public void PlayerPrefabContainsCombatHitboxAndHealthComponents()
        {
            GameObject player =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Assert.NotNull(player, "Player prefab is missing.");
            Assert.NotNull(player.GetComponent<CharacterController>());
            Assert.NotNull(player.GetComponent<CombatHealth>());
            CombatHurtbox hurtbox = player.GetComponent<CombatHurtbox>();
            Assert.NotNull(hurtbox);
            Assert.AreSame(
                player.GetComponent<CharacterController>(),
                hurtbox.Volume);
            Assert.AreSame(
                player.GetComponent<CombatHealth>(),
                hurtbox.OwnerHealth);
            Assert.NotNull(player.GetComponent<MeleeHitboxRig>());
            Assert.NotNull(player.GetComponent<MeleeDamageDealer>());

            PlayerCombatController combat =
                player.GetComponent<PlayerCombatController>();
            Assert.NotNull(combat);
            Assert.AreEqual(4, combat.ComboAttacks.Count);
            Assert.AreEqual(
                MeleeHitboxLimb.LeftHand,
                combat.ComboAttacks[0].ResolveHitLimb());
            Assert.AreEqual(
                MeleeHitboxLimb.RightHand,
                combat.ComboAttacks[1].ResolveHitLimb());
            Assert.AreEqual(
                MeleeHitboxLimb.LeftHand,
                combat.ComboAttacks[2].ResolveHitLimb());
            Assert.AreEqual(
                MeleeHitboxLimb.RightHand,
                combat.ComboAttacks[3].ResolveHitLimb());

            SerializedObject serializedCombat =
                new SerializedObject(combat);
            Assert.AreEqual(
                (int)MeleeHitboxLimb.RightFoot,
                serializedCombat.FindProperty(
                    "neutralKickAttack.hitLimb").enumValueIndex);
            Assert.AreEqual(
                (int)MeleeHitboxLimb.LeftFoot,
                serializedCombat.FindProperty(
                    "forwardKickAttack.hitLimb").enumValueIndex);
            Assert.AreEqual(
                (int)MeleeHitboxLimb.RightFoot,
                serializedCombat.FindProperty(
                    "backwardKickAttack.hitLimb").enumValueIndex);
        }

        [Test]
        public void NamPrefabResolvesAllFourAnimatedLimbHitboxes()
        {
            GameObject playerAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(playerAsset)
                    as GameObject;
                Assert.NotNull(instance);

                Animator animator = instance.GetComponent<Animator>();
                MeleeHitboxRig rig = instance.GetComponent<MeleeHitboxRig>();
                Assert.NotNull(animator);
                Assert.IsTrue(animator.isHuman);
                Assert.NotNull(rig);
                Assert.IsTrue(rig.ConfigureForNam(animator));

                MeleeHitboxLimb[] limbs =
                {
                    MeleeHitboxLimb.LeftHand,
                    MeleeHitboxLimb.RightHand,
                    MeleeHitboxLimb.LeftFoot,
                    MeleeHitboxLimb.RightFoot
                };
                for (int i = 0; i < limbs.Length; i++)
                {
                    Assert.IsTrue(rig.TryGetHitbox(
                        limbs[i],
                        out BoxCollider volume));
                    Assert.NotNull(volume);
                    Assert.IsTrue(volume.isTrigger);
                    Assert.IsFalse(volume.enabled);
                    Assert.IsTrue(rig.TryGetPose(limbs[i], out var pose));
                    Assert.Greater(pose.HalfExtents.sqrMagnitude, 0f);
                }
            }
            finally
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void CombatHealthClampsDamageAndRaisesDeathOnlyOnce()
        {
            GameObject owner = new GameObject("HealthOwner");
            try
            {
                CombatHealth health = owner.AddComponent<CombatHealth>();
                int healthEvents = 0;
                int deathEvents = 0;
                health.HealthChanged += (_, _) => healthEvents++;
                health.Died += () => deathEvents++;

                health.TakeDamage(-1f);
                Assert.AreEqual(100f, health.CurrentHealth);

                health.TakeDamage(35f);
                Assert.AreEqual(65f, health.CurrentHealth);
                Assert.IsFalse(health.IsDead);

                health.TakeDamage(1000f);
                health.TakeDamage(10f);
                Assert.AreEqual(0f, health.CurrentHealth);
                Assert.IsTrue(health.IsDead);
                Assert.AreEqual(2, healthEvents);
                Assert.AreEqual(1, deathEvents);
                Assert.IsTrue(owner.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BoxQueryDamagesOneOwnerOnlyOncePerAttack()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            Assert.GreaterOrEqual(playerLayer, 0, "Player layer is missing.");

            GameObject attacker = new GameObject("HenryHitboxTest");
            GameObject target = new GameObject("NamHurtboxTest");
            try
            {
                GameObject foot = new GameObject("CC_Base_R_Foot_024");
                foot.transform.SetParent(attacker.transform, false);

                MeleeHitboxRig rig = attacker.AddComponent<MeleeHitboxRig>();
                Assert.IsTrue(rig.ConfigureForHenry());
                Assert.IsTrue(rig.TryGetHitbox(
                    MeleeHitboxLimb.RightFoot,
                    out BoxCollider attackVolume));
                attackVolume.center = Vector3.zero;
                attackVolume.size = Vector3.one;

                MeleeDamageDealer dealer =
                    attacker.AddComponent<MeleeDamageDealer>();
                dealer.Configure(
                    rig,
                    1 << playerLayer,
                    QueryTriggerInteraction.Collide);

                target.layer = playerLayer;
                CombatHealth health = target.AddComponent<CombatHealth>();
                BoxCollider targetCollider = target.AddComponent<BoxCollider>();
                CombatHurtbox hurtbox = target.AddComponent<CombatHurtbox>();
                hurtbox.Configure(targetCollider, health);

                GameObject secondHurtbox = new GameObject("SecondHurtbox");
                secondHurtbox.layer = playerLayer;
                secondHurtbox.transform.SetParent(target.transform, false);
                secondHurtbox.AddComponent<BoxCollider>();

                Physics.SyncTransforms();
                int hitCount = dealer.PerformSingleHit(
                    MeleeHitboxLimb.RightFoot,
                    25f,
                    1);

                Assert.AreEqual(1, hitCount);
                Assert.AreEqual(75f, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void SweptBoxDetectsTargetBetweenAnimationFrames()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            Assert.GreaterOrEqual(playerLayer, 0, "Player layer is missing.");

            GameObject attacker = new GameObject("SweptHitboxTest");
            GameObject target = new GameObject("SweptTarget");
            try
            {
                GameObject foot = new GameObject("CC_Base_R_Foot_024");
                foot.transform.SetParent(attacker.transform, false);
                foot.transform.position = new Vector3(-2f, 0f, 0f);

                MeleeHitboxRig rig = attacker.AddComponent<MeleeHitboxRig>();
                Assert.IsTrue(rig.ConfigureForHenry());
                Assert.IsTrue(rig.TryGetHitbox(
                    MeleeHitboxLimb.RightFoot,
                    out BoxCollider attackVolume));
                attackVolume.center = Vector3.zero;
                attackVolume.size = Vector3.one * 0.2f;

                MeleeDamageDealer dealer =
                    attacker.AddComponent<MeleeDamageDealer>();
                dealer.Configure(
                    rig,
                    1 << playerLayer,
                    QueryTriggerInteraction.Collide);

                target.layer = playerLayer;
                target.transform.position = Vector3.zero;
                CombatHealth health = target.AddComponent<CombatHealth>();
                BoxCollider targetCollider = target.AddComponent<BoxCollider>();
                targetCollider.size = Vector3.one * 0.5f;
                CombatHurtbox hurtbox = target.AddComponent<CombatHurtbox>();
                hurtbox.Configure(targetCollider, health);

                Physics.SyncTransforms();
                Assert.IsTrue(dealer.BeginHitWindow(
                    MeleeHitboxLimb.RightFoot,
                    25f,
                    10));

                foot.transform.position = new Vector3(2f, 0f, 0f);
                int hitCount = dealer.EvaluateHitWindow();
                int repeatedHitCount = dealer.EvaluateHitWindow();

                Assert.AreEqual(1, hitCount);
                Assert.AreEqual(1, repeatedHitCount);
                Assert.AreEqual(75f, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void HenryRightFootHitboxUsesExpectedLocalVolume()
        {
            GameObject henry = new GameObject("HenryRigVolumeTest");
            try
            {
                GameObject foot = new GameObject("CC_Base_R_Foot_024");
                foot.transform.SetParent(henry.transform, false);

                MeleeHitboxRig rig = henry.AddComponent<MeleeHitboxRig>();
                Assert.IsTrue(rig.ConfigureForHenry());
                Assert.IsTrue(rig.TryGetHitbox(
                    MeleeHitboxLimb.RightFoot,
                    out BoxCollider volume));

                Assert.AreEqual(new Vector3(0f, 9f, 1f), volume.center);
                Assert.AreEqual(new Vector3(14f, 28f, 10f), volume.size);
                Assert.IsTrue(volume.isTrigger);
                Assert.IsFalse(volume.enabled);
            }
            finally
            {
                Object.DestroyImmediate(henry);
            }
        }

        [Test]
        public void HenryCombatClipsRemainLegacyAndAvailable()
        {
            AnimationClip mma =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryMmaClipPath);
            AnimationClip roundhouse =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryRoundhouseClipPath);

            Assert.NotNull(mma);
            Assert.NotNull(roundhouse);
            Assert.IsTrue(mma.legacy);
            Assert.IsTrue(roundhouse.legacy);
            Assert.AreEqual(
                HenryRunAnimationPlayer.MmaKickClipName,
                mma.name);
            Assert.AreEqual(
                HenryRunAnimationPlayer.RoundhouseKickClipName,
                roundhouse.name);
        }
    }
}
