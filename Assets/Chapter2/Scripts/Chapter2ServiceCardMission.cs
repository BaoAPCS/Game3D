using System;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter2
{
    public enum Chapter2ServiceCardMissionState
    {
        AwaitingBedInspection = 0,
        InspectingBed = 1,
        CrowbarCollected = 2,
        PryingToilet = 3,
        ServiceCardRevealed = 4,
        Completed = 5
    }

    [DisallowMultipleComponent]
    public sealed class Chapter2ServiceCardMission : MonoBehaviour
    {
        public const string CrowbarItemId = "crow_bar";
        public const string ServiceCardItemId = "service_card";

        private const string InputLockReason =
            "Chapter2.Mission01.ServiceCard";

        [SerializeField, Range(0.01f, 1f)]
        private float pryProgressPerPress = 0.12f;
        [SerializeField, Min(0f)]
        private float pryDecayPerSecond = 0.18f;

        private GameObject crowbarObject;
        private GameObject serviceCardObject;
        private Camera bedCamera;
        private AudioListener bedAudioListener;
        private Collider toiletCollider;
        private Chapter2MissionTriggerZone bedZone;
        private Chapter2MissionTriggerZone toiletZone;
        private Chapter2BedInspectionInteractable bedInteractable;
        private Chapter2ServiceCardInteractable cardInteractable;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private InventoryController inventory;
        private BackpackPhoneInputController backpackInput;
        private PlayerVisualController playerVisual;
        private PlayerCombatController playerCombat;
        private Camera gameplayCamera;
        private AudioListener gameplayAudioListener;
        private ThirdPersonCameraRig cameraRig;
        private ItemDefinition crowbarDefinition;
        private ItemDefinition serviceCardDefinition;
        private Chapter2SaveManager saveManager;
        private Chapter2ServiceCardMissionUI missionUI;

        private Chapter2ServiceCardMissionState state =
            Chapter2ServiceCardMissionState.AwaitingBedInspection;
        private float pryProgress;
        private bool configured;
        private bool isObservingBed;
        private bool isPryingToilet;
        private bool ownInputLockHeld;

        private bool modalStateCaptured;
        private bool interactionControllerWasEnabled;
        private bool backpackInputWasEnabled;
        private bool combatWasEnabled;
        private bool combatDisabledForModal;
        private bool playerVisualWasVisible;
        private bool playerVisualHiddenForModal;
        private bool gameplayCameraWasEnabled;
        private bool gameplayAudioWasEnabled;

        public event Action<Chapter2ServiceCardMissionState>
            StateChanged;
        public event Action MissionCompleted;

        public Chapter2ServiceCardMissionState State => state;
        public float PryProgress => pryProgress;
        public bool IsObservingBed => isObservingBed;
        public bool IsPryingToilet => isPryingToilet;
        public bool HasCrowbar =>
            state >= Chapter2ServiceCardMissionState.CrowbarCollected;
        public bool HasServiceCard =>
            state == Chapter2ServiceCardMissionState.Completed;

        public bool CanInspectBed =>
            configured &&
            !isObservingBed &&
            !isPryingToilet &&
            state == Chapter2ServiceCardMissionState
                .AwaitingBedInspection &&
            bedZone != null &&
            bedZone.ContainsPlayer &&
            !HasAnyInputLock();

        public bool CanCollectServiceCard =>
            configured &&
            !isObservingBed &&
            !isPryingToilet &&
            state == Chapter2ServiceCardMissionState
                .ServiceCardRevealed &&
            !HasAnyInputLock();

        public void Configure(
            GameObject crowbar,
            GameObject serviceCard,
            Camera inspectionCamera,
            AudioListener inspectionAudioListener,
            Collider toiletInteractionCollider,
            Chapter2MissionTriggerZone bedTriggerZone,
            Chapter2MissionTriggerZone toiletTriggerZone,
            Chapter2BedInspectionInteractable inspectionInteractable,
            Chapter2ServiceCardInteractable serviceCardInteractable,
            Chapter1InputReader playerInputReader,
            Camera playerGameplayCamera,
            ItemDefinition crowbarItem,
            ItemDefinition serviceCardItem,
            Chapter2SaveManager chapter2SaveManager)
        {
            crowbarObject = crowbar;
            serviceCardObject = serviceCard;
            bedCamera = inspectionCamera;
            bedAudioListener = inspectionAudioListener;
            toiletCollider = toiletInteractionCollider;
            bedZone = bedTriggerZone;
            toiletZone = toiletTriggerZone;
            bedInteractable = inspectionInteractable;
            cardInteractable = serviceCardInteractable;
            inputReader = playerInputReader;
            gameplayCamera = playerGameplayCamera;
            crowbarDefinition = crowbarItem;
            serviceCardDefinition = serviceCardItem;
            saveManager = chapter2SaveManager;

            ResolvePlayerReferences();

            if (missionUI == null)
            {
                missionUI = Chapter2ServiceCardMissionUI.Create(
                    transform);
            }

            bedInteractable?.Configure(this, bedZone);
            cardInteractable?.Configure(this);
            SetBedCameraActive(false);
            configured = ValidateRequiredReferences();
            if (configured)
            {
                RestoreProgress();
            }
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            if (isObservingBed)
            {
                UpdateBedObservation();
                return;
            }

            if (isPryingToilet)
            {
                UpdateToiletMinigame();
                return;
            }

            UpdateWorldPrompt();
        }

        private void OnDisable()
        {
            if (isObservingBed)
            {
                EndBedObservation();
            }

            if (isPryingToilet)
            {
                CancelToiletMinigame();
            }

            RestoreModalState();
            SetBedCameraActive(false);
            missionUI?.HideAll();
        }

        private void OnValidate()
        {
            pryProgressPerPress = Mathf.Clamp(
                pryProgressPerPress,
                0.01f,
                1f);
            pryDecayPerSecond = Mathf.Max(
                0f,
                pryDecayPerSecond);
        }

        public bool TryBeginBedObservation(
            InteractionContext context)
        {
            if (!CanInspectBed || context.PlayerObject == null)
            {
                return false;
            }

            if (gameplayCamera == null &&
                context.InteractionController != null)
            {
                gameplayCamera =
                    context.InteractionController.GameplayCamera;
                ResolveCameraReferences();
            }

            if (gameplayCamera == null || bedCamera == null)
            {
                Chapter1EventBus.RaiseNotification(
                    "Không thể chuyển sang góc quan sát lúc này.");
                return false;
            }

            CaptureAndApplyModalState(
                hidePlayerVisual: true,
                disableCombat: false);
            CaptureAndSwitchToBedCamera();
            isObservingBed = true;
            SetState(
                Chapter2ServiceCardMissionState.InspectingBed);
            UpdateObservationPrompt();
            return true;
        }

        public bool TryCollectServiceCard()
        {
            if (!CanCollectServiceCard ||
                serviceCardObject == null ||
                !EnsureInventoryItem(serviceCardDefinition))
            {
                return false;
            }

            cardInteractable?.DisableInteraction();
            serviceCardObject.SetActive(false);
            SetState(Chapter2ServiceCardMissionState.Completed);
            SaveProgress(
                crowbarCollected: true,
                toiletPried: true,
                serviceCardCollected: true);
            MissionCompleted?.Invoke();
            return true;
        }

        [ContextMenu("Reset Mission 01 - Service Card")]
        public void ResetMission()
        {
            if (isObservingBed)
            {
                EndBedObservation();
            }

            if (isPryingToilet)
            {
                CancelToiletMinigame();
            }

            inventory?.RemoveItem(CrowbarItemId);
            inventory?.RemoveItem(ServiceCardItemId);

            pryProgress = 0f;
            crowbarObject?.SetActive(true);
            serviceCardObject?.SetActive(false);
            cardInteractable?.DisableInteraction();
            bedInteractable?.EnableInteraction();
            SetState(
                Chapter2ServiceCardMissionState
                    .AwaitingBedInspection);
            missionUI?.HideAll();
            saveManager?.ResetMission01();
        }

        public static float ApplyPryPress(
            float currentProgress,
            float progressPerPress)
        {
            return Mathf.Clamp01(
                currentProgress + Mathf.Max(0f, progressPerPress));
        }

        public static float ApplyPryDecay(
            float currentProgress,
            float decayPerSecond,
            float deltaTime)
        {
            return Mathf.Clamp01(
                currentProgress -
                Mathf.Max(0f, decayPerSecond) *
                Mathf.Max(0f, deltaTime));
        }

        private void UpdateBedObservation()
        {
            if (HasExternalInputLock())
            {
                EndBedObservation();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (!HasCrowbar &&
                keyboard.eKey.wasPressedThisFrame)
            {
                TryCollectCrowbar();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                EndBedObservation();
            }
        }

        private void UpdateObservationPrompt()
        {
            missionUI?.HideProgress();
            missionUI?.SetPrompt(
                HasCrowbar
                    ? "[Esc] Thoát quan sát"
                    : "[E] Nhặt xà beng\n[Esc] Thoát quan sát");
        }

        private void TryCollectCrowbar()
        {
            if (HasCrowbar ||
                !isObservingBed ||
                !EnsureInventoryItem(crowbarDefinition))
            {
                return;
            }

            crowbarObject?.SetActive(false);
            SetState(
                Chapter2ServiceCardMissionState.CrowbarCollected);
            SaveProgress(
                crowbarCollected: true,
                toiletPried: false,
                serviceCardCollected: false);
            UpdateObservationPrompt();
            Chapter1EventBus.RaiseNotification(
                "Đã nhặt xà beng và cất vào balo.");
        }

        private void EndBedObservation()
        {
            if (!isObservingBed)
            {
                return;
            }

            isObservingBed = false;
            RestoreGameplayCamera();
            RestoreModalState();
            missionUI?.HideAll();

            if (!HasCrowbar)
            {
                SetState(
                    Chapter2ServiceCardMissionState
                        .AwaitingBedInspection);
            }
        }

        private void UpdateWorldPrompt()
        {
            if (state != Chapter2ServiceCardMissionState
                    .CrowbarCollected ||
                toiletZone == null ||
                !toiletZone.ContainsPlayer ||
                HasAnyInputLock())
            {
                missionUI?.SetPrompt(string.Empty);
                missionUI?.HideProgress();
                return;
            }

            missionUI?.SetPrompt("[Enter] Cạy toilet");
            if (WasEnterPressedThisFrame())
            {
                BeginToiletMinigame();
                AddPryProgress();
            }
        }

        private void BeginToiletMinigame()
        {
            if (isPryingToilet ||
                state != Chapter2ServiceCardMissionState
                    .CrowbarCollected ||
                toiletZone == null ||
                !toiletZone.ContainsPlayer ||
                HasAnyInputLock())
            {
                return;
            }

            CaptureAndApplyModalState(
                hidePlayerVisual: false,
                disableCombat: true);
            isPryingToilet = true;
            pryProgress = 0f;
            SetState(
                Chapter2ServiceCardMissionState.PryingToilet);
            missionUI?.SetPrompt(
                "Nhấn [Enter] liên tục!  [Esc] Hủy");
            missionUI?.SetPryProgress(pryProgress);
        }

        private void UpdateToiletMinigame()
        {
            if (HasExternalInputLock())
            {
                CancelToiletMinigame();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelToiletMinigame();
                return;
            }

            if (WasEnterPressedThisFrame())
            {
                AddPryProgress();
            }
            else
            {
                pryProgress = ApplyPryDecay(
                    pryProgress,
                    pryDecayPerSecond,
                    Time.unscaledDeltaTime);
            }

            missionUI?.SetPryProgress(pryProgress);
            if (pryProgress >= 1f)
            {
                CompleteToiletMinigame();
            }
        }

        private void AddPryProgress()
        {
            pryProgress = ApplyPryPress(
                pryProgress,
                pryProgressPerPress);
            missionUI?.SetPryProgress(pryProgress);
        }

        private void CompleteToiletMinigame()
        {
            if (!isPryingToilet)
            {
                return;
            }

            isPryingToilet = false;
            pryProgress = 1f;
            serviceCardObject?.SetActive(true);
            cardInteractable?.EnableInteraction();
            SetState(
                Chapter2ServiceCardMissionState
                    .ServiceCardRevealed);
            RestoreModalState();
            missionUI?.HideAll();
            SaveProgress(
                crowbarCollected: true,
                toiletPried: true,
                serviceCardCollected: false);
            Chapter1EventBus.RaiseNotification(
                "Toilet đã được cạy ra. Thẻ kích hoạt đã lộ diện.");
        }

        private void CancelToiletMinigame()
        {
            if (!isPryingToilet)
            {
                return;
            }

            isPryingToilet = false;
            pryProgress = 0f;
            SetState(
                Chapter2ServiceCardMissionState.CrowbarCollected);
            RestoreModalState();
            missionUI?.HideAll();
        }

        private void CaptureAndApplyModalState(
            bool hidePlayerVisual,
            bool disableCombat)
        {
            if (modalStateCaptured)
            {
                return;
            }

            modalStateCaptured = true;
            interactionControllerWasEnabled =
                interactionController != null &&
                interactionController.enabled;
            backpackInputWasEnabled =
                backpackInput != null && backpackInput.enabled;
            combatWasEnabled =
                playerCombat != null && playerCombat.enabled;
            combatDisabledForModal = disableCombat;
            playerVisualWasVisible =
                playerVisual == null || playerVisual.IsVisible;
            playerVisualHiddenForModal = hidePlayerVisual;

            inputLock?.Lock(InputLockReason);
            ownInputLockHeld = inputLock != null;

            if (interactionController != null)
            {
                interactionController.enabled = false;
            }

            if (backpackInput != null)
            {
                backpackInput.enabled = false;
            }

            if (disableCombat && playerCombat != null)
            {
                playerCombat.enabled = false;
            }

            if (hidePlayerVisual)
            {
                playerVisual?.SetVisible(false);
            }
        }

        private void RestoreModalState()
        {
            if (!modalStateCaptured)
            {
                ReleaseOwnInputLock();
                return;
            }

            if (playerVisualHiddenForModal && playerVisual != null)
            {
                playerVisual.SetVisible(playerVisualWasVisible);
            }

            if (combatDisabledForModal && playerCombat != null)
            {
                playerCombat.enabled = combatWasEnabled;
            }

            if (backpackInput != null)
            {
                backpackInput.enabled = backpackInputWasEnabled;
            }

            ReleaseOwnInputLock();

            if (interactionController != null)
            {
                interactionController.enabled =
                    interactionControllerWasEnabled;
            }

            modalStateCaptured = false;
            combatDisabledForModal = false;
            playerVisualHiddenForModal = false;
        }

        private void CaptureAndSwitchToBedCamera()
        {
            ResolveCameraReferences();
            gameplayCameraWasEnabled =
                gameplayCamera != null && gameplayCamera.enabled;
            gameplayAudioWasEnabled =
                gameplayAudioListener != null &&
                gameplayAudioListener.enabled;

            cameraRig?.SetLookEnabled(false);
            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled = false;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = false;
            }

            SetBedCameraActive(true);
        }

        private void RestoreGameplayCamera()
        {
            SetBedCameraActive(false);
            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = gameplayCameraWasEnabled;
            }

            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled =
                    gameplayAudioWasEnabled;
            }

            cameraRig?.SetLookEnabled(true);
        }

        private void SetBedCameraActive(bool active)
        {
            if (bedAudioListener != null)
            {
                bedAudioListener.enabled = active;
            }

            if (bedCamera != null)
            {
                bedCamera.enabled = active;
            }
        }

        private void RestoreProgress()
        {
            Chapter2SaveData data = saveManager != null
                ? saveManager.CurrentData
                : Chapter2SaveData.CreateDefault();
            data.EnsureValidDefaults();

            bool cardCollected =
                data.Mission01ServiceCardCollected;
            bool toiletPried = data.Mission01ToiletPried;
            bool crowbarCollected =
                data.Mission01CrowbarCollected;

            if (crowbarCollected)
            {
                EnsureInventoryItem(crowbarDefinition);
                crowbarObject?.SetActive(false);
            }
            else
            {
                inventory?.RemoveItem(CrowbarItemId);
                crowbarObject?.SetActive(true);
            }

            if (cardCollected)
            {
                EnsureInventoryItem(serviceCardDefinition);
                serviceCardObject?.SetActive(false);
                cardInteractable?.DisableInteraction();
                SetState(
                    Chapter2ServiceCardMissionState.Completed);
            }
            else if (toiletPried)
            {
                inventory?.RemoveItem(ServiceCardItemId);
                serviceCardObject?.SetActive(true);
                cardInteractable?.EnableInteraction();
                SetState(
                    Chapter2ServiceCardMissionState
                        .ServiceCardRevealed);
            }
            else
            {
                inventory?.RemoveItem(ServiceCardItemId);
                serviceCardObject?.SetActive(false);
                cardInteractable?.DisableInteraction();
                SetState(
                    crowbarCollected
                        ? Chapter2ServiceCardMissionState
                            .CrowbarCollected
                        : Chapter2ServiceCardMissionState
                            .AwaitingBedInspection);
            }

            pryProgress = toiletPried ? 1f : 0f;
            missionUI?.HideAll();
            SetBedCameraActive(false);
        }

        private void ResolvePlayerReferences()
        {
            if (inputReader == null)
            {
                return;
            }

            GameObject player = inputReader.gameObject;
            inputLock = player.GetComponent<PlayerInputLock>();
            interactionController =
                player.GetComponent<Chapter1InteractionController>();
            inventory = player.GetComponent<InventoryController>();
            backpackInput =
                player.GetComponent<BackpackPhoneInputController>();
            playerVisual =
                player.GetComponentInChildren<PlayerVisualController>(true);
            playerCombat =
                player.GetComponent<PlayerCombatController>();
            ResolveCameraReferences();
        }

        private void ResolveCameraReferences()
        {
            if (gameplayCamera == null &&
                interactionController != null)
            {
                gameplayCamera =
                    interactionController.GameplayCamera;
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            gameplayAudioListener = gameplayCamera != null
                ? gameplayCamera.GetComponent<AudioListener>()
                : null;
            cameraRig = gameplayCamera != null
                ? gameplayCamera.GetComponentInParent<
                    ThirdPersonCameraRig>()
                : null;
        }

        private bool ValidateRequiredReferences()
        {
            bool valid = crowbarObject != null &&
                         serviceCardObject != null &&
                         bedCamera != null &&
                         bedZone != null &&
                         toiletZone != null &&
                         toiletCollider != null &&
                         bedInteractable != null &&
                         cardInteractable != null &&
                         inputReader != null &&
                         inputLock != null &&
                         interactionController != null &&
                         inventory != null &&
                         gameplayCamera != null &&
                         crowbarDefinition != null &&
                         serviceCardDefinition != null &&
                         saveManager != null;

            if (!valid)
            {
                Debug.LogError(
                    "[Chapter2Mission01] Thiếu reference để chạy nhiệm vụ lấy thẻ kích hoạt.",
                    this);
            }

            return valid;
        }

        private bool EnsureInventoryItem(ItemDefinition definition)
        {
            if (inventory == null || definition == null)
            {
                return false;
            }

            return inventory.HasItem(definition.ItemId) ||
                   inventory.AddItem(definition);
        }

        private void SaveProgress(
            bool crowbarCollected,
            bool toiletPried,
            bool serviceCardCollected)
        {
            saveManager?.SaveMission01Progress(
                crowbarCollected,
                toiletPried,
                serviceCardCollected);
        }

        private bool HasAnyInputLock()
        {
            return inputLock != null && inputLock.IsLocked;
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

        private static bool WasEnterPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        private void SetState(
            Chapter2ServiceCardMissionState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state);
        }
    }
}
