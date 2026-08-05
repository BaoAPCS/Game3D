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
        [SerializeField, Min(0.1f)] private float acceleration = 22f;
        [SerializeField, Min(0.1f)] private float deceleration = 28f;
        [SerializeField, Min(0f)] private float stopSnapSpeed = 0.08f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedForce = -2f;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField, Min(0.01f)] private float crouchHeightSmoothTime = 0.08f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.25f;
        [SerializeField, Range(0f, 0.3f)] private float jumpInputBufferTime = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.1f;
        [SerializeField, Range(0f, 1f)] private float airborneControlMultiplier = 0.85f;
        [SerializeField] private float terminalVelocity = -35f;
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
        private bool combatMovementModifierActive;
        private bool combatLocksMovement;
        private float combatMoveSpeedMultiplier = 1f;
        private float targetControllerHeight;
        private float controllerHeightVelocity;
        private float lastGroundedTime;
        private float jumpInputExpireTime;

        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsJumping { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float VerticalVelocity => verticalVelocity;
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

            targetControllerHeight = GetControllerHeight(false);
            ApplyControllerHeight(targetControllerHeight);
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.CrouchPressed += ToggleCrouch;
                inputReader.JumpPressed += QueueJump;
            }

            Chapter1EventBus.PlayerHiddenChanged += OnPlayerHiddenChanged;
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.CrouchPressed -= ToggleCrouch;
                inputReader.JumpPressed -= QueueJump;
            }

            Chapter1EventBus.PlayerHiddenChanged -= OnPlayerHiddenChanged;
        }

        private void OnValidate()
        {
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            stopSnapSpeed = Mathf.Max(0f, stopSnapSpeed);
            crouchHeightSmoothTime = Mathf.Max(0.01f, crouchHeightSmoothTime);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            jumpInputBufferTime = Mathf.Clamp(jumpInputBufferTime, 0f, 0.3f);
            coyoteTime = Mathf.Clamp(coyoteTime, 0f, 0.3f);
            airborneControlMultiplier = Mathf.Clamp01(airborneControlMultiplier);
        }

        private void Update()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            UpdateGroundedState();
            if (!movementEnabled || inputLock.IsLocked || IsCombatMovementLocked())
            {
                StopHorizontalMovement();
                UpdateVisualState();
                UpdateControllerHeight(Time.deltaTime);
                playerStamina.TickRegeneration(Time.deltaTime);
                MoveWithGravityOnly();
                return;
            }

            Vector2 moveInput = inputReader.MoveInput;
            Vector3 moveDirection = GetCameraRelativeMoveDirection(moveInput);
            bool hasMoveInput = moveDirection.sqrMagnitude > 0.0001f;
            IsSprinting = ShouldSprint(hasMoveInput);
            float targetSpeed = GetCurrentMoveSpeed(hasMoveInput);
            ProcessBufferedJump();

            if (IsSprinting)
            {
                playerStamina.ConsumeSprint(Time.deltaTime);
            }
            else
            {
                playerStamina.TickRegeneration(Time.deltaTime);
            }

            UpdateHorizontalVelocity(moveDirection, targetSpeed, Time.deltaTime);
            IsMoving = horizontalVelocity.sqrMagnitude > stopSnapSpeed * stopSnapSpeed;
            CurrentSpeed = IsMoving ? horizontalVelocity.magnitude : 0f;
            UpdateRotation(moveDirection);
            UpdateControllerHeight(Time.deltaTime);
            MoveCharacter();
            UpdateGroundedState();
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
            IsJumping = false;
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

        public void SetCombatMovementModifier(bool active, float speedMultiplier, bool lockMovement)
        {
            combatMovementModifierActive = active;
            combatLocksMovement = active && lockMovement;
            combatMoveSpeedMultiplier = active ? Mathf.Clamp01(speedMultiplier) : 1f;

            if (combatLocksMovement)
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
            jumpInputExpireTime = 0f;
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
                && !combatMovementModifierActive
                && playerStamina.CanSprint
                && !isPlayerHidden;
        }

        private float GetCurrentMoveSpeed(bool hasMoveInput)
        {
            if (!hasMoveInput)
            {
                return 0f;
            }

            float speed;
            if (IsCrouching)
            {
                speed = crouchSpeed;
            }
            else
            {
                speed = IsSprinting ? sprintSpeed : walkSpeed;
            }

            speed = combatMovementModifierActive ? speed * combatMoveSpeedMultiplier : speed;
            return IsGrounded ? speed : speed * airborneControlMultiplier;
        }

        private void QueueJump()
        {
            jumpInputExpireTime = Time.time + jumpInputBufferTime;
        }

        private void ProcessBufferedJump()
        {
            if (jumpInputExpireTime <= 0f || Time.time > jumpInputExpireTime)
            {
                jumpInputExpireTime = 0f;
                return;
            }

            if (!CanJumpNow())
            {
                return;
            }

            jumpInputExpireTime = 0f;
            IsJumping = true;
            IsGrounded = false;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        private bool CanJumpNow()
        {
            if (!movementEnabled
                || inputLock == null
                || inputLock.IsLocked
                || IsCrouching
                || combatMovementModifierActive
                || isPlayerHidden)
            {
                return false;
            }

            return Time.time <= lastGroundedTime + coyoteTime;
        }

        private void UpdateGroundedState()
        {
            bool grounded = characterController != null && characterController.isGrounded;
            IsGrounded = grounded;
            if (!grounded)
            {
                return;
            }

            lastGroundedTime = Time.time;
            if (verticalVelocity <= 0f)
            {
                IsJumping = false;
            }
        }

        private void UpdateHorizontalVelocity(Vector3 moveDirection, float targetSpeed, float deltaTime)
        {
            Vector3 targetVelocity = moveDirection * targetSpeed;
            float rate = targetVelocity.sqrMagnitude > horizontalVelocity.sqrMagnitude
                ? acceleration
                : deceleration;

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * deltaTime);
            if (targetVelocity.sqrMagnitude <= 0.0001f && horizontalVelocity.sqrMagnitude <= stopSnapSpeed * stopSnapSpeed)
            {
                horizontalVelocity = Vector3.zero;
            }
        }

        private bool IsCombatMovementLocked()
        {
            return combatMovementModifierActive && combatLocksMovement;
        }

        private void UpdateRotation(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void MoveCharacter()
        {
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity);
            Velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(Velocity * Time.deltaTime);
        }

        private void MoveWithGravityOnly()
        {
            if (characterController == null)
            {
                return;
            }

            UpdateGroundedState();
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity);
            Velocity = Vector3.up * verticalVelocity;
            characterController.Move(Velocity * Time.deltaTime);
            UpdateGroundedState();
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
            targetControllerHeight = GetControllerHeight(IsCrouching);
            if (IsCrouching)
            {
                jumpInputExpireTime = 0f;
            }

            if (playerVisualController != null)
            {
                playerVisualController.SetCrouching(IsCrouching);
            }

            if (cameraTarget != null)
            {
                cameraTarget.SetCrouching(IsCrouching);
            }
        }

        private float GetControllerHeight(bool crouched)
        {
            return Mathf.Max(characterController != null ? characterController.radius * 2f : 0.01f, crouched ? crouchingHeight : standingHeight);
        }

        private void UpdateControllerHeight(float deltaTime)
        {
            if (characterController == null)
            {
                return;
            }

            if (targetControllerHeight <= 0f)
            {
                targetControllerHeight = GetControllerHeight(IsCrouching);
            }

            float currentHeight = characterController.height;
            float nextHeight = Mathf.SmoothDamp(
                currentHeight,
                targetControllerHeight,
                ref controllerHeightVelocity,
                crouchHeightSmoothTime,
                Mathf.Infinity,
                deltaTime);

            if (Mathf.Abs(nextHeight - targetControllerHeight) <= 0.005f)
            {
                nextHeight = targetControllerHeight;
                controllerHeightVelocity = 0f;
            }

            ApplyControllerHeight(nextHeight);
        }

        private void ApplyControllerHeight(float height)
        {
            if (characterController == null)
            {
                return;
            }

            float safeHeight = Mathf.Max(characterController.radius * 2f, height);
            characterController.height = safeHeight;
            characterController.center = new Vector3(0f, safeHeight * 0.5f, 0f);
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
