using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter1
{
    public sealed class Chapter1InputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private InputActionReference lookActionReference;
        [SerializeField] private InputActionReference attackActionReference;
        [SerializeField] private InputActionReference kickActionReference;
        [SerializeField] private InputActionReference jumpActionReference;
        [SerializeField] private InputActionReference sprintActionReference;
        [SerializeField] private InputActionReference crouchActionReference;
        [SerializeField] private InputActionReference interactActionReference;
        [SerializeField] private InputActionReference talkActionReference;
        [SerializeField] private InputActionReference toggleFlashlightActionReference;
        [SerializeField] private InputActionReference throwCanActionReference;
        [SerializeField] private InputActionReference inventoryActionReference;
        [SerializeField] private InputActionReference pauseActionReference;

        private readonly List<InputAction> cachedActions = new List<InputAction>();
        private bool callbacksRegistered;
        private bool gameplayInputEnabled = true;
        private bool combatOnlyMode;
        private bool policeArrestMode;
        private bool referencesValidated;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool TalkHeld { get; private set; }
        public bool ThrowCanHeld { get; private set; }
        public bool GameplayInputEnabled =>
            gameplayInputEnabled && isActiveAndEnabled;
        public bool CombatOnlyMode =>
            combatOnlyMode && isActiveAndEnabled;
        public bool PoliceArrestMode =>
            policeArrestMode && isActiveAndEnabled;

        public event Action CrouchPressed;
        public event Action AttackPressed;
        public event Action KickPressed;
        public event Action JumpPressed;
        public event Action InteractPressed;
        public event Action TalkPressed;
        public event Action TalkReleased;
        public event Action ToggleFlashlightPressed;
        public event Action ThrowCanPressed;
        public event Action ThrowCanReleased;
        public event Action InventoryPressed;
        public event Action PausePressed;

        private void OnEnable()
        {
            ValidateReferencesOnce();
            RegisterCallbacks();
            ApplyGameplayInputState();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            DisableAllActions();
            ResetReadValues();
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            if (gameplayInputEnabled == enabled)
            {
                return;
            }

            gameplayInputEnabled = enabled;
            ApplyGameplayInputState();

            if (!gameplayInputEnabled)
            {
                ResetReadValues();
            }
        }

        /// <summary>
        /// Keeps locomotion, camera, sprint, jump, crouch, combat, door
        /// interaction and pause available while suppressing every unrelated
        /// gameplay action.
        /// Global gameplay input disabling still takes precedence over this
        /// mode.
        /// </summary>
        public void SetCombatOnlyMode(bool active)
        {
            if (combatOnlyMode == active)
            {
                return;
            }

            if (active)
            {
                // Clear held values before disabling their InputActions so a
                // generated canceled callback cannot be interpreted as an E
                // interaction or a petrol-can throw during fight startup.
                TalkHeld = false;
                ThrowCanHeld = false;
            }

            combatOnlyMode = active;
            ApplyGameplayInputState();
        }

        /// <summary>
        /// Restricts input to camera look and pause while the police arrest
        /// sequence owns the player. This mode takes precedence over combat
        /// mode, but global gameplay input disabling still takes precedence
        /// over both modes.
        /// </summary>
        public void SetPoliceArrestMode(bool active)
        {
            if (policeArrestMode == active)
            {
                return;
            }

            // Clear values before disabling actions. In particular, Talk and
            // ThrowCan must be false before their canceled callbacks run so
            // releasing E cannot complete an interaction or throw a can while
            // the arrest sequence is starting.
            ResetReadValues();
            policeArrestMode = active;
            ApplyGameplayInputState();
        }

        private void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            RegisterValueCallbacks(moveActionReference, OnMovePerformed, OnMoveCanceled);
            RegisterValueCallbacks(lookActionReference, OnLookPerformed, OnLookCanceled);
            RegisterValueCallbacks(sprintActionReference, OnSprintPerformed, OnSprintCanceled);
            RegisterButtonCallback(attackActionReference, OnAttackPerformed);
            RegisterButtonCallback(kickActionReference, OnKickPerformed);
            RegisterButtonCallback(jumpActionReference, OnJumpPerformed);
            RegisterButtonCallback(crouchActionReference, OnCrouchPerformed);
            RegisterButtonCallback(interactActionReference, OnInteractPerformed);
            RegisterValueCallbacks(
                talkActionReference,
                OnTalkPerformed,
                OnTalkCanceled);
            RegisterButtonCallback(toggleFlashlightActionReference, OnToggleFlashlightPerformed);
            RegisterValueCallbacks(
                throwCanActionReference,
                OnThrowCanPerformed,
                OnThrowCanCanceled);
            RegisterButtonCallback(inventoryActionReference, OnInventoryPerformed);
            RegisterButtonCallback(pauseActionReference, OnPausePerformed);

            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!callbacksRegistered)
            {
                return;
            }

            UnregisterValueCallbacks(moveActionReference, OnMovePerformed, OnMoveCanceled);
            UnregisterValueCallbacks(lookActionReference, OnLookPerformed, OnLookCanceled);
            UnregisterValueCallbacks(sprintActionReference, OnSprintPerformed, OnSprintCanceled);
            UnregisterButtonCallback(attackActionReference, OnAttackPerformed);
            UnregisterButtonCallback(kickActionReference, OnKickPerformed);
            UnregisterButtonCallback(jumpActionReference, OnJumpPerformed);
            UnregisterButtonCallback(crouchActionReference, OnCrouchPerformed);
            UnregisterButtonCallback(interactActionReference, OnInteractPerformed);
            UnregisterValueCallbacks(
                talkActionReference,
                OnTalkPerformed,
                OnTalkCanceled);
            UnregisterButtonCallback(toggleFlashlightActionReference, OnToggleFlashlightPerformed);
            UnregisterValueCallbacks(
                throwCanActionReference,
                OnThrowCanPerformed,
                OnThrowCanCanceled);
            UnregisterButtonCallback(inventoryActionReference, OnInventoryPerformed);
            UnregisterButtonCallback(pauseActionReference, OnPausePerformed);

            callbacksRegistered = false;
        }

        private void RegisterValueCallbacks(InputActionReference actionReference, Action<InputAction.CallbackContext> performedCallback, Action<InputAction.CallbackContext> canceledCallback)
        {
            InputAction action = GetAction(actionReference);
            if (action == null)
            {
                return;
            }

            action.performed += performedCallback;
            action.canceled += canceledCallback;
            CacheAction(action);
        }

        private void UnregisterValueCallbacks(InputActionReference actionReference, Action<InputAction.CallbackContext> performedCallback, Action<InputAction.CallbackContext> canceledCallback)
        {
            InputAction action = GetAction(actionReference);
            if (action == null)
            {
                return;
            }

            action.performed -= performedCallback;
            action.canceled -= canceledCallback;
        }

        private void RegisterButtonCallback(InputActionReference actionReference, Action<InputAction.CallbackContext> performedCallback)
        {
            InputAction action = GetAction(actionReference);
            if (action == null)
            {
                return;
            }

            action.performed += performedCallback;
            CacheAction(action);
        }

        private void UnregisterButtonCallback(InputActionReference actionReference, Action<InputAction.CallbackContext> performedCallback)
        {
            InputAction action = GetAction(actionReference);
            if (action == null)
            {
                return;
            }

            action.performed -= performedCallback;
        }

        private void ApplyGameplayInputState()
        {
            for (int i = 0; i < cachedActions.Count; i++)
            {
                InputAction action = cachedActions[i];
                if (action == null)
                {
                    continue;
                }

                bool shouldEnable = gameplayInputEnabled &&
                    (policeArrestMode
                        ? IsPoliceArrestAllowedAction(action)
                        : !combatOnlyMode || IsCombatAllowedAction(action));
                if (shouldEnable)
                {
                    action.Enable();
                }
                else
                {
                    action.Disable();
                }
            }
        }

        private bool IsCombatAllowedAction(InputAction action)
        {
            return action == GetAction(moveActionReference) ||
                   action == GetAction(lookActionReference) ||
                   action == GetAction(sprintActionReference) ||
                   action == GetAction(jumpActionReference) ||
                   action == GetAction(crouchActionReference) ||
                   action == GetAction(attackActionReference) ||
                   action == GetAction(kickActionReference) ||
                   action == GetAction(interactActionReference) ||
                   action == GetAction(pauseActionReference);
        }

        private bool IsPoliceArrestAllowedAction(InputAction action)
        {
            return action == GetAction(lookActionReference) ||
                   action == GetAction(pauseActionReference);
        }

        private void DisableAllActions()
        {
            for (int i = 0; i < cachedActions.Count; i++)
            {
                cachedActions[i]?.Disable();
            }
        }

        private void CacheAction(InputAction action)
        {
            if (action != null && !cachedActions.Contains(action))
            {
                cachedActions.Add(action);
            }
        }

        private InputAction GetAction(InputActionReference actionReference)
        {
            return actionReference != null ? actionReference.action : null;
        }

        private void ValidateReferencesOnce()
        {
            if (referencesValidated)
            {
                return;
            }

            referencesValidated = true;
            ValidateReference(moveActionReference, "Move");
            ValidateReference(lookActionReference, "Look");
            ValidateReference(attackActionReference, "Attack");
            ValidateReference(kickActionReference, "Kick");
            ValidateReference(jumpActionReference, "Jump");
            ValidateReference(sprintActionReference, "Sprint");
            ValidateReference(crouchActionReference, "Crouch");
            ValidateReference(interactActionReference, "Interact");
            ValidateReference(talkActionReference, "Talk");
            ValidateReference(toggleFlashlightActionReference, "ToggleFlashlight");
            ValidateReference(throwCanActionReference, "ThrowCan");
            ValidateReference(inventoryActionReference, "Inventory");
            ValidateReference(pauseActionReference, "Pause");
        }

        private void ValidateReference(InputActionReference actionReference, string actionName)
        {
            if (actionReference == null || actionReference.action == null)
            {
                Debug.LogWarning($"[Chapter1InputReader] GameObject '{gameObject.name}' thiếu InputActionReference cho action '{actionName}'.", this);
            }
        }

        private void ResetReadValues()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            SprintHeld = false;
            TalkHeld = false;
            ThrowCanHeld = false;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (CanProcessGameplayAction())
            {
                MoveInput = context.ReadValue<Vector2>();
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            MoveInput = Vector2.zero;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                LookInput = context.ReadValue<Vector2>();
            }
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            LookInput = Vector2.zero;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessGameplayAction())
            {
                SprintHeld = context.ReadValueAsButton();
            }
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            SprintHeld = false;
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessGameplayAction())
            {
                AttackPressed?.Invoke();
            }
        }

        private void OnKickPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessGameplayAction())
            {
                KickPressed?.Invoke();
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessGameplayAction())
            {
                JumpPressed?.Invoke();
            }
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessGameplayAction())
            {
                CrouchPressed?.Invoke();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            // Interact remains enabled in combat so the interaction
            // controller can route it exclusively to DoorInteractable.
            if (CanProcessGameplayAction())
            {
                InteractPressed?.Invoke();
            }
        }

        private void OnTalkPerformed(InputAction.CallbackContext context)
        {
            TalkHeld = CanProcessNonCombatAction() &&
                       context.ReadValueAsButton();
            if (TalkHeld)
            {
                TalkPressed?.Invoke();
            }
        }

        private void OnTalkCanceled(InputAction.CallbackContext context)
        {
            bool wasHeld = TalkHeld;
            TalkHeld = false;
            if (wasHeld)
            {
                TalkReleased?.Invoke();
            }
        }

        private void OnToggleFlashlightPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessNonCombatAction())
            {
                ToggleFlashlightPressed?.Invoke();
            }
        }

        private void OnThrowCanPerformed(InputAction.CallbackContext context)
        {
            ThrowCanHeld = CanProcessNonCombatAction() &&
                           context.ReadValueAsButton();
            if (ThrowCanHeld)
            {
                ThrowCanPressed?.Invoke();
            }
        }

        private void OnThrowCanCanceled(InputAction.CallbackContext context)
        {
            bool wasHeld = ThrowCanHeld;
            ThrowCanHeld = false;
            if (wasHeld)
            {
                ThrowCanReleased?.Invoke();
            }
        }

        private void OnInventoryPerformed(InputAction.CallbackContext context)
        {
            if (CanProcessNonCombatAction())
            {
                InventoryPressed?.Invoke();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                PausePressed?.Invoke();
            }
        }

        private bool CanProcessNonCombatAction()
        {
            return CanProcessGameplayAction() && !combatOnlyMode;
        }

        private bool CanProcessGameplayAction()
        {
            return gameplayInputEnabled && !policeArrestMode;
        }
    }
}
