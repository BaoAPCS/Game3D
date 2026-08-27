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
        private const string HenryPunchClipPath =
            "Assets/Chapter1/Resources/Henry/Punch.anim";
        private const string HenryDefeatedClipPath =
            "Assets/Chapter1/Resources/Henry/Defeated.anim";
        private const string HenryModelPath =
            "Assets/Chapter1/ExternalAssets/henry_animated_cartoon_character.glb";

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
                GameObject hand = new GameObject("CC_Base_R_Hand_086");
                hand.transform.SetParent(attacker.transform, false);
                GameObject foot = new GameObject("CC_Base_R_Foot_024");
                foot.transform.SetParent(attacker.transform, false);

                MeleeHitboxRig rig = attacker.AddComponent<MeleeHitboxRig>();
                Assert.IsTrue(rig.ConfigureForHenry());
                Assert.IsTrue(rig.TryGetHitbox(
                    MeleeHitboxLimb.RightHand,
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
                    MeleeHitboxLimb.RightHand,
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
                GameObject hand = new GameObject("CC_Base_R_Hand_086");
                hand.transform.SetParent(attacker.transform, false);
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
                GameObject hand = new GameObject("CC_Base_R_Hand_086");
                hand.transform.SetParent(henry.transform, false);
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
        public void HenryRightHandHitboxUsesExpectedLocalVolume()
        {
            GameObject henry = new GameObject("HenryHandRigVolumeTest");
            try
            {
                GameObject hand = new GameObject("CC_Base_R_Hand_086");
                hand.transform.SetParent(henry.transform, false);
                GameObject foot = new GameObject("CC_Base_R_Foot_024");
                foot.transform.SetParent(henry.transform, false);

                MeleeHitboxRig rig = henry.AddComponent<MeleeHitboxRig>();
                Assert.IsTrue(rig.ConfigureForHenry());
                Assert.IsTrue(rig.TryGetHitbox(
                    MeleeHitboxLimb.RightHand,
                    out BoxCollider volume));

                Assert.AreEqual(new Vector3(0f, 7f, 0f), volume.center);
                Assert.AreEqual(new Vector3(12f, 18f, 12f), volume.size);
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
            AnimationClip punch =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryPunchClipPath);
            AnimationClip defeated =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryDefeatedClipPath);

            Assert.NotNull(mma);
            Assert.NotNull(roundhouse);
            Assert.NotNull(punch);
            Assert.NotNull(defeated);
            Assert.IsTrue(mma.legacy);
            Assert.IsTrue(roundhouse.legacy);
            Assert.IsTrue(punch.legacy);
            Assert.IsTrue(defeated.legacy);
            Assert.AreEqual(
                HenryRunAnimationPlayer.MmaKickClipName,
                mma.name);
            Assert.AreEqual(
                HenryRunAnimationPlayer.RoundhouseKickClipName,
                roundhouse.name);
            Assert.AreEqual("Punch", punch.name);
            Assert.AreEqual(WrapMode.Once, punch.wrapMode);

            EditorCurveBinding[] punchBindings =
                AnimationUtility.GetCurveBindings(punch);
            Assert.GreaterOrEqual(
                punchBindings.Length,
                200,
                "Punch must be baked onto Henry's CC_Base skeleton.");
            bool hasAnimatedRightPunchArm = false;
            for (int i = 0; i < punchBindings.Length; i++)
            {
                EditorCurveBinding binding = punchBindings[i];
                if (!binding.propertyName.StartsWith("m_LocalRotation") ||
                    (!binding.path.Contains("CC_Base_R_Upperarm") &&
                     !binding.path.Contains("CC_Base_R_Forearm") &&
                     !binding.path.Contains("CC_Base_R_Hand")))
                {
                    continue;
                }

                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(punch, binding);
                if (curve == null || curve.length < 2)
                {
                    continue;
                }

                float minimum = float.PositiveInfinity;
                float maximum = float.NegativeInfinity;
                for (int keyIndex = 0; keyIndex < curve.length; keyIndex++)
                {
                    minimum = Mathf.Min(
                        minimum,
                        curve.keys[keyIndex].value);
                    maximum = Mathf.Max(
                        maximum,
                        curve.keys[keyIndex].value);
                }

                if (maximum - minimum > 0.001f)
                {
                    hasAnimatedRightPunchArm = true;
                    break;
                }
            }

            Assert.IsTrue(
                hasAnimatedRightPunchArm,
                "Punch contains bindings but no changing right-arm pose.");

            AnimationClipSettings punchSettings =
                AnimationUtility.GetAnimationClipSettings(punch);
            Assert.IsFalse(punchSettings.loopTime);
            Assert.AreEqual("Defeated", defeated.name);
            Assert.AreEqual(WrapMode.ClampForever, defeated.wrapMode);
            EditorCurveBinding[] defeatedBindings =
                AnimationUtility.GetCurveBindings(defeated);
            Assert.GreaterOrEqual(
                defeatedBindings.Length,
                200,
                "Defeated must be baked onto Henry's CC_Base skeleton, " +
                "not left as an empty placeholder or raw Mixamo clip.");

            bool hasAnimatedBodyRotation = false;
            bool hasHipVerticalMotion = false;
            for (int i = 0; i < defeatedBindings.Length; i++)
            {
                EditorCurveBinding binding = defeatedBindings[i];
                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(defeated, binding);
                if (curve == null || curve.length < 2)
                {
                    continue;
                }

                float minimum = float.PositiveInfinity;
                float maximum = float.NegativeInfinity;
                for (int keyIndex = 0; keyIndex < curve.length; keyIndex++)
                {
                    float value = curve.keys[keyIndex].value;
                    minimum = Mathf.Min(minimum, value);
                    maximum = Mathf.Max(maximum, value);
                }

                bool changesPose = maximum - minimum > 0.001f;
                if (changesPose &&
                    binding.propertyName.StartsWith("m_LocalRotation") &&
                    (binding.path.Contains("CC_Base_Spine") ||
                     binding.path.Contains("CC_Base_Thigh")))
                {
                    hasAnimatedBodyRotation = true;
                }

                if (changesPose &&
                    binding.propertyName == "m_LocalPosition.y" &&
                    binding.path.Contains("CC_Base_Hip"))
                {
                    hasHipVerticalMotion = true;
                }
            }

            Assert.IsTrue(
                hasAnimatedBodyRotation,
                "Defeated contains bindings but no changing spine/leg pose.");
            Assert.IsTrue(
                hasHipVerticalMotion,
                "Defeated must move Henry's hips vertically as he falls.");

            AnimationClipSettings defeatedSettings =
                AnimationUtility.GetAnimationClipSettings(defeated);
            Assert.IsFalse(defeatedSettings.loopTime);
        }

        [Test]
        public void HenryDefeatedClipAppliesAChangedFinalPoseToHenryModel()
        {
            GameObject modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(HenryModelPath);
            AnimationClip defeated =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryDefeatedClipPath);

            Assert.NotNull(modelAsset);
            Assert.NotNull(defeated);

            GameObject instance = Object.Instantiate(modelAsset);
            try
            {
                Animation targetAnimation =
                    instance.GetComponentInChildren<Animation>(true);
                Assert.NotNull(targetAnimation);

                Transform hips = FindTransformByPrefix(
                    targetAnimation.transform,
                    "CC_Base_Hip");
                Transform leftThigh = FindTransformByPrefix(
                    targetAnimation.transform,
                    "CC_Base_L_Thigh");
                Assert.NotNull(hips);
                Assert.NotNull(leftThigh);

                defeated.SampleAnimation(targetAnimation.gameObject, 0f);
                float startHipY = hips.localPosition.y;
                Quaternion startThighRotation = leftThigh.localRotation;

                defeated.SampleAnimation(
                    targetAnimation.gameObject,
                    defeated.length);
                float endHipY = hips.localPosition.y;
                Quaternion endThighRotation = leftThigh.localRotation;

                Assert.Greater(
                    Mathf.Abs(endHipY - startHipY),
                    0.001f,
                    "The baked clip does not lower/move Henry's hips.");
                Assert.Greater(
                    Quaternion.Angle(
                        startThighRotation,
                        endThighRotation),
                    1f,
                    "The baked clip does not change Henry's leg pose.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void HenryPunchStartsInGuardAndReachesFullExtension()
        {
            GameObject modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(HenryModelPath);
            AnimationClip punch =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryPunchClipPath);

            Assert.NotNull(modelAsset);
            Assert.NotNull(punch);

            GameObject instance = Object.Instantiate(modelAsset);
            try
            {
                Animation targetAnimation =
                    instance.GetComponentInChildren<Animation>(true);
                Assert.NotNull(targetAnimation);

                Transform hips = FindTransformByPrefix(
                    targetAnimation.transform,
                    "CC_Base_Hip");
                Transform rightForearm = FindTransformByPrefix(
                    targetAnimation.transform,
                    "CC_Base_R_Forearm_082");
                Transform rightHand = FindTransformByPrefix(
                    targetAnimation.transform,
                    "CC_Base_R_Hand_086");
                Assert.NotNull(hips);
                Assert.NotNull(rightForearm);
                Assert.NotNull(rightHand);

                Quaternion bindForearmRotation = rightForearm.localRotation;
                punch.SampleAnimation(targetAnimation.gameObject, 0f);
                Quaternion guardForearmRotation = rightForearm.localRotation;
                Vector3 guardHandPosition =
                    hips.InverseTransformPoint(rightHand.position);

                punch.SampleAnimation(
                    targetAnimation.gameObject,
                    punch.length * 0.3f);
                Vector3 strikeHandPosition =
                    hips.InverseTransformPoint(rightHand.position);

                Assert.Greater(
                    Quaternion.Angle(
                        bindForearmRotation,
                        guardForearmRotation),
                    45f,
                    "Punch starts from Henry's bind/idle arm instead of " +
                    "a fighting guard pose.");
                Assert.Greater(
                    Vector3.Distance(
                        guardHandPosition,
                        strikeHandPosition),
                    25f,
                    "Punch never extends Henry's right fist far enough " +
                    "from the guard pose.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void HenryPunchGuardKeepsBothHandsClearOfTorso()
        {
            GameObject modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(HenryModelPath);
            AnimationClip punch =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HenryPunchClipPath);

            Assert.NotNull(modelAsset);
            Assert.NotNull(punch);

            GameObject instance = Object.Instantiate(modelAsset);
            try
            {
                Animation targetAnimation =
                    instance.GetComponentInChildren<Animation>(true);
                Assert.NotNull(targetAnimation);

                Transform animationRoot = targetAnimation.transform;
                Transform chest = FindTransformByPrefix(
                    animationRoot,
                    "CC_Base_Spine02_038");
                Transform leftHand = FindTransformByPrefix(
                    animationRoot,
                    "CC_Base_L_Hand_058");
                Transform rightHand = FindTransformByPrefix(
                    animationRoot,
                    "CC_Base_R_Hand_086");
                Assert.NotNull(chest);
                Assert.NotNull(leftHand);
                Assert.NotNull(rightHand);

                float[] guardSamples =
                    { 0f, 0.1f, 0.2f, 0.25f, 0.4f, 0.55f, 0.75f, 1f };
                for (int i = 0; i < guardSamples.Length; i++)
                {
                    float normalizedTime = guardSamples[i];
                    punch.SampleAnimation(
                        targetAnimation.gameObject,
                        punch.length * normalizedTime);
                    Vector3 chestPosition =
                        animationRoot.InverseTransformPoint(chest.position);
                    Vector3 leftPosition =
                        animationRoot.InverseTransformPoint(leftHand.position);
                    Vector3 rightPosition =
                        animationRoot.InverseTransformPoint(rightHand.position);

                    Assert.LessOrEqual(
                        leftPosition.x,
                        chestPosition.x - 0.19f,
                        $"Left fist moved back into Henry's torso at " +
                        $"normalized time {normalizedTime:0.00}.");
                    Assert.GreaterOrEqual(
                        rightPosition.x,
                        chestPosition.x + 0.18f,
                        $"Right fist moved back into Henry's torso at " +
                        $"normalized time {normalizedTime:0.00}.");
                    Assert.GreaterOrEqual(
                        leftPosition.y,
                        chestPosition.y + 0.14f,
                        $"Left fist dropped into Henry's torso at " +
                        $"normalized time {normalizedTime:0.00}.");
                    Assert.GreaterOrEqual(
                        rightPosition.y,
                        chestPosition.y + 0.125f,
                        $"Right fist dropped into Henry's torso at " +
                        $"normalized time {normalizedTime:0.00}.");
                    Assert.GreaterOrEqual(
                        leftPosition.z,
                        chestPosition.z + 0.23f,
                        $"Left fist is not far enough in front of Henry's " +
                        $"torso at normalized time {normalizedTime:0.00}.");
                    Assert.GreaterOrEqual(
                        rightPosition.z,
                        chestPosition.z + 0.215f,
                        $"Right fist is not far enough in front of Henry's " +
                        $"torso at normalized time {normalizedTime:0.00}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform FindTransformByPrefix(
            Transform root,
            string prefix)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.StartsWith(prefix))
                {
                    return transforms[i];
                }
            }

            return null;
        }
    }
}
