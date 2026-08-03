using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryRunAnimationPlayer : MonoBehaviour
    {
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

        public AnimationClip RunClip => runClip;

        private void Awake()
        {
            Configure();
            StopAtInitialPose();
        }

        private void Start()
        {
            StopAtInitialPose();
        }

        public bool Configure()
        {
            if (configured)
            {
                return legacyAnimation != null && runClip != null;
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
            runClip = Resources.Load<AnimationClip>(
                RunClipResourcePath);
            if (runClip == null)
            {
                LogMissingAnimation(
                    $"Không tải được '{RunClipResourcePath}' từ Resources.");
                return false;
            }

            legacyAnimation.playAutomatically = false;
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
