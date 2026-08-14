using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Chapter1InputReader))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        private const int HitBufferSize = 32;
        private const string WalkStateName = "Walk";
        private const string RunStateName = "Run";
        private const string ForcedStunInputLockReason = "ForcedStun";
        private static readonly int MoveSpeedParameter = Animator.StringToHash("MoveSpeed");
        private static readonly int IsGroundedParameter = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingParameter = Animator.StringToHash("IsSprinting");
        private static readonly int IsCrouchingParameter = Animator.StringToHash("IsCrouching");
        private static readonly int IsJumpingParameter = Animator.StringToHash("IsJumping");
        private static readonly int IsAttackingParameter = Animator.StringToHash("IsAttacking");
        private static readonly int VerticalSpeedParameter = Animator.StringToHash("VerticalSpeed");
        private static readonly int JumpParameter = Animator.StringToHash("Jump");
        private static readonly int WalkStateHash = Animator.StringToHash("Base Layer.Walk");
        private static readonly int RunStateHash = Animator.StringToHash("Base Layer.Run");
        private static readonly int StunnedStateHash = Animator.StringToHash("Base Layer.Stunned");

        [Header("References")]
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private Chapter1PlayerMotor playerMotor;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private PlayerVisualController playerVisualController;
        [SerializeField] private Animation legacyAnimationToPause;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private Transform proceduralAnimationRoot;

        [Header("Combo")]
        [SerializeField] private List<ComboAttack> comboAttacks = new List<ComboAttack>();
        [SerializeField, Min(0f)] private float comboResetDelay = 1f;
        [SerializeField, Range(0f, 0.3f)] private float postAttackInputBufferTime = 0.14f;

        [Header("Kicks")]
        [SerializeField] private ComboAttack neutralKickAttack;
        [SerializeField] private ComboAttack forwardKickAttack;
        [SerializeField] private ComboAttack backwardKickAttack;
        [SerializeField, Range(0.05f, 1f)] private float directionalKickThreshold = 0.35f;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Movement")]
        [SerializeField] private bool lockMovementDuringAttack;
        [SerializeField, Range(0f, 1f)] private float attackMoveSpeedMultiplier = 0.6f;
        [SerializeField] private bool allowCombatWhileCrouching;

        [Header("Animation")]
        [SerializeField] private bool enableAnimatorOnlyDuringAttack = true;
        [SerializeField] private bool enableAnimatorWhileCrouching = true;
        [SerializeField] private bool enableAnimatorWhileIdle = false;
        [SerializeField] private bool enableAnimatorWhileMoving = true;
        [SerializeField] private bool enableAnimatorWhileJumping = true;
        [SerializeField] private bool suspendLegacyAnimationDuringAttack = true;
        [SerializeField, Range(0f, 0.3f)] private float animatorReleaseDelay = 0.08f;
        [SerializeField, Range(0f, 0.35f)] private float locomotionBlendDuration = 0.14f;
        [SerializeField, Range(0f, 0.3f)] private float moveSpeedDampTime = 0.1f;
        [SerializeField, Min(0f)] private float runStateSpeedThreshold = 4.75f;
        [SerializeField] private bool playProceduralFallback = true;
        [SerializeField, Range(0f, 1f)] private float proceduralFallbackStrength = 1f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private bool logAttackDebug;

        private readonly Collider[] overlapBuffer = new Collider[HitBufferSize];
        private readonly RaycastHit[] sphereCastBuffer = new RaycastHit[HitBufferSize];
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        private readonly HashSet<string> missingTriggerWarnings = new HashSet<string>(StringComparer.Ordinal);

        private Coroutine attackTimelineRoutine;
        private Coroutine recoveryRoutine;
        private Coroutine comboResetRoutine;
        private Coroutine proceduralAnimationRoutine;
        private Coroutine animatorReleaseRoutine;
        private Vector3 proceduralBaseLocalPosition;
        private Quaternion proceduralBaseLocalRotation;
        private Vector3 proceduralBaseLocalScale;
        private bool hasProceduralBasePose;
        private CharacterController characterController;
        private int attackSequence;
        private int currentAttackIndex = -1;
        private int nextAttackIndex;
        private float currentAttackElapsed;
        private ComboAttack currentAttack;
        private int currentFallbackPoseIndex;
        private bool isAttackActive;
        private bool currentAttackUsesHandCombo;
        private bool legacyAnimationSuspended;
        private bool animationEnded;
        private bool comboWindowOpen;
        private bool bufferedNextAttack;
        private bool hitPerformed;
        private bool wasJumpingForAnimator;
        private bool queuedHandAttackAfterAttack;
        private ComboAttack queuedKickAttackAfterAttack;
        private int queuedKickFallbackPoseIndex;
        private float queuedInputExpireTime;
        private int requestedMovementStateHash;
        private bool isForcedStunned;

        public bool IsAttacking => isAttackActive;
        public bool IsForcedStunned => isForcedStunned;
        public int CurrentAttackIndex => currentAttackIndex;
        public Transform AttackPoint => attackPoint;
        public LayerMask EnemyLayerMask => enemyLayerMask;
        public Animator CombatAnimator => animator;
        public Animation LegacyAnimationToPause => legacyAnimationToPause;
        public bool UsesCombatAnimatorOnlyDuringAttack => enableAnimatorOnlyDuringAttack;
        public bool SuspendsLegacyAnimationDuringAttack => suspendLegacyAnimationDuringAttack;
        public bool UsesAnimatorWhileCrouching => enableAnimatorWhileCrouching;
        public bool UsesAnimatorWhileIdle => enableAnimatorWhileIdle;
        public bool UsesAnimatorWhileMoving => enableAnimatorWhileMoving;
        public bool UsesAnimatorWhileJumping => enableAnimatorWhileJumping;
        public float ComboResetDelay => comboResetDelay;
        public IReadOnlyList<ComboAttack> ComboAttacks => comboAttacks;

        private void Reset()
        {
            ResolveReferences();
            EnsureDefaultCombo();
            EnsureDefaultKicks();
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                enemyLayerMask = 1 << enemyLayer;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureDefaultCombo();
            EnsureDefaultKicks();
            UpdateAnimatorActivity(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (!isAttackActive)
            {
                UpdateAnimatorActivity(false);
            }

            if (inputReader != null)
            {
                inputReader.AttackPressed += HandleAttackPressed;
                inputReader.KickPressed += HandleKickPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.AttackPressed -= HandleAttackPressed;
                inputReader.KickPressed -= HandleKickPressed;
            }

            StopAllCombatRoutines();
            ClearAttackState();
            ApplyCombatMovement(false);
            SetAnimatorBoolIfPresent(IsAttackingParameter, "IsAttacking", false);
            SetAnimatorBoolIfPresent(IsJumpingParameter, "IsJumping", false);
            wasJumpingForAnimator = false;
            UpdateAnimatorActivity(true);
            SetLegacyAnimationSuspended(false);
            ReleaseForcedStun();
        }

        private void Update()
        {
            if (isForcedStunned)
            {
                return;
            }

            UpdateAnimatorActivity(false);
            UpdateAnimatorMovementParameters();
        }

        private void OnValidate()
        {
            comboResetDelay = Mathf.Max(0f, comboResetDelay);
            postAttackInputBufferTime = Mathf.Clamp(postAttackInputBufferTime, 0f, 0.3f);
            attackMoveSpeedMultiplier = Mathf.Clamp01(attackMoveSpeedMultiplier);
            animatorReleaseDelay = Mathf.Clamp(animatorReleaseDelay, 0f, 0.3f);
            locomotionBlendDuration = Mathf.Clamp(locomotionBlendDuration, 0f, 0.35f);
            moveSpeedDampTime = Mathf.Clamp(moveSpeedDampTime, 0f, 0.3f);
            runStateSpeedThreshold = Mathf.Max(0f, runStateSpeedThreshold);
            proceduralFallbackStrength = Mathf.Clamp01(proceduralFallbackStrength);
            directionalKickThreshold = Mathf.Clamp(directionalKickThreshold, 0.05f, 1f);
            for (int i = 0; i < comboAttacks.Count; i++)
            {
                comboAttacks[i]?.Sanitize();
            }
        }

        public void OpenComboWindow()
        {
            if (isAttackActive)
            {
                comboWindowOpen = true;
            }
        }

        public void CloseComboWindow()
        {
            comboWindowOpen = false;
        }

        public void EnableAttackHitbox()
        {
            PerformAttackHit();
        }

        public void PerformAttackHit()
        {
            if (!isAttackActive || hitPerformed || !TryGetCurrentAttack(out ComboAttack attack))
            {
                return;
            }

            hitPerformed = true;
            damagedTargets.Clear();

            ResolveAttackPose(out Vector3 origin, out Vector3 direction);
            float radius = Mathf.Max(0.01f, attack.attackRadius);
            float range = Mathf.Max(0f, attack.attackRange);

            int overlapCount = Physics.OverlapSphereNonAlloc(
                origin,
                radius,
                overlapBuffer,
                enemyLayerMask,
                triggerInteraction);
            for (int i = 0; i < overlapCount; i++)
            {
                TryDamageCollider(overlapBuffer[i], attack);
            }

            if (range > 0f)
            {
                int castCount = Physics.SphereCastNonAlloc(
                    origin,
                    radius,
                    direction,
                    sphereCastBuffer,
                    range,
                    enemyLayerMask,
                    triggerInteraction);
                for (int i = 0; i < castCount; i++)
                {
                    TryDamageCollider(sphereCastBuffer[i].collider, attack);
                }
            }

            if (logAttackDebug)
            {
                Debug.Log(
                    $"[PlayerCombatController] {attack.attackName} hit {damagedTargets.Count} target(s).",
                    this);
            }
        }

        public void EndAttack()
        {
            EndAttackInternal(true);
        }

        public void ResetComboProgress()
        {
            if (isAttackActive)
            {
                return;
            }

            nextAttackIndex = 0;
            currentAttackIndex = -1;
            bufferedNextAttack = false;
            StopComboResetRoutine();
        }

        public void EnsureDefaultComboIfEmpty()
        {
            EnsureDefaultCombo();
            EnsureDefaultKicks();
        }

        /// <summary>
        /// Cancels every active player attack, locks movement/input, and plays
        /// the terminal Stunned state. The caller owns the later game-over flow.
        /// </summary>
        public bool EnterForcedStun(float transitionDuration = 0.08f)
        {
            if (isForcedStunned)
            {
                return true;
            }

            ResolveReferences();

            // Validate the animation before changing gameplay state. If the
            // controller reference is ever lost, Nam must not become locked in
            // place without actually entering the Stunned animation.
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning(
                    "[PlayerCombatController] Không tìm thấy Animator để phát Stunned.",
                    this);
                return false;
            }

            if (!animator.HasState(0, StunnedStateHash))
            {
                Debug.LogWarning(
                    "[PlayerCombatController] Animator Controller không có state 'Base Layer.Stunned'.",
                    this);
                return false;
            }

            StopAllCombatRoutines();
            attackSequence++;
            ClearAttackState();
            ApplyCombatMovement(false);
            isForcedStunned = true;

            playerMotor?.SetMovementEnabled(false);
            inputLock?.Lock(ForcedStunInputLockReason);
            SetLegacyAnimationSuspended(true);

            animator.keepAnimatorStateOnDisable = true;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.CrossFadeInFixedTime(
                StunnedStateHash,
                Mathf.Max(0f, transitionDuration),
                0,
                0f);
            animator.Update(0f);
            return true;
        }

        public void ReleaseForcedStun()
        {
            if (!isForcedStunned)
            {
                return;
            }

            isForcedStunned = false;
            inputLock?.Unlock(ForcedStunInputLockReason);
            playerMotor?.SetMovementEnabled(true);
            SetLegacyAnimationSuspended(false);
            UpdateAnimatorActivity(false);
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<Chapter1InputReader>();
            }

            if (playerMotor == null)
            {
                playerMotor = GetComponent<Chapter1PlayerMotor>();
            }

            if (inputLock == null)
            {
                inputLock = GetComponent<PlayerInputLock>();
            }

            if (playerVisualController == null)
            {
                playerVisualController = GetComponentInChildren<PlayerVisualController>(true);
            }

            if (legacyAnimationToPause == null)
            {
                legacyAnimationToPause = playerVisualController != null
                    ? playerVisualController.LegacyAnimation
                    : GetComponentInChildren<Animation>(true);
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (proceduralAnimationRoot == null)
            {
                proceduralAnimationRoot = FindChildRecursive(transform, "ModelAnchor")
                    ?? FindChildRecursive(transform, "Visual");
            }
        }

        private void HandleAttackPressed()
        {
            if (comboAttacks.Count == 0)
            {
                return;
            }

            if (isAttackActive)
            {
                if (currentAttackUsesHandCombo)
                {
                    BufferNextAttackIfAllowed();
                }

                QueuePostAttackHandInputIfAllowed();
                return;
            }

            if (!CanStartCombat())
            {
                return;
            }

            StopComboResetRoutine();
            nextAttackIndex = Mathf.Clamp(nextAttackIndex, 0, comboAttacks.Count - 1);
            StartHandAttack(nextAttackIndex);
        }

        private void HandleKickPressed()
        {
            if (isAttackActive)
            {
                QueuePostAttackKickInputIfAllowed();
                return;
            }

            if (!CanStartCombat())
            {
                return;
            }

            StopComboResetRoutine();
            ComboAttack selectedKick = SelectDirectionalKick(out int fallbackPoseIndex);
            if (selectedKick == null)
            {
                return;
            }

            StartAttack(selectedKick, -1, fallbackPoseIndex, false);
        }

        private void QueuePostAttackHandInputIfAllowed()
        {
            if (!CanQueuePostAttackInput())
            {
                return;
            }

            queuedHandAttackAfterAttack = true;
            queuedKickAttackAfterAttack = null;
            queuedInputExpireTime = Time.time + postAttackInputBufferTime;
        }

        private void QueuePostAttackKickInputIfAllowed()
        {
            if (!CanQueuePostAttackInput())
            {
                return;
            }

            queuedKickAttackAfterAttack = SelectDirectionalKick(out queuedKickFallbackPoseIndex);
            queuedHandAttackAfterAttack = false;
            queuedInputExpireTime = Time.time + postAttackInputBufferTime;
        }

        private bool CanQueuePostAttackInput()
        {
            if (postAttackInputBufferTime <= 0f || !isAttackActive || !TryGetCurrentAttack(out ComboAttack attack))
            {
                return false;
            }

            float remainingAttackTime = Mathf.Max(0f, attack.attackDuration - currentAttackElapsed);
            return animationEnded || remainingAttackTime <= postAttackInputBufferTime;
        }

        private void BufferNextAttackIfAllowed()
        {
            if (bufferedNextAttack || comboAttacks.Count <= 1 || !TryGetCurrentAttack(out ComboAttack attack))
            {
                return;
            }

            bool finalAttack = currentAttackIndex >= comboAttacks.Count - 1;
            bool stillAcceptingInput = comboWindowOpen || currentAttackElapsed <= attack.comboInputEndTime;
            if (!finalAttack && stillAcceptingInput)
            {
                bufferedNextAttack = true;
            }
        }

        private bool CanStartCombat()
        {
            if (!isActiveAndEnabled || isForcedStunned)
            {
                return false;
            }

            if (inputLock != null && inputLock.IsLocked)
            {
                return false;
            }

            if (!allowCombatWhileCrouching && playerMotor != null && playerMotor.IsCrouching)
            {
                return false;
            }

            return true;
        }

        private void StartHandAttack(int attackIndex)
        {
            if (comboAttacks.Count == 0)
            {
                return;
            }

            attackIndex = Mathf.Clamp(attackIndex, 0, comboAttacks.Count - 1);
            ComboAttack attack = comboAttacks[attackIndex];
            attack?.Sanitize();
            if (attack == null)
            {
                return;
            }

            StartAttack(attack, attackIndex, attackIndex, true);
        }

        private void StartAttack(ComboAttack attack, int attackIndex, int fallbackPoseIndex, bool usesHandCombo)
        {
            if (attack == null)
            {
                return;
            }

            attack.Sanitize();
            StopAllCombatRoutines();
            ClearQueuedPostAttackInput();
            attackSequence++;
            currentAttackIndex = attackIndex;
            currentAttack = attack;
            currentFallbackPoseIndex = fallbackPoseIndex;
            currentAttackUsesHandCombo = usesHandCombo;
            currentAttackElapsed = 0f;
            isAttackActive = true;
            animationEnded = false;
            comboWindowOpen = false;
            bufferedNextAttack = false;
            hitPerformed = false;
            damagedTargets.Clear();

            ApplyCombatMovement(true);
            SetLegacyAnimationSuspended(true);
            SetCombatAnimatorActive(true);
            UpdateAnimatorMovementParameters(true);
            SetAnimatorBoolIfPresent(IsAttackingParameter, "IsAttacking", true);
            requestedMovementStateHash = 0;
            bool animatorPlayed = PlayAttackAnimation(attack);
            if (!animatorPlayed)
            {
                PlayProceduralAttackFallback(fallbackPoseIndex, attack);
            }

            attackTimelineRoutine = StartCoroutine(AttackTimelineRoutine(attackSequence, attack));
        }

        private IEnumerator AttackTimelineRoutine(int sequence, ComboAttack attack)
        {
            bool hitTimeReached = false;
            bool comboWindowOpened = false;
            bool comboWindowClosed = false;

            while (isAttackActive && sequence == attackSequence && currentAttackElapsed < attack.attackDuration)
            {
                currentAttackElapsed += Time.deltaTime;

                if (!hitTimeReached && currentAttackElapsed >= attack.hitTime)
                {
                    hitTimeReached = true;
                    PerformAttackHit();
                }

                if (!comboWindowOpened && currentAttackElapsed >= attack.comboInputStartTime)
                {
                    comboWindowOpened = true;
                    OpenComboWindow();
                }

                if (!comboWindowClosed && currentAttackElapsed >= attack.comboInputEndTime)
                {
                    comboWindowClosed = true;
                    CloseComboWindow();
                }

                yield return null;
            }

            if (isAttackActive && sequence == attackSequence)
            {
                EndAttackInternal(false);
            }
        }

        private void EndAttackInternal(bool stopTimeline)
        {
            if (!isAttackActive || animationEnded)
            {
                return;
            }

            animationEnded = true;
            comboWindowOpen = false;

            if (stopTimeline && attackTimelineRoutine != null)
            {
                StopCoroutine(attackTimelineRoutine);
            }

            attackTimelineRoutine = null;

            if (recoveryRoutine == null && TryGetCurrentAttack(out ComboAttack attack))
            {
                if (attack.recoveryTime <= 0f)
                {
                    FinishAttack();
                }
                else
                {
                    recoveryRoutine = StartCoroutine(FinishAfterRecoveryRoutine(attackSequence, attack.recoveryTime));
                }
            }
        }

        private IEnumerator FinishAfterRecoveryRoutine(int sequence, float recoveryTime)
        {
            float elapsed = 0f;
            while (sequence == attackSequence && elapsed < recoveryTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (sequence == attackSequence)
            {
                recoveryRoutine = null;
                FinishAttack();
            }
        }

        private void FinishAttack()
        {
            int completedAttackIndex = currentAttackIndex;
            bool completedHandComboAttack = currentAttackUsesHandCombo;
            bool completedFinalAttack = completedHandComboAttack && completedAttackIndex >= comboAttacks.Count - 1;
            bool shouldContinueCombo = completedHandComboAttack && bufferedNextAttack && !completedFinalAttack && CanStartCombat();
            bool queuedInputFresh = QueuedInputIsFresh() && CanStartCombat();
            ComboAttack queuedKickAttack = !shouldContinueCombo && queuedInputFresh ? queuedKickAttackAfterAttack : null;
            int queuedKickFallback = queuedKickFallbackPoseIndex;
            bool shouldStartQueuedKick = queuedKickAttack != null;
            bool shouldStartQueuedHandAttack = !shouldContinueCombo
                && !shouldStartQueuedKick
                && queuedInputFresh
                && queuedHandAttackAfterAttack
                && comboAttacks.Count > 0;
            bool shouldStartAnotherAttack = shouldContinueCombo || shouldStartQueuedKick || shouldStartQueuedHandAttack;

            recoveryRoutine = null;
            isAttackActive = false;
            currentAttack = null;
            currentFallbackPoseIndex = 0;
            currentAttackUsesHandCombo = false;
            animationEnded = false;
            comboWindowOpen = false;
            bufferedNextAttack = false;
            hitPerformed = false;
            damagedTargets.Clear();
            currentAttackIndex = -1;
            currentAttackElapsed = 0f;
            StopProceduralAnimation(true);
            if (!shouldStartAnotherAttack)
            {
                ApplyCombatMovement(false);
                SetAnimatorBoolIfPresent(IsAttackingParameter, "IsAttacking", false);
                UpdateAnimatorMovementParameters(true);
                if (ShouldUseAnimatorForMovementPose())
                {
                    SetLegacyAnimationSuspended(true);
                    SetCombatAnimatorActive(true);
                    TryCrossFadeToCurrentMovementState(locomotionBlendDuration);
                }
                else
                {
                    UpdateAnimatorActivity(false);
                }
            }

            if (completedHandComboAttack)
            {
                nextAttackIndex = completedFinalAttack ? 0 : Mathf.Clamp(completedAttackIndex + 1, 0, comboAttacks.Count - 1);
            }

            if (shouldContinueCombo)
            {
                StartHandAttack(nextAttackIndex);
            }
            else if (shouldStartQueuedKick)
            {
                ClearQueuedPostAttackInput();
                StartAttack(queuedKickAttack, -1, queuedKickFallback, false);
            }
            else if (shouldStartQueuedHandAttack)
            {
                ClearQueuedPostAttackInput();
                nextAttackIndex = Mathf.Clamp(nextAttackIndex, 0, comboAttacks.Count - 1);
                StartHandAttack(nextAttackIndex);
            }
            else if (completedHandComboAttack && !completedFinalAttack)
            {
                ScheduleComboReset();
            }
        }

        private bool QueuedInputIsFresh()
        {
            return queuedInputExpireTime > 0f && Time.time <= queuedInputExpireTime;
        }

        private void ClearQueuedPostAttackInput()
        {
            queuedHandAttackAfterAttack = false;
            queuedKickAttackAfterAttack = null;
            queuedKickFallbackPoseIndex = 0;
            queuedInputExpireTime = 0f;
        }

        private void ScheduleComboReset()
        {
            StopComboResetRoutine();
            if (comboResetDelay <= 0f)
            {
                ResetComboProgress();
                return;
            }

            comboResetRoutine = StartCoroutine(ComboResetRoutine());
        }

        private IEnumerator ComboResetRoutine()
        {
            float elapsed = 0f;
            while (!isAttackActive && elapsed < comboResetDelay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isAttackActive)
            {
                nextAttackIndex = 0;
            }

            comboResetRoutine = null;
        }

        private void StopAllCombatRoutines()
        {
            if (attackTimelineRoutine != null)
            {
                StopCoroutine(attackTimelineRoutine);
                attackTimelineRoutine = null;
            }

            if (recoveryRoutine != null)
            {
                StopCoroutine(recoveryRoutine);
                recoveryRoutine = null;
            }

            StopComboResetRoutine();
            StopProceduralAnimation(true);
            StopAnimatorReleaseRoutine();
        }

        private void StopComboResetRoutine()
        {
            if (comboResetRoutine == null)
            {
                return;
            }

            StopCoroutine(comboResetRoutine);
            comboResetRoutine = null;
        }

        private void ClearAttackState()
        {
            isAttackActive = false;
            animationEnded = false;
            comboWindowOpen = false;
            bufferedNextAttack = false;
            hitPerformed = false;
            damagedTargets.Clear();
            currentAttackIndex = -1;
            currentAttack = null;
            currentFallbackPoseIndex = 0;
            currentAttackUsesHandCombo = false;
            currentAttackElapsed = 0f;
            ClearQueuedPostAttackInput();
            SetAnimatorBoolIfPresent(IsAttackingParameter, "IsAttacking", false);
            SetAnimatorBoolIfPresent(IsJumpingParameter, "IsJumping", false);
            wasJumpingForAnimator = false;
            UpdateAnimatorActivity(false);
        }

        private void ApplyCombatMovement(bool active)
        {
            if (playerMotor != null)
            {
                playerMotor.SetCombatMovementModifier(active, attackMoveSpeedMultiplier, lockMovementDuringAttack);
            }
        }

        private bool PlayAttackAnimation(ComboAttack attack)
        {
            if (animator == null || string.IsNullOrWhiteSpace(attack.animationTrigger))
            {
                return false;
            }

            SetCombatAnimatorActive(true);
            if (!animator.isActiveAndEnabled)
            {
                return false;
            }

            if (!HasAnimatorTrigger(attack.animationTrigger))
            {
                WarnMissingTriggerOnce(attack.animationTrigger);
                return false;
            }

            for (int i = 0; i < comboAttacks.Count; i++)
            {
                string trigger = comboAttacks[i]?.animationTrigger;
                if (!string.IsNullOrWhiteSpace(trigger) && HasAnimatorTrigger(trigger))
                {
                    animator.ResetTrigger(trigger);
                }
            }

            ResetKickTrigger(neutralKickAttack);
            ResetKickTrigger(forwardKickAttack);
            ResetKickTrigger(backwardKickAttack);

            animator.SetTrigger(attack.animationTrigger);
            return true;
        }

        private void ResetKickTrigger(ComboAttack kickAttack)
        {
            string trigger = kickAttack?.animationTrigger;
            ResetAnimatorTrigger(trigger);
        }

        private void ResetAnimatorTrigger(string trigger)
        {
            if (!string.IsNullOrWhiteSpace(trigger) && HasAnimatorTrigger(trigger))
            {
                animator.ResetTrigger(trigger);
            }
        }

        private void SetCombatAnimatorActive(bool active, bool ignoreCrouchPose = false)
        {
            if (!enableAnimatorOnlyDuringAttack || animator == null)
            {
                return;
            }

            if (active)
            {
                StopAnimatorReleaseRoutine();
            }

            if (!ignoreCrouchPose)
            {
                active = active || ShouldUseAnimatorForDrivenPose();
            }

            if (active)
            {
                animator.keepAnimatorStateOnDisable = true;
                if (!animator.enabled)
                {
                    animator.enabled = true;
                    UpdateAnimatorMovementParameters(true);
                    animator.Update(0f);
                }

                return;
            }

            if (!animator.enabled)
            {
                return;
            }

            for (int i = 0; i < comboAttacks.Count; i++)
            {
                string trigger = comboAttacks[i]?.animationTrigger;
                if (!string.IsNullOrWhiteSpace(trigger) && HasAnimatorTrigger(trigger))
                {
                    animator.ResetTrigger(trigger);
                }
            }

            ResetKickTrigger(neutralKickAttack);
            ResetKickTrigger(forwardKickAttack);
            ResetKickTrigger(backwardKickAttack);
            ResetAnimatorTrigger("Jump");
            SetAnimatorBoolIfPresent(IsAttackingParameter, "IsAttacking", false);
            SetAnimatorBoolIfPresent(IsJumpingParameter, "IsJumping", false);
            wasJumpingForAnimator = false;
            requestedMovementStateHash = 0;
            animator.Update(0f);
            animator.enabled = false;
        }

        private void UpdateAnimatorActivity(bool forceOff)
        {
            if (isAttackActive && !forceOff)
            {
                return;
            }

            bool shouldUseMovementAnimator = !forceOff && ShouldUseAnimatorForMovementPose();
            bool shouldUseAnimator = !forceOff && ShouldUseAnimatorForDrivenPose();
            if (shouldUseAnimator)
            {
                StopAnimatorReleaseRoutine();
                SetLegacyAnimationSuspended(true);
                bool wasAnimatorEnabled = animator != null && animator.enabled;
                SetCombatAnimatorActive(true);
                if (shouldUseMovementAnimator)
                {
                    TryCrossFadeToCurrentMovementState(wasAnimatorEnabled ? locomotionBlendDuration : Mathf.Min(0.08f, locomotionBlendDuration));
                }
                else
                {
                    requestedMovementStateHash = 0;
                }

                return;
            }

            if (forceOff
                || ShouldReleaseImmediatelyToLegacyLocomotion()
                || animatorReleaseDelay <= 0f
                || animator == null
                || !animator.enabled)
            {
                StopAnimatorReleaseRoutine();
                SetCombatAnimatorActive(false, forceOff);
                SetLegacyAnimationSuspended(false);
                requestedMovementStateHash = 0;
                return;
            }

            SetLegacyAnimationSuspended(true);
            if (animatorReleaseRoutine == null)
            {
                animatorReleaseRoutine = StartCoroutine(ReleaseCombatAnimatorAfterDelay());
            }
        }

        private IEnumerator ReleaseCombatAnimatorAfterDelay()
        {
            float elapsed = 0f;
            while (!isAttackActive && elapsed < animatorReleaseDelay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            animatorReleaseRoutine = null;
            if (isAttackActive || ShouldUseAnimatorForDrivenPose())
            {
                yield break;
            }

            SetCombatAnimatorActive(false, true);
            SetLegacyAnimationSuspended(false);
            requestedMovementStateHash = 0;
        }

        private void StopAnimatorReleaseRoutine()
        {
            if (animatorReleaseRoutine == null)
            {
                return;
            }

            StopCoroutine(animatorReleaseRoutine);
            animatorReleaseRoutine = null;
        }

        private bool ShouldUseAnimatorForCrouchPose()
        {
            return enableAnimatorWhileCrouching
                && playerMotor != null
                && playerMotor.IsCrouching;
        }

        private bool ShouldUseAnimatorForIdlePose()
        {
            return enableAnimatorWhileIdle
                && playerMotor != null
                && playerMotor.IsGrounded
                && !playerMotor.IsCrouching
                && !playerMotor.IsMoving
                && playerMotor.CurrentSpeed <= 0.05f;
        }

        private bool ShouldUseAnimatorForMovementPose()
        {
            return enableAnimatorWhileMoving
                && playerMotor != null
                && playerMotor.IsGrounded
                && !playerMotor.IsCrouching
                && playerMotor.IsMoving
                && playerMotor.CurrentSpeed > 0.05f;
        }

        private bool ShouldUseAnimatorForJumpPose()
        {
            return enableAnimatorWhileJumping
                && playerMotor != null
                && !playerMotor.IsCrouching
                && (playerMotor.IsJumping || !playerMotor.IsGrounded);
        }

        private bool ShouldUseAnimatorForDrivenPose()
        {
            return ShouldUseAnimatorForCrouchPose()
                || ShouldUseAnimatorForMovementPose()
                || ShouldUseAnimatorForJumpPose()
                || ShouldUseAnimatorForIdlePose();
        }

        private bool ShouldReleaseImmediatelyToLegacyLocomotion()
        {
            return playerMotor != null
                && playerMotor.IsMoving
                && !ShouldUseAnimatorForMovementPose()
                && !ShouldUseAnimatorForCrouchPose()
                && !ShouldUseAnimatorForJumpPose();
        }

        private void UpdateAnimatorMovementParameters(bool immediate = false)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return;
            }

            bool isGrounded = playerMotor != null ? playerMotor.IsGrounded : characterController == null || characterController.isGrounded;
            bool isJumping = playerMotor != null && playerMotor.IsJumping;

            float targetMoveSpeed = playerMotor != null ? playerMotor.CurrentSpeed : 0f;
            float moveSpeedDamp = immediate || isAttackActive ? 0f : moveSpeedDampTime;
            SetAnimatorFloatIfPresent(MoveSpeedParameter, "MoveSpeed", targetMoveSpeed, moveSpeedDamp);
            SetAnimatorFloatIfPresent(VerticalSpeedParameter, "VerticalSpeed", playerMotor != null ? playerMotor.VerticalVelocity : 0f);
            SetAnimatorBoolIfPresent(IsGroundedParameter, "IsGrounded", isGrounded);
            SetAnimatorBoolIfPresent(IsSprintingParameter, "IsSprinting", playerMotor != null && playerMotor.IsSprinting);
            SetAnimatorBoolIfPresent(IsCrouchingParameter, "IsCrouching", playerMotor != null && playerMotor.IsCrouching);
            SetAnimatorBoolIfPresent(IsJumpingParameter, "IsJumping", isJumping);
            SetAnimatorBoolIfPresent(IsAttackingParameter, "IsAttacking", isAttackActive);

            if (isJumping && !wasJumpingForAnimator && HasAnimatorTrigger("Jump"))
            {
                animator.SetTrigger(JumpParameter);
            }

            wasJumpingForAnimator = isJumping;
        }

        private void TryCrossFadeToCurrentMovementState(float blendDuration)
        {
            if (animator == null
                || !animator.isActiveAndEnabled
                || animator.runtimeAnimatorController == null
                || isAttackActive
                || !TryGetCurrentMovementStateHash(out int targetStateHash))
            {
                return;
            }

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.fullPathHash == targetStateHash)
            {
                requestedMovementStateHash = targetStateHash;
                return;
            }

            if (requestedMovementStateHash == targetStateHash && animator.IsInTransition(0))
            {
                return;
            }

            requestedMovementStateHash = targetStateHash;
            animator.CrossFadeInFixedTime(targetStateHash, Mathf.Max(0f, blendDuration), 0, 0f);
        }

        private bool TryGetCurrentMovementStateHash(out int stateHash)
        {
            stateHash = 0;
            if (playerMotor == null || !playerMotor.IsMoving || playerMotor.CurrentSpeed <= 0.05f)
            {
                return false;
            }

            stateHash = playerMotor.CurrentSpeed >= runStateSpeedThreshold ? RunStateHash : WalkStateHash;
            if (animator == null || animator.HasState(0, stateHash))
            {
                return true;
            }

            string fallbackStateName = playerMotor.CurrentSpeed >= runStateSpeedThreshold ? RunStateName : WalkStateName;
            int fallbackHash = Animator.StringToHash(fallbackStateName);
            if (animator.HasState(0, fallbackHash))
            {
                stateHash = fallbackHash;
                return true;
            }

            return false;
        }

        private void SetAnimatorFloatIfPresent(int parameterHash, string parameterName, float value, float dampTime = 0f)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                if (Application.isPlaying && dampTime > 0f)
                {
                    animator.SetFloat(parameterHash, value, dampTime, Time.deltaTime);
                }
                else
                {
                    animator.SetFloat(parameterHash, value);
                }
            }
        }

        private void SetAnimatorBoolIfPresent(int parameterHash, string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterHash, value);
            }
        }

        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == parameterType
                    && string.Equals(parameter.name, parameterName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetLegacyAnimationSuspended(bool suspended)
        {
            if (!suspendLegacyAnimationDuringAttack || legacyAnimationSuspended == suspended)
            {
                return;
            }

            legacyAnimationSuspended = suspended;

            if (playerVisualController != null)
            {
                playerVisualController.SetLegacyAnimationSuspended(suspended);
                return;
            }

            if (legacyAnimationToPause == null)
            {
                return;
            }

            if (suspended)
            {
                legacyAnimationToPause.Stop();
            }
        }

        private void PlayProceduralAttackFallback(int attackIndex, ComboAttack attack)
        {
            if (!playProceduralFallback || proceduralFallbackStrength <= 0f)
            {
                return;
            }

            ResolveReferences();
            if (proceduralAnimationRoot == null)
            {
                return;
            }

            StopProceduralAnimation(true);
            CacheProceduralBasePose();
            proceduralAnimationRoutine = StartCoroutine(ProceduralAttackFallbackRoutine(
                attackSequence,
                attackIndex,
                Mathf.Max(0.01f, attack.attackDuration)));
        }

        private IEnumerator ProceduralAttackFallbackRoutine(int sequence, int attackIndex, float duration)
        {
            float elapsed = 0f;
            while (sequence == attackSequence && isAttackActive && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float strikeEnvelope = Mathf.Sin(normalizedTime * Mathf.PI);
                ApplyProceduralPose(attackIndex, strikeEnvelope * proceduralFallbackStrength);
                yield return null;
            }

            if (sequence == attackSequence)
            {
                RestoreProceduralBasePose();
                proceduralAnimationRoutine = null;
            }
        }

        private void ApplyProceduralPose(int attackIndex, float weight)
        {
            if (proceduralAnimationRoot == null || !hasProceduralBasePose)
            {
                return;
            }

            GetProceduralPose(attackIndex, out Vector3 offset, out Vector3 euler);
            proceduralAnimationRoot.localPosition = proceduralBaseLocalPosition + offset * weight;
            proceduralAnimationRoot.localRotation = proceduralBaseLocalRotation * Quaternion.Euler(euler * weight);
            proceduralAnimationRoot.localScale = proceduralBaseLocalScale;
        }

        private static void GetProceduralPose(int attackIndex, out Vector3 offset, out Vector3 euler)
        {
            switch (attackIndex % 4)
            {
                case 0:
                    offset = new Vector3(-0.04f, 0f, 0.16f);
                    euler = new Vector3(-5f, -14f, 5f);
                    break;
                case 1:
                    offset = new Vector3(0.04f, 0f, 0.18f);
                    euler = new Vector3(-5f, 14f, -5f);
                    break;
                case 2:
                    offset = new Vector3(0.08f, 0.03f, 0.14f);
                    euler = new Vector3(-3f, -24f, 11f);
                    break;
                default:
                    offset = new Vector3(0f, 0.05f, 0.25f);
                    euler = new Vector3(-13f, 0f, 0f);
                    break;
            }
        }

        private void CacheProceduralBasePose()
        {
            if (proceduralAnimationRoot == null)
            {
                hasProceduralBasePose = false;
                return;
            }

            proceduralBaseLocalPosition = proceduralAnimationRoot.localPosition;
            proceduralBaseLocalRotation = proceduralAnimationRoot.localRotation;
            proceduralBaseLocalScale = proceduralAnimationRoot.localScale;
            hasProceduralBasePose = true;
        }

        private void StopProceduralAnimation(bool restorePose)
        {
            if (proceduralAnimationRoutine != null)
            {
                StopCoroutine(proceduralAnimationRoutine);
                proceduralAnimationRoutine = null;
            }

            if (restorePose)
            {
                RestoreProceduralBasePose();
            }
        }

        private void RestoreProceduralBasePose()
        {
            if (proceduralAnimationRoot == null || !hasProceduralBasePose)
            {
                return;
            }

            proceduralAnimationRoot.localPosition = proceduralBaseLocalPosition;
            proceduralAnimationRoot.localRotation = proceduralBaseLocalRotation;
            proceduralAnimationRoot.localScale = proceduralBaseLocalScale;
            hasProceduralBasePose = false;
        }

        private bool HasAnimatorTrigger(string triggerName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Trigger
                    && string.Equals(parameter.name, triggerName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void WarnMissingTriggerOnce(string triggerName)
        {
            if (missingTriggerWarnings.Add(triggerName))
            {
                Debug.LogWarning(
                    $"[PlayerCombatController] Animator is missing trigger '{triggerName}'. Combat timing and damage still run through fallback timers.",
                    this);
            }
        }

        private void ResolveAttackPose(out Vector3 origin, out Vector3 direction)
        {
            Transform source = attackPoint != null ? attackPoint : transform;
            origin = attackPoint != null ? attackPoint.position : transform.position + Vector3.up + transform.forward * 0.5f;
            direction = source.forward.sqrMagnitude > 0.0001f ? source.forward.normalized : transform.forward;
        }

        private void TryDamageCollider(Collider hitCollider, ComboAttack attack)
        {
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
            {
                return;
            }

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || damagedTargets.Contains(damageable))
            {
                return;
            }

            damagedTargets.Add(damageable);
            damageable.TakeDamage(attack.damage);
        }

        private bool TryGetCurrentAttack(out ComboAttack attack)
        {
            if (currentAttack != null)
            {
                attack = currentAttack;
                return true;
            }

            attack = null;
            return false;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void EnsureDefaultCombo()
        {
            bool legacyMixedKickCombo = comboAttacks.Count >= 4
                && (HasAttackTrigger("KickSide") || HasAttackTrigger("KickHeavy") || HasAttackTrigger("SpinningBackKick"));
            if (comboAttacks.Count >= 4 && !legacyMixedKickCombo)
            {
                return;
            }

            comboAttacks.Clear();
            comboAttacks.Add(CreateAttack("Punch Left", "PunchLeft", 10f, 1.1f, 0.3f, 0.29f, 0.58f, 0.203f, 0.4408f, 0.04f));
            comboAttacks.Add(CreateAttack("Punch Right", "PunchRight", 12f, 1.15f, 0.32f, 0.31f, 0.62f, 0.217f, 0.4712f, 0.0434f));
            comboAttacks.Add(CreateAttack("Hook", "Hook", 15f, 1.15f, 0.34f, 0.374f, 0.68f, 0.2584f, 0.5576f, 0.068f));
            comboAttacks.Add(CreateAttack("Right Hook", "RightHook", 18f, 1.2f, 0.36f, 0.4032f, 0.72f, 0.2736f, 0.5904f, 0.072f));
        }

        private bool HasAttackTrigger(string trigger)
        {
            for (int i = 0; i < comboAttacks.Count; i++)
            {
                if (comboAttacks[i] != null && string.Equals(comboAttacks[i].animationTrigger, trigger, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureDefaultKicks()
        {
            if (neutralKickAttack == null || string.IsNullOrWhiteSpace(neutralKickAttack.animationTrigger))
            {
                neutralKickAttack = CreateAttack("Heavy Kick", "KickHeavy", 28f, 1.6f, 0.42f, 0.58f, 1f, 0.36f, 0.84f, 0.18f);
            }

            if (forwardKickAttack == null || string.IsNullOrWhiteSpace(forwardKickAttack.animationTrigger))
            {
                forwardKickAttack = CreateAttack("Side Kick", "KickSide", 18f, 1.45f, 0.38f, 0.476f, 0.85f, 0.2975f, 0.697f, 0.085f);
            }

            if (backwardKickAttack == null || string.IsNullOrWhiteSpace(backwardKickAttack.animationTrigger))
            {
                backwardKickAttack = CreateAttack("Spinning Back Kick", "SpinningBackKick", 30f, 1.55f, 0.42f, 0.609f, 1.05f, 0.399f, 0.882f, 0.189f);
            }
        }

        private ComboAttack SelectDirectionalKick(out int fallbackPoseIndex)
        {
            Vector2 moveInput = inputReader != null ? inputReader.MoveInput : Vector2.zero;
            if (moveInput.y >= directionalKickThreshold)
            {
                fallbackPoseIndex = 2;
                return forwardKickAttack;
            }

            if (moveInput.y <= -directionalKickThreshold)
            {
                fallbackPoseIndex = 3;
                return backwardKickAttack;
            }

            fallbackPoseIndex = 4;
            return neutralKickAttack;
        }

        private static ComboAttack CreateAttack(
            string attackName,
            string animationTrigger,
            float damage,
            float attackRange,
            float attackRadius,
            float hitTime,
            float attackDuration,
            float comboInputStartTime,
            float comboInputEndTime,
            float recoveryTime)
        {
            ComboAttack attack = new ComboAttack
            {
                attackName = attackName,
                animationTrigger = animationTrigger,
                damage = damage,
                attackRange = attackRange,
                attackRadius = attackRadius,
                hitTime = hitTime,
                attackDuration = attackDuration,
                comboInputStartTime = comboInputStartTime,
                comboInputEndTime = comboInputEndTime,
                recoveryTime = recoveryTime
            };
            attack.Sanitize();
            return attack;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            ComboAttack attack = null;
            if (Application.isPlaying && currentAttack != null)
            {
                attack = currentAttack;
            }
            else if (comboAttacks.Count > 0)
            {
                attack = comboAttacks[Mathf.Clamp(nextAttackIndex, 0, comboAttacks.Count - 1)];
            }

            if (attack == null)
            {
                return;
            }

            ResolveAttackPose(out Vector3 origin, out Vector3 direction);
            float radius = Mathf.Max(0.01f, attack.attackRadius);
            float range = Mathf.Max(0f, attack.attackRange);
            Vector3 end = origin + direction * range;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, 0.08f);
            Gizmos.DrawLine(origin, end);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawWireSphere(end, radius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, direction * range);
        }
    }
}
