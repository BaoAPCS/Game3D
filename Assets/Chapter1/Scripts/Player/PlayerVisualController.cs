using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class PlayerVisualController : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 standingLocalPosition = Vector3.zero;
        [SerializeField] private float crouchingYOffset;
        [SerializeField] private float smoothTime = 0.08f;
        [SerializeField] private bool enableWalkBob;
        [SerializeField] private float walkBobAmplitude = 0.025f;
        [SerializeField] private float walkBobFrequency = 7f;
        [SerializeField] private Animation legacyAnimation;
        [SerializeField] private Transform animatedModelRoot;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip runClip;
        [SerializeField] private float runSpeedThreshold = 4.75f;
        [SerializeField] private float animationFadeDuration = 0.12f;
        [SerializeField] private float walkReferenceSpeed = 3.5f;
        [SerializeField] private float runReferenceSpeed = 6f;

        private Vector3 smoothVelocity;
        private bool isCrouching;
        private bool isMoving;
        private float currentSpeed;
        private float bobTimer;
        private Vector3 animatedModelLocalPosition;
        private Quaternion animatedModelLocalRotation;
        private Vector3 animatedModelLocalScale;
        private bool hasAnimatedModelTransform;
        private bool missingAnimationWarningIssued;
        private LocomotionAnimationState currentAnimationState = LocomotionAnimationState.Uninitialized;
        private string walkStateName;
        private string runStateName;

        private enum LocomotionAnimationState
        {
            Uninitialized,
            Idle,
            Walk,
            Run
        }

        private void Awake()
        {
            InitializeLegacyAnimation();
            CacheAnimatedModelTransform();
            SetLocomotionAnimationState(LocomotionAnimationState.Idle);
        }

        private void LateUpdate()
        {
            RestoreAnimatedModelTransform();

            if (visualRoot == null)
            {
                return;
            }

            Vector3 targetPosition = standingLocalPosition;
            if (isCrouching)
            {
                targetPosition += Vector3.up * crouchingYOffset;
            }

            if (enableWalkBob && isMoving && currentSpeed > 0.01f)
            {
                bobTimer += Time.deltaTime * walkBobFrequency;
                targetPosition += Vector3.up * (Mathf.Sin(bobTimer) * walkBobAmplitude);
            }
            else
            {
                bobTimer = 0f;
            }

            visualRoot.localPosition = Vector3.SmoothDamp(visualRoot.localPosition, targetPosition, ref smoothVelocity, smoothTime);
        }

        public void SetCrouching(bool value)
        {
            isCrouching = value;
        }

        public void SetMovementState(bool moving, float speed)
        {
            isMoving = moving;
            currentSpeed = speed;

            LocomotionAnimationState targetState = LocomotionAnimationState.Idle;
            if (isMoving && currentSpeed > 0.01f)
            {
                targetState = currentSpeed >= runSpeedThreshold
                    ? LocomotionAnimationState.Run
                    : LocomotionAnimationState.Walk;
            }

            SetLocomotionAnimationState(targetState);
            UpdatePlaybackSpeed(targetState);
        }

        private void InitializeLegacyAnimation()
        {
            if (legacyAnimation == null)
            {
                legacyAnimation = GetComponentInChildren<Animation>(true);
            }

            if (legacyAnimation == null)
            {
                return;
            }

            legacyAnimation.playAutomatically = false;
            legacyAnimation.Stop();

            if (animatedModelRoot == null)
            {
                animatedModelRoot = legacyAnimation.transform;
            }

            if (walkClip == null)
            {
                walkClip = FindClip("walk 1");
            }

            if (runClip == null)
            {
                runClip = FindClip("run ") ?? FindClip("run");
            }

            walkStateName = RegisterClip(walkClip, "__PlayerVisualWalk");
            runStateName = RegisterClip(runClip, "__PlayerVisualRun");
        }

        private AnimationClip FindClip(string clipName)
        {
            AnimationClip clip = legacyAnimation.GetClip(clipName);
            if (clip != null)
            {
                return clip;
            }

            foreach (AnimationState state in legacyAnimation)
            {
                if (state.clip != null && state.clip.name == clipName)
                {
                    return state.clip;
                }
            }

            return null;
        }

        private string RegisterClip(AnimationClip clip, string fallbackStateName)
        {
            if (clip == null)
            {
                return null;
            }

            foreach (AnimationState state in legacyAnimation)
            {
                if (state.clip == clip)
                {
                    return state.name;
                }
            }

            legacyAnimation.AddClip(clip, fallbackStateName);
            return legacyAnimation.GetClip(fallbackStateName) == clip ? fallbackStateName : null;
        }

        private void SetLocomotionAnimationState(LocomotionAnimationState targetState)
        {
            if (currentAnimationState == targetState)
            {
                return;
            }

            currentAnimationState = targetState;

            if (targetState == LocomotionAnimationState.Idle)
            {
                StopAndRewindAnimation();
                return;
            }

            string stateName = targetState == LocomotionAnimationState.Run
                ? runStateName
                : walkStateName;

            if (legacyAnimation == null || string.IsNullOrEmpty(stateName))
            {
                WarnMissingAnimationOnce(targetState);
                StopAndRewindAnimation();
                return;
            }

            legacyAnimation.CrossFade(stateName, Mathf.Max(0f, animationFadeDuration));
        }

        private void UpdatePlaybackSpeed(LocomotionAnimationState state)
        {
            if (legacyAnimation == null)
            {
                return;
            }

            string stateName;
            float referenceSpeed;

            if (state == LocomotionAnimationState.Run)
            {
                stateName = runStateName;
                referenceSpeed = runReferenceSpeed;
            }
            else if (state == LocomotionAnimationState.Walk)
            {
                stateName = walkStateName;
                referenceSpeed = walkReferenceSpeed;
            }
            else
            {
                return;
            }

            AnimationState animationState = string.IsNullOrEmpty(stateName)
                ? null
                : legacyAnimation[stateName];
            if (animationState == null)
            {
                return;
            }

            animationState.speed = currentSpeed / Mathf.Max(0.01f, referenceSpeed);
        }

        private void StopAndRewindAnimation()
        {
            if (legacyAnimation == null)
            {
                return;
            }

            legacyAnimation.Stop();
            legacyAnimation.Rewind();
        }

        private void WarnMissingAnimationOnce(LocomotionAnimationState requestedState)
        {
            if (missingAnimationWarningIssued)
            {
                return;
            }

            missingAnimationWarningIssued = true;
            Debug.LogWarning(
                $"Player visual cannot play {requestedState.ToString().ToLowerInvariant()} animation because its Legacy Animation component or clip is missing. The model will remain static.",
                this);
        }

        private void CacheAnimatedModelTransform()
        {
            if (animatedModelRoot == null)
            {
                return;
            }

            animatedModelLocalPosition = animatedModelRoot.localPosition;
            animatedModelLocalRotation = animatedModelRoot.localRotation;
            animatedModelLocalScale = animatedModelRoot.localScale;
            hasAnimatedModelTransform = true;
        }

        private void RestoreAnimatedModelTransform()
        {
            if (!hasAnimatedModelTransform || animatedModelRoot == null)
            {
                return;
            }

            animatedModelRoot.localPosition = animatedModelLocalPosition;
            animatedModelRoot.localRotation = animatedModelLocalRotation;
            animatedModelRoot.localScale = animatedModelLocalScale;
        }
    }
}
