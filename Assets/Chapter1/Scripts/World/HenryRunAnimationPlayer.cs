using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public enum HenryCombatAttack
    {
        MmaKick = 0,
        RoundhouseKick = 1
    }

    [DisallowMultipleComponent]
    public sealed class HenryRunAnimationPlayer : MonoBehaviour
    {
        public const string IdleClipName = "Henry_Idle";
        public const string RunClipName = "Henry_Run";
        public const string RunClipResourcePath =
            "Henry/Henry_Run_FastRun";
        public const string MmaKickClipName = "Henry_Mma_Kick";
        public const string RoundhouseKickClipName =
            "Henry_Roundhouse_Kick";
        public const string MmaKickClipResourcePath =
            "Henry/Henry_Mma_Kick";
        public const string RoundhouseKickClipResourcePath =
            "Henry/Henry_Roundhouse_Kick";

        private Animation legacyAnimation;
        private AnimationClip initialClip;
        private AnimationClip runClip;
        private AnimationClip mmaKickClip;
        private AnimationClip roundhouseKickClip;
        private bool configured;
        private bool combatClipsConfigured;
        private bool missingAnimationLogged;

        public bool IsRunPlaying =>
            legacyAnimation != null &&
            legacyAnimation.IsPlaying(RunClipName);
        public bool IsIdlePlaying =>
            legacyAnimation != null &&
            legacyAnimation.IsPlaying(IdleClipName);

        public AnimationClip IdleClip => initialClip;
        public AnimationClip RunClip => runClip;
        public AnimationClip MmaKickClip => mmaKickClip;
        public AnimationClip RoundhouseKickClip => roundhouseKickClip;

        private void Awake()
        {
            Configure();
            PlayIdle();
        }

        private void Start()
        {
            if (!IsRunPlaying &&
                !IsCombatAttackPlaying(HenryCombatAttack.MmaKick) &&
                !IsCombatAttackPlaying(HenryCombatAttack.RoundhouseKick))
            {
                PlayIdle();
            }
        }

        public bool Configure()
        {
            if (configured)
            {
                return legacyAnimation != null &&
                       initialClip != null &&
                       runClip != null &&
                       legacyAnimation.GetClip(IdleClipName) != null &&
                       legacyAnimation.GetClip(RunClipName) != null;
            }

            configured = true;
            legacyAnimation = GetComponentInChildren<Animation>(true);
            if (legacyAnimation == null)
            {
                LogMissingAnimation(
                    "Henry không có Legacy Animation component.");
                return false;
            }

            initialClip = legacyAnimation.clip;
            if (initialClip == null)
            {
                LogMissingAnimation(
                    "Henry không có animation gốc để phát idle.");
                return false;
            }

            runClip = Resources.Load<AnimationClip>(
                RunClipResourcePath);
            if (runClip == null)
            {
                LogMissingAnimation(
                    $"Không tải được '{RunClipResourcePath}' từ Resources.");
                return false;
            }

            legacyAnimation.playAutomatically = false;
            if (legacyAnimation.GetClip(IdleClipName) != null)
            {
                legacyAnimation.RemoveClip(IdleClipName);
            }

            legacyAnimation.AddClip(initialClip, IdleClipName);
            AnimationState idleState = legacyAnimation[IdleClipName];
            if (idleState == null)
            {
                LogMissingAnimation(
                    $"Không gắn được clip '{IdleClipName}' cho Henry.");
                return false;
            }

            idleState.wrapMode = WrapMode.Loop;
            idleState.speed = 1f;
            idleState.layer = 0;
            idleState.weight = 1f;
            idleState.blendMode = AnimationBlendMode.Blend;

            if (legacyAnimation.GetClip(RunClipName) != null)
            {
                legacyAnimation.RemoveClip(RunClipName);
            }

            legacyAnimation.AddClip(runClip, RunClipName);
            AnimationState runState = legacyAnimation[RunClipName];
            if (runState == null)
            {
                LogMissingAnimation(
                    $"Không gắn được clip '{RunClipName}' cho Henry.");
                return false;
            }

            runState.wrapMode = WrapMode.Loop;
            runState.speed = 1f;
            runState.layer = 0;
            runState.weight = 1f;
            runState.blendMode = AnimationBlendMode.Blend;
            return true;
        }

        public bool PlayIdle()
        {
            if (!Configure())
            {
                return false;
            }

            if (IsIdlePlaying)
            {
                return true;
            }

            legacyAnimation.Stop();
            bool started = legacyAnimation.Play(
                IdleClipName,
                PlayMode.StopAll);
            if (!started)
            {
                Debug.LogError(
                    $"[Henry] Không thể phát animation '{IdleClipName}'.",
                    this);
            }

            return started;
        }

        public bool PlayRun()
        {
            if (!Configure())
            {
                return false;
            }

            legacyAnimation.Stop();
            SampleInitialPose();
            bool started = legacyAnimation.Play(
                RunClipName,
                PlayMode.StopAll);
            if (!started)
            {
                Debug.LogError(
                    $"[Henry] Không thể phát animation '{RunClipName}'.",
                    this);
            }

            return started;
        }

        public bool ConfigureCombatClips()
        {
            if (combatClipsConfigured)
            {
                return legacyAnimation != null &&
                       mmaKickClip != null &&
                       roundhouseKickClip != null &&
                       legacyAnimation.GetClip(MmaKickClipName) != null &&
                       legacyAnimation.GetClip(RoundhouseKickClipName) != null;
            }

            if (!Configure())
            {
                return false;
            }

            mmaKickClip = Resources.Load<AnimationClip>(
                MmaKickClipResourcePath);
            roundhouseKickClip = Resources.Load<AnimationClip>(
                RoundhouseKickClipResourcePath);
            if (mmaKickClip == null || roundhouseKickClip == null)
            {
                LogMissingAnimation(
                    "Không tải được hai animation đá của Henry trong Resources/Henry.");
                return false;
            }

            bool mmaReady = ConfigureCombatClip(
                mmaKickClip,
                MmaKickClipName);
            bool roundhouseReady = ConfigureCombatClip(
                roundhouseKickClip,
                RoundhouseKickClipName);
            combatClipsConfigured = mmaReady && roundhouseReady;
            return combatClipsConfigured;
        }

        public bool TryPlayCombatAttack(
            HenryCombatAttack attack,
            out AnimationState state)
        {
            state = null;
            if (!ConfigureCombatClips())
            {
                return false;
            }

            string clipName = GetCombatClipName(attack);
            state = legacyAnimation[clipName];
            if (state == null)
            {
                return false;
            }

            legacyAnimation.Stop();
            SampleInitialPose();
            state.time = 0f;
            state.speed = 1f;
            bool started = legacyAnimation.Play(
                clipName,
                PlayMode.StopAll);
            if (!started)
            {
                Debug.LogError(
                    $"[Henry] Không thể phát animation '{clipName}'.",
                    this);
            }

            return started;
        }

        public bool IsCombatAttackPlaying(HenryCombatAttack attack)
        {
            return legacyAnimation != null &&
                   legacyAnimation.IsPlaying(GetCombatClipName(attack));
        }

        public float GetCombatAttackNormalizedTime(
            HenryCombatAttack attack)
        {
            if (legacyAnimation == null)
            {
                return 0f;
            }

            AnimationState state =
                legacyAnimation[GetCombatClipName(attack)];
            return state != null ? state.normalizedTime : 0f;
        }

        public void StopAtInitialPose()
        {
            if (!Configure() || legacyAnimation == null)
            {
                return;
            }

            legacyAnimation.Stop();
            legacyAnimation.playAutomatically = false;
            SampleInitialPose();
            if (initialClip != null)
            {
                legacyAnimation.clip = initialClip;
            }
        }

        private void SampleInitialPose()
        {
            if (initialClip != null && legacyAnimation != null)
            {
                initialClip.SampleAnimation(
                    legacyAnimation.gameObject,
                    0f);
            }
        }

        private bool ConfigureCombatClip(
            AnimationClip clip,
            string clipName)
        {
            if (legacyAnimation.GetClip(clipName) != null)
            {
                legacyAnimation.RemoveClip(clipName);
            }

            legacyAnimation.AddClip(clip, clipName);
            AnimationState state = legacyAnimation[clipName];
            if (state == null)
            {
                LogMissingAnimation(
                    $"Không gắn được clip '{clipName}' cho Henry.");
                return false;
            }

            state.wrapMode = WrapMode.Once;
            state.speed = 1f;
            state.layer = 0;
            state.weight = 1f;
            state.blendMode = AnimationBlendMode.Blend;
            return true;
        }

        private static string GetCombatClipName(
            HenryCombatAttack attack)
        {
            return attack == HenryCombatAttack.RoundhouseKick
                ? RoundhouseKickClipName
                : MmaKickClipName;
        }

        private void LogMissingAnimation(string message)
        {
            if (missingAnimationLogged)
            {
                return;
            }

            missingAnimationLogged = true;
            Debug.LogError($"[Henry] {message}", this);
        }
    }
}
