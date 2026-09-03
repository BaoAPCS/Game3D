using System;
using System.Collections;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2WifiSignalScannerMission : MonoBehaviour
    {
        public const string MissionObjectName =
            "Chapter2_Mission05_WifiSignalScanner";
        public const string InputLockReason =
            "Chapter2.RouterInspection";
        public const string ClassifiedDocumentItemId =
            "classified_document";
        public const float SignalSampleInterval = 0.2f;
        public const string StrongSignalNotification =
            "Tín hiệu cực mạnh — hãy kiểm tra khu vực xung quanh.";
        public const string CompletionNotification =
            "Đã lấy được tài liệu mật. Nhiệm vụ 5 hoàn thành.";

        private readonly Chapter2WifiSignalModel signalModel =
            new Chapter2WifiSignalModel();

        private Chapter2SaveManager saveManager;
        private Transform router;
        private Collider routerSignalCollider;
        private Camera routerCamera;
        private AudioListener routerAudioListener;
        private Chapter2MissionTriggerZone interactionZone;
        private Chapter2RouterInteractable routerInteractable;
        private Chapter1InputReader inputReader;
        private InventoryController inventory;
        private ItemDefinition documentDefinition;
        private GameObject documentVisual;
        private PhoneUIController phoneUI;

        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackInput;
        private PlayerCombatController playerCombat;
        private Camera gameplayCamera;
        private AudioListener gameplayAudioListener;
        private ThirdPersonCameraRig cameraRig;
        private Chapter2RouterInspectionUI inspectionUI;

        private bool configured;
        private bool inspecting;
        private bool modalStateCaptured;
        private bool ownInputLockHeld;
        private bool inputReaderWasEnabled;
        private bool interactionControllerWasEnabled;
        private bool backpackInputWasEnabled;
        private bool combatWasEnabled;
        private bool gameplayCameraWasEnabled;
        private bool gameplayAudioWasEnabled;
        private CursorLockMode cursorLockBeforeSession;
        private bool cursorVisibleBeforeSession;
        private bool strongSignalNotificationShown;
        private float nextSignalSampleTime;
        private float lastSignalSampleTime = -1f;
        private float currentHorizontalDistance = float.MaxValue;
        private int lastPhoneAvailabilityHash = int.MinValue;
        private PhoneUIController configuredPhone;
        private Coroutine delayedRestore;

        public event Action<int> SignalBarsChanged;
        public event Action MissionCompleted;

        public bool IsInspecting => inspecting;
        public int CurrentSignalBars => signalModel.CurrentBars;
        public float CurrentHorizontalDistance =>
            currentHorizontalDistance;
        public bool IsCompleted =>
            saveManager != null &&
            saveManager.CurrentData.Mission05Completed;
        public bool ScannerAvailable =>
            configured &&
            saveManager != null &&
            saveManager.CurrentData.Mission04MinhMessagesRead &&
            !saveManager.CurrentData.Mission05Completed;
        public bool CanInspectRouter =>
            ScannerAvailable &&
            !inspecting &&
            phoneUI != null &&
            phoneUI.IsSignalScannerActive &&
            phoneUI.IsScannerWalkMode &&
            CurrentSignalBars >=
                Chapter2WifiSignalModel.MaximumBars &&
            interactionZone != null &&
            interactionZone.ContainsPlayer &&
            (inputLock == null || !inputLock.IsLocked);

        public void Configure(
            Chapter2SaveManager chapter2SaveManager,
            Transform routerTransform,
            Collider signalCollider,
            Camera inspectionCamera,
            Chapter2MissionTriggerZone zone,
            Chapter2RouterInteractable interactable,
            Chapter1InputReader playerInputReader,
            InventoryController playerInventory,
            ItemDefinition classifiedDocumentDefinition,
            GameObject classifiedDocumentVisual,
            PhoneUIController phoneController)
        {
            saveManager = chapter2SaveManager;
            router = routerTransform;
            routerSignalCollider = signalCollider;
            routerCamera = inspectionCamera;
            routerAudioListener = routerCamera != null
                ? routerCamera.GetComponent<AudioListener>()
                : null;
            interactionZone = zone;
            routerInteractable = interactable;
            inputReader = playerInputReader;
            inventory = playerInventory;
            documentDefinition = classifiedDocumentDefinition;
            documentVisual = classifiedDocumentVisual;
            phoneUI = phoneController;

            ResolvePlayerReferences();
            EnsureInspectionUI();
            configured = ValidateRequiredReferences();
            SetRouterCameraActive(false);
            RestoreWorldProgress();
            ConfigurePhoneScanner(true);

            if (delayedRestore != null)
            {
                StopCoroutine(delayedRestore);
            }

            delayedRestore = StartCoroutine(
                RestoreAfterOtherSceneBootstraps());
        }

        private IEnumerator RestoreAfterOtherSceneBootstraps()
        {
            // Chapter2CarryOverBootstrap clears and then reconstructs the
            // chapter inventory on scene load. Reconcile one frame later so
            // Mission 05 owns the final exact state of its document item.
            yield return null;
            delayedRestore = null;
            ResolvePhoneReference();
            ReconcileDocumentInventory();
            RestoreWorldProgress();
            ConfigurePhoneScanner(true);
        }

        private void Update()
        {
            SynchronizePhoneAvailability();
            if (inspecting)
            {
                UpdateInspectionInput();
                return;
            }

            if (!ScannerAvailable || phoneUI == null ||
                !phoneUI.IsSignalScannerActive)
            {
                return;
            }

            if (phoneUI.IsSignalScannerSuspended)
            {
                return;
            }

            if (HasExternalScannerInputLock())
            {
                phoneUI.StopScanner();
                return;
            }

            if (Time.unscaledTime >= nextSignalSampleTime)
            {
                SampleSignal(false);
            }
        }

        private void OnDisable()
        {
            EndRouterInspection(false);
            phoneUI?.StopScanner();
        }

        private void OnDestroy()
        {
            EndRouterInspection(false);
        }

        public int GetSignalBars()
        {
            return ScannerAvailable
                ? signalModel.CurrentBars
                : Chapter2WifiSignalModel.MinimumBars;
        }

        public bool TryBeginRouterInspection(
            InteractionContext context)
        {
            ResolvePlayerReferences();
            if (!CanInspectRouter || !ValidateRequiredReferences())
            {
                return false;
            }

            phoneUI.SuspendScannerForModal();
            CaptureModalState();
            SwitchToRouterCamera();
            inspecting = true;

            if (!saveManager.CurrentData.Mission05RouterInspected)
            {
                saveManager.SaveMission05RouterInspected();
            }

            SetDocumentVisible(true);
            inspectionUI?.Show(true);
            return true;
        }

        public bool TryCollectClassifiedDocument()
        {
            if (!inspecting || saveManager == null ||
                inventory == null || documentDefinition == null)
            {
                return false;
            }

            bool hasDocument = inventory.HasItem(
                ClassifiedDocumentItemId);
            if (!hasDocument &&
                !inventory.AddItem(documentDefinition))
            {
                Debug.LogError(
                    "[Chapter2Mission05] Không thể thêm tài liệu mật vào balo của Nam.",
                    this);
                return false;
            }

            if (!saveManager.CurrentData
                    .Mission05SecretDocumentCollected)
            {
                saveManager.SaveMission05DocumentCollected();
            }

            SetDocumentVisible(false);
            EndRouterInspection(false);
            phoneUI?.StopScanner();
            ConfigurePhoneScanner(true);
            Chapter1EventBus.RaiseNotification(
                CompletionNotification);
            MissionCompleted?.Invoke();
            return true;
        }

        public void EndRouterInspection(bool resumeScanner)
        {
            if (!inspecting && !modalStateCaptured)
            {
                inspectionUI?.Hide();
                SetRouterCameraActive(false);
                return;
            }

            inspecting = false;
            inspectionUI?.Hide();
            RestoreGameplayCamera();
            RestoreModalState();

            if (resumeScanner && ScannerAvailable &&
                phoneUI != null &&
                phoneUI.IsSignalScannerActive)
            {
                phoneUI.ResumeScannerWalkMode();
                SampleSignal(true);
            }
        }

        [ContextMenu("Reset Mission 05 - Wi-Fi Signal Scanner")]
        public void ResetMission()
        {
            EndRouterInspection(false);
            phoneUI?.StopScanner();
            saveManager?.ResetMission05();
            inventory?.RemoveItem(ClassifiedDocumentItemId);
            SetDocumentVisible(false);
            ConfigurePhoneScanner(true);
        }

        private void HandleScannerActiveChanged(bool active)
        {
            if (!active)
            {
                strongSignalNotificationShown = false;
                lastSignalSampleTime = -1f;
                nextSignalSampleTime = 0f;
                return;
            }

            if (!ScannerAvailable)
            {
                phoneUI?.StopScanner();
                return;
            }

            strongSignalNotificationShown = false;
            lastSignalSampleTime = -1f;
            SampleSignal(true);
        }

        private void SampleSignal(bool resetModel)
        {
            if (inputReader == null || routerSignalCollider == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            currentHorizontalDistance =
                Chapter2WifiSignalModel.HorizontalDistance(
                    inputReader.transform.position,
                    routerSignalCollider.bounds.center);
            int previousBars = signalModel.CurrentBars;
            int nextBars;
            if (resetModel || lastSignalSampleTime < 0f)
            {
                nextBars = signalModel.Reset(
                    currentHorizontalDistance);
            }
            else
            {
                nextBars = signalModel.Update(
                    currentHorizontalDistance,
                    Mathf.Max(0f, now - lastSignalSampleTime));
            }

            lastSignalSampleTime = now;
            nextSignalSampleTime = now + SignalSampleInterval;
            if (nextBars != previousBars)
            {
                SignalBarsChanged?.Invoke(nextBars);
            }

            if (nextBars >= Chapter2WifiSignalModel.MaximumBars &&
                !strongSignalNotificationShown)
            {
                strongSignalNotificationShown = true;
                Chapter1EventBus.RaiseNotification(
                    StrongSignalNotification);
            }
        }

        private void UpdateInspectionInput()
        {
            if (HasExternalModalInputLock())
            {
                EndRouterInspection(false);
                phoneUI?.StopScanner();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                TryCollectClassifiedDocument();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                EndRouterInspection(true);
            }
        }

        private void SynchronizePhoneAvailability()
        {
            ResolvePhoneReference();
            int hash = CalculateAvailabilityHash();
            if (phoneUI != configuredPhone ||
                hash != lastPhoneAvailabilityHash)
            {
                ConfigurePhoneScanner(true);
            }
        }

        private void ConfigurePhoneScanner(bool force)
        {
            ResolvePhoneReference();
            if (phoneUI == null || saveManager == null)
            {
                return;
            }

            int hash = CalculateAvailabilityHash();
            if (!force && phoneUI == configuredPhone &&
                hash == lastPhoneAvailabilityHash)
            {
                return;
            }

            bool available = ScannerAvailable;
            if (!available && phoneUI.IsSignalScannerActive)
            {
                phoneUI.StopScanner();
            }

            phoneUI.ConfigureWifiSignalScanner(
                available,
                GetSignalBars,
                HandleScannerActiveChanged);
            configuredPhone = phoneUI;
            lastPhoneAvailabilityHash = hash;
        }

        private int CalculateAvailabilityHash()
        {
            if (saveManager == null)
            {
                return 0;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            int hash = 17;
            hash = hash * 31 +
                   (data.Mission04MinhMessagesRead ? 1 : 0);
            hash = hash * 31 +
                   (data.Mission05RouterInspected ? 1 : 0);
            hash = hash * 31 +
                   (data.Mission05SecretDocumentCollected ? 1 : 0);
            return hash;
        }

        private void RestoreWorldProgress()
        {
            if (saveManager == null)
            {
                SetDocumentVisible(false);
                return;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            data.EnsureValidDefaults();
            SetDocumentVisible(
                data.Mission05RouterInspected &&
                !data.Mission05SecretDocumentCollected);
            inspectionUI?.Hide();
            SetRouterCameraActive(false);
        }

        private void ReconcileDocumentInventory()
        {
            if (saveManager == null || inventory == null)
            {
                return;
            }

            bool shouldOwn = saveManager.CurrentData
                .Mission05SecretDocumentCollected;
            bool owns = inventory.HasItem(ClassifiedDocumentItemId);
            if (!shouldOwn)
            {
                if (owns)
                {
                    inventory.RemoveItem(ClassifiedDocumentItemId);
                }

                return;
            }

            if (!owns && documentDefinition != null &&
                !inventory.AddItem(documentDefinition))
            {
                Debug.LogError(
                    "[Chapter2Mission05] Không thể khôi phục tài liệu mật vào balo.",
                    this);
            }
        }

        private void ResolvePhoneReference()
        {
            if (phoneUI != null)
            {
                return;
            }

            PhoneUIController[] candidates =
                FindObjectsByType<PhoneUIController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                PhoneUIController candidate = candidates[i];
                if (candidate != null &&
                    candidate.gameObject.scene == gameObject.scene)
                {
                    phoneUI = candidate;
                    return;
                }
            }

            if (backpackInput != null)
            {
                phoneUI = backpackInput.PhoneUIController;
            }
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
            backpackInput =
                player.GetComponent<BackpackPhoneInputController>();
            playerCombat = player.GetComponent<PlayerCombatController>();
            inventory ??= player.GetComponent<InventoryController>();
            gameplayCamera = interactionController != null
                ? interactionController.GameplayCamera
                : Camera.main;
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
            bool valid = saveManager != null &&
                         router != null &&
                         routerSignalCollider != null &&
                         routerCamera != null &&
                         interactionZone != null &&
                         routerInteractable != null &&
                         inputReader != null &&
                         inputLock != null &&
                         interactionController != null &&
                         inventory != null &&
                         gameplayCamera != null &&
                         documentDefinition != null &&
                         documentVisual != null &&
                         inspectionUI != null;
            if (!valid)
            {
                Debug.LogError(
                    "[Chapter2Mission05] Thiếu reference cho Wi-Fi Signal Scanner/router/tài liệu mật.",
                    this);
            }

            return valid;
        }

        private void EnsureInspectionUI()
        {
            if (inspectionUI == null)
            {
                inspectionUI =
                    Chapter2RouterInspectionUI.Create(transform);
            }

            inspectionUI.Hide();
        }

        private void SetDocumentVisible(bool visible)
        {
            if (documentVisual != null)
            {
                documentVisual.SetActive(visible);
            }
        }

        private void CaptureModalState()
        {
            if (modalStateCaptured)
            {
                return;
            }

            modalStateCaptured = true;
            inputReaderWasEnabled = inputReader.GameplayInputEnabled;
            interactionControllerWasEnabled =
                interactionController.enabled;
            backpackInputWasEnabled =
                backpackInput != null && backpackInput.enabled;
            combatWasEnabled =
                playerCombat != null && playerCombat.enabled;
            cursorLockBeforeSession = Cursor.lockState;
            cursorVisibleBeforeSession = Cursor.visible;

            inputLock.Lock(InputLockReason);
            ownInputLockHeld = true;
            inputReader.SetGameplayInputEnabled(false);
            interactionController.enabled = false;
            if (backpackInput != null)
            {
                backpackInput.enabled = false;
            }

            if (playerCombat != null)
            {
                playerCombat.enabled = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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

        private void SwitchToRouterCamera()
        {
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

            SetRouterCameraActive(true);
        }

        private void RestoreGameplayCamera()
        {
            SetRouterCameraActive(false);
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

        private void SetRouterCameraActive(bool active)
        {
            if (routerAudioListener != null)
            {
                routerAudioListener.enabled = active;
            }

            if (routerCamera != null)
            {
                routerCamera.enabled = active;
            }
        }

        private bool HasExternalScannerInputLock()
        {
            if (inputLock == null || !inputLock.IsLocked)
            {
                return false;
            }

            foreach (string reason in inputLock.ActiveLocks)
            {
                if (!string.Equals(
                        reason,
                        PlayerInputLock.PhoneReason,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasExternalModalInputLock()
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
    }
}
