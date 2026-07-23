using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private float targetHeight = 1.4f;
        [SerializeField] private float distance = 4f;
        [SerializeField] private float minimumDistance = 1f;
        [SerializeField] private float maximumDistance = 6f;
        [SerializeField] private float horizontalSensitivity = 120f;
        [SerializeField] private float verticalSensitivity = 90f;
        [SerializeField] private float minimumPitch = -20f;
        [SerializeField] private float maximumPitch = 65f;
        [SerializeField] private float positionSmoothTime = 0.08f;
        [SerializeField] private float rotationSmoothSpeed = 18f;
        [SerializeField] private bool lockLookWhenInputLocked = true;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float cameraRadius = 0.2f;
        [SerializeField] private float collisionPadding = 0.08f;

        private Vector3 positionVelocity;
        private float yaw;
        private float pitch = 20f;
        private bool lookEnabled = true;
        private bool targetIsCameraTarget;
        private bool missingTargetLogged;
        private bool missingCameraLogged;

        public float Yaw => yaw;
        public float Pitch => pitch;

        private void Awake()
        {
            if (cameraPivot == null)
            {
                cameraPivot = transform;
            }

            if (controlledCamera == null)
            {
                controlledCamera = GetComponentInChildren<Camera>(true);
            }

            if (controlledCamera == null && Camera.main != null)
            {
                controlledCamera = Camera.main;
            }

            if (target != null)
            {
                targetIsCameraTarget = target.GetComponent<CameraTarget>() != null;
            }

            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
        }

        private void Start()
        {
            if (controlledCamera == null && !missingCameraLogged)
            {
                missingCameraLogged = true;
                Debug.LogWarning($"[ThirdPersonCameraRig] GameObject '{gameObject.name}' chưa có Camera được liên kết.", this);
            }
        }

        private void Update()
        {
            if (!lookEnabled)
            {
                return;
            }

            if (lockLookWhenInputLocked && inputLock != null && inputLock.IsLocked)
            {
                return;
            }

            Vector2 lookInput = inputReader != null ? inputReader.LookInput : Vector2.zero;
            if (lookInput.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            yaw += lookInput.x * horizontalSensitivity * Time.unscaledDeltaTime;
            pitch -= lookInput.y * verticalSensitivity * Time.unscaledDeltaTime;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (!missingTargetLogged)
                {
                    missingTargetLogged = true;
                    Debug.LogWarning($"[ThirdPersonCameraRig] GameObject '{gameObject.name}' chưa có target camera.", this);
                }

                return;
            }

            Vector3 focusPosition = GetFocusPosition();
            transform.position = Vector3.SmoothDamp(transform.position, focusPosition, ref positionVelocity, positionSmoothTime);

            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.rotation = Quaternion.Slerp(cameraPivot.rotation, targetRotation, rotationSmoothSpeed * Time.unscaledDeltaTime);
            }

            UpdateCameraDistance(cameraPivot != null ? cameraPivot.rotation : targetRotation);
        }

        private void OnValidate()
        {
            minimumDistance = Mathf.Max(0.1f, minimumDistance);
            maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
            distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            cameraRadius = Mathf.Max(0.01f, cameraRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            positionSmoothTime = Mathf.Max(0f, positionSmoothTime);
            rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
            maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetIsCameraTarget = target != null && target.GetComponent<CameraTarget>() != null;
            missingTargetLogged = false;
        }

        public void SetLookEnabled(bool enabled)
        {
            lookEnabled = enabled;
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void SnapBehindTarget()
        {
            if (target == null)
            {
                return;
            }

            yaw = target.eulerAngles.y;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            Vector3 focusPosition = GetFocusPosition();
            transform.position = focusPosition;
            positionVelocity = Vector3.zero;

            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.rotation = targetRotation;
            }

            UpdateCameraDistance(targetRotation);
        }

        public void ResetCameraState()
        {
            yaw = target != null ? target.eulerAngles.y : 0f;
            pitch = Mathf.Clamp(20f, minimumPitch, maximumPitch);
            positionVelocity = Vector3.zero;
            SnapBehindTarget();
        }

        private Vector3 GetFocusPosition()
        {
            if (targetIsCameraTarget)
            {
                return target.position;
            }

            return target.position + Vector3.up * targetHeight;
        }

        private void UpdateCameraDistance(Quaternion pivotRotation)
        {
            if (controlledCamera == null)
            {
                return;
            }

            float resolvedDistance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            Vector3 castDirection = pivotRotation * Vector3.back;

            if (collisionMask.value != 0 && Physics.SphereCast(transform.position, cameraRadius, castDirection, out RaycastHit hitInfo, resolvedDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                resolvedDistance = Mathf.Clamp(hitInfo.distance - collisionPadding, minimumDistance, maximumDistance);
            }

            controlledCamera.transform.localPosition = new Vector3(0f, 0f, -resolvedDistance);
            controlledCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
