using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class CameraTarget : MonoBehaviour
    {
        [SerializeField] private Chapter1PlayerMotor playerMotor;
        [SerializeField] private Vector3 standingLocalOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private Vector3 crouchingLocalOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField] private float smoothTime = 0.08f;

        private Vector3 smoothVelocity;
        private bool isCrouching;

        private void Awake()
        {
            if (playerMotor == null)
            {
                playerMotor = GetComponentInParent<Chapter1PlayerMotor>();
            }

            transform.localPosition = standingLocalOffset;
        }

        private void LateUpdate()
        {
            if (playerMotor != null)
            {
                isCrouching = playerMotor.IsCrouching;
            }

            Vector3 targetOffset = isCrouching ? crouchingLocalOffset : standingLocalOffset;
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetOffset, ref smoothVelocity, smoothTime);
        }

        public void SetCrouching(bool value)
        {
            isCrouching = value;
        }
    }
}
