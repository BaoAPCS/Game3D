using System;
using System.Collections;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2ConfiscatedItemsMission : MonoBehaviour
    {
        public const string PhoneItemId = "phone";
        public const string PoliceKeyItemId = "police_station_key";

        private const string InputLockReason =
            "Chapter2ConfiscatedItems";
        private const float CompletionDisplaySeconds = 0.65f;

        private Chapter2SaveManager saveManager;
        private Chapter2ClosetInteractable closetInteractable;
        private Chapter2MissionTriggerZone triggerZone;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackInput;
        private PlayerCombatController playerCombat;
        private InventoryController inventory;
        private ItemDefinition phoneDefinition;
        private ItemDefinition policeKeyDefinition;
        private Chapter2ConfiscatedItemsUI missionUI;
        private Coroutine completionRoutine;

        private bool configured;
        private bool sessionOpen;
        private bool modalStateCaptured;
        private bool ownInputLockHeld;
        private bool inputReaderWasEnabled;
        private bool interactionControllerWasEnabled;
        private bool backpackInputWasEnabled;
        private bool combatWasEnabled;
        private CursorLockMode cursorLockBeforeSession;
        private bool cursorVisibleBeforeSession;
        private bool savedStateApplied;
        private bool lastAppliedUnlocked;
        private bool lastAppliedCompleted;
        private bool lastAppliedPhoneRecovered;
        private bool lastAppliedPoliceKeyRecovered;

        private EventSystem sessionEventSystem;
        private InputSystemUIInputModule sessionInputModule;
        private BaseInputModule[] sessionOtherInputModules;
        private bool[] sessionOtherInputModuleStates;
        private bool eventSystemStateCaptured;
        private bool eventSystemCreatedForSession;
        private bool eventSystemWasActive;
        private bool inputModuleAddedForSession;
        private bool inputModuleWasEnabled;

        public event Action MissionCompleted;

        public bool IsUnlocked =>
            saveManager != null &&
            saveManager.CurrentData.Mission02JailObstacleDisabled;

        public bool PhoneRecovered =>
            saveManager != null &&
            saveManager.CurrentData.Mission03PhoneRecovered;

        public bool PoliceKeyRecovered =>
            saveManager != null &&
            saveManager.CurrentData.Mission03PoliceKeyRecovered;

        public bool IsCompleted =>
            saveManager != null &&
            saveManager.CurrentData.Mission03Completed;

        public bool IsSessionOpen => sessionOpen;

        public bool CanInspect =>
            configured &&
            !sessionOpen &&
            IsUnlocked &&
            !IsCompleted &&
            triggerZone != null &&
            triggerZone.ContainsPlayer &&
            (inputLock == null || !inputLock.IsLocked);

        public void Configure(
            Chapter2SaveManager chapter2SaveManager,
            Chapter2ClosetInteractable interactable,
            Chapter2MissionTriggerZone zone,
            Chapter1InputReader playerInputReader,
            InventoryController playerInventory,
            ItemDefinition confiscatedPhone,
            ItemDefinition confiscatedPoliceKey)
        {
            saveManager = chapter2SaveManager;
            closetInteractable = interactable;
            triggerZone = zone;
            inputReader = playerInputReader;
            inventory = playerInventory;
            phoneDefinition = confiscatedPhone;
            policeKeyDefinition = confiscatedPoliceKey;

            ResolvePlayerReferences(null);
            EnsureMissionUI();
            configured = ValidateRequiredReferences();
            ApplySavedState();
        }

        private void Update()
        {
            if (configured &&
                !sessionOpen &&
                (!savedStateApplied ||
                 lastAppliedUnlocked != IsUnlocked ||
                 lastAppliedCompleted != IsCompleted ||
                 lastAppliedPhoneRecovered != PhoneRecovered ||
                 lastAppliedPoliceKeyRecovered !=
                 PoliceKeyRecovered))
            {
                ApplySavedState();
            }

            if (!sessionOpen || completionRoutine != null)
            {
                return;
            }

            if (HasExternalInputLock())
            {
                CloseSession();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseSession();
            }
        }

        private void OnDisable()
        {
            CloseSessionInternal();
        }

        private void OnDestroy()
        {
            CloseSessionInternal();
        }

        public bool TryOpen(InteractionContext context)
        {
            ResolvePlayerReferences(context.PlayerObject);
            if (!CanInspect || !ValidateRequiredReferences())
            {
                return false;
            }

            EnsureRuntimeEventSystem();
            CaptureAndApplyModalState();
            sessionOpen = true;
            missionUI.Refresh(PhoneRecovered, PoliceKeyRecovered);
            missionUI.Show();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return true;
        }

        public void CloseSession()
        {
            if (completionRoutine == null)
            {
                CloseSessionInternal();
            }
        }

        [ContextMenu("Reset Mission 03 - Confiscated Items")]
        public void ResetMission()
        {
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }

            CloseSessionInternal();
            saveManager?.ResetMission03();
            RemoveInventoryItem(PhoneItemId);
            RemoveInventoryItem(PoliceKeyItemId);
            savedStateApplied = false;
            ApplySavedState();
        }

        private void EnsureMissionUI()
        {
            if (missionUI == null)
            {
                missionUI = Chapter2ConfiscatedItemsUI.Create(transform);
            }

            missionUI.Configure(
                phoneDefinition,
                policeKeyDefinition,
                RecoverPhone,
                RecoverPoliceKey,
                CloseSession);
            missionUI.Hide();
        }

        private void RecoverPhone()
        {
            if (!sessionOpen || completionRoutine != null ||
                PhoneRecovered)
            {
                return;
            }

            if (!EnsureInventoryItem(phoneDefinition))
            {
                Debug.LogError(
                    "[Chapter2Mission03] Không thể thêm điện thoại vào balo.",
                    this);
                return;
            }

            CommitRecovery(true, PoliceKeyRecovered);
            Chapter1EventBus.RaiseNotification(
                "Đã nhận lại điện thoại.");
        }

        private void RecoverPoliceKey()
        {
            if (!sessionOpen || completionRoutine != null ||
                PoliceKeyRecovered)
            {
                return;
            }

            if (!EnsureInventoryItem(policeKeyDefinition))
            {
                Debug.LogError(
                    "[Chapter2Mission03] Không thể thêm chìa khóa của James vào balo.",
                    this);
                return;
            }

            CommitRecovery(PhoneRecovered, true);
            Chapter1EventBus.RaiseNotification(
                "Đã nhận lại chìa khóa của James.");
        }

        private void CommitRecovery(
            bool phoneRecovered,
            bool policeKeyRecovered)
        {
            saveManager.SaveMission03Progress(
                phoneRecovered,
                policeKeyRecovered);
            savedStateApplied = true;
            lastAppliedUnlocked = IsUnlocked;
            lastAppliedCompleted = IsCompleted;
            lastAppliedPhoneRecovered = PhoneRecovered;
            lastAppliedPoliceKeyRecovered = PoliceKeyRecovered;
            missionUI.Refresh(PhoneRecovered, PoliceKeyRecovered);

            if (!IsCompleted)
            {
                return;
            }

            closetInteractable?.DisableInteraction();
            completionRoutine = StartCoroutine(
                CloseAfterCompletionFeedback());
        }

        private IEnumerator CloseAfterCompletionFeedback()
        {
            yield return new WaitForSecondsRealtime(
                CompletionDisplaySeconds);

            completionRoutine = null;
            CloseSessionInternal();
            Chapter1EventBus.RaiseNotification(
                "Đã lấy lại điện thoại và chìa khóa của James.");
            MissionCompleted?.Invoke();
        }

        private void ApplySavedState()
        {
            if (saveManager == null)
            {
                return;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            data.EnsureValidDefaults();
            ReconcileInventoryItem(
                PhoneItemId,
                phoneDefinition,
                data.Mission03PhoneRecovered);
            ReconcileInventoryItem(
                PoliceKeyItemId,
                policeKeyDefinition,
                data.Mission03PoliceKeyRecovered);

            savedStateApplied = true;
            lastAppliedUnlocked = IsUnlocked;
            lastAppliedCompleted = IsCompleted;
            lastAppliedPhoneRecovered = PhoneRecovered;
            lastAppliedPoliceKeyRecovered = PoliceKeyRecovered;
            if (IsUnlocked && !IsCompleted)
            {
                closetInteractable?.EnableInteraction();
            }
            else
            {
                closetInteractable?.DisableInteraction();
            }

            missionUI?.Hide();
        }

        private void ReconcileInventoryItem(
            string itemId,
            ItemDefinition definition,
            bool shouldOwn)
        {
            if (inventory == null)
            {
                return;
            }

            bool ownsItem = inventory.HasItem(itemId);
            if (shouldOwn && !ownsItem)
            {
                EnsureInventoryItem(definition);
            }
            else if (!shouldOwn && ownsItem)
            {
                RemoveInventoryItem(itemId);
            }
        }

        private bool EnsureInventoryItem(ItemDefinition definition)
        {
            return inventory != null &&
                   definition != null &&
                   (inventory.HasItem(definition.ItemId) ||
                    inventory.AddItem(definition));
        }

        private void RemoveInventoryItem(string itemId)
        {
            if (inventory != null && inventory.HasItem(itemId))
            {
                inventory.RemoveItem(itemId);
            }
        }

        private void ResolvePlayerReferences(GameObject playerObject)
        {
            GameObject player = playerObject != null
                ? playerObject
                : inputReader != null
                    ? inputReader.gameObject
                    : null;
            if (player == null)
            {
                return;
            }

            inputReader ??= player.GetComponent<Chapter1InputReader>();
            inputLock = player.GetComponent<PlayerInputLock>();
            interactionController =
                player.GetComponent<Chapter1InteractionController>();
            backpackInput =
                player.GetComponent<BackpackPhoneInputController>();
            playerCombat = player.GetComponent<PlayerCombatController>();
            inventory ??= player.GetComponent<InventoryController>();
        }

        private bool ValidateRequiredReferences()
        {
            bool valid = saveManager != null &&
                         closetInteractable != null &&
                         triggerZone != null &&
                         inputReader != null &&
                         inputLock != null &&
                         interactionController != null &&
                         inventory != null &&
                         phoneDefinition != null &&
                         policeKeyDefinition != null &&
                         missionUI != null;
            if (!valid)
            {
                Debug.LogError(
                    "[Chapter2Mission03] Thiếu reference để chạy tủ đồ tịch thu.",
                    this);
            }

            return valid;
        }

        private void CaptureAndApplyModalState()
        {
            if (modalStateCaptured)
            {
                return;
            }

            modalStateCaptured = true;
            inputReaderWasEnabled =
                inputReader != null &&
                inputReader.GameplayInputEnabled;
            interactionControllerWasEnabled =
                interactionController != null &&
                interactionController.enabled;
            backpackInputWasEnabled =
                backpackInput != null && backpackInput.enabled;
            combatWasEnabled =
                playerCombat != null && playerCombat.enabled;
            cursorLockBeforeSession = Cursor.lockState;
            cursorVisibleBeforeSession = Cursor.visible;

            inputLock?.Lock(InputLockReason);
            ownInputLockHeld = inputLock != null;
            inputReader?.SetGameplayInputEnabled(false);
            if (interactionController != null)
            {
                interactionController.enabled = false;
            }

            if (backpackInput != null)
            {
                backpackInput.enabled = false;
            }

            if (playerCombat != null)
            {
                playerCombat.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreModalState()
        {
            if (!modalStateCaptured)
            {
                ReleaseOwnInputLock();
                return;
            }

            if (playerCombat != null)
            {
                playerCombat.enabled = combatWasEnabled;
            }

            if (backpackInput != null)
            {
                backpackInput.enabled = backpackInputWasEnabled;
            }

            inputReader?.SetGameplayInputEnabled(
                inputReaderWasEnabled);
            ReleaseOwnInputLock();

            if (interactionController != null)
            {
                interactionController.enabled =
                    interactionControllerWasEnabled;
            }

            if (inputLock != null && inputLock.IsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = cursorLockBeforeSession;
                Cursor.visible = cursorVisibleBeforeSession;
            }

            modalStateCaptured = false;
        }

        private void CloseSessionInternal()
        {
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }

            sessionOpen = false;
            missionUI?.Hide();
            RestoreModalState();
            RestoreRuntimeEventSystem();
        }

        private bool HasExternalInputLock()
        {
            if (inputLock == null || !inputLock.IsLocked)
            {
                return false;
            }

            foreach (string reason in inputLock.ActiveLocks)
            {
                if (!string.Equals(
                        reason,
                        InputLockReason,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReleaseOwnInputLock()
        {
            if (ownInputLockHeld && inputLock != null)
            {
                inputLock.Unlock(InputLockReason);
            }

            ownInputLockHeld = false;
        }

        private void EnsureRuntimeEventSystem()
        {
            if (eventSystemStateCaptured)
            {
                return;
            }

            EventSystem eventSystem = FindActiveEventSystem();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject(
                    "Chapter2ConfiscatedItemsEventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(
                    eventSystemObject,
                    gameObject.scene);
                eventSystem =
                    eventSystemObject.GetComponent<EventSystem>();
                eventSystemCreatedForSession = true;
            }
            else
            {
                eventSystemWasActive = eventSystem.gameObject.activeSelf;
            }

            InputSystemUIInputModule inputSystemModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                inputSystemModule = eventSystem.gameObject.AddComponent<
                    InputSystemUIInputModule>();
                inputModuleAddedForSession = true;
            }
            else
            {
                inputModuleWasEnabled = inputSystemModule.enabled;
            }

            BaseInputModule[] modules =
                eventSystem.GetComponents<BaseInputModule>();
            int otherCount = 0;
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null &&
                    modules[i] != inputSystemModule)
                {
                    otherCount++;
                }
            }

            sessionOtherInputModules =
                new BaseInputModule[otherCount];
            sessionOtherInputModuleStates = new bool[otherCount];
            int otherIndex = 0;
            for (int i = 0; i < modules.Length; i++)
            {
                BaseInputModule module = modules[i];
                if (module == null || module == inputSystemModule)
                {
                    continue;
                }

                sessionOtherInputModules[otherIndex] = module;
                sessionOtherInputModuleStates[otherIndex] =
                    module.enabled;
                module.enabled = false;
                otherIndex++;
            }

            inputSystemModule.enabled = true;
            sessionEventSystem = eventSystem;
            sessionInputModule = inputSystemModule;
            eventSystemStateCaptured = true;
        }

        private void RestoreRuntimeEventSystem()
        {
            if (!eventSystemStateCaptured)
            {
                return;
            }

            if (sessionOtherInputModules != null &&
                sessionOtherInputModuleStates != null)
            {
                int count = Mathf.Min(
                    sessionOtherInputModules.Length,
                    sessionOtherInputModuleStates.Length);
                for (int i = 0; i < count; i++)
                {
                    if (sessionOtherInputModules[i] != null)
                    {
                        sessionOtherInputModules[i].enabled =
                            sessionOtherInputModuleStates[i];
                    }
                }
            }

            if (sessionInputModule != null)
            {
                if (inputModuleAddedForSession)
                {
                    sessionInputModule.enabled = false;
                    Destroy(sessionInputModule);
                }
                else
                {
                    sessionInputModule.enabled =
                        inputModuleWasEnabled;
                }
            }

            if (sessionEventSystem != null &&
                !eventSystemCreatedForSession &&
                sessionEventSystem.gameObject.activeSelf !=
                eventSystemWasActive)
            {
                sessionEventSystem.gameObject.SetActive(
                    eventSystemWasActive);
            }

            sessionEventSystem = null;
            sessionInputModule = null;
            sessionOtherInputModules = null;
            sessionOtherInputModuleStates = null;
            eventSystemStateCaptured = false;
            eventSystemCreatedForSession = false;
            eventSystemWasActive = false;
            inputModuleAddedForSession = false;
            inputModuleWasEnabled = false;
        }

        private static EventSystem FindActiveEventSystem()
        {
            EventSystem[] candidates =
                FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null &&
                    candidates[i].gameObject.activeInHierarchy)
                {
                    return candidates[i];
                }
            }

            return null;
        }
    }
}
