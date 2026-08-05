using System;
using System.Collections.Generic;
using System.IO;
using DormitoryMystery.Chapter1;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class MeleeCombatEditorTool
    {
        private const string Chapter1InputActionsPath = "Assets/Chapter1/Settings/Chapter1Controls.inputactions";
        private const string ProjectInputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string InputReferencesFolderPath = "Assets/Chapter1/Settings/InputReferences";
        private const string AttackReferencePath = InputReferencesFolderPath + "/Attack.inputactionreference.asset";
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string PlayerPrototypeScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string AnimationFolderPath = "Assets/Chapter1/Animations";
        private const string CombatAnimationFolderPath = AnimationFolderPath + "/Combat";
        private const string AnimationControllerFolderPath = AnimationFolderPath + "/Controllers";
        private const string PlayerAnimatorControllerPath = AnimationControllerFolderPath + "/Chapter1PlayerAnimator.controller";
        private const string GameplayMapName = "Gameplay";
        private const string PlayerMapName = "Player";
        private const string AttackActionName = "Attack";
        private const string AttackMouseBinding = "<Mouse>/leftButton";
        private const string LocomotionStateName = "Locomotion";
        private const string AutoSetupSessionKey = "DormitoryMystery.Chapter1.MeleeCombatAnimationAutoSetup";

        private static readonly string[] AttackTriggers =
        {
            "PunchLeft",
            "PunchRight",
            "KickSide",
            "KickHeavy"
        };

        private static readonly AnimatorParameterSpec[] AnimatorParameters =
        {
            new AnimatorParameterSpec("MoveSpeed", AnimatorControllerParameterType.Float),
            new AnimatorParameterSpec("IsGrounded", AnimatorControllerParameterType.Bool),
            new AnimatorParameterSpec("IsSprinting", AnimatorControllerParameterType.Bool),
            new AnimatorParameterSpec("IsCrouching", AnimatorControllerParameterType.Bool),
            new AnimatorParameterSpec("IsAttacking", AnimatorControllerParameterType.Bool),
            new AnimatorParameterSpec("PunchLeft", AnimatorControllerParameterType.Trigger),
            new AnimatorParameterSpec("PunchRight", AnimatorControllerParameterType.Trigger),
            new AnimatorParameterSpec("KickSide", AnimatorControllerParameterType.Trigger),
            new AnimatorParameterSpec("KickHeavy", AnimatorControllerParameterType.Trigger)
        };

        private static readonly AttackAnimationSpec[] AttackAnimationSpecs =
        {
            new AttackAnimationSpec(
                "PunchLeft",
                CombatAnimationFolderPath + "/PunchLeft_Placeholder.anim",
                0.55f,
                0.18f,
                0.2f,
                0.48f,
                new[] { "punchleft", "leftpunch", "punch_l", "jab" },
                new Vector3(-0.04f, 0f, 0.12f),
                new Vector3(-4f, -14f, 5f),
                "Bip01 L UpperArm",
                new Vector3(28f, -18f, -36f),
                "Bip01 L Forearm",
                new Vector3(12f, 4f, -58f)),
            new AttackAnimationSpec(
                "PunchRight",
                CombatAnimationFolderPath + "/PunchRight_Placeholder.anim",
                0.58f,
                0.2f,
                0.22f,
                0.5f,
                new[] { "punchright", "rightpunch", "punch_r", "cross" },
                new Vector3(0.04f, 0f, 0.14f),
                new Vector3(-4f, 14f, -5f),
                "Bip01 R UpperArm",
                new Vector3(28f, 18f, 36f),
                "Bip01 R Forearm",
                new Vector3(12f, -4f, 58f)),
            new AttackAnimationSpec(
                "KickSide",
                CombatAnimationFolderPath + "/KickSide_Placeholder.anim",
                0.75f,
                0.28f,
                0.32f,
                0.65f,
                new[] { "kickside", "sidekick", "kick_l", "roundhouse" },
                new Vector3(0.08f, 0.03f, 0.08f),
                new Vector3(-3f, -24f, 11f),
                "Bip01 L Thigh",
                new Vector3(-46f, 18f, 12f),
                "Bip01 L Calf",
                new Vector3(54f, -6f, 0f)),
            new AttackAnimationSpec(
                "KickHeavy",
                CombatAnimationFolderPath + "/KickHeavy_Placeholder.anim",
                0.9f,
                0.34f,
                0.38f,
                0.78f,
                new[] { "kickheavy", "heavykick", "kick_r", "frontkick" },
                new Vector3(0f, 0.05f, 0.16f),
                new Vector3(-13f, 0f, 0f),
                "Bip01 R Thigh",
                new Vector3(-58f, -14f, -8f),
                "Bip01 R Calf",
                new Vector3(58f, 8f, 0f))
        };

        [InitializeOnLoadMethod]
        private static void ScheduleAutoMeleeCombatAnimationSetup()
        {
            // Downloaded combat animation integration is now explicit through the Tools/Player menu.
            // Keeping this method as a no-op prevents older placeholder setup from running on domain reload.
        }

        private static void AutoSetupMeleeCombatAnimationIfNeeded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += AutoSetupMeleeCombatAnimationIfNeeded;
                return;
            }

            if (IsAnimationSetupAlreadyApplied())
            {
                return;
            }

            SetupMeleeCombatInternal("Animation Auto Setup", true);
        }

        [MenuItem("Tools/Player/Setup Melee Combat")]
        public static void SetupMeleeCombat()
        {
            ExecuteDownloadedCombatAnimationTool("IntegrateDownloadedCombatAnimations");
        }

        [MenuItem("Tools/Player/Setup Melee Combat Animation")]
        public static void SetupMeleeCombatAnimation()
        {
            ExecuteDownloadedCombatAnimationTool("IntegrateDownloadedCombatAnimations");
        }

        private static void SetupMeleeCombatInternal(string reportTitle, bool includePrefab)
        {
            List<string> report = new List<string>();

            EnsureLayer("Enemy", report);
            EnsureProjectPlayerAttackAction(report);
            InputActionAsset inputActionAsset = EnsureAttackAction(
                Chapter1InputActionsPath,
                GameplayMapName,
                true,
                report);
            InputActionReference attackReference = EnsureAttackReference(inputActionAsset, report);

            GameObject player = FindOrOpenScenePlayer(report);
            if (player == null)
            {
                LogFail("Could not find a Player object in the active scene or Chapter1 prototype scene.");
                PrintReport(reportTitle, report);
                return;
            }

            CombatAnimationAssets animationAssets = EnsureCombatAnimationAssets(player.transform, report);

            PlayerCombatController combatController = EnsureSingleCombatController(player, report);
            if (combatController == null)
            {
                PrintReport(reportTitle, report);
                return;
            }

            combatController.EnsureDefaultComboIfEmpty();
            Transform attackPoint = EnsureAttackPoint(player, report);
            Animator animator = EnsurePlayerAnimator(player, animationAssets.Controller, report, true, "Scene Player");

            AssignRuntimeReferences(player, combatController, attackPoint, animator, report);
            AssignAttackInputReference(player.GetComponent<Chapter1InputReader>(), attackReference, report);
            ConfigureAnimator(animator, animationAssets, report);

            if (includePrefab)
            {
                ConfigurePlayerPrefab(animationAssets, attackReference, report);
            }

            EditorUtility.SetDirty(combatController);
            if (player.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(player.scene);
            }

            AssetDatabase.SaveAssets();
            PrintReport(reportTitle, report);
        }

        [MenuItem("Tools/Player/Validate Melee Combat")]
        public static void ValidateMeleeCombat()
        {
            ExecuteDownloadedCombatAnimationTool("ValidateDownloadedCombatAnimations");
        }

        [MenuItem("Tools/Player/Validate Melee Combat Animation")]
        public static void ValidateMeleeCombatAnimation()
        {
            ExecuteDownloadedCombatAnimationTool("ValidateDownloadedCombatAnimations");
        }

        private static void ValidateMeleeCombatInternal(string reportTitle)
        {
            List<string> report = new List<string>();
            GameObject player = FindScenePlayer();
            if (player == null)
            {
                LogFail("Player object was not found in the active scene.");
                return;
            }

            ValidateCombatController(player, report);
            ValidateAttackInput(report);
            ValidateEnemyLayer(report);
            ValidateAnimatorAsset(report);
            ValidatePrefabAnimationSetup(report);
            PrintReport(reportTitle, report);
        }

        private static PlayerCombatController EnsureSingleCombatController(GameObject player, List<string> report, bool useUndo = true)
        {
            PlayerCombatController[] combatControllers = player.GetComponents<PlayerCombatController>();
            if (combatControllers.Length > 1)
            {
                report.Add($"[FAIL] Player has {combatControllers.Length} PlayerCombatController components. Remove duplicates manually.");
                return combatControllers[0];
            }

            if (combatControllers.Length == 1)
            {
                report.Add("[PASS] Player already has PlayerCombatController.");
                return combatControllers[0];
            }

            PlayerCombatController combatController = useUndo
                ? Undo.AddComponent<PlayerCombatController>(player)
                : player.AddComponent<PlayerCombatController>();
            report.Add("[PASS] Added PlayerCombatController to Player.");
            return combatController;
        }

        private static void AssignRuntimeReferences(GameObject player, PlayerCombatController combatController, Transform attackPoint, Animator animator, List<string> report)
        {
            SerializedObject serializedCombat = new SerializedObject(combatController);
            SetObjectReference(serializedCombat, "inputReader", player.GetComponent<Chapter1InputReader>());
            SetObjectReference(serializedCombat, "playerMotor", player.GetComponent<Chapter1PlayerMotor>());
            SetObjectReference(serializedCombat, "inputLock", player.GetComponent<PlayerInputLock>());
            SetObjectReference(serializedCombat, "playerVisualController", player.GetComponentInChildren<PlayerVisualController>(true));
            SetObjectReference(serializedCombat, "legacyAnimationToPause", player.GetComponentInChildren<Animation>(true));
            SetObjectReference(serializedCombat, "attackPoint", attackPoint);
            SetObjectReference(serializedCombat, "animator", animator);
            SetObjectReference(serializedCombat, "proceduralAnimationRoot", FindChildRecursive(player.transform, "ModelAnchor") ?? FindChildRecursive(player.transform, "Visual"));

            SerializedProperty enemyLayerMask = serializedCombat.FindProperty("enemyLayerMask");
            int enemyMask = LayerMask.GetMask("Enemy");
            if (enemyLayerMask != null)
            {
                enemyLayerMask.intValue = enemyMask;
            }

            serializedCombat.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(combatController))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(combatController);
            }

            report.Add(enemyMask != 0
                ? "[PASS] Assigned Enemy LayerMask to PlayerCombatController."
                : "[FAIL] Could not assign Enemy LayerMask because layer Enemy is missing.");

            if (animator != null)
            {
                report.Add($"[PASS] Assigned Animator '{animator.name}' to PlayerCombatController.");
            }
            else
            {
                report.Add("[WARNING] Player has no Animator. Combat fallback timing and damage work, but animation triggers need an AnimatorController or animation clips.");
            }
        }

        private static void AssignAttackInputReference(Chapter1InputReader inputReader, InputActionReference attackReference, List<string> report)
        {
            if (inputReader == null)
            {
                report.Add("[FAIL] Player is missing Chapter1InputReader.");
                return;
            }

            if (attackReference == null)
            {
                report.Add("[FAIL] Attack InputActionReference could not be assigned because the asset is missing.");
                return;
            }

            SerializedObject serializedReader = new SerializedObject(inputReader);
            SetObjectReference(serializedReader, "attackActionReference", attackReference);
            serializedReader.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(inputReader))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(inputReader);
            }

            EditorUtility.SetDirty(inputReader);
            report.Add("[PASS] Assigned Attack InputActionReference to Chapter1InputReader.");
        }

        private static Transform EnsureAttackPoint(GameObject player, List<string> report, bool useUndo = true)
        {
            Transform existing = FindChildRecursive(player.transform, "AttackPoint");
            if (existing != null)
            {
                report.Add("[PASS] AttackPoint already exists.");
                return existing;
            }

            GameObject attackPointObject = new GameObject("AttackPoint");
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(attackPointObject, "Create AttackPoint");
            }

            attackPointObject.transform.SetParent(player.transform);
            attackPointObject.transform.localPosition = new Vector3(0f, 1f, 0.85f);
            attackPointObject.transform.localRotation = Quaternion.identity;
            attackPointObject.transform.localScale = Vector3.one;

            report.Add("[PASS] Created AttackPoint as a child of Player root at local position (0, 1, 0.85).");
            return attackPointObject.transform;
        }

        private static Animator EnsurePlayerAnimator(GameObject player, AnimatorController animatorController, List<string> report, bool useUndo, string label)
        {
            Animator animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = useUndo ? Undo.AddComponent<Animator>(player) : player.AddComponent<Animator>();
                report.Add($"[PASS] Added Animator to {label} root.");
            }
            else
            {
                report.Add($"[PASS] {label} root already has Animator.");
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = animatorController;
            EditorUtility.SetDirty(animator);

            report.Add(animatorController != null
                ? $"[PASS] Assigned {PlayerAnimatorControllerPath} to {label} Animator and disabled Apply Root Motion."
                : $"[FAIL] Could not assign AnimatorController to {label} Animator.");

            return animator;
        }

        private static CombatAnimationAssets EnsureCombatAnimationAssets(Transform authoringRoot, List<string> report)
        {
            EnsureFolder("Assets/Chapter1", "Animations");
            EnsureFolder(AnimationFolderPath, "Combat");
            EnsureFolder(AnimationFolderPath, "Controllers");

            CombatAnimationAssets assets = new CombatAnimationAssets();
            for (int i = 0; i < AttackAnimationSpecs.Length; i++)
            {
                AttackAnimationSpec spec = AttackAnimationSpecs[i];
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(spec.PlaceholderClipPath);
                if (clip == null)
                {
                    clip = FindExistingCompatibleAttackClip(spec, report);
                }

                if (clip == null || string.Equals(AssetDatabase.GetAssetPath(clip), spec.PlaceholderClipPath, StringComparison.Ordinal))
                {
                    clip = CreateOrUpdatePlaceholderClip(spec, authoringRoot, report);
                }

                assets.SetClip(spec.StateName, clip);
            }

            assets.Controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorControllerPath);
            if (assets.Controller == null)
            {
                assets.Controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorControllerPath);
                report.Add($"[PASS] Created {PlayerAnimatorControllerPath}.");
            }
            else
            {
                report.Add($"[PASS] AnimatorController already exists at {PlayerAnimatorControllerPath}.");
            }

            return assets;
        }

        private static AnimationClip FindExistingCompatibleAttackClip(AttackAnimationSpec spec, List<string> report)
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
            bool foundLegacyOnly = false;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || path.StartsWith(CombatAnimationFolderPath, StringComparison.Ordinal))
                {
                    continue;
                }

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    AnimationClip clip = assets[j] as AnimationClip;
                    if (clip == null || !NameMatchesAttackSpec(clip.name + " " + path, spec))
                    {
                        continue;
                    }

                    if (clip.legacy)
                    {
                        foundLegacyOnly = true;
                        continue;
                    }

                    report.Add($"[PASS] Reusing compatible attack clip '{clip.name}' from {path} for {spec.StateName}.");
                    return clip;
                }
            }

            report.Add(foundLegacyOnly
                ? $"[WARNING] Found only Legacy clip candidates for {spec.StateName}; creating Mecanim placeholder instead."
                : $"[PASS] No existing compatible clip found for {spec.StateName}; creating placeholder.");
            return null;
        }

        private static bool NameMatchesAttackSpec(string value, AttackAnimationSpec spec)
        {
            string normalized = NormalizeName(value);
            for (int i = 0; i < spec.Keywords.Length; i++)
            {
                if (normalized.Contains(spec.Keywords[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int index = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if (char.IsLetterOrDigit(c))
                {
                    buffer[index] = c;
                    index++;
                }
            }

            return new string(buffer, 0, index);
        }

        private static AnimationClip CreateOrUpdatePlaceholderClip(AttackAnimationSpec spec, Transform authoringRoot, List<string> report)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(spec.PlaceholderClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = Path.GetFileNameWithoutExtension(spec.PlaceholderClipPath),
                    frameRate = 60f,
                    wrapMode = WrapMode.Once,
                    legacy = false
                };
                AssetDatabase.CreateAsset(clip, spec.PlaceholderClipPath);
                report.Add($"[PASS] Created {spec.PlaceholderClipPath}.");
            }
            else
            {
                report.Add($"[PASS] Repaired placeholder clip {spec.PlaceholderClipPath}.");
            }

            RemoveAllCurves(clip);
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Once;
            clip.legacy = false;

            Transform anchor = authoringRoot != null ? FindChildRecursive(authoringRoot, "ModelAnchor") : null;
            if (anchor != null)
            {
                string anchorPath = GetRelativePath(authoringRoot, anchor);
                SetPositionCurves(clip, anchorPath, anchor.localPosition, spec.AnchorOffset, spec.Duration);
                SetEulerCurves(clip, anchorPath, anchor.localEulerAngles, spec.AnchorEulerOffset, spec.Duration, spec.HitTime);
            }
            else
            {
                SetPositionCurves(clip, "Visual/ModelAnchor", Vector3.zero, spec.AnchorOffset, spec.Duration);
                SetEulerCurves(clip, "Visual/ModelAnchor", Vector3.zero, spec.AnchorEulerOffset, spec.Duration, spec.HitTime);
                report.Add($"[WARNING] Could not find ModelAnchor while authoring {spec.StateName}; used default Visual/ModelAnchor binding.");
            }

            AddBoneEulerCurves(clip, authoringRoot, "Bip01 Spine2", spec.AnchorEulerOffset * 0.45f, spec.Duration, spec.HitTime, report);
            AddBoneEulerCurves(clip, authoringRoot, spec.PrimaryBoneName, spec.PrimaryBoneEulerOffset, spec.Duration, spec.HitTime, report);
            AddBoneEulerCurves(clip, authoringRoot, spec.SecondaryBoneName, spec.SecondaryBoneEulerOffset, spec.Duration, spec.HitTime, report);
            SetAnimationEvents(clip, spec);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            settings.loopBlendOrientation = false;
            settings.loopBlendPositionY = false;
            settings.loopBlendPositionXZ = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void RemoveAllCurves(AnimationClip clip)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < curveBindings.Length; i++)
            {
                AnimationUtility.SetEditorCurve(clip, curveBindings[i], null);
            }

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int i = 0; i < objectBindings.Length; i++)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, objectBindings[i], null);
            }
        }

        private static void AddBoneEulerCurves(
            AnimationClip clip,
            Transform authoringRoot,
            string boneName,
            Vector3 eulerOffset,
            float duration,
            float hitTime,
            List<string> report)
        {
            Transform bone = authoringRoot != null ? FindChildRecursive(authoringRoot, boneName) : null;
            if (bone == null)
            {
                report.Add($"[WARNING] Could not bind placeholder curve for missing bone {boneName}.");
                return;
            }

            SetEulerCurves(clip, GetRelativePath(authoringRoot, bone), bone.localEulerAngles, eulerOffset, duration, hitTime);
        }

        private static void SetAnimationEvents(AnimationClip clip, AttackAnimationSpec spec)
        {
            AnimationEvent[] events =
            {
                CreateAnimationEvent("OpenComboWindow", Mathf.Clamp(spec.ComboWindowStart, 0f, spec.Duration)),
                CreateAnimationEvent("PerformAttackHit", Mathf.Clamp(spec.HitTime, 0f, spec.Duration)),
                CreateAnimationEvent("CloseComboWindow", Mathf.Clamp(spec.ComboWindowEnd, 0f, spec.Duration)),
                CreateAnimationEvent("EndAttack", Mathf.Max(0f, spec.Duration - 0.02f))
            };
            AnimationUtility.SetAnimationEvents(clip, events);
        }

        private static AnimationEvent CreateAnimationEvent(string functionName, float time)
        {
            return new AnimationEvent
            {
                functionName = functionName,
                time = time,
                messageOptions = SendMessageOptions.DontRequireReceiver
            };
        }

        private static void SetPositionCurves(AnimationClip clip, string path, Vector3 basePosition, Vector3 offset, float duration)
        {
            float recoilTime = Mathf.Max(0.01f, duration * 0.72f);
            SetFloatCurve(clip, path, "m_LocalPosition.x", basePosition.x, basePosition.x + offset.x, basePosition.x - offset.x * 0.25f, basePosition.x, duration, recoilTime);
            SetFloatCurve(clip, path, "m_LocalPosition.y", basePosition.y, basePosition.y + offset.y, basePosition.y - offset.y * 0.25f, basePosition.y, duration, recoilTime);
            SetFloatCurve(clip, path, "m_LocalPosition.z", basePosition.z, basePosition.z + offset.z, basePosition.z - offset.z * 0.25f, basePosition.z, duration, recoilTime);
        }

        private static void SetEulerCurves(AnimationClip clip, string path, Vector3 baseEuler, Vector3 offset, float duration, float hitTime)
        {
            float strikeTime = Mathf.Clamp(hitTime, 0.01f, Mathf.Max(0.01f, duration - 0.04f));
            float recoilTime = Mathf.Clamp(duration * 0.72f, strikeTime + 0.01f, duration);
            Vector3 strikeEuler = baseEuler + offset;
            Vector3 recoilEuler = baseEuler - offset * 0.22f;

            SetFloatCurve(clip, path, "localEulerAnglesRaw.x", baseEuler.x, strikeEuler.x, recoilEuler.x, baseEuler.x, duration, recoilTime, strikeTime);
            SetFloatCurve(clip, path, "localEulerAnglesRaw.y", baseEuler.y, strikeEuler.y, recoilEuler.y, baseEuler.y, duration, recoilTime, strikeTime);
            SetFloatCurve(clip, path, "localEulerAnglesRaw.z", baseEuler.z, strikeEuler.z, recoilEuler.z, baseEuler.z, duration, recoilTime, strikeTime);
        }

        private static void SetFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            float startValue,
            float strikeValue,
            float recoilValue,
            float endValue,
            float duration,
            float recoilTime,
            float strikeTime = 0.18f)
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, startValue),
                new Keyframe(Mathf.Clamp(strikeTime, 0.01f, Mathf.Max(0.01f, duration - 0.04f)), strikeValue),
                new Keyframe(Mathf.Clamp(recoilTime, 0.02f, duration), recoilValue),
                new Keyframe(duration, endValue));

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName), curve);
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null || root == target)
            {
                return string.Empty;
            }

            List<string> segments = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static void ConfigurePlayerPrefab(CombatAnimationAssets animationAssets, InputActionReference attackReference, List<string> report)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                report.Add($"[FAIL] Player prefab was not found at {PlayerPrefabPath}.");
                return;
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerCombatController combatController = EnsureSingleCombatController(prefabContents, report, false);
                if (combatController == null)
                {
                    return;
                }

                combatController.EnsureDefaultComboIfEmpty();
                Transform attackPoint = EnsureAttackPoint(prefabContents, report, false);
                Animator animator = EnsurePlayerAnimator(prefabContents, animationAssets.Controller, report, false, "Player prefab");
                AssignRuntimeReferences(prefabContents, combatController, attackPoint, animator, report);
                AssignAttackInputReference(prefabContents.GetComponent<Chapter1InputReader>(), attackReference, report);
                ConfigureAnimator(animator, animationAssets, report);

                EditorUtility.SetDirty(prefabContents);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath);
                report.Add($"[PASS] Saved animation combat setup into {PlayerPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static void ConfigureAnimator(Animator animator, CombatAnimationAssets animationAssets, List<string> report)
        {
            if (animator == null)
            {
                report.Add("[WARNING] Animator parameters/states were not configured because Player has no Animator.");
                return;
            }

            AnimatorController animatorController = animationAssets.Controller != null
                ? animationAssets.Controller
                : animator.runtimeAnimatorController as AnimatorController;
            if (animatorController == null)
            {
                report.Add("[FAIL] Animator has no editable AnimatorController.");
                return;
            }

            Undo.RecordObject(animatorController, "Setup Melee Combat Animator");
            bool changed = false;
            foreach (AnimatorParameterSpec parameterSpec in AnimatorParameters)
            {
                if (!AnimatorControllerHasParameter(animatorController, parameterSpec.Name, parameterSpec.Type))
                {
                    animatorController.AddParameter(parameterSpec.Name, parameterSpec.Type);
                    changed = true;
                    report.Add($"[PASS] Added Animator parameter {parameterSpec.Name} ({parameterSpec.Type}).");
                }
                else
                {
                    report.Add($"[PASS] Animator parameter {parameterSpec.Name} already exists.");
                }
            }

            changed |= EnsureCombatStates(animatorController, animationAssets, report);

            if (changed)
            {
                EditorUtility.SetDirty(animatorController);
                AssetDatabase.SaveAssets();
            }
        }

        private static bool EnsureCombatStates(AnimatorController animatorController, CombatAnimationAssets animationAssets, List<string> report)
        {
            if (animatorController.layers == null || animatorController.layers.Length == 0)
            {
                report.Add("[FAIL] AnimatorController has no layers.");
                return false;
            }

            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            if (stateMachine == null)
            {
                report.Add("[FAIL] AnimatorController base layer has no state machine.");
                return false;
            }

            AnimatorState locomotionState = EnsureLocomotionState(stateMachine, report);
            bool changed = false;
            foreach (AttackAnimationSpec spec in AttackAnimationSpecs)
            {
                AnimatorState attackState = FindState(stateMachine, spec.StateName);
                if (attackState == null)
                {
                    attackState = stateMachine.AddState(spec.StateName);
                    attackState.writeDefaultValues = true;
                    changed = true;
                    report.Add($"[PASS] Created Animator state {spec.StateName}.");
                }
                else
                {
                    report.Add($"[PASS] Animator state {spec.StateName} already exists.");
                }

                AnimationClip attackClip = animationAssets.GetClip(spec.StateName);
                if (attackClip != null && attackState.motion != attackClip)
                {
                    attackState.motion = attackClip;
                    changed = true;
                    report.Add($"[PASS] Assigned Motion {attackClip.name} to state {spec.StateName}.");
                }
                else if (attackState.motion != null)
                {
                    report.Add($"[PASS] State {spec.StateName} already has Motion {attackState.motion.name}.");
                }
                else
                {
                    report.Add($"[FAIL] State {spec.StateName} has Motion = None.");
                }

                attackState.speed = 1f;
                attackState.writeDefaultValues = true;
                changed |= EnsureAnyStateTriggerTransition(stateMachine, attackState, spec.StateName);
                if (locomotionState != null && locomotionState != attackState)
                {
                    changed |= EnsureReturnTransition(attackState, locomotionState);
                }
                else
                {
                    report.Add($"[FAIL] State {spec.StateName} has no safe locomotion default state to return to.");
                }
            }

            return changed;
        }

        private static AnimatorState EnsureLocomotionState(AnimatorStateMachine stateMachine, List<string> report)
        {
            AnimatorState locomotionState = stateMachine.defaultState;
            if (locomotionState == null || Array.Exists(AttackTriggers, trigger => string.Equals(locomotionState.name, trigger, StringComparison.Ordinal)))
            {
                locomotionState = FindState(stateMachine, LocomotionStateName);
                if (locomotionState == null)
                {
                    locomotionState = stateMachine.AddState(LocomotionStateName, new Vector3(240f, 40f, 0f));
                    locomotionState.writeDefaultValues = false;
                    report.Add("[PASS] Created default Locomotion Animator state.");
                }

                stateMachine.defaultState = locomotionState;
            }

            report.Add($"[PASS] Animator returns attacks to {locomotionState.name}.");
            return locomotionState;
        }

        private static bool EnsureAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destinationState, string trigger)
        {
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            {
                if (transition.destinationState == destinationState && HasCondition(transition, trigger))
                {
                    transition.hasExitTime = false;
                    transition.duration = 0.03f;
                    transition.canTransitionToSelf = false;
                    return false;
                }
            }

            AnimatorStateTransition newTransition = stateMachine.AddAnyStateTransition(destinationState);
            newTransition.hasExitTime = false;
            newTransition.hasFixedDuration = true;
            newTransition.duration = 0.03f;
            newTransition.canTransitionToSelf = false;
            newTransition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            return true;
        }

        private static bool EnsureReturnTransition(AnimatorState attackState, AnimatorState locomotionState)
        {
            foreach (AnimatorStateTransition transition in attackState.transitions)
            {
                if (transition.destinationState == locomotionState)
                {
                    transition.hasExitTime = true;
                    transition.exitTime = 0.98f;
                    transition.duration = 0.03f;
                    return false;
                }
            }

            AnimatorStateTransition returnTransition = attackState.AddTransition(locomotionState);
            returnTransition.hasExitTime = true;
            returnTransition.exitTime = 0.98f;
            returnTransition.hasFixedDuration = true;
            returnTransition.duration = 0.03f;
            return true;
        }

        private static bool HasCondition(AnimatorStateTransition transition, string trigger)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].mode == AnimatorConditionMode.If
                    && string.Equals(conditions[i].parameter, trigger, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] childStates = stateMachine.states;
            for (int i = 0; i < childStates.Length; i++)
            {
                AnimatorState state = childStates[i].state;
                if (state != null && string.Equals(state.name, stateName, StringComparison.Ordinal))
                {
                    return state;
                }
            }

            return null;
        }

        private static bool AnimatorControllerHasParameter(AnimatorController animatorController, string parameterName, AnimatorControllerParameterType parameterType)
        {
            AnimatorControllerParameter[] parameters = animatorController.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == parameterType
                    && string.Equals(parameter.name, parameterName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static InputActionAsset EnsureProjectPlayerAttackAction(List<string> report)
        {
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectInputActionsPath) == null)
            {
                report.Add("[WARNING] Project-wide InputSystem_Actions asset was not found; Chapter1 Gameplay/Attack will be used by the active player.");
                return null;
            }

            return EnsureAttackAction(ProjectInputActionsPath, PlayerMapName, false, report);
        }

        private static InputActionAsset EnsureAttackAction(string assetPath, string mapName, bool required, List<string> report)
        {
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (inputActionAsset == null)
            {
                string level = required ? "[FAIL]" : "[WARNING]";
                report.Add($"{level} Missing Input Action Asset: {assetPath}.");
                return null;
            }

            bool changed = false;
            InputActionMap actionMap = inputActionAsset.FindActionMap(mapName, false);
            if (actionMap == null)
            {
                actionMap = inputActionAsset.AddActionMap(mapName);
                changed = true;
            }

            InputAction attackAction = actionMap.FindAction(AttackActionName, false);
            if (attackAction == null)
            {
                attackAction = actionMap.AddAction(AttackActionName, InputActionType.Button, expectedControlLayout: "Button");
                changed = true;
            }

            if (!HasBinding(attackAction, AttackMouseBinding))
            {
                attackAction.AddBinding(AttackMouseBinding);
                changed = true;
            }

            if (changed)
            {
                string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
                File.WriteAllText(physicalPath, inputActionAsset.ToJson());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                DisableGeneratedInputWrapper(assetPath);
                inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                report.Add($"[PASS] Updated {assetPath}: ensured {mapName}/Attack bound to {AttackMouseBinding}.");
            }
            else
            {
                report.Add($"[PASS] {assetPath} already has {mapName}/Attack bound to {AttackMouseBinding}.");
            }

            return inputActionAsset;
        }

        private static bool HasBinding(InputAction action, string path)
        {
            if (action == null)
            {
                return false;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (string.Equals(action.bindings[i].effectivePath, path, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(action.bindings[i].path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static InputActionReference EnsureAttackReference(InputActionAsset inputActionAsset, List<string> report)
        {
            if (inputActionAsset == null)
            {
                return null;
            }

            InputAction attackAction = inputActionAsset.FindAction($"{GameplayMapName}/{AttackActionName}", false);
            if (attackAction == null)
            {
                report.Add("[FAIL] Could not find Gameplay/Attack after updating Chapter1Controls.");
                return null;
            }

            EnsureFolder("Assets/Chapter1/Settings", "InputReferences");
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(AttackReferencePath);
            if (reference == null)
            {
                reference = InputActionReference.Create(attackAction);
                reference.name = "Attack.inputactionreference";
                AssetDatabase.CreateAsset(reference, AttackReferencePath);
                AssetDatabase.SaveAssets();
                report.Add($"[PASS] Created {AttackReferencePath}.");
                return reference;
            }

            if (reference.action == null || reference.action.id != attackAction.id)
            {
                try
                {
                    reference.Set(attackAction);
                    EditorUtility.SetDirty(reference);
                    AssetDatabase.SaveAssets();
                    report.Add($"[PASS] Repaired {AttackReferencePath}.");
                }
                catch (InvalidOperationException)
                {
                    AssetDatabase.DeleteAsset(AttackReferencePath);
                    reference = InputActionReference.Create(attackAction);
                    reference.name = "Attack.inputactionreference";
                    AssetDatabase.CreateAsset(reference, AttackReferencePath);
                    AssetDatabase.SaveAssets();
                    report.Add($"[PASS] Recreated {AttackReferencePath}.");
                }
            }
            else
            {
                report.Add("[PASS] Attack InputActionReference already points to Gameplay/Attack.");
            }

            return reference;
        }

        private static void ValidateCombatController(GameObject player, List<string> report)
        {
            PlayerCombatController[] controllers = player.GetComponents<PlayerCombatController>();
            if (controllers.Length == 1)
            {
                report.Add("[PASS] Player has exactly one PlayerCombatController.");
            }
            else
            {
                report.Add($"[FAIL] Player has {controllers.Length} PlayerCombatController components; expected exactly one.");
                if (controllers.Length == 0)
                {
                    return;
                }
            }

            PlayerCombatController controller = controllers[0];
            ValidateAssignedReference(controller.AttackPoint, "AttackPoint", report);
            ValidateAnimator(player, controller, report);
            ValidateEnemyLayerMask(controller.EnemyLayerMask, report);
            ValidateCombo(controller, report);
        }

        private static void ValidateAssignedReference(Object reference, string label, List<string> report)
        {
            report.Add(reference != null
                ? $"[PASS] {label} is assigned."
                : $"[FAIL] {label} is not assigned.");
        }

        private static void ValidateAnimator(GameObject player, PlayerCombatController combatController, List<string> report)
        {
            Animator animator = combatController.CombatAnimator;
            if (animator == null)
            {
                report.Add("[FAIL] PlayerCombatController Animator is not assigned.");
                return;
            }

            report.Add(animator.enabled
                ? "[PASS] Animator is assigned and enabled."
                : "[FAIL] Animator is assigned but disabled.");

            report.Add(!animator.applyRootMotion
                ? "[PASS] Apply Root Motion is disabled."
                : "[FAIL] Apply Root Motion is enabled.");

            Animator discoveredAnimator = player.GetComponent<Animator>() ?? player.GetComponentInChildren<Animator>(true);
            report.Add(discoveredAnimator == animator
                ? "[PASS] PlayerCombatController references the Player Animator."
                : "[FAIL] PlayerCombatController does not reference the Player Animator.");

            AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;
            ValidateAnimatorController(animatorController, report);
            ValidateLegacyConflict(player, combatController, animator, report);

            report.Add(!Application.isPlaying || !combatController.IsAttacking
                ? "[PASS] Combo is not stuck at validation time."
                : "[WARNING] Combo is currently attacking in Play Mode; wait for EndAttack or run setup again if IsAttacking stays true.");
        }

        private static void ValidateAnimatorController(AnimatorController animatorController, List<string> report)
        {
            if (animatorController == null)
            {
                report.Add("[FAIL] Animator Controller is null or not editable.");
                return;
            }

            report.Add("[PASS] Animator Controller is assigned.");
            foreach (AnimatorParameterSpec parameterSpec in AnimatorParameters)
            {
                report.Add(AnimatorControllerHasParameter(animatorController, parameterSpec.Name, parameterSpec.Type)
                    ? $"[PASS] Animator parameter {parameterSpec.Name} exists."
                    : $"[FAIL] Animator parameter {parameterSpec.Name} is missing or has wrong type.");
            }

            if (animatorController.layers == null || animatorController.layers.Length == 0 || animatorController.layers[0].stateMachine == null)
            {
                report.Add("[FAIL] Animator Controller has no valid base state machine.");
                return;
            }

            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            AnimatorState locomotionState = stateMachine.defaultState;
            for (int i = 0; i < AttackAnimationSpecs.Length; i++)
            {
                AttackAnimationSpec spec = AttackAnimationSpecs[i];
                AnimatorState state = FindState(stateMachine, spec.StateName);
                if (state == null)
                {
                    report.Add($"[FAIL] Animator state {spec.StateName} is missing.");
                    continue;
                }

                report.Add($"[PASS] Animator state {spec.StateName} exists.");
                if (state.motion == null)
                {
                    report.Add($"[FAIL] Animator state {spec.StateName} has Motion = None.");
                }
                else
                {
                    report.Add($"[PASS] Animator state {spec.StateName} has Motion {state.motion.name}.");
                    ValidateAttackMotion(state.motion, spec, report);
                }

                report.Add(HasAnyStateTriggerTransition(stateMachine, state, spec.StateName)
                    ? $"[PASS] Any State transition for {spec.StateName} is responsive."
                    : $"[FAIL] Any State transition for {spec.StateName} is missing.");

                report.Add(locomotionState != null && HasReturnTransition(state, locomotionState)
                    ? $"[PASS] {spec.StateName} returns to locomotion."
                    : $"[FAIL] {spec.StateName} has no return transition to locomotion.");
            }
        }

        private static void ValidateAttackMotion(Motion motion, AttackAnimationSpec spec, List<string> report)
        {
            AnimationClip clip = motion as AnimationClip;
            if (clip == null)
            {
                report.Add($"[WARNING] Motion for {spec.StateName} is not an AnimationClip; loop and event checks were skipped.");
                return;
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            report.Add(!settings.loopTime && !clip.isLooping
                ? $"[PASS] Clip {clip.name} is not looping."
                : $"[FAIL] Clip {clip.name} is looping.");

            ValidateClipEvents(clip, report);
        }

        private static void ValidateClipEvents(AnimationClip clip, List<string> report)
        {
            string[] requiredEvents =
            {
                "OpenComboWindow",
                "PerformAttackHit",
                "CloseComboWindow",
                "EndAttack"
            };

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            for (int i = 0; i < requiredEvents.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < events.Length; j++)
                {
                    if (string.Equals(events[j].functionName, requiredEvents[i], StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                report.Add(found
                    ? $"[PASS] Clip {clip.name} has Animation Event {requiredEvents[i]}."
                    : $"[FAIL] Clip {clip.name} is missing Animation Event {requiredEvents[i]}.");
            }
        }

        private static bool HasAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState state, string trigger)
        {
            AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState == state
                    && !transition.hasExitTime
                    && transition.duration <= 0.05f
                    && HasCondition(transition, trigger))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasReturnTransition(AnimatorState attackState, AnimatorState locomotionState)
        {
            AnimatorStateTransition[] transitions = attackState.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState == locomotionState && transition.hasExitTime)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateLegacyConflict(GameObject player, PlayerCombatController combatController, Animator animator, List<string> report)
        {
            Animation legacyAnimation = player.GetComponentInChildren<Animation>(true);
            if (legacyAnimation == null)
            {
                report.Add("[PASS] No Legacy Animation component was found on Player.");
                return;
            }

            if (animator != null && animator.gameObject == legacyAnimation.gameObject)
            {
                report.Add("[FAIL] Animator and Legacy Animation are on the same GameObject and will conflict.");
                return;
            }

            bool relayAssigned = combatController.LegacyAnimationToPause == legacyAnimation
                && combatController.SuspendsLegacyAnimationDuringAttack;
            report.Add(relayAssigned
                ? "[PASS] Legacy Animation is kept for locomotion and suspended during combat."
                : "[FAIL] Legacy Animation exists but PlayerCombatController is not configured to suspend it during combat.");
        }

        private static void ValidateAnimatorAsset(List<string> report)
        {
            AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorControllerPath);
            if (animatorController == null)
            {
                report.Add($"[FAIL] Missing AnimatorController asset: {PlayerAnimatorControllerPath}.");
                return;
            }

            report.Add($"[PASS] AnimatorController asset exists: {PlayerAnimatorControllerPath}.");
            ValidateAnimatorController(animatorController, report);
        }

        private static void ValidatePrefabAnimationSetup(List<string> report)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                report.Add($"[FAIL] Player prefab was not found at {PlayerPrefabPath}.");
                return;
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerCombatController[] controllers = prefabContents.GetComponents<PlayerCombatController>();
                if (controllers.Length != 1)
                {
                    report.Add($"[FAIL] Player prefab has {controllers.Length} PlayerCombatController components; expected exactly one.");
                    return;
                }

                Animator animator = prefabContents.GetComponent<Animator>();
                report.Add(animator != null
                    ? "[PASS] Player prefab root has Animator."
                    : "[FAIL] Player prefab root is missing Animator.");

                if (animator != null)
                {
                    report.Add(!animator.applyRootMotion
                        ? "[PASS] Player prefab Animator has Apply Root Motion disabled."
                        : "[FAIL] Player prefab Animator has Apply Root Motion enabled.");
                }

                ValidateAnimator(prefabContents, controllers[0], report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static void ValidateEnemyLayer(List<string> report)
        {
            report.Add(LayerMask.NameToLayer("Enemy") >= 0
                ? "[PASS] Enemy layer exists."
                : "[FAIL] Enemy layer is missing.");
        }

        private static void ValidateEnemyLayerMask(LayerMask enemyLayerMask, List<string> report)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            bool valid = enemyLayer >= 0 && (enemyLayerMask.value & (1 << enemyLayer)) != 0;
            report.Add(valid
                ? "[PASS] PlayerCombatController Enemy LayerMask includes Enemy."
                : "[FAIL] PlayerCombatController Enemy LayerMask does not include Enemy.");
        }

        private static void ValidateCombo(PlayerCombatController controller, List<string> report)
        {
            IReadOnlyList<ComboAttack> combo = controller.ComboAttacks;
            if (combo == null || combo.Count == 0)
            {
                report.Add("[FAIL] Combo list is empty.");
                return;
            }

            report.Add($"[PASS] Combo has {combo.Count} attack(s).");
            if (combo.Count != AttackTriggers.Length)
            {
                report.Add($"[FAIL] Combo has {combo.Count} attack(s); expected exactly {AttackTriggers.Length}.");
            }

            for (int i = 0; i < combo.Count; i++)
            {
                ComboAttack attack = combo[i];
                if (attack == null)
                {
                    report.Add($"[FAIL] Combo attack #{i + 1} is null.");
                    continue;
                }

                bool validTiming =
                    attack.attackDuration > 0f
                    && attack.hitTime >= 0f
                    && attack.hitTime <= attack.attackDuration
                    && attack.comboInputStartTime >= 0f
                    && attack.comboInputStartTime <= attack.comboInputEndTime
                    && attack.comboInputEndTime <= attack.attackDuration
                    && attack.recoveryTime >= 0f
                    && controller.ComboResetDelay >= 0f;

                report.Add(validTiming
                    ? $"[PASS] Timing for combo attack #{i + 1} ({attack.attackName}) is valid."
                    : $"[FAIL] Timing for combo attack #{i + 1} ({attack.attackName}) is invalid.");

                if (i < AttackTriggers.Length)
                {
                    report.Add(string.Equals(attack.animationTrigger, AttackTriggers[i], StringComparison.Ordinal)
                        ? $"[PASS] Combo attack #{i + 1} uses trigger {AttackTriggers[i]}."
                        : $"[FAIL] Combo attack #{i + 1} uses trigger '{attack.animationTrigger}', expected {AttackTriggers[i]}.");
                }
            }
        }

        private static void ValidateAttackInput(List<string> report)
        {
            bool gameplayAttackValid = ValidateAttackAction(
                Chapter1InputActionsPath,
                GameplayMapName,
                "[FAIL]",
                report);

            ValidateAttackAction(
                ProjectInputActionsPath,
                PlayerMapName,
                gameplayAttackValid ? "[WARNING]" : "[FAIL]",
                report);

            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(AttackReferencePath);
            if (reference != null && reference.action != null)
            {
                report.Add("[PASS] Attack InputActionReference asset exists and resolves an action.");
            }
            else
            {
                report.Add("[FAIL] Attack InputActionReference asset is missing or unresolved.");
            }
        }

        private static bool ValidateAttackAction(string assetPath, string mapName, string missingLevel, List<string> report)
        {
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (inputActionAsset == null)
            {
                report.Add($"{missingLevel} Missing Input Action Asset: {assetPath}.");
                return false;
            }

            InputAction attackAction = inputActionAsset.FindAction($"{mapName}/{AttackActionName}", false);
            if (attackAction == null)
            {
                report.Add($"{missingLevel} {assetPath} is missing {mapName}/Attack.");
                return false;
            }

            bool hasMouseBinding = HasBinding(attackAction, AttackMouseBinding);
            report.Add(hasMouseBinding
                ? $"[PASS] {assetPath} has {mapName}/Attack bound to {AttackMouseBinding}."
                : $"[FAIL] {assetPath} {mapName}/Attack is missing {AttackMouseBinding} binding.");
            return hasMouseBinding;
        }

        private static void EnsureLayer(string layerName, List<string> report)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                report.Add($"[PASS] Layer {layerName} already exists.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                report.Add($"[PASS] Added layer {layerName} at slot {i}.");
                return;
            }

            report.Add($"[FAIL] No empty user layer slot is available for {layerName}.");
        }

        private static GameObject FindScenePlayer()
        {
            try
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null)
                {
                    return taggedPlayer;
                }
            }
            catch (UnityException)
            {
                // Tag may not exist in a partially configured project.
            }

            Chapter1PlayerMotor motor = Object.FindAnyObjectByType<Chapter1PlayerMotor>();
            if (motor != null)
            {
                return motor.gameObject;
            }

            Chapter1InputReader inputReader = Object.FindAnyObjectByType<Chapter1InputReader>();
            return inputReader != null ? inputReader.gameObject : null;
        }

        private static bool IsAnimationSetupAlreadyApplied()
        {
            AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorControllerPath);
            if (!AnimatorControllerLooksConfigured(animatorController))
            {
                return false;
            }

            if (!PrefabAnimationSetupLooksConfigured(animatorController))
            {
                return false;
            }

            GameObject scenePlayer = FindScenePlayer();
            return scenePlayer == null || PlayerAnimationSetupLooksConfigured(scenePlayer, animatorController);
        }

        private static bool AnimatorControllerLooksConfigured(AnimatorController animatorController)
        {
            if (animatorController == null)
            {
                return false;
            }

            for (int i = 0; i < AnimatorParameters.Length; i++)
            {
                AnimatorParameterSpec parameterSpec = AnimatorParameters[i];
                if (!AnimatorControllerHasParameter(animatorController, parameterSpec.Name, parameterSpec.Type))
                {
                    return false;
                }
            }

            if (animatorController.layers == null || animatorController.layers.Length == 0 || animatorController.layers[0].stateMachine == null)
            {
                return false;
            }

            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            for (int i = 0; i < AttackAnimationSpecs.Length; i++)
            {
                AnimatorState state = FindState(stateMachine, AttackAnimationSpecs[i].StateName);
                if (state == null || state.motion == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PrefabAnimationSetupLooksConfigured(AnimatorController animatorController)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                return false;
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                return PlayerAnimationSetupLooksConfigured(prefabContents, animatorController);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static bool PlayerAnimationSetupLooksConfigured(GameObject player, AnimatorController animatorController)
        {
            if (player == null)
            {
                return false;
            }

            Animator animator = player.GetComponent<Animator>();
            PlayerCombatController combatController = player.GetComponent<PlayerCombatController>();
            return animator != null
                && animator.enabled
                && !animator.applyRootMotion
                && animator.runtimeAnimatorController == animatorController
                && combatController != null
                && combatController.CombatAnimator == animator
                && combatController.AttackPoint != null
                && combatController.LegacyAnimationToPause != null
                && combatController.SuspendsLegacyAnimationDuringAttack;
        }

        private static GameObject FindOrOpenScenePlayer(List<string> report)
        {
            GameObject player = FindScenePlayer();
            if (player != null)
            {
                return player;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayerPrototypeScenePath) == null)
            {
                report.Add($"[FAIL] Prototype scene was not found at {PlayerPrototypeScenePath}.");
                return null;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Add("[FAIL] Scene switch was cancelled before opening Chapter1_PlayerPrototype.");
                return null;
            }

            EditorSceneManager.OpenScene(PlayerPrototypeScenePath, OpenSceneMode.Single);
            report.Add($"[PASS] Opened {PlayerPrototypeScenePath} to configure the scene Player.");
            return FindScenePlayer();
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string fullPath = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void DisableGeneratedInputWrapper(string assetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                return;
            }

            SerializedObject importerObject = new SerializedObject(importer);
            SerializedProperty generateWrapperCode = importerObject.FindProperty("m_GenerateWrapperCode");
            if (generateWrapperCode != null)
            {
                generateWrapperCode.boolValue = false;
                importerObject.ApplyModifiedProperties();
                importer.SaveAndReimport();
            }
        }

        private static void PrintReport(string title, List<string> report)
        {
            string message = $"[Melee Combat {title}]";
            for (int i = 0; i < report.Count; i++)
            {
                message += "\n" + report[i];
            }

            Debug.Log(message);
        }

        private static void LogFail(string message)
        {
            Debug.LogError($"[Melee Combat Validator] [FAIL] {message}");
        }

        private static void ExecuteDownloadedCombatAnimationTool(string methodName)
        {
            Type toolType = typeof(MeleeCombatEditorTool).Assembly.GetType(
                "DormitoryMystery.Chapter1.Editor.DownloadedCombatAnimationIntegrator");
            System.Reflection.MethodInfo method = toolType?.GetMethod(
                methodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                Debug.LogError("[Melee Combat] Downloaded combat animation integrator is not available yet. Wait for Unity to finish compiling scripts, then run the menu again.");
                return;
            }

            method.Invoke(null, null);
        }

        private readonly struct AnimatorParameterSpec
        {
            public AnimatorParameterSpec(string name, AnimatorControllerParameterType type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }
            public AnimatorControllerParameterType Type { get; }
        }

        private readonly struct AttackAnimationSpec
        {
            public AttackAnimationSpec(
                string stateName,
                string placeholderClipPath,
                float duration,
                float hitTime,
                float comboWindowStart,
                float comboWindowEnd,
                string[] keywords,
                Vector3 anchorOffset,
                Vector3 anchorEulerOffset,
                string primaryBoneName,
                Vector3 primaryBoneEulerOffset,
                string secondaryBoneName,
                Vector3 secondaryBoneEulerOffset)
            {
                StateName = stateName;
                PlaceholderClipPath = placeholderClipPath;
                Duration = duration;
                HitTime = hitTime;
                ComboWindowStart = comboWindowStart;
                ComboWindowEnd = comboWindowEnd;
                Keywords = keywords;
                AnchorOffset = anchorOffset;
                AnchorEulerOffset = anchorEulerOffset;
                PrimaryBoneName = primaryBoneName;
                PrimaryBoneEulerOffset = primaryBoneEulerOffset;
                SecondaryBoneName = secondaryBoneName;
                SecondaryBoneEulerOffset = secondaryBoneEulerOffset;
            }

            public string StateName { get; }
            public string PlaceholderClipPath { get; }
            public float Duration { get; }
            public float HitTime { get; }
            public float ComboWindowStart { get; }
            public float ComboWindowEnd { get; }
            public string[] Keywords { get; }
            public Vector3 AnchorOffset { get; }
            public Vector3 AnchorEulerOffset { get; }
            public string PrimaryBoneName { get; }
            public Vector3 PrimaryBoneEulerOffset { get; }
            public string SecondaryBoneName { get; }
            public Vector3 SecondaryBoneEulerOffset { get; }
        }

        private sealed class CombatAnimationAssets
        {
            private readonly Dictionary<string, AnimationClip> clipsByState = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

            public AnimatorController Controller { get; set; }

            public AnimationClip GetClip(string stateName)
            {
                return clipsByState.TryGetValue(stateName, out AnimationClip clip) ? clip : null;
            }

            public void SetClip(string stateName, AnimationClip clip)
            {
                clipsByState[stateName] = clip;
            }
        }
    }
}
