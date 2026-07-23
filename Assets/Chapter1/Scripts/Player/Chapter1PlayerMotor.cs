using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Chapter1InputReader))]
    [RequireComponent(typeof(PlayerInputLock))]
    [RequireComponent(typeof(PlayerStamina))]
    public sealed class Chapter1PlayerMotor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerVisualController playerVisualController;
        [SerializeField] private CameraTarget cameraTarget;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedForce = -2f;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField] private LayerMask standUpBlockingMask = ~0;
        [SerializeField] private float standUpCheckPadding = 0.05f;

        private CharacterController characterController;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private PlayerStamina playerStamina;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool movementEnabled = true;
        private bool isPlayerHidden;
        private bool missingCameraLogged;

        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public float CurrentSpeed { get; private set; }
        public Vector3 Velocity { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputReader = GetComponent<Chapter1InputReader>();
            inputLock = GetComponent<PlayerInputLock>();
            playerStamina = GetComponent<PlayerStamina>();

            if (playerVisualController == null)
            {
                playerVisualController = GetComponentInChildren<PlayerVisualController>();
            }

            if (cameraTarget == null)
            {
                cameraTarget = GetComponentInChildren<CameraTarget>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            ApplyControllerHeight(false);
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.CrouchPressed += ToggleCrouch;
            }

            Chapter1EventBus.PlayerHiddenChanged += OnPlayerHiddenChanged;
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.CrouchPressed -= ToggleCrouch;
            }

            Chapter1EventBus.PlayerHiddenChanged -= OnPlayerHiddenChanged;
        }

        private void Update()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            if (!movementEnabled || inputLock.IsLocked)
            {
                StopHorizontalMovement();
                playerStamina.TickRegeneration(Time.deltaTime);
                MoveWithGravityOnly();
                return;
            }

            Vector2 moveInput = inputReader.MoveInput;
            Vector3 moveDirection = GetCameraRelativeMoveDirection(moveInput);
            IsMoving = moveDirection.sqrMagnitude > 0.0001f;
            IsSprinting = ShouldSprint(IsMoving);
            CurrentSpeed = GetCurrentMoveSpeed();

            if (IsSprinting)
            {
                playerStamina.ConsumeSprint(Time.deltaTime);
            }
            else
            {
                playerStamina.TickRegeneration(Time.deltaTime);
            }

            horizontalVelocity = moveDirection * CurrentSpeed;
            UpdateRotation(moveDirection);
            MoveCharacter();
            UpdateVisualState();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            bool wasEnabled = characterController != null && characterController.enabled;
            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(position, rotation);

            if (characterController != null)
            {
                characterController.enabled = wasEnabled;
            }

            verticalVelocity = 0f;
            ResetMovementState();
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!movementEnabled)
            {
                StopHorizontalMovement();
                UpdateVisualState();
            }
        }

        public void ForceCrouch(bool crouched)
        {
            SetCrouching(crouched);
        }

        public void ResetMovementState()
        {
            StopHorizontalMovement();
            IsSprinting = false;
            CurrentSpeed = 0f;
            Velocity = Vector3.zero;
            UpdateVisualState();
        }

        private bool HasRequiredReferences()
        {
            bool isValid = true;
            if (characterController == null)
            {
                Debug.LogError($"[Chapter1PlayerMotor] GameObject '{gameObject.name}' thiếu CharacterController.", this);
                isValid = false;
            }

            if (inputReader == null)
            {
                Debug.LogError($"[Chapter1PlayerMotor] GameObject '{gameObject.name}' thiếu Chapter1InputReader.", this);
                isValid = false;
            }

            if (inputLock == null)
            {
                Debug.LogError($"[Chapter1PlayerMotor] GameObject '{gameObject.name}' thiếu PlayerInputLock.", this);
                isValid = false;
            }

            if (playerStamina == null)
            {
                Debug.LogError($"[Chapter1PlayerMotor] GameObject '{gameObject.name}' thiếu PlayerStamina.", this);
                isValid = false;
            }

            if (cameraTransform == null && !missingCameraLogged)
            {
                missingCameraLogged = true;
                Debug.LogWarning($"[Chapter1PlayerMotor] GameObject '{gameObject.name}' chưa có cameraTransform. Player sẽ dùng hướng thế giới.", this);
            }

            return isValid;
        }

        private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(moveInput, 1f);
            if (clampedInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = forward * clampedInput.y + right * clampedInput.x;
            return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
        }

        private bool ShouldSprint(bool hasMoveInput)
        {
            return inputReader.SprintHeld
                && hasMoveInput
                && !IsCrouching
                && !inputLock.IsLocked
                && playerStamina.CanSprint
                && !isPlayerHidden;
        }

        private float GetCurrentMoveSpeed()
        {
            if (!IsMoving)
            {
                return 0f;
            }

            if (IsCrouching)
            {
                return crouchSpeed;
            }

            return IsSprinting ? sprintSpeed : walkSpeed;
        }

        private void UpdateRotation(Vector3 moveDirection)
        {
            if (!IsMoving)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void MoveCharacter()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(Velocity * Time.deltaTime);
        }

        private void MoveWithGravityOnly()
        {
            if (characterController == null)
            {
                return;
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Velocity = Vector3.up * verticalVelocity;
            characterController.Move(Velocity * Time.deltaTime);
        }

        private void ToggleCrouch()
        {
            if (!movementEnabled || inputLock != null && inputLock.IsLocked)
            {
                return;
            }

            SetCrouching(!IsCrouching);
        }

        private void SetCrouching(bool crouched)
        {
            if (IsCrouching == crouched)
            {
                return;
            }

            if (!crouched && !CanStandUp())
            {
                Debug.LogWarning($"[Chapter1PlayerMotor] GameObject '{gameObject.name}' chưa thể đứng lên vì có vật cản phía trên.", this);
                return;
            }

            IsCrouching = crouched;
            ApplyControllerHeight(IsCrouching);

            if (playerVisualController != null)
            {
                playerVisualController.SetCrouching(IsCrouching);
            }

            if (cameraTarget != null)
            {
                cameraTarget.SetCrouching(IsCrouching);
            }
        }

        private void ApplyControllerHeight(bool crouched)
        {
            if (characterController == null)
            {
                return;
            }

            float targetHeight = Mathf.Max(characterController.radius * 2f, crouched ? crouchingHeight : standingHeight);
            characterController.height = targetHeight;
            characterController.center = new Vector3(0f, targetHeight * 0.5f, 0f);
        }

        private bool CanStandUp()
        {
            int blockingMask = standUpBlockingMask.value & ~(1 << gameObject.layer);
            if (blockingMask == 0)
            {
                return true;
            }

            float radius = characterController != null ? characterController.radius : 0.3f;
            float safeRadius = Mathf.Max(0.05f, radius - standUpCheckPadding);
            float bottomY = safeRadius + standUpCheckPadding;
            float topY = Mathf.Max(bottomY, standingHeight - safeRadius - standUpCheckPadding);
            Vector3 bottom = transform.position + Vector3.up * bottomY;
            Vector3 top = transform.position + Vector3.up * topY;

            return !Physics.CheckCapsule(bottom, top, safeRadius, blockingMask, QueryTriggerInteraction.Ignore);
        }

        private void StopHorizontalMovement()
        {
            horizontalVelocity = Vector3.zero;
            IsMoving = false;
            IsSprinting = false;
            CurrentSpeed = 0f;
        }

        private void UpdateVisualState()
        {
            if (playerVisualController != null)
            {
                playerVisualController.SetMovementState(IsMoving, CurrentSpeed);
            }
        }

        private void OnPlayerHiddenChanged(bool hidden)
        {
            isPlayerHidden = hidden;
        }
    }
}
