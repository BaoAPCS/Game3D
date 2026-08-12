using System;
using System.Collections.Generic;
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
    public static class DownloadedCombatAnimationIntegrator
    {
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string PlayerPrototypeScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string PlayerDormitoryScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        private const string PlayerModelFbxPath = "Assets/Chapter1/ExternalAssets/Nam.fbx";
        private const string ControllerPath = "Assets/Chapter1/Animations/Controllers/Chapter1PlayerAnimator.controller";
        private const string KickInputReferencePath = "Assets/Chapter1/Settings/InputReferences/Kick.inputactionreference.asset";
        private const string JumpInputReferencePath = "Assets/Chapter1/Settings/InputReferences/Jump.inputactionreference.asset";
        private const string BackupFolderPath = "Assets/Chapter1/Backups";
        private const string PreferredDownloadedFolder = "Assets/ExternalAssets/Animations/Combat";
        private const string CurrentDownloadedFolder = "Assets/Chapter1/Animations/Combat";
        private const float MoveStartThreshold = 0.1f;
        private const float RunSpeedThreshold = 4.75f;
        private const float LocomotionTransitionDuration = 0.12f;
        private const float AttackReturnTransitionDuration = 0.14f;

        private const string MoveSpeed = "MoveSpeed";
        private const string IsGrounded = "IsGrounded";
        private const string IsSprinting = "IsSprinting";
        private const string IsCrouching = "IsCrouching";
        private const string IsJumping = "IsJumping";
        private const string IsAttacking = "IsAttacking";
        private const string VerticalSpeed = "VerticalSpeed";
        private const string Jump = "Jump";

        private static readonly AnimationRoleSpec[] RoleSpecs =
        {
            new AnimationRoleSpec(
                CombatAnimationRole.StandIdle,
                "StandIdle",
                "Stand Idle",
                true,
                new[] { "standingidle", "standidle", "standstill" },
                0.35f,
                0.5f,
                0.78f),
            new AnimationRoleSpec(
                CombatAnimationRole.CombatIdle,
                "CombatIdle",
                "Combat Idle",
                true,
                new[] { "combatidle", "mmaidle", "idle" },
                0.35f,
                0.5f,
                0.78f),
            new AnimationRoleSpec(
                CombatAnimationRole.Walk,
                "Walk",
                "Walk",
                true,
                new[] { "walking", "walk" },
                0.35f,
                0.5f,
                0.78f),
            new AnimationRoleSpec(
                CombatAnimationRole.Run,
                "Run",
                "Run",
                true,
                new[] { "running", "run" },
                0.35f,
                0.5f,
                0.78f),
            new AnimationRoleSpec(
                CombatAnimationRole.PunchLeft,
                "PunchLeft",
                "Punch Left",
                false,
                new[] { "combatpunchleft", "punchleft", "leadjab", "jab" },
                0.35f,
                0.5f,
                0.76f,
                0.58f,
                10f,
                1.1f,
                0.3f,
                0.06f),
            new AnimationRoleSpec(
                CombatAnimationRole.PunchRight,
                "PunchRight",
                "Punch Right",
                false,
                new[] { "combatpunchright", "punchright", "crosspunch", "cross" },
                0.35f,
                0.5f,
                0.76f,
                0.62f,
                12f,
                1.15f,
                0.32f,
                0.07f),
            new AnimationRoleSpec(
                CombatAnimationRole.Hook,
                "Hook",
                "Hook",
                false,
                new[] { "hook" },
                0.38f,
                0.55f,
                0.82f,
                0.68f,
                15f,
                1.15f,
                0.34f,
                0.1f),
            new AnimationRoleSpec(
                CombatAnimationRole.RightHook,
                "RightHook",
                "Right Hook",
                false,
                new[] { "righthook" },
                0.38f,
                0.56f,
                0.82f,
                0.72f,
                18f,
                1.2f,
                0.36f,
                0.1f),
            new AnimationRoleSpec(
                CombatAnimationRole.KickSide,
                "KickSide",
                "Side Kick",
                false,
                new[] { "combatkickside", "kickside", "mmasidekick", "sidekick" },
                0.35f,
                0.56f,
                0.82f,
                0.85f,
                18f,
                1.45f,
                0.38f,
                0.1f),
            new AnimationRoleSpec(
                CombatAnimationRole.KickHeavy,
                "KickHeavy",
                "Heavy Kick",
                false,
                new[] { "combatkickheavy", "kickheavy", "highkick", "frontkick", "mmakick" },
                0.36f,
                0.58f,
                0.84f,
                1f,
                28f,
                1.6f,
                0.42f,
                0.18f),
            new AnimationRoleSpec(
                CombatAnimationRole.SpinningBackKick,
                "SpinningBackKick",
                "Spinning Back Kick",
                false,
                new[] { "spiningbackkick", "spinningbackkick", "backkick" },
                0.38f,
                0.58f,
                0.84f,
                1.05f,
                30f,
                1.55f,
                0.42f,
                0.18f),
            new AnimationRoleSpec(
                CombatAnimationRole.SitDown,
                "SitDown",
                "Sit Down",
                true,
                new[] { "malesittingpose", "sitdown", "sittingpose", "sitting" },
                0.2f,
                0.5f,
                0.8f),
            new AnimationRoleSpec(
                CombatAnimationRole.Jump,
                "Jump",
                "Jump",
                false,
                new[] { "jump" },
                0.2f,
                0.45f,
                0.8f),
            new AnimationRoleSpec(
                CombatAnimationRole.Stunned,
                "Stunned",
                "Stunned",
                false,
                new[] { "stunned" },
                0.2f,
                0.5f,
                0.8f)
        };

        [MenuItem("Tools/Player/Integrate Downloaded Combat Animations")]
        public static void IntegrateDownloadedCombatAnimations()
        {
            IntegrationReport report = new IntegrationReport();
            GameObject scenePlayer = FindScenePlayer();
            ReportCurrentPlayerState(scenePlayer, report);

            DownloadedCombatAssetSet downloadedAssets = FindDownloadedAnimationAssets(report);
            if (!downloadedAssets.HasAllRequiredAssets(report))
            {
                report.Print("Downloaded Combat Animation Integration");
                return;
            }

            BackupImportantAssets(report);
            ConfigurePlayerModelImporter(report);
            ConfigureDownloadedAnimationImporters(downloadedAssets, report);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            downloadedAssets.LoadImportedClips(report);
            if (!downloadedAssets.HasAllRequiredClips(report))
            {
                report.Print("Downloaded Combat Animation Integration");
                return;
            }

            ConfigureDownloadedAnimationEventsFromClipLengths(downloadedAssets, report);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            downloadedAssets.LoadImportedClips(report);

            AnimatorController controller = EnsureAnimatorController(downloadedAssets, report);
            Avatar playerAvatar = LoadAvatar(PlayerModelFbxPath);

            ConfigureAnimatorController(controller, downloadedAssets, report);
            ConfigurePlayerPrefab(controller, playerAvatar, downloadedAssets, report);
            ConfigureTargetScenes(controller, playerAvatar, downloadedAssets, report);

            AssetDatabase.SaveAssets();
            report.Print("Downloaded Combat Animation Integration");
        }

        [MenuItem("Tools/Player/Validate Downloaded Combat Animations")]
        public static void ValidateDownloadedCombatAnimations()
        {
            IntegrationReport report = new IntegrationReport();
            DownloadedCombatAssetSet downloadedAssets = FindDownloadedAnimationAssets(report);
            downloadedAssets.LoadImportedClips(report);

            ValidateDownloadedAssets(downloadedAssets, report);
            ValidatePlayerModelRig(report);
            ValidateAnimatorController(downloadedAssets, report);
            ValidatePlayerPrefab(report);
            ValidateScene(PlayerPrototypeScenePath, report);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayerDormitoryScenePath) != null)
            {
                ValidateScene(PlayerDormitoryScenePath, report);
            }

            report.Print("Downloaded Combat Animation Validation");
        }

        private static void ReportCurrentPlayerState(GameObject player, IntegrationReport report)
        {
            if (player == null)
            {
                report.Warning("No Player object found in the active scene before integration.");
                return;
            }

            PlayerVisualController visualController = player.GetComponentInChildren<PlayerVisualController>(true);
            Animation legacyAnimation = player.GetComponentInChildren<Animation>(true);
            Animator animator = player.GetComponentInChildren<Animator>(true);
            Transform modelRoot = visualController != null && visualController.AnimatedModelRoot != null
                ? visualController.AnimatedModelRoot
                : legacyAnimation != null ? legacyAnimation.transform : null;

            report.Pass($"Pre-check Player object: {GetHierarchyPath(player.transform)}.");
            report.Pass($"Pre-check model object: {(modelRoot != null ? GetHierarchyPath(modelRoot) : "not found")}.");
            report.Pass($"Pre-check Player model rig: {GetRigDescription(PlayerModelFbxPath)}.");
            report.Pass($"Pre-check animation system: Legacy Animation {(legacyAnimation != null ? "present" : "missing")}, Animator {(animator != null ? "present" : "missing")}.");
            report.Pass($"Pre-check Animator Controller: {GetControllerPath(animator)}.");
            report.Pass($"Pre-check locomotion clips: {DescribeLegacyClips(legacyAnimation)}.");
        }

        private static DownloadedCombatAssetSet FindDownloadedAnimationAssets(IntegrationReport report)
        {
            DownloadedCombatAssetSet assets = new DownloadedCombatAssetSet();
            string[] searchFolders = GetExistingSearchFolders();
            string[] guids = AssetDatabase.FindAssets("t:Model", searchFolders);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string normalized = Normalize(path);
                for (int roleIndex = 0; roleIndex < RoleSpecs.Length; roleIndex++)
                {
                    AnimationRoleSpec spec = RoleSpecs[roleIndex];
                    if (spec.Role == CombatAnimationRole.Hook && normalized.Contains("righthook"))
                    {
                        continue;
                    }

                    if (spec.Role == CombatAnimationRole.CombatIdle && normalized.Contains("standingidle"))
                    {
                        continue;
                    }

                    if (assets.GetPath(spec.Role) != null || !spec.Matches(normalized))
                    {
                        continue;
                    }

                    assets.SetPath(spec.Role, path);
                    report.Pass($"Found {spec.DisplayName}: {path}.");
                    break;
                }
            }

            return assets;
        }

        private static string[] GetExistingSearchFolders()
        {
            List<string> folders = new List<string>();
            if (AssetDatabase.IsValidFolder(PreferredDownloadedFolder))
            {
                folders.Add(PreferredDownloadedFolder);
            }

            if (AssetDatabase.IsValidFolder(CurrentDownloadedFolder))
            {
                folders.Add(CurrentDownloadedFolder);
            }

            if (folders.Count == 0)
            {
                folders.Add("Assets");
            }

            return folders.ToArray();
        }

        private static void BackupImportantAssets(IntegrationReport report)
        {
            EnsureFolder("Assets/Chapter1", "Backups");
            CopyAssetIfMissing(PlayerPrefabPath, BackupFolderPath + "/Player_BackupBeforeDownloadedCombat.prefab", report);
            CopyAssetIfMissing(ControllerPath, BackupFolderPath + "/Chapter1PlayerAnimator_BackupBeforeDownloadedCombat.controller", report);
        }

        private static void CopyAssetIfMissing(string sourcePath, string backupPath, IntegrationReport report)
        {
            if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                return;
            }

            if (AssetDatabase.LoadMainAssetAtPath(backupPath) != null)
            {
                report.Pass($"Backup already exists: {backupPath}.");
                return;
            }

            if (AssetDatabase.CopyAsset(sourcePath, backupPath))
            {
                report.Pass($"Created backup: {backupPath}.");
            }
            else
            {
                report.Warning($"Could not create backup for {sourcePath}.");
            }
        }

        private static void ConfigurePlayerModelImporter(IntegrationReport report)
        {
            ModelImporter importer = AssetImporter.GetAtPath(PlayerModelFbxPath) as ModelImporter;
            if (importer == null)
            {
                report.Fail($"Player model importer not found: {PlayerModelFbxPath}.");
                return;
            }

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                report.Pass("Configured Nam as a Humanoid Avatar source for Animator retargeting.");
            }
            else
            {
                report.Pass("Nam is already configured as a Humanoid Avatar source.");
            }
        }

        private static void ConfigureDownloadedAnimationImporters(DownloadedCombatAssetSet assets, IntegrationReport report)
        {
            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                AnimationRoleSpec spec = RoleSpecs[i];
                string path = assets.GetPath(spec.Role);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    report.Fail($"ModelImporter not found for {path}.");
                    continue;
                }

                bool changed = false;
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    changed = true;
                }

                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }

                if (!importer.importAnimation)
                {
                    importer.importAnimation = true;
                    changed = true;
                }

                ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
                if (clipAnimations == null || clipAnimations.Length == 0)
                {
                    clipAnimations = importer.defaultClipAnimations;
                }

                if (clipAnimations == null || clipAnimations.Length == 0)
                {
                    report.Fail($"No clip animations found in {path}.");
                    continue;
                }

                for (int clipIndex = 0; clipIndex < clipAnimations.Length; clipIndex++)
                {
                    ModelImporterClipAnimation clip = clipAnimations[clipIndex];
                    clip.name = spec.StateName;
                    clip.loopTime = spec.Loops;
                    clip.loopPose = spec.Loops;
                    clip.wrapMode = spec.Loops ? WrapMode.Loop : WrapMode.Once;
                    bool isCodeDrivenLocomotion = spec.Role == CombatAnimationRole.Walk || spec.Role == CombatAnimationRole.Run;
                    clip.lockRootRotation = !isCodeDrivenLocomotion;
                    clip.keepOriginalOrientation = !isCodeDrivenLocomotion;
                    clip.lockRootHeightY = true;
                    clip.keepOriginalPositionY = true;
                    clip.lockRootPositionXZ = !isCodeDrivenLocomotion;
                    clip.keepOriginalPositionXZ = !isCodeDrivenLocomotion;
                    clip.events = spec.IsAttack ? CreateEventsForSpec(spec, 1f) : Array.Empty<AnimationEvent>();
                    clipAnimations[clipIndex] = clip;
                    changed = true;
                }

                importer.clipAnimations = clipAnimations;
                if (changed)
                {
                    importer.SaveAndReimport();
                }

                report.Pass($"Configured {spec.DisplayName} importer as Humanoid, loop={spec.Loops}, root motion prepared for {(spec.Role == CombatAnimationRole.Walk || spec.Role == CombatAnimationRole.Run ? "code-driven locomotion" : "attack poses")}.");
            }
        }

        private static AnimationEvent[] CreateEventsForSpec(AnimationRoleSpec spec, float clipLength)
        {
            float safeLength = Mathf.Max(0.01f, clipLength);
            return new[]
            {
                CreateAnimationEvent("OpenComboWindow", safeLength * spec.OpenWindowPercent),
                CreateAnimationEvent("PerformAttackHit", safeLength * spec.HitPercent),
                CreateAnimationEvent("CloseComboWindow", safeLength * spec.CloseWindowPercent),
                CreateAnimationEvent("EndAttack", safeLength * 0.95f)
            };
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

        private static void ConfigureDownloadedAnimationEventsFromClipLengths(DownloadedCombatAssetSet assets, IntegrationReport report)
        {
            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                AnimationRoleSpec spec = RoleSpecs[i];
                if (!spec.IsAttack)
                {
                    continue;
                }

                string path = assets.GetPath(spec.Role);
                AnimationClip importedClip = assets.GetClip(spec.Role);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || importedClip == null)
                {
                    continue;
                }

                ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
                if (clipAnimations == null || clipAnimations.Length == 0)
                {
                    clipAnimations = importer.defaultClipAnimations;
                }

                for (int clipIndex = 0; clipIndex < clipAnimations.Length; clipIndex++)
                {
                    ModelImporterClipAnimation clip = clipAnimations[clipIndex];
                    clip.name = spec.StateName;
                    clip.loopTime = false;
                    clip.wrapMode = WrapMode.Once;
                    clip.events = CreateEventsForSpec(spec, importedClip.length);
                    clipAnimations[clipIndex] = clip;
                }

                importer.clipAnimations = clipAnimations;
                importer.SaveAndReimport();
                report.Pass($"Wrote Animation Events for {spec.StateName} using clip length {importedClip.length:0.###}s.");
            }
        }

        private static AnimatorController EnsureAnimatorController(DownloadedCombatAssetSet assets, IntegrationReport report)
        {
            EnsureFolder("Assets/Chapter1/Animations", "Controllers");
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                report.Pass($"Created Animator Controller: {ControllerPath}.");
            }
            else
            {
                report.Pass($"Reusing Animator Controller: {ControllerPath}.");
            }

            return controller;
        }

        private static void ConfigureAnimatorController(
            AnimatorController controller,
            DownloadedCombatAssetSet assets,
            IntegrationReport report)
        {
            if (controller == null)
            {
                report.Fail("Animator Controller is null.");
                return;
            }

            Undo.RecordObject(controller, "Integrate Downloaded Combat Animations");
            EnsureParameter(controller, MoveSpeed, AnimatorControllerParameterType.Float, report);
            EnsureParameter(controller, IsGrounded, AnimatorControllerParameterType.Bool, report);
            EnsureParameter(controller, IsSprinting, AnimatorControllerParameterType.Bool, report);
            EnsureParameter(controller, IsCrouching, AnimatorControllerParameterType.Bool, report);
            EnsureParameter(controller, IsJumping, AnimatorControllerParameterType.Bool, report);
            EnsureParameter(controller, IsAttacking, AnimatorControllerParameterType.Bool, report);
            EnsureParameter(controller, VerticalSpeed, AnimatorControllerParameterType.Float, report);
            EnsureParameter(controller, Jump, AnimatorControllerParameterType.Trigger, report);

            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                if (RoleSpecs[i].IsAttack)
                {
                    EnsureParameter(controller, RoleSpecs[i].StateName, AnimatorControllerParameterType.Trigger, report);
                }
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimationClip idleClip = assets.GetClip(CombatAnimationRole.StandIdle) ?? assets.GetClip(CombatAnimationRole.CombatIdle);
            AnimationClip sitClip = assets.GetClip(CombatAnimationRole.SitDown) ?? idleClip;
            AnimationClip jumpClip = assets.GetClip(CombatAnimationRole.Jump);
            AnimationClip stunnedClip = assets.GetClip(CombatAnimationRole.Stunned);
            AnimationClip safeWalkClip = assets.GetClip(CombatAnimationRole.Walk) ?? idleClip;
            AnimationClip safeRunClip = assets.GetClip(CombatAnimationRole.Run) ?? safeWalkClip;

            if (safeWalkClip == null)
            {
                report.Warning("Walk clip was not found; CombatIdle is used as a temporary Walk motion.");
            }

            if (safeRunClip == null)
            {
                report.Warning("Run clip was not found; Walk/CombatIdle is used as a temporary Run motion.");
            }

            AnimatorState idle = EnsureState(stateMachine, "Idle", idleClip, new Vector3(220f, 40f, 0f), report);
            AnimatorState walk = EnsureState(stateMachine, "Walk", safeWalkClip, new Vector3(220f, 120f, 0f), report);
            AnimatorState run = EnsureState(stateMachine, "Run", safeRunClip, new Vector3(220f, 200f, 0f), report);
            AnimatorState crouchIdle = EnsureState(stateMachine, "Crouch Idle", sitClip, new Vector3(220f, 300f, 0f), report);
            AnimatorState crouchWalk = EnsureState(stateMachine, "Crouch Walk", sitClip, new Vector3(220f, 380f, 0f), report);
            AnimatorState jump = EnsureState(stateMachine, "Jump", jumpClip, new Vector3(220f, 470f, 0f), report);
            AnimatorState stunned = EnsureState(stateMachine, "Stunned", stunnedClip, new Vector3(520f, 700f, 0f), report);
            stunned.writeDefaultValues = true;
            stunned.speed = 1f;
            stateMachine.defaultState = idle;

            EnsureTransition(idle, walk, LocomotionTransitionDuration, false, report, Condition.Greater(MoveSpeed, MoveStartThreshold), Condition.IfNot(IsCrouching));
            EnsureTransition(walk, idle, LocomotionTransitionDuration, false, report, Condition.Less(MoveSpeed, MoveStartThreshold), Condition.IfNot(IsCrouching));
            EnsureTransition(walk, run, LocomotionTransitionDuration, false, report, Condition.Greater(MoveSpeed, RunSpeedThreshold), Condition.IfNot(IsCrouching));
            EnsureTransition(run, walk, LocomotionTransitionDuration, false, report, Condition.Less(MoveSpeed, RunSpeedThreshold), Condition.IfNot(IsCrouching));
            EnsureTransition(idle, crouchIdle, LocomotionTransitionDuration, false, report, Condition.If(IsCrouching));
            EnsureTransition(walk, crouchWalk, LocomotionTransitionDuration, false, report, Condition.If(IsCrouching));
            EnsureTransition(run, crouchWalk, LocomotionTransitionDuration, false, report, Condition.If(IsCrouching));
            EnsureTransition(crouchIdle, idle, LocomotionTransitionDuration, false, report, Condition.IfNot(IsCrouching));
            EnsureTransition(crouchWalk, walk, LocomotionTransitionDuration, false, report, Condition.IfNot(IsCrouching), Condition.Greater(MoveSpeed, MoveStartThreshold));
            EnsureTransition(crouchWalk, crouchIdle, LocomotionTransitionDuration, false, report, Condition.Less(MoveSpeed, MoveStartThreshold), Condition.If(IsCrouching));
            EnsureTransition(crouchIdle, crouchWalk, LocomotionTransitionDuration, false, report, Condition.Greater(MoveSpeed, MoveStartThreshold), Condition.If(IsCrouching));
            EnsureAnyStateTransition(stateMachine, jump, Jump, report);
            EnsureTransition(jump, idle, LocomotionTransitionDuration, true, report, Condition.If(IsGrounded));

            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                AnimationRoleSpec spec = RoleSpecs[i];
                if (!spec.IsAttack)
                {
                    continue;
                }

                AnimationClip attackClip = assets.GetClip(spec.Role);
                AnimatorState attackState = EnsureState(
                    stateMachine,
                    spec.StateName,
                    attackClip,
                    new Vector3(520f, 60f + i * 80f, 0f),
                    report);
                attackState.writeDefaultValues = true;
                attackState.speed = GetAttackPlaybackSpeed(spec, attackClip);
                if (attackClip != null && !Mathf.Approximately(attackState.speed, 1f))
                {
                    report.Pass($"State {spec.StateName} playback speed set to {attackState.speed:0.##}x for a {GetEffectiveAttackDuration(spec, attackClip):0.##}s gameplay beat.");
                }

                EnsureAnyStateTransition(stateMachine, attackState, spec.StateName, report);
                RemoveUnconditionalTransitions(attackState, idle, report);
                EnsureTransition(attackState, run, AttackReturnTransitionDuration, true, report, Condition.Greater(MoveSpeed, RunSpeedThreshold), Condition.IfNot(IsCrouching));
                EnsureTransition(attackState, walk, AttackReturnTransitionDuration, true, report, Condition.Greater(MoveSpeed, MoveStartThreshold), Condition.Less(MoveSpeed, RunSpeedThreshold + 0.01f), Condition.IfNot(IsCrouching));
                EnsureTransition(attackState, idle, AttackReturnTransitionDuration, true, report, Condition.Less(MoveSpeed, MoveStartThreshold), Condition.IfNot(IsCrouching));
                EnsureTransition(attackState, crouchIdle, AttackReturnTransitionDuration, true, report, Condition.If(IsCrouching));
            }

            EditorUtility.SetDirty(controller);
            report.Pass("Animator Controller locomotion and combat states are configured.");
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType,
            IntegrationReport report)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                {
                    if (parameters[i].type == parameterType)
                    {
                        return;
                    }

                    report.Warning($"Animator parameter {parameterName} exists with type {parameters[i].type}; expected {parameterType}.");
                    return;
                }
            }

            controller.AddParameter(parameterName, parameterType);
            report.Pass($"Added Animator parameter {parameterName}.");
        }

        private static AnimatorState EnsureState(
            AnimatorStateMachine stateMachine,
            string stateName,
            Motion motion,
            Vector3 position,
            IntegrationReport report)
        {
            AnimatorState state = FindState(stateMachine, stateName);
            if (state == null)
            {
                state = stateMachine.AddState(stateName, position);
                report.Pass($"Created Animator state {stateName}.");
            }

            if (motion != null)
            {
                state.motion = motion;
                report.Pass($"Assigned Motion {motion.name} to state {stateName}.");
            }
            else
            {
                report.Fail($"State {stateName} has no Motion candidate.");
            }

            return state;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static float GetEffectiveAttackDuration(AnimationRoleSpec spec, AnimationClip clip)
        {
            if (!spec.IsAttack)
            {
                return clip != null ? Mathf.Max(0.01f, clip.length) : 0.01f;
            }

            return spec.PreferredDuration > 0f
                ? spec.PreferredDuration
                : clip != null ? Mathf.Max(0.01f, clip.length) : 0.65f;
        }

        private static float GetAttackPlaybackSpeed(AnimationRoleSpec spec, AnimationClip clip)
        {
            if (!spec.IsAttack || clip == null || clip.length <= 0f)
            {
                return 1f;
            }

            float effectiveDuration = GetEffectiveAttackDuration(spec, clip);
            return Mathf.Clamp(clip.length / Mathf.Max(0.01f, effectiveDuration), 0.25f, 6f);
        }

        private static void EnsureAnyStateTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState destination,
            string triggerName,
            IntegrationReport report)
        {
            AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState == destination && HasCondition(transition, triggerName, AnimatorConditionMode.If))
                {
                    transition.hasExitTime = false;
                    transition.hasFixedDuration = true;
                    transition.duration = 0.05f;
                    transition.canTransitionToSelf = false;
                    transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
                    transition.orderedInterruption = true;
                    return;
                }
            }

            AnimatorStateTransition newTransition = stateMachine.AddAnyStateTransition(destination);
            newTransition.hasExitTime = false;
            newTransition.hasFixedDuration = true;
            newTransition.duration = 0.05f;
            newTransition.canTransitionToSelf = false;
            newTransition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
            newTransition.orderedInterruption = true;
            newTransition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
            report.Pass($"Created Any State transition for {triggerName}.");
        }

        private static void EnsureTransition(
            AnimatorState source,
            AnimatorState destination,
            float duration,
            bool hasExitTime,
            IntegrationReport report,
            params Condition[] conditions)
        {
            AnimatorStateTransition existing = FindTransition(source, destination, conditions);
            if (existing == null)
            {
                existing = source.AddTransition(destination);
                for (int i = 0; i < conditions.Length; i++)
                {
                    existing.AddCondition(conditions[i].Mode, conditions[i].Threshold, conditions[i].Parameter);
                }

                report.Pass($"Created transition {source.name} -> {destination.name}.");
            }

            existing.hasExitTime = hasExitTime;
            existing.exitTime = hasExitTime ? 0.98f : 0f;
            existing.hasFixedDuration = true;
            existing.duration = duration;
            existing.canTransitionToSelf = false;
            existing.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
            existing.orderedInterruption = true;
        }

        private static AnimatorStateTransition FindTransition(AnimatorState source, AnimatorState destination, Condition[] conditions)
        {
            AnimatorStateTransition[] transitions = source.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState != destination || transition.conditions.Length != conditions.Length)
                {
                    continue;
                }

                bool allMatch = true;
                for (int conditionIndex = 0; conditionIndex < conditions.Length; conditionIndex++)
                {
                    Condition expected = conditions[conditionIndex];
                    if (!HasCondition(transition, expected.Parameter, expected.Mode))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return transition;
                }
            }

            return null;
        }

        private static void RemoveUnconditionalTransitions(AnimatorState source, AnimatorState destination, IntegrationReport report)
        {
            AnimatorStateTransition[] transitions = source.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState == destination && transition.conditions.Length == 0)
                {
                    source.RemoveTransition(transition);
                    report.Pass($"Removed unconditional transition {source.name} -> {destination.name} so movement can blend directly after attacks.");
                }
            }
        }

        private static bool HasCondition(AnimatorStateTransition transition, string parameterName, AnimatorConditionMode mode)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].mode == mode && conditions[i].parameter == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigurePlayerPrefab(
            AnimatorController controller,
            Avatar avatar,
            DownloadedCombatAssetSet assets,
            IntegrationReport report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                report.Fail($"Player prefab not found: {PlayerPrefabPath}.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ConfigurePlayerObject(contents, controller, avatar, assets, report, "Player prefab");
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                report.Pass($"Saved Player prefab: {PlayerPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureTargetScenes(
            AnimatorController controller,
            Avatar avatar,
            DownloadedCombatAssetSet assets,
            IntegrationReport report)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                GameObject activePlayer = FindScenePlayer();
                if (activePlayer != null)
                {
                    ConfigurePlayerObject(activePlayer, controller, avatar, assets, report, $"active scene {activeScene.name}");
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
            }

            string activeScenePath = activeScene.path;
            string[] targetScenes = { PlayerPrototypeScenePath, PlayerDormitoryScenePath };
            bool needsSceneSwitch = false;
            for (int i = 0; i < targetScenes.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetScenes[i]) != null
                    && !string.Equals(activeScenePath, targetScenes[i], StringComparison.OrdinalIgnoreCase))
                {
                    needsSceneSwitch = true;
                    break;
                }
            }

            if (!needsSceneSwitch)
            {
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Warning("Skipped non-active scene integration because saving/switching scenes was cancelled.");
                return;
            }

            for (int i = 0; i < targetScenes.Length; i++)
            {
                string scenePath = targetScenes[i];
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null
                    || string.Equals(activeScenePath, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject player = FindScenePlayer();
                if (player == null)
                {
                    report.Warning($"No Player object found in {scenePath}.");
                    continue;
                }

                ConfigurePlayerObject(player, controller, avatar, assets, report, scenePath);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.Pass($"Saved scene integration: {scenePath}.");
            }

            if (!string.IsNullOrEmpty(activeScenePath) && AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScenePath) != null)
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }
        }

        private static void ConfigurePlayerObject(
            GameObject player,
            AnimatorController controller,
            Avatar avatar,
            DownloadedCombatAssetSet assets,
            IntegrationReport report,
            string label)
        {
            PlayerCombatController combatController = player.GetComponent<PlayerCombatController>();
            if (combatController == null)
            {
                combatController = player.AddComponent<PlayerCombatController>();
                report.Pass($"Added PlayerCombatController to {label}.");
            }

            combatController.EnsureDefaultComboIfEmpty();
            Transform attackPoint = FindChildRecursive(player.transform, "AttackPoint");
            if (attackPoint == null)
            {
                GameObject attackPointObject = new GameObject("AttackPoint");
                attackPointObject.transform.SetParent(player.transform);
                attackPointObject.transform.localPosition = new Vector3(0f, 1f, 0.85f);
                attackPointObject.transform.localRotation = Quaternion.identity;
                attackPointObject.transform.localScale = Vector3.one;
                attackPoint = attackPointObject.transform;
                report.Pass($"Created AttackPoint in {label}.");
            }

            PlayerVisualController visualController = player.GetComponentInChildren<PlayerVisualController>(true);
            Animation legacyAnimation = player.GetComponentInChildren<Animation>(true);
            Animator animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.AddComponent<Animator>();
                report.Pass($"Added Animator to Player root in {label}.");
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.keepAnimatorStateOnDisable = true;
            animator.enabled = true;

            CombatAnimationEventRelay relay = animator.GetComponent<CombatAnimationEventRelay>();
            if (relay == null)
            {
                relay = animator.gameObject.AddComponent<CombatAnimationEventRelay>();
                report.Pass($"Added CombatAnimationEventRelay to {label} Animator object.");
            }

            if (legacyAnimation != null)
            {
                legacyAnimation.playAutomatically = false;
                legacyAnimation.Stop();
                legacyAnimation.enabled = false;
                report.Pass($"Disabled obsolete Legacy Animation in {label}; the root Animator now drives all poses continuously.");
            }

            SerializedObject serializedVisual = visualController != null ? new SerializedObject(visualController) : null;
            if (serializedVisual != null)
            {
                SetBool(serializedVisual, "useLegacyLocomotion", false);
                SetObject(serializedVisual, "legacyAnimation", null);
                SetObject(serializedVisual, "walkClip", null);
                SetObject(serializedVisual, "runClip", null);
                SetFloat(serializedVisual, "smoothTime", 0.1f);
                SetFloat(serializedVisual, "animationFadeDuration", 0.16f);
                serializedVisual.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(visualController);
            }

            Chapter1PlayerMotor playerMotor = player.GetComponent<Chapter1PlayerMotor>();
            SerializedObject serializedMotor = playerMotor != null ? new SerializedObject(playerMotor) : null;
            if (serializedMotor != null)
            {
                SetFloat(serializedMotor, "acceleration", 22f);
                SetFloat(serializedMotor, "deceleration", 28f);
                SetFloat(serializedMotor, "stopSnapSpeed", 0.08f);
                SetFloat(serializedMotor, "crouchHeightSmoothTime", 0.08f);
                SetFloat(serializedMotor, "jumpHeight", 1.25f);
                SetFloat(serializedMotor, "jumpInputBufferTime", 0.12f);
                SetFloat(serializedMotor, "coyoteTime", 0.1f);
                SetFloat(serializedMotor, "airborneControlMultiplier", 0.85f);
                SetFloat(serializedMotor, "terminalVelocity", -35f);
                serializedMotor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(playerMotor);
            }

            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            if (inputReader != null)
            {
                SerializedObject serializedInput = new SerializedObject(inputReader);
                SetObject(serializedInput, "kickActionReference", AssetDatabase.LoadAssetAtPath<InputActionReference>(KickInputReferencePath));
                SetObject(serializedInput, "jumpActionReference", AssetDatabase.LoadAssetAtPath<InputActionReference>(JumpInputReferencePath));
                serializedInput.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(inputReader);
            }

            SerializedObject serializedCombat = new SerializedObject(combatController);
            SetObject(serializedCombat, "inputReader", inputReader);
            SetObject(serializedCombat, "playerMotor", playerMotor);
            SetObject(serializedCombat, "inputLock", player.GetComponent<PlayerInputLock>());
            SetObject(serializedCombat, "playerVisualController", visualController);
            SetObject(serializedCombat, "legacyAnimationToPause", null);
            SetObject(serializedCombat, "animator", animator);
            SetObject(serializedCombat, "attackPoint", attackPoint);
            SetObject(serializedCombat, "proceduralAnimationRoot", FindChildRecursive(player.transform, "ModelAnchor") ?? FindChildRecursive(player.transform, "Visual"));
            SetFloat(serializedCombat, "postAttackInputBufferTime", 0.14f);
            SetFloat(serializedCombat, "attackMoveSpeedMultiplier", 0.6f);
            SetBool(serializedCombat, "enableAnimatorOnlyDuringAttack", false);
            SetBool(serializedCombat, "enableAnimatorWhileCrouching", true);
            SetBool(serializedCombat, "enableAnimatorWhileIdle", true);
            SetBool(serializedCombat, "enableAnimatorWhileMoving", true);
            SetBool(serializedCombat, "enableAnimatorWhileJumping", true);
            SetBool(serializedCombat, "suspendLegacyAnimationDuringAttack", false);
            SetFloat(serializedCombat, "animatorReleaseDelay", 0.08f);
            SetFloat(serializedCombat, "locomotionBlendDuration", 0.14f);
            SetFloat(serializedCombat, "moveSpeedDampTime", 0.1f);
            SetFloat(serializedCombat, "runStateSpeedThreshold", RunSpeedThreshold);
            SetComboFromClips(serializedCombat, assets, report);
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedRelay = new SerializedObject(relay);
            SetObject(serializedRelay, "combatController", combatController);
            serializedRelay.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(relay);
            EditorUtility.SetDirty(combatController);
            report.Pass($"Assigned Nam Avatar, continuous Animator locomotion, relay, combo timings, and real clips for {label}.");
        }

        private static void SetComboFromClips(SerializedObject serializedCombat, DownloadedCombatAssetSet assets, IntegrationReport report)
        {
            SerializedProperty combo = serializedCombat.FindProperty("comboAttacks");
            if (combo == null)
            {
                report.Fail("PlayerCombatController comboAttacks property was not found.");
                return;
            }

            if (combo.arraySize != 4)
            {
                combo.arraySize = 4;
                report.Warning("Combo list was resized to exactly four hand attacks.");
            }

            int attackSlot = 0;
            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                AnimationRoleSpec spec = RoleSpecs[i];
                if (!spec.IsHandCombo)
                {
                    continue;
                }

                AnimationClip clip = assets.GetClip(spec.Role);
                SerializedProperty element = combo.GetArrayElementAtIndex(attackSlot);
                float length = GetEffectiveAttackDuration(spec, clip);

                SetString(element, "attackName", spec.DisplayName);
                SetString(element, "animationTrigger", spec.StateName);
                SetFloat(element, "damage", spec.Damage);
                SetFloat(element, "attackRange", spec.Range);
                SetFloat(element, "attackRadius", spec.Radius);
                SetFloat(element, "attackDuration", length);
                SetFloat(element, "hitTime", length * spec.HitPercent);
                SetFloat(element, "comboInputStartTime", length * spec.OpenWindowPercent);
                SetFloat(element, "comboInputEndTime", length * spec.CloseWindowPercent);
                SetFloat(element, "recoveryTime", Mathf.Max(0.04f, length * spec.RecoveryPercent));
                report.Pass($"Updated {spec.StateName} gameplay timing to {length:0.###}s.");

                attackSlot++;
            }

            SetKickAttackFromClip(serializedCombat, "neutralKickAttack", FindSpec(CombatAnimationRole.KickHeavy), assets, report);
            SetKickAttackFromClip(serializedCombat, "forwardKickAttack", FindSpec(CombatAnimationRole.KickSide), assets, report);
            SetKickAttackFromClip(serializedCombat, "backwardKickAttack", FindSpec(CombatAnimationRole.SpinningBackKick), assets, report);
            SetFloat(serializedCombat, "directionalKickThreshold", 0.35f);
        }

        private static void SetKickAttackFromClip(
            SerializedObject serializedCombat,
            string propertyName,
            AnimationRoleSpec spec,
            DownloadedCombatAssetSet assets,
            IntegrationReport report)
        {
            SerializedProperty element = serializedCombat.FindProperty(propertyName);
            if (element == null)
            {
                report.Fail($"PlayerCombatController {propertyName} property was not found.");
                return;
            }

            AnimationClip clip = assets.GetClip(spec.Role);
            float length = GetEffectiveAttackDuration(spec, clip);

            SetString(element, "attackName", spec.DisplayName);
            SetString(element, "animationTrigger", spec.StateName);
            SetFloat(element, "damage", spec.Damage);
            SetFloat(element, "attackRange", spec.Range);
            SetFloat(element, "attackRadius", spec.Radius);
            SetFloat(element, "attackDuration", length);
            SetFloat(element, "hitTime", length * spec.HitPercent);
            SetFloat(element, "comboInputStartTime", length * spec.OpenWindowPercent);
            SetFloat(element, "comboInputEndTime", length * spec.CloseWindowPercent);
            SetFloat(element, "recoveryTime", Mathf.Max(0.04f, length * spec.RecoveryPercent));
            report.Pass($"Updated {propertyName} gameplay timing to {length:0.###}s.");
        }

        private static bool TimingLooksDefault(SerializedProperty element, int attackSlot)
        {
            float duration = GetFloat(element, "attackDuration");
            float[] defaultDurations = { 0.55f, 0.58f, 0.75f, 0.9f };
            return duration <= 0f || Mathf.Abs(duration - defaultDurations[Mathf.Clamp(attackSlot, 0, defaultDurations.Length - 1)]) < 0.02f;
        }

        private static void ValidateDownloadedAssets(DownloadedCombatAssetSet assets, IntegrationReport report)
        {
            assets.HasAllRequiredAssets(report);
            assets.HasAllRequiredClips(report);
            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                AnimationRoleSpec spec = RoleSpecs[i];
                string path = assets.GetPath(spec.Role);
                AnimationClip clip = assets.GetClip(spec.Role);

                if (!string.IsNullOrEmpty(path))
                {
                    report.Pass($"{spec.DisplayName} path: {path}.");
                    ValidateImporter(path, spec, report);
                }

                if (clip != null)
                {
                    report.Pass($"{spec.DisplayName} clip length: {clip.length:0.###}s.");
                    ValidateClipLoopAndEvents(clip, spec, report);
                }
            }
        }

        private static void ValidateImporter(string path, AnimationRoleSpec spec, IntegrationReport report)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                report.Fail($"Missing ModelImporter for {path}.");
                return;
            }

            if (importer.animationType == ModelImporterAnimationType.Human)
            {
                report.Pass($"{spec.DisplayName} importer is Humanoid.");
            }
            else
            {
                report.Fail($"{spec.DisplayName} importer is {importer.animationType}, expected Humanoid.");
            }

            if (spec.Role == CombatAnimationRole.Walk || spec.Role == CombatAnimationRole.Run)
            {
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                ModelImporterClipAnimation clip = clips != null && clips.Length > 0 ? clips[0] : null;
                if (clip != null)
                {
                    report.Add(clip.loopPose, $"{spec.DisplayName} loop pose is enabled for seamless locomotion.");
                    report.Add(!clip.lockRootPositionXZ && !clip.keepOriginalPositionXZ, $"{spec.DisplayName} horizontal root motion stays out of the bones for code-driven movement.");
                }
                else
                {
                    report.Fail($"{spec.DisplayName} importer has no clip to validate root motion settings.");
                }
            }
        }

        private static void ValidateClipLoopAndEvents(AnimationClip clip, AnimationRoleSpec spec, IntegrationReport report)
        {
            if (clip.length > 0f)
            {
                report.Pass($"{spec.DisplayName} clip length is valid.");
            }
            else
            {
                report.Fail($"{spec.DisplayName} clip length is zero.");
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            bool loops = settings.loopTime || clip.isLooping;
            if (spec.Loops == loops)
            {
                report.Pass($"{spec.DisplayName} loop setting is correct.");
            }
            else
            {
                report.Fail($"{spec.DisplayName} loop setting is wrong.");
            }

            if (spec.IsAttack)
            {
                ValidateRequiredEvents(clip, report);
            }
        }

        private static void ValidateRequiredEvents(AnimationClip clip, IntegrationReport report)
        {
            string[] required =
            {
                "OpenComboWindow",
                "PerformAttackHit",
                "CloseComboWindow",
                "EndAttack"
            };
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            for (int i = 0; i < required.Length; i++)
            {
                bool found = false;
                for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                {
                    if (events[eventIndex].functionName == required[i])
                    {
                        found = true;
                        report.Pass($"{clip.name} event {required[i]} at {events[eventIndex].time:0.###}s.");
                        break;
                    }
                }

                if (!found)
                {
                    report.Fail($"{clip.name} is missing Animation Event {required[i]}.");
                }
            }
        }

        private static void ValidatePlayerModelRig(IntegrationReport report)
        {
            ModelImporter importer = AssetImporter.GetAtPath(PlayerModelFbxPath) as ModelImporter;
            if (importer != null
                && importer.animationType == ModelImporterAnimationType.Human
                && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
            {
                report.Pass("Nam model importer is configured to create a Humanoid Avatar.");
            }
            else
            {
                report.Fail("Nam model importer is not configured as a Humanoid Avatar source.");
            }

            Avatar avatar = LoadAvatar(PlayerModelFbxPath);
            if (avatar != null && avatar.isValid && avatar.isHuman)
            {
                report.Pass("Nam Avatar is valid Humanoid.");
            }
            else
            {
                report.Fail("Nam Avatar is missing or invalid.");
            }
        }

        private static void ValidateAnimatorController(DownloadedCombatAssetSet assets, IntegrationReport report)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                report.Fail($"Animator Controller missing: {ControllerPath}.");
                return;
            }

            report.Pass($"Animator Controller exists: {ControllerPath}.");
            string[] parameters =
            {
                MoveSpeed,
                IsGrounded,
                IsSprinting,
                IsCrouching,
                IsJumping,
                IsAttacking,
                VerticalSpeed,
                Jump,
                "PunchLeft",
                "PunchRight",
                "Hook",
                "RightHook",
                "KickSide",
                "KickHeavy",
                "SpinningBackKick"
            };
            for (int i = 0; i < parameters.Length; i++)
            {
                report.Add(ControllerHasParameter(controller, parameters[i]), $"Animator parameter {parameters[i]} exists.");
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            string[] animatorPoseStates = { "Idle", "Walk", "Run", "Crouch Idle", "Crouch Walk", "Jump" };
            for (int i = 0; i < animatorPoseStates.Length; i++)
            {
                AnimatorState state = FindState(stateMachine, animatorPoseStates[i]);
                report.Add(state != null && state.motion != null, $"Animator-driven pose state {animatorPoseStates[i]} has Motion.");
            }

            AnimatorState idleState = FindState(stateMachine, "Idle");
            AnimationClip standIdleClip = assets.GetClip(CombatAnimationRole.StandIdle);
            report.Add(idleState != null && standIdleClip != null && idleState.motion == standIdleClip, $"Idle state uses Stand Idle clip {standIdleClip?.name}.");

            AnimatorState walkState = FindState(stateMachine, "Walk");
            AnimationClip walkAnimationClip = assets.GetClip(CombatAnimationRole.Walk);
            report.Add(walkState != null && walkAnimationClip != null && walkState.motion == walkAnimationClip, $"Walk state uses Walking clip {walkAnimationClip?.name}.");

            AnimatorState runState = FindState(stateMachine, "Run");
            AnimationClip runAnimationClip = assets.GetClip(CombatAnimationRole.Run);
            report.Add(runState != null && runAnimationClip != null && runState.motion == runAnimationClip, $"Run state uses Running clip {runAnimationClip?.name}.");

            AnimatorState jumpState = FindState(stateMachine, "Jump");
            AnimationClip jumpClip = assets.GetClip(CombatAnimationRole.Jump);
            report.Add(jumpState != null && jumpClip != null && jumpState.motion == jumpClip, $"Jump state uses Jump clip {jumpClip?.name}.");
            report.Add(CountAnyStateTransitions(stateMachine, Jump) == 1, "Jump trigger has exactly one Any State transition.");

            AnimatorState stunnedState = FindState(stateMachine, "Stunned");
            AnimationClip stunnedClip = assets.GetClip(CombatAnimationRole.Stunned);
            report.Add(stunnedState != null, "Stunned state exists.");
            report.Add(stunnedState != null && stunnedClip != null && stunnedState.motion == stunnedClip, $"Stunned state uses Stunned clip {stunnedClip?.name}.");
            report.Add(stunnedState != null && Mathf.Approximately(stunnedState.speed, 1f), "Stunned state playback speed is 1.");
            report.Add(stunnedState != null && stunnedState.transitions.Length == 0, "Stunned state has no outgoing transitions.");
            report.Add(!ControllerHasParameter(controller, "Stunned"), "Stunned has no Animator parameter or trigger.");
            report.Add(stunnedState != null && CountAnyStateTransitionsToState(stateMachine, stunnedState) == 0, "Stunned has no Any State transition.");

            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                AnimationRoleSpec spec = RoleSpecs[i];
                if (!spec.IsAttack)
                {
                    continue;
                }

                AnimatorState state = FindState(stateMachine, spec.StateName);
                AnimationClip expectedClip = assets.GetClip(spec.Role);
                report.Add(state != null, $"Attack state {spec.StateName} exists.");
                report.Add(state != null && state.motion != null, $"Attack state {spec.StateName} has Motion.");
                report.Add(state != null && expectedClip != null && state.motion == expectedClip, $"Attack state {spec.StateName} uses downloaded clip {expectedClip?.name}.");
                report.Add(CountAnyStateTransitions(stateMachine, spec.StateName) == 1, $"Attack trigger {spec.StateName} has exactly one Any State transition.");
            }
        }

        private static void ValidatePlayerPrefab(IntegrationReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                report.Fail($"Player prefab missing: {PlayerPrefabPath}.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ValidatePlayerObject(contents, "Player prefab", report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ValidateScene(string scenePath, IntegrationReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                report.Warning($"Scene not found: {scenePath}.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == scenePath)
            {
                ValidatePlayerObject(FindScenePlayer(), scenePath, report);
                return;
            }

            report.Warning($"Scene validation for {scenePath} is best run while that scene is open; prefab validation still covers prefab instances.");
        }

        private static void ValidatePlayerObject(GameObject player, string label, IntegrationReport report)
        {
            if (player == null)
            {
                report.Fail($"Player object missing in {label}.");
                return;
            }

            Animator animator = player.GetComponentInChildren<Animator>(true);
            PlayerCombatController combatController = player.GetComponent<PlayerCombatController>();
            CombatAnimationEventRelay relay = animator != null ? animator.GetComponent<CombatAnimationEventRelay>() : null;
            Animation legacyAnimation = player.GetComponentInChildren<Animation>(true);
            PlayerVisualController visualController = player.GetComponentInChildren<PlayerVisualController>(true);
            bool combatAnimatorOnly = combatController != null && combatController.UsesCombatAnimatorOnlyDuringAttack;
            bool crouchAnimatorEnabled = combatController != null && combatController.UsesAnimatorWhileCrouching;
            bool idleAnimatorEnabled = combatController != null && combatController.UsesAnimatorWhileIdle;
            bool movingAnimatorEnabled = combatController != null && combatController.UsesAnimatorWhileMoving;
            bool jumpAnimatorEnabled = combatController != null && combatController.UsesAnimatorWhileJumping;

            report.Add(animator != null && animator.enabled, $"{label} Animator exists and remains enabled for continuous locomotion/combat.");
            report.Add(animator != null && animator.runtimeAnimatorController != null, $"{label} Animator Controller is assigned.");
            report.Add(animator != null && !animator.applyRootMotion, $"{label} Apply Root Motion is disabled.");
            report.Add(animator != null && animator.avatar != null && animator.avatar.isValid, $"{label} Animator Avatar is valid.");
            report.Add(animator != null && AssetDatabase.GetAssetPath(animator.avatar) == PlayerModelFbxPath, $"{label} Animator uses the Nam Avatar.");
            report.Add(combatController != null, $"{label} PlayerCombatController exists.");
            report.Add(combatController != null && animator != null && combatController.CombatAnimator == animator, $"{label} PlayerCombatController references the Animator.");
            report.Add(!combatAnimatorOnly, $"{label} Animator is not restricted to attacks.");
            report.Add(crouchAnimatorEnabled, $"{label} crouch/sit pose can use Animator.");
            report.Add(idleAnimatorEnabled, $"{label} standing idle uses Animator.");
            report.Add(movingAnimatorEnabled, $"{label} walking/running can use Animator clips.");
            report.Add(jumpAnimatorEnabled, $"{label} jump pose can use Animator.");
            report.Add(relay != null, $"{label} CombatAnimationEventRelay is attached to Animator object.");
            report.Add(legacyAnimation == null || !legacyAnimation.enabled, $"{label} obsolete Legacy Animation is absent or disabled.");
            report.Add(visualController == null || !visualController.UsesLegacyLocomotion, $"{label} PlayerVisualController legacy fallback is disabled.");
        }

        private static bool ControllerHasParameter(AnimatorController controller, string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountAnyStateTransitions(AnimatorStateMachine stateMachine, string triggerName)
        {
            int count = 0;
            AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (HasCondition(transitions[i], triggerName, AnimatorConditionMode.If))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAnyStateTransitionsToState(AnimatorStateMachine stateMachine, AnimatorState destination)
        {
            int count = 0;
            AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i].destinationState == destination)
                {
                    count++;
                }
            }

            return count;
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
            }

            Chapter1PlayerMotor motor = Object.FindAnyObjectByType<Chapter1PlayerMotor>();
            return motor != null ? motor.gameObject : null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static AnimationClip LoadFirstClip(string assetPath, string preferredName)
        {
            AnimationClip firstClip = null;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null)
                {
                    continue;
                }

                if (firstClip == null)
                {
                    firstClip = clip;
                }

                if (clip.name == preferredName)
                {
                    return clip;
                }
            }

            return firstClip;
        }

        private static Avatar LoadAvatar(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                Avatar avatar = assets[i] as Avatar;
                if (avatar != null)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static string GetRigDescription(string modelPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                return "missing importer";
            }

            Avatar avatar = LoadAvatar(modelPath);
            string avatarStatus = avatar != null ? $"avatar valid={avatar.isValid}, human={avatar.isHuman}" : "avatar missing";
            return $"{importer.animationType}, {avatarStatus}";
        }

        private static string GetControllerPath(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return "none";
            }

            return AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
        }

        private static string DescribeLegacyClips(Animation legacyAnimation)
        {
            if (legacyAnimation == null)
            {
                return "none";
            }

            List<string> names = new List<string>();
            foreach (AnimationState state in legacyAnimation)
            {
                if (state.clip != null && !names.Contains(state.clip.name))
                {
                    names.Add(state.clip.name);
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : "none";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "null";
            }

            List<string> names = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static string Normalize(string value)
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
                    buffer[index++] = c;
                }
            }

            return new string(buffer, 0, index);
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string fullPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static AnimationRoleSpec FindSpec(CombatAnimationRole role)
        {
            for (int i = 0; i < RoleSpecs.Length; i++)
            {
                if (RoleSpecs[i].Role == role)
                {
                    return RoleSpecs[i];
                }
            }

            return default;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetString(SerializedProperty element, string propertyName, string value)
        {
            SerializedProperty property = element.FindPropertyRelative(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetFloat(SerializedProperty element, string propertyName, float value)
        {
            SerializedProperty property = element.FindPropertyRelative(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static float GetFloat(SerializedProperty element, string propertyName)
        {
            SerializedProperty property = element.FindPropertyRelative(propertyName);
            return property != null ? property.floatValue : 0f;
        }

        private enum CombatAnimationRole
        {
            StandIdle,
            CombatIdle,
            Walk,
            Run,
            PunchLeft,
            PunchRight,
            Hook,
            RightHook,
            KickSide,
            KickHeavy,
            SpinningBackKick,
            SitDown,
            Jump,
            Stunned
        }

        private readonly struct AnimationRoleSpec
        {
            public AnimationRoleSpec(
                CombatAnimationRole role,
                string stateName,
                string displayName,
                bool loops,
                string[] keywords,
                float openWindowPercent,
                float hitPercent,
                float closeWindowPercent,
                float preferredDuration = 0f,
                float damage = 0f,
                float range = 0f,
                float radius = 0f,
                float recoveryPercent = 0.08f)
            {
                Role = role;
                StateName = stateName;
                DisplayName = displayName;
                Loops = loops;
                Keywords = keywords;
                OpenWindowPercent = openWindowPercent;
                HitPercent = hitPercent;
                CloseWindowPercent = closeWindowPercent;
                PreferredDuration = preferredDuration;
                Damage = damage;
                Range = range;
                Radius = radius;
                RecoveryPercent = recoveryPercent;
            }

            public CombatAnimationRole Role { get; }
            public string StateName { get; }
            public string DisplayName { get; }
            public bool Loops { get; }
            public string[] Keywords { get; }
            public float OpenWindowPercent { get; }
            public float HitPercent { get; }
            public float CloseWindowPercent { get; }
            public float PreferredDuration { get; }
            public float Damage { get; }
            public float Range { get; }
            public float Radius { get; }
            public float RecoveryPercent { get; }
            public bool IsHandCombo =>
                Role == CombatAnimationRole.PunchLeft
                || Role == CombatAnimationRole.PunchRight
                || Role == CombatAnimationRole.Hook
                || Role == CombatAnimationRole.RightHook;
            public bool IsKickAttack =>
                Role == CombatAnimationRole.KickSide
                || Role == CombatAnimationRole.KickHeavy
                || Role == CombatAnimationRole.SpinningBackKick;
            public bool IsAttack => IsHandCombo || IsKickAttack;
            public bool IsCrouchPose => Role == CombatAnimationRole.SitDown;
            public bool IsLocomotionPose =>
                Role == CombatAnimationRole.StandIdle
                || Role == CombatAnimationRole.CombatIdle
                || Role == CombatAnimationRole.Walk
                || Role == CombatAnimationRole.Run
                || Role == CombatAnimationRole.Jump
                || Role == CombatAnimationRole.SitDown;

            public bool Matches(string normalizedPath)
            {
                for (int i = 0; i < Keywords.Length; i++)
                {
                    if (normalizedPath.Contains(Keywords[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly struct Condition
        {
            private Condition(string parameter, AnimatorConditionMode mode, float threshold)
            {
                Parameter = parameter;
                Mode = mode;
                Threshold = threshold;
            }

            public string Parameter { get; }
            public AnimatorConditionMode Mode { get; }
            public float Threshold { get; }

            public static Condition If(string parameter) => new Condition(parameter, AnimatorConditionMode.If, 0f);
            public static Condition IfNot(string parameter) => new Condition(parameter, AnimatorConditionMode.IfNot, 0f);
            public static Condition Greater(string parameter, float threshold) => new Condition(parameter, AnimatorConditionMode.Greater, threshold);
            public static Condition Less(string parameter, float threshold) => new Condition(parameter, AnimatorConditionMode.Less, threshold);
        }

        private sealed class DownloadedCombatAssetSet
        {
            private readonly Dictionary<CombatAnimationRole, string> paths = new Dictionary<CombatAnimationRole, string>();
            private readonly Dictionary<CombatAnimationRole, AnimationClip> clips = new Dictionary<CombatAnimationRole, AnimationClip>();

            public void SetPath(CombatAnimationRole role, string path)
            {
                paths[role] = path;
            }

            public string GetPath(CombatAnimationRole role)
            {
                return paths.TryGetValue(role, out string path) ? path : null;
            }

            public AnimationClip GetClip(CombatAnimationRole role)
            {
                return clips.TryGetValue(role, out AnimationClip clip) ? clip : null;
            }

            public void LoadImportedClips(IntegrationReport report)
            {
                clips.Clear();
                for (int i = 0; i < RoleSpecs.Length; i++)
                {
                    AnimationRoleSpec spec = RoleSpecs[i];
                    string path = GetPath(spec.Role);
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    AnimationClip clip = LoadFirstClip(path, spec.StateName);
                    if (clip != null)
                    {
                        clips[spec.Role] = clip;
                        report.Pass($"Loaded clip {clip.name} for {spec.DisplayName}.");
                    }
                    else
                    {
                        report.Fail($"Could not load AnimationClip for {spec.DisplayName} from {path}.");
                    }
                }
            }

            public bool HasAllRequiredAssets(IntegrationReport report)
            {
                bool result = true;
                for (int i = 0; i < RoleSpecs.Length; i++)
                {
                    AnimationRoleSpec spec = RoleSpecs[i];
                    if (string.IsNullOrEmpty(GetPath(spec.Role)))
                    {
                        report.Fail($"Missing downloaded animation asset for {spec.DisplayName}.");
                        result = false;
                    }
                }

                return result;
            }

            public bool HasAllRequiredClips(IntegrationReport report)
            {
                bool result = true;
                for (int i = 0; i < RoleSpecs.Length; i++)
                {
                    AnimationRoleSpec spec = RoleSpecs[i];
                    if (GetClip(spec.Role) == null)
                    {
                        report.Fail($"Missing imported AnimationClip for {spec.DisplayName}.");
                        result = false;
                    }
                }

                return result;
            }
        }

        private sealed class IntegrationReport
        {
            private readonly List<string> lines = new List<string>();
            private int passCount;
            private int warningCount;
            private int failCount;

            public void Add(bool pass, string message)
            {
                if (pass)
                {
                    Pass(message);
                }
                else
                {
                    Fail(message);
                }
            }

            public void Pass(string message)
            {
                passCount++;
                lines.Add("[PASS] " + message);
            }

            public void Warning(string message)
            {
                warningCount++;
                lines.Add("[WARNING] " + message);
            }

            public void Fail(string message)
            {
                failCount++;
                lines.Add("[FAIL] " + message);
            }

            public void Print(string title)
            {
                string message = $"[{title}] PASS={passCount}, WARNING={warningCount}, FAIL={failCount}";
                for (int i = 0; i < lines.Count; i++)
                {
                    message += "\n" + lines[i];
                }

                if (failCount > 0)
                {
                    Debug.LogError(message);
                }
                else if (warningCount > 0)
                {
                    Debug.LogWarning(message);
                }
                else
                {
                    Debug.Log(message);
                }
            }
        }
    }
}
