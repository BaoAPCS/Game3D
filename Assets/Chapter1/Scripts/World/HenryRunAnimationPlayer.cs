using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryRunAnimationPlayer : MonoBehaviour
    {
        public const string RunClipName = "Henry_Run";
        public const string RunClipResourcePath = "Henry/Henry_Run_FastRun";

        private Animation legacyAnimation;
        private AnimationClip initialClip;
        private AnimationClip runClip;
        private bool configured;
        private bool missingAnimationLogged;
        private readonly List<PoseCorrection> poseCorrections =
            new List<PoseCorrection>();
        private bool poseCorrectionReady;

        private sealed class PoseCorrection
        {
            public Transform transform;
            public Quaternion initialRotation;
            public Quaternion runReferenceRotation;
            public Vector3 initialPosition;
            public Vector3 runReferencePosition;
            public Vector3 initialScale;
            public Vector3 runReferenceScale;
        }

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
            if (legacyAnimation.GetClip(RunClipName) == null)
            {
                legacyAnimation.AddClip(runClip, RunClipName);
            }

            AnimationState runState = legacyAnimation[RunClipName];
            if (runState == null)
            {
                LogMissingAnimation(
                    $"Không gắn được clip '{RunClipName}' cho Henry.");
                return false;
            }

            runState.wrapMode = WrapMode.Loop;
            runState.speed = 1f;
            runState.layer = 1;
            runState.blendMode = AnimationBlendMode.Blend;
            PreparePoseCorrection();
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
            PreparePoseCorrection();
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

        private void PreparePoseCorrection()
        {
            if (poseCorrectionReady ||
                legacyAnimation == null ||
                runClip == null)
            {
                return;
            }

            legacyAnimation.Stop();
            SampleInitialPose();

            poseCorrections.Clear();
            Transform animationRoot = legacyAnimation.transform;
            Transform[] transforms =
                animationRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate == animationRoot)
                {
                    continue;
                }

                poseCorrections.Add(new PoseCorrection
                {
                    transform = candidate,
                    initialRotation = candidate.localRotation,
                    initialPosition = candidate.localPosition,
                    initialScale = candidate.localScale
                });
            }

            runClip.SampleAnimation(
                legacyAnimation.gameObject,
                0f);
            for (int i = 0; i < poseCorrections.Count; i++)
            {
                PoseCorrection correction = poseCorrections[i];
                correction.runReferenceRotation =
                    correction.transform.localRotation;
                correction.runReferencePosition =
                    correction.transform.localPosition;
                correction.runReferenceScale =
                    correction.transform.localScale;
            }

            SampleInitialPose();
            if (initialClip != null)
            {
                legacyAnimation.clip = initialClip;
            }

            poseCorrectionReady = true;
        }

        private void LateUpdate()
        {
            if (!poseCorrectionReady || !IsRunPlaying)
            {
                return;
            }

            for (int i = 0; i < poseCorrections.Count; i++)
            {
                PoseCorrection correction = poseCorrections[i];
                Transform candidate = correction.transform;
                if (candidate == null)
                {
                    continue;
                }

                Quaternion relativeRotation =
                    Quaternion.Inverse(correction.runReferenceRotation) *
                    candidate.localRotation;
                candidate.localRotation =
                    correction.initialRotation * relativeRotation;

                Vector3 relativePosition =
                    candidate.localPosition - correction.runReferencePosition;
                candidate.localPosition =
                    correction.initialPosition + relativePosition;

                Vector3 relativeScale =
                    candidate.localScale - correction.runReferenceScale;
                candidate.localScale =
                    correction.initialScale + relativeScale;
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
