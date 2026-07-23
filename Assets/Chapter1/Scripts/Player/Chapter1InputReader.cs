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
        [SerializeField] private InputActionReference sprintActionReference;
        [SerializeField] private InputActionReference crouchActionReference;
        [SerializeField] private InputActionReference interactActionReference;
        [SerializeField] private InputActionReference toggleFlashlightActionReference;
        [SerializeField] private InputActionReference throwCanActionReference;
        [SerializeField] private InputActionReference pauseActionReference;

        private readonly List<InputAction> cachedActions = new List<InputAction>();
        private bool callbacksRegistered;
        private bool gameplayInputEnabled = true;
        private bool referencesValidated;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool SprintHeld { get; private set; }

        public event Action CrouchPressed;
        public event Action InteractPressed;
        public event Action ToggleFlashlightPressed;
        public event Action ThrowCanPressed;
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

        private void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            RegisterValueCallbacks(moveActionReference, OnMovePerformed, OnMoveCanceled);
            RegisterValueCallbacks(lookActionReference, OnLookPerformed, OnLookCanceled);
            RegisterValueCallbacks(sprintActionReference, OnSprintPerformed, OnSprintCanceled);
            RegisterButtonCallback(crouchActionReference, OnCrouchPerformed);
            RegisterButtonCallback(interactActionReference, OnInteractPerformed);
            RegisterButtonCallback(toggleFlashlightActionReference, OnToggleFlashlightPerformed);
            RegisterButtonCallback(throwCanActionReference, OnThrowCanPerformed);
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
            UnregisterButtonCallback(crouchActionReference, OnCrouchPerformed);
            UnregisterButtonCallback(interactActionReference, OnInteractPerformed);
            UnregisterButtonCallback(toggleFlashlightActionReference, OnToggleFlashlightPerformed);
            UnregisterButtonCallback(throwCanActionReference, OnThrowCanPerformed);
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
            if (gameplayInputEnabled)
            {
                EnableAllActions();
            }
            else
            {
                DisableAllActions();
            }
        }

        private void EnableAllActions()
        {
            for (int i = 0; i < cachedActions.Count; i++)
            {
                cachedActions[i]?.Enable();
            }
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
            ValidateReference(sprintActionReference, "Sprint");
            ValidateReference(crouchActionReference, "Crouch");
            ValidateReference(interactActionReference, "Interact");
            ValidateReference(toggleFlashlightActionReference, "ToggleFlashlight");
            ValidateReference(throwCanActionReference, "ThrowCan");
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
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
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
            if (gameplayInputEnabled)
            {
                SprintHeld = context.ReadValueAsButton();
            }
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            SprintHeld = false;
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                CrouchPressed?.Invoke();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                InteractPressed?.Invoke();
            }
        }

        private void OnToggleFlashlightPerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                ToggleFlashlightPressed?.Invoke();
            }
        }

        private void OnThrowCanPerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                ThrowCanPressed?.Invoke();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (gameplayInputEnabled)
            {
                PausePressed?.Invoke();
            }
        }
    }
}
