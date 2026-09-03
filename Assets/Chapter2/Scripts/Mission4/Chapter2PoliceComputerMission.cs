using System;
using System.Collections;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2PoliceComputerMission : MonoBehaviour
    {
        public const string MissionObjectName =
            "Chapter2_Mission04_PoliceWifi";
        public const string InputLockReason =
            "Chapter2.PoliceComputer";
        public const string ObservationHint =
            "[LMB] Mở máy tính    [ESC] Thoát";
        public const string CompletionNotification =
            "Đã kết nối Wi-Fi đồn cảnh sát. Nhiệm vụ 4 hoàn thành.";

        private Chapter2SaveManager saveManager;
        private Chapter2DeskComputerInteractable deskInteractable;
        private Chapter2MissionTriggerZone triggerZone;
        private Chapter2DesktopClickTarget desktopTarget;
        private Camera desktopCamera;
        private AudioListener desktopAudioListener;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackInput;
        private PlayerCombatController playerCombat;
        private PhoneUIController phoneUI;
        private Camera gameplayCamera;
        private AudioListener gameplayAudioListener;
        private ThirdPersonCameraRig cameraRig;
        private Chapter2PoliceComputerUI computerUI;
        private GameObject observationHintRoot;

        private bool configured;
        private bool observing;
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
        private int lastPhoneStateHash = int.MinValue;
        private Coroutine delayedPhoneConfiguration;

        public event Action MissionCompleted;

        public bool IsObserving => observing;
        public bool ComputerUiVisible =>
            computerUI != null && computerUI.IsVisible;
        public bool IsCompleted =>
            saveManager != null &&
            saveManager.CurrentData.Mission04Completed;
        public bool CanInspect =>
            configured &&
            !observing &&
            saveManager != null &&
            saveManager.CurrentData.Mission03Completed &&
            triggerZone != null &&
            triggerZone.ContainsPlayer &&
            (inputLock == null || !inputLock.IsLocked);

        public void Configure(
            Chapter2SaveManager chapter2SaveManager,
            Chapter2DeskComputerInteractable interactable,
            Chapter2MissionTriggerZone zone,
            Chapter2DesktopClickTarget clickTarget,
            Camera workstationCamera,
            Chapter1InputReader playerInputReader,
            PhoneUIController phoneController)
        {
            saveManager = chapter2SaveManager;
            deskInteractable = interactable;
            triggerZone = zone;
            desktopTarget = clickTarget;
            desktopCamera = workstationCamera;
            desktopAudioListener = desktopCamera != null
                ? desktopCamera.GetComponent<AudioListener>()
                : null;
            inputReader = playerInputReader;
            phoneUI = phoneController;

            ResolvePlayerReferences();
            EnsureComputerUI();
            configured = ValidateRequiredReferences();
            SetDesktopCameraActive(false);
            ConfigurePhoneFromSave();

            if (delayedPhoneConfiguration != null)
            {
                StopCoroutine(delayedPhoneConfiguration);
            }

            delayedPhoneConfiguration = StartCoroutine(
                ConfigurePhoneAfterSceneBootstraps());
        }

        private IEnumerator ConfigurePhoneAfterSceneBootstraps()
        {
            yield return null;
            delayedPhoneConfiguration = null;
            ResolvePhoneReference();
            ConfigurePhoneFromSave();
        }

        private void Update()
        {
            SynchronizePhoneIfNeeded();
            if (!observing)
            {
                return;
            }

            if (HasExternalInputLock())
            {
                EndObservation();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                if (computerUI != null && computerUI.IsVisible)
                {
                    computerUI.Hide();
                    ShowObservationHint();
                }
                else
                {
                    EndObservation();
                }

                return;
            }

            if ((computerUI == null || !computerUI.IsVisible) &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryOpenComputerAtPointer(
                    Mouse.current.position.ReadValue());
            }
        }

        private void OnDisable()
        {
            EndObservation();
        }

        private void OnDestroy()
        {
            EndObservation();
        }

        public bool TryBeginObservation(InteractionContext context)
        {
            ResolvePlayerReferences();
            if (!CanInspect || !ValidateRequiredReferences())
            {
                return false;
            }

            EnsureRuntimeEventSystem();
            CaptureModalState();
            SwitchToDesktopCamera();
            observing = true;
            computerUI?.Hide();
            ShowObservationHint();
            return true;
        }

        public bool TryOpenComputerAtPointer(Vector2 screenPosition)
        {
            if (!observing || desktopCamera == null ||
                desktopTarget == null ||
                desktopTarget.HitCollider == null)
            {
                return false;
            }

            Ray ray = desktopCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                5f,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            bool hitDesktop = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (desktopTarget.Matches(hits[i].collider))
                {
                    hitDesktop = true;
                    break;
                }
            }

            if (!hitDesktop)
            {
                return false;
            }

            HideObservationHint();
            Chapter2SaveData data = saveManager.CurrentData;
            computerUI.Show(
                data.Mission04ComputerUnlocked,
                data.Mission04WifiPasswordDiscovered);
            return true;
        }

        public static bool IsCorrectWifiPassword(string password)
        {
            return string.Equals(
                password,
                Chapter2PoliceComputerUI.WifiPassword,
                StringComparison.Ordinal);
        }

        public bool TryConnectPoliceWifi(string password)
        {
            if (saveManager == null)
            {
                return false;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            if (data.Mission04PoliceWifiConnected)
            {
                return true;
            }

            if (!data.Mission03Completed ||
                !data.Mission04WifiPasswordDiscovered ||
                !IsCorrectWifiPassword(password))
            {
                return false;
            }

            saveManager.SaveMission04WifiConnected();
            ConfigurePhoneFromSave();
            Chapter1EventBus.RaiseNotification(
                CompletionNotification);
            MissionCompleted?.Invoke();
            return true;
        }

        public void EndObservation()
        {
            if (!observing && !modalStateCaptured)
            {
                computerUI?.Hide();
                HideObservationHint();
                return;
            }

            observing = false;
            computerUI?.Hide();
            HideObservationHint();
            RestoreGameplayCamera();
            RestoreModalState();
        }

        [ContextMenu("Reset Mission 04 - Police Wi-Fi")]
        public void ResetMission()
        {
            EndObservation();
            saveManager?.ResetMission04();
            ConfigurePhoneFromSave();
        }

        private void HandleComputerUnlocked()
        {
            saveManager?.SaveMission04ComputerUnlocked();
            SynchronizePhoneIfNeeded(true);
        }

        private void HandleWifiPasswordRevealed()
        {
            saveManager?.SaveMission04WifiPasswordDiscovered();
            ConfigurePhoneFromSave();
            Chapter1EventBus.RaiseNotification(
                "Đã tìm thấy mật khẩu Wi-Fi của đồn cảnh sát.");
        }

        private void HandleMinhMessagesRead()
        {
            if (saveManager == null ||
                saveManager.CurrentData.Mission04MinhMessagesRead)
            {
                return;
            }

            saveManager.SaveMission04MinhMessagesRead();
            SynchronizePhoneIfNeeded(true);
        }

        private void EnsureComputerUI()
        {
            if (computerUI == null)
            {
                computerUI = Chapter2PoliceComputerUI.Create(transform);
            }

            computerUI.Configure(
                HandleComputerUnlocked,
                HandleWifiPasswordRevealed);
            computerUI.Hide();
        }

        private void ConfigurePhoneFromSave()
        {
            if (saveManager == null)
            {
                return;
            }

            ResolvePhoneReference();
            if (phoneUI == null)
            {
                return;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            data.EnsureValidDefaults();
            phoneUI.ConfigureWifiNetwork(
                Chapter2PoliceComputerUI.WifiSsid,
                data.Mission04PoliceWifiConnected,
                data.Mission04WifiPasswordDiscovered,
                TryConnectPoliceWifi);
            phoneUI.ConfigureMinhMissionMessages(
                data.Mission04PoliceWifiConnected,
                data.Mission04MinhMessagesRead,
                HandleMinhMessagesRead);
            lastPhoneStateHash = CalculatePhoneStateHash(data);
        }

        private void SynchronizePhoneIfNeeded(bool force = false)
        {
            if (saveManager == null)
            {
                return;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            int stateHash = CalculatePhoneStateHash(data);
            if (force || stateHash != lastPhoneStateHash ||
                phoneUI == null)
            {
                ConfigurePhoneFromSave();
            }
        }

        private static int CalculatePhoneStateHash(
            Chapter2SaveData data)
        {
            int hash = 17;
            hash = hash * 31 +
                   (data.Mission04WifiPasswordDiscovered ? 1 : 0);
            hash = hash * 31 +
                   (data.Mission04PoliceWifiConnected ? 1 : 0);
            hash = hash * 31 +
                   (data.Mission04MinhMessagesRead ? 1 : 0);
            return hash;
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
                if (candidates[i] != null &&
                    candidates[i].gameObject.scene == gameObject.scene)
                {
                    phoneUI = candidates[i];
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
                         deskInteractable != null &&
                         triggerZone != null &&
                         desktopTarget != null &&
                         desktopTarget.HitCollider != null &&
                         desktopCamera != null &&
                         inputReader != null &&
                         inputLock != null &&
                         interactionController != null &&
                         gameplayCamera != null &&
                         computerUI != null;
            if (!valid)
            {
                Debug.LogError(
                    "[Chapter2Mission04] Thiếu reference cho máy tính/Wi-Fi đồn cảnh sát.",
                    this);
            }

            return valid;
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

        private void SwitchToDesktopCamera()
        {
            gameplayCameraWasEnabled = gameplayCamera.enabled;
            gameplayAudioWasEnabled = gameplayAudioListener != null &&
                                      gameplayAudioListener.enabled;
            cameraRig?.SetLookEnabled(false);
            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled = false;
            }

            gameplayCamera.enabled = false;
            SetDesktopCameraActive(true);
        }

        private void RestoreGameplayCamera()
        {
            SetDesktopCameraActive(false);
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

        private void SetDesktopCameraActive(bool active)
        {
            if (desktopAudioListener != null)
            {
                desktopAudioListener.enabled = active;
            }

            if (desktopCamera != null)
            {
                desktopCamera.enabled = active;
            }
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

        private void ShowObservationHint()
        {
            EnsureObservationHint();
            observationHintRoot.SetActive(true);
        }

        private void HideObservationHint()
        {
            if (observationHintRoot != null)
            {
                observationHintRoot.SetActive(false);
            }
        }

        private void EnsureObservationHint()
        {
            if (observationHintRoot != null)
            {
                return;
            }

            observationHintRoot = new GameObject(
                "Chapter2Mission04ObservationHint",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            observationHintRoot.transform.SetParent(transform, false);

            Canvas canvas = observationHintRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 735;
            CanvasScaler scaler =
                observationHintRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject(
                "HintPanel",
                typeof(RectTransform),
                typeof(Image));
            panelObject.transform.SetParent(
                observationHintRoot.transform,
                false);
            RectTransform panel =
                panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 38f);
            panel.sizeDelta = new Vector2(620f, 66f);
            panelObject.GetComponent<Image>().color =
                new Color(0.015f, 0.045f, 0.07f, 0.9f);

            GameObject textObject = new GameObject(
                "HintText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel, false);
            RectTransform textRect =
                textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 8f);
            textRect.offsetMax = new Vector2(-18f, -8f);
            TextMeshProUGUI hintText =
                textObject.GetComponent<TextMeshProUGUI>();
            hintText.text = ObservationHint;
            hintText.fontSize = 25f;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = new Color(0.55f, 0.9f, 1f, 1f);
            hintText.raycastTarget = false;

            observationHintRoot.SetActive(false);
        }

        private void EnsureRuntimeEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                EventSystem[] candidates =
                    FindObjectsByType<EventSystem>(
                        FindObjectsInactive.Include);
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] != null &&
                        candidates[i].gameObject.activeInHierarchy)
                    {
                        eventSystem = candidates[i];
                        break;
                    }
                }
            }

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject(
                    "Chapter2Mission04EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                UnityEngine.SceneManagement.SceneManager
                    .MoveGameObjectToScene(
                        eventSystemObject,
                        gameObject.scene);
                return;
            }

            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<
                    InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
        }
    }
}
