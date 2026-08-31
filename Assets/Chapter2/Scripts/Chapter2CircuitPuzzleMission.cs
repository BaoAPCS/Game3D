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
    public sealed class Chapter2CircuitPuzzleMission : MonoBehaviour
    {
        private const string InputLockReason =
            "Chapter2CircuitPuzzle";
        private const string CompletionNotification =
            "Đã vô hiệu hóa cửa phòng giam";
        private const float CompletionDisplaySeconds = 0.65f;

        private Chapter2SaveManager saveManager;
        private Chapter2CircuitBoxInteractable boxInteractable;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackInput;
        private PlayerCombatController playerCombat;
        private GameObject jailObstacle;
        private Chapter2CircuitPuzzleUI missionUI;
        private Chapter2CircuitPuzzle puzzle;
        private Coroutine completionRoutine;

        private int selectedTileIndex = -1;
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
        private bool savedCompletionStateApplied;
        private bool lastAppliedCompletionState;
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

        public bool HasServiceCard =>
            saveManager != null &&
            saveManager.CurrentData.Mission01ServiceCardCollected;

        public bool IsCompleted =>
            saveManager != null &&
            saveManager.CurrentData.Mission02JailObstacleDisabled;

        public bool IsSessionOpen => sessionOpen;

        public bool CanActivate =>
            configured &&
            !sessionOpen &&
            HasServiceCard &&
            !IsCompleted &&
            (inputLock == null || !inputLock.IsLocked);

        public void Configure(
            Chapter2SaveManager chapter2SaveManager,
            Chapter2CircuitBoxInteractable electricBoxInteractable,
            Chapter1InputReader playerInputReader,
            GameObject obstacle)
        {
            saveManager = chapter2SaveManager;
            boxInteractable = electricBoxInteractable;
            inputReader = playerInputReader;
            jailObstacle = obstacle;
            puzzle ??= new Chapter2CircuitPuzzle();

            ResolvePlayerReferences(null);
            EnsureMissionUI();
            configured = ValidateRequiredReferences();
            ApplySavedState();
        }

        private void Update()
        {
            if (configured &&
                !sessionOpen &&
                (!savedCompletionStateApplied ||
                 lastAppliedCompletionState != IsCompleted))
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
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseSession();
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetPuzzleBoard();
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                RotateSelectedTile();
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                MoveSelection(-1, 0);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                MoveSelection(1, 0);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                MoveSelection(0, -1);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                MoveSelection(0, 1);
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
            if (!CanActivate || !ValidateRequiredReferences())
            {
                return false;
            }

            EnsureRuntimeEventSystem();
            CaptureAndApplyModalState();
            selectedTileIndex = -1;
            sessionOpen = true;
            missionUI.Show();
            missionUI.Refresh(puzzle, selectedTileIndex);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return true;
        }

        public void CloseSession()
        {
            if (completionRoutine != null)
            {
                return;
            }

            CloseSessionInternal();
        }

        [ContextMenu("Reset Mission 02 - Circuit Puzzle")]
        public void ResetMission()
        {
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }

            CloseSessionInternal();
            puzzle ??= new Chapter2CircuitPuzzle();
            puzzle.Reset();
            selectedTileIndex = -1;
            saveManager?.ResetMission02();
            savedCompletionStateApplied = true;
            lastAppliedCompletionState = false;
            if (jailObstacle != null)
            {
                jailObstacle.SetActive(true);
            }

            boxInteractable?.EnableInteraction();
        }

        private void EnsureMissionUI()
        {
            if (missionUI == null)
            {
                missionUI = Chapter2CircuitPuzzleUI.Create(transform);
            }

            missionUI.Configure(
                SelectTile,
                RotateSelectedTile,
                ResetPuzzleBoard,
                CloseSession);
            missionUI.Hide();
        }

        private void SelectTile(int index)
        {
            if (!sessionOpen ||
                completionRoutine != null ||
                index < 0 ||
                index >= Chapter2CircuitPuzzle.TileCount)
            {
                return;
            }

            selectedTileIndex = index;
            missionUI.Refresh(puzzle, selectedTileIndex);
        }

        private void MoveSelection(int deltaX, int deltaY)
        {
            int x;
            int y;
            if (selectedTileIndex < 0)
            {
                x = 0;
                y = 0;
            }
            else
            {
                x = selectedTileIndex % Chapter2CircuitPuzzle.Width;
                y = selectedTileIndex / Chapter2CircuitPuzzle.Width;
                x = Mathf.Clamp(
                    x + deltaX,
                    0,
                    Chapter2CircuitPuzzle.Width - 1);
                y = Mathf.Clamp(
                    y + deltaY,
                    0,
                    Chapter2CircuitPuzzle.Height - 1);
            }

            SelectTile(y * Chapter2CircuitPuzzle.Width + x);
        }

        private void RotateSelectedTile()
        {
            if (!sessionOpen ||
                completionRoutine != null ||
                selectedTileIndex < 0 ||
                selectedTileIndex >= Chapter2CircuitPuzzle.TileCount)
            {
                return;
            }

            int x = selectedTileIndex % Chapter2CircuitPuzzle.Width;
            int y = selectedTileIndex / Chapter2CircuitPuzzle.Width;
            puzzle.RotateClockwise(x, y);
            missionUI.Refresh(puzzle, selectedTileIndex);
            if (puzzle.IsSolved)
            {
                CommitMissionCompletion();
                completionRoutine =
                    StartCoroutine(CloseAfterCompletionFeedback());
            }
        }

        private void ResetPuzzleBoard()
        {
            if (!sessionOpen || completionRoutine != null)
            {
                return;
            }

            puzzle.Reset();
            selectedTileIndex = -1;
            missionUI.Refresh(puzzle, selectedTileIndex);
        }

        private void CommitMissionCompletion()
        {
            saveManager.SaveMission02Completed();
            savedCompletionStateApplied = true;
            lastAppliedCompletionState = true;
            if (jailObstacle != null)
            {
                jailObstacle.SetActive(false);
            }

            boxInteractable?.DisableInteraction();
        }

        private IEnumerator CloseAfterCompletionFeedback()
        {
            yield return new WaitForSecondsRealtime(
                CompletionDisplaySeconds);

            completionRoutine = null;
            CloseSessionInternal();
            Chapter1EventBus.RaiseNotification(
                CompletionNotification);
            MissionCompleted?.Invoke();
        }

        private void ApplySavedState()
        {
            bool completed = IsCompleted;
            savedCompletionStateApplied = true;
            lastAppliedCompletionState = completed;
            if (jailObstacle != null)
            {
                jailObstacle.SetActive(!completed);
            }

            if (completed)
            {
                boxInteractable?.DisableInteraction();
            }
            else
            {
                boxInteractable?.EnableInteraction();
            }

            missionUI?.Hide();
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

            if (inputReader != null)
            {
                inputReader.SetGameplayInputEnabled(false);
            }

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

            if (inputReader != null)
            {
                inputReader.SetGameplayInputEnabled(
                    inputReaderWasEnabled);
            }

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
        }

        private bool ValidateRequiredReferences()
        {
            bool valid = saveManager != null &&
                         boxInteractable != null &&
                         inputReader != null &&
                         inputLock != null &&
                         interactionController != null &&
                         jailObstacle != null &&
                         missionUI != null &&
                         puzzle != null;
            if (!valid)
            {
                Debug.LogError(
                    "[Chapter2Mission02] Thiếu reference để chạy minigame mạch điện.",
                    this);
            }

            return valid;
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
                    "Chapter2CircuitEventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(
                    eventSystemObject,
                    gameObject.scene);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
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
                sessionOtherInputModuleStates[otherIndex] = module.enabled;
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
