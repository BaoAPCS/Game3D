using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryRunAnimationPlayer : MonoBehaviour
    {
        public const string IdleClipName = "Henry_Idle";
        public const string RunClipName = "Henry_Run";
        public const string RunClipResourcePath =
            "Henry/Henry_Run_FastRun";

        private Animation legacyAnimation;
        private AnimationClip initialClip;
        private AnimationClip runClip;
        private bool configured;
        private bool missingAnimationLogged;

        public bool IsRunPlaying =>
            legacyAnimation != null &&
            legacyAnimation.IsPlaying(RunClipName);
        public bool IsIdlePlaying =>
            legacyAnimation != null &&
            legacyAnimation.IsPlaying(IdleClipName);

        public AnimationClip IdleClip => initialClip;
        public AnimationClip RunClip => runClip;

        private void Awake()
        {
            Configure();
            PlayIdle();
        }

        private void Start()
        {
            if (!IsRunPlaying)
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
