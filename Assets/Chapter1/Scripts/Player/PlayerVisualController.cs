using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class PlayerVisualController : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 standingLocalPosition = Vector3.zero;
        [SerializeField] private float crouchingYOffset = -0.35f;
        [SerializeField] private float smoothTime = 0.08f;
        [SerializeField] private bool enableWalkBob;
        [SerializeField] private float walkBobAmplitude = 0.025f;
        [SerializeField] private float walkBobFrequency = 7f;

        private Vector3 smoothVelocity;
        private bool isCrouching;
        private bool isMoving;
        private float currentSpeed;
        private float bobTimer;

        private void LateUpdate()
        {
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
        }
    }
}
