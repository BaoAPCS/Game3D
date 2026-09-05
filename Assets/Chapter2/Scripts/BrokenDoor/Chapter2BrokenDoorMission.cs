using System;
using System.Collections;
using DormitoryMystery.Chapter1;
using NavKeypad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2BrokenDoorMission : MonoBehaviour
    {
        public const string MissionObjectName =
            "Chapter2_Mission05_BrokenDoor";
        public const string InputLockReason =
            "Chapter2.BrokenDoorInspection";
        public const int CorrectKeypadCombo = 13071980;
        public const float DefaultDoorOpenHeight = 2.2f;
        public const float DefaultDoorOpenSpeed = 1.5f;

        private readonly RaycastHit[] keypadHitBuffer =
            new RaycastHit[32];

        private Chapter2SaveManager saveManager;
        private SphereCollider contractCollider;
        private Chapter2MissionTriggerZone contractZone;
        private Camera contractCamera;
        private AudioListener contractAudioListener;
        private SphereCollider keypadCollider;
        private Chapter2MissionTriggerZone keypadZone;
        private Keypad keypad;
        private Camera keypadCamera;
        private AudioListener keypadAudioListener;
        private Transform doorOnePanel;
        private Transform doorTwoPanel;
        private BoxCollider doorwayHeaderCollider;
        private Chapter1InputReader inputReader;
        private PhoneUIController phoneUI;

        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private Camera gameplayCamera;
        private AudioListener gameplayAudioListener;
        private ThirdPersonCameraRig cameraRig;
        private GameObject activePlayer;
        private Chapter2BrokenDoorInspection activeInspection;
        private Coroutine doorAnimation;
        private Vector3 doorOneClosedPosition;
        private Vector3 doorTwoClosedPosition;
        private bool closedPositionsCaptured;
        private bool configured;
        private bool modalStateCaptured;
        private bool ownInputLockHeld;
        private bool playerWasActive;
        private bool gameplayCameraWasEnabled;
        private bool gameplayAudioWasEnabled;
        private bool resumeScannerAfterInspection;
        private CursorLockMode cursorLockBeforeInspection;
        private bool cursorVisibleBeforeInspection;
        private int lastProgressHash = int.MinValue;

        public Chapter2BrokenDoorInspection ActiveInspection =>
            activeInspection;
        public bool IsInspecting =>
            activeInspection != Chapter2BrokenDoorInspection.None;
        public bool InteractionsAvailable =>
            configured &&
            saveManager != null &&
            saveManager.CurrentData.Mission05ScannerActivated;
        public bool DoorUnlocked =>
            saveManager != null &&
            saveManager.CurrentData.Mission05BrokenDoorUnlocked;

        public void Configure(
            Chapter2SaveManager chapter2SaveManager,
            SphereCollider contractTrigger,
            Chapter2MissionTriggerZone contractTriggerZone,
            Camera contractInspectionCamera,
            SphereCollider keypadTrigger,
            Chapter2MissionTriggerZone keypadTriggerZone,
            Keypad copiedKeypad,
            Camera keypadInspectionCamera,
            Transform firstDoorPanel,
            Transform secondDoorPanel,
            BoxCollider passageHeaderCollider,
            Chapter1InputReader playerInputReader,
            PhoneUIController phoneController)
        {
            UnsubscribeFromKeypad();

            saveManager = chapter2SaveManager;
            contractCollider = contractTrigger;
            contractZone = contractTriggerZone;
            contractCamera = contractInspectionCamera;
            contractAudioListener = contractCamera != null
                ? contractCamera.GetComponent<AudioListener>()
                : null;
            keypadCollider = keypadTrigger;
            keypadZone = keypadTriggerZone;
            keypad = copiedKeypad;
            keypadCamera = keypadInspectionCamera;
            keypadAudioListener = keypadCamera != null
                ? keypadCamera.GetComponent<AudioListener>()
                : null;
            doorOnePanel = firstDoorPanel;
            doorTwoPanel = secondDoorPanel;
            doorwayHeaderCollider = passageHeaderCollider;
            inputReader = playerInputReader;
            phoneUI = phoneController;

            ResolvePhoneReference();
            ResolvePlayerReferences();
            CaptureClosedDoorPositions();
            ConfigureCopiedKeypad();
            SubscribeToKeypad();
            SetAuxiliaryCamerasActive(
                Chapter2BrokenDoorInspection.None);
            configured = ValidateRequiredReferences();
            SynchronizeProgress(true);
        }

        private void Update()
        {
            SynchronizeProgress(false);
            if (!IsInspecting)
            {
                return;
            }

            if (HasExternalInputLock())
            {
                EndInspection();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                EndInspection();
                return;
            }

            if (activeInspection ==
                Chapter2BrokenDoorInspection.Keypad)
            {
                HandleKeypadPointerInput();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromKeypad();
            EndInspection();
            StopDoorAnimation();
            SetDoorOpenImmediate(DoorUnlocked);
            SetAuxiliaryCamerasActive(
                Chapter2BrokenDoorInspection.None);
        }

        private void OnDestroy()
        {
            UnsubscribeFromKeypad();
        }

        public bool CanInspect(
            Chapter2BrokenDoorInspection inspection)
        {
            if (!InteractionsAvailable || IsInspecting ||
                inspection == Chapter2BrokenDoorInspection.None ||
                (inputLock != null && inputLock.IsLocked))
            {
                return false;
            }

            return inspection !=
                       Chapter2BrokenDoorInspection.Keypad ||
                   !DoorUnlocked;
        }

        public bool TryBeginInspection(
            Chapter2BrokenDoorInspection inspection,
            InteractionContext context)
        {
            if (!CanInspect(inspection) ||
                context.PlayerObject == null ||
                context.PlayerTransform == null)
            {
                return false;
            }

            ResolvePlayerReferences(context);
            if (inputLock == null || gameplayCamera == null)
            {
                Debug.LogError(
                    "[Chapter2BrokenDoor] Thiếu PlayerInputLock hoặc gameplay camera.",
                    this);
                return false;
            }

            activeInspection = inspection;
            CaptureModalState(context.PlayerObject);
            if (inspection == Chapter2BrokenDoorInspection.Keypad)
            {
                ConfigureCopiedKeypad();
                if (Application.isPlaying)
                {
                    keypad.ResetForNewAttempt();
                }
            }

            SetAuxiliaryCamerasActive(inspection);
            ApplyInspectionCursor(inspection);
            return true;
        }

        public void EndInspection()
        {
            SetAuxiliaryCamerasActive(
                Chapter2BrokenDoorInspection.None);
            if (!modalStateCaptured)
            {
                activeInspection =
                    Chapter2BrokenDoorInspection.None;
                ReleaseOwnInputLock();
                return;
            }

            if (activePlayer != null &&
                playerWasActive &&
                !activePlayer.activeSelf)
            {
                activePlayer.SetActive(true);
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled =
                    gameplayCameraWasEnabled;
            }

            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled =
                    gameplayAudioWasEnabled;
            }

            cameraRig?.SetLookEnabled(true);
            ReleaseOwnInputLock();
            if (inputLock != null && inputLock.IsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState =
                    cursorLockBeforeInspection;
                Cursor.visible =
                    cursorVisibleBeforeInspection;
            }

            if (resumeScannerAfterInspection &&
                phoneUI != null &&
                phoneUI.IsSignalScannerActive)
            {
                phoneUI.ResumeScannerWalkMode();
            }

            activeInspection =
                Chapter2BrokenDoorInspection.None;
            activePlayer = null;
            resumeScannerAfterInspection = false;
            modalStateCaptured = false;
        }

        private void HandleKeypadPointerInput()
        {
            if (keypad == null || keypadCamera == null ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 pointerPosition =
                Mouse.current.position.ReadValue();
            Ray pointerRay =
                keypadCamera.ScreenPointToRay(pointerPosition);
            int hitCount = Physics.RaycastNonAlloc(
                pointerRay,
                keypadHitBuffer,
                10f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            KeypadButton nearestButton = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider =
                    keypadHitBuffer[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                KeypadButton candidate =
                    hitCollider.GetComponentInParent<
                        KeypadButton>();
                if (candidate == null ||
                    !candidate.transform.IsChildOf(
                        keypad.transform) ||
                    keypadHitBuffer[i].distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestButton = candidate;
                nearestDistance =
                    keypadHitBuffer[i].distance;
            }

            nearestButton?.PressButton();
        }

        private void HandleAccessGranted()
        {
            if (activeInspection !=
                Chapter2BrokenDoorInspection.Keypad ||
                DoorUnlocked)
            {
                return;
            }

            EndInspection();
            saveManager.SaveMission05BrokenDoorUnlocked();
            lastProgressHash = CalculateProgressHash();
            if (keypadCollider != null)
            {
                keypadCollider.enabled = false;
            }

            SetDoorwayBlocked(false);
            StartDoorOpening();
            Chapter1EventBus.RaiseNotification(
                "Mật khẩu chính xác. Cửa đã được mở.");
        }

        private void CaptureModalState(GameObject player)
        {
            if (modalStateCaptured)
            {
                return;
            }

            modalStateCaptured = true;
            activePlayer = player;
            playerWasActive = activePlayer.activeSelf;
            gameplayCameraWasEnabled =
                gameplayCamera.enabled;
            gameplayAudioWasEnabled =
                gameplayAudioListener != null &&
                gameplayAudioListener.enabled;
            cursorLockBeforeInspection = Cursor.lockState;
            cursorVisibleBeforeInspection = Cursor.visible;

            resumeScannerAfterInspection =
                phoneUI != null &&
                phoneUI.IsSignalScannerActive &&
                !phoneUI.IsSignalScannerSuspended;
            if (resumeScannerAfterInspection)
            {
                phoneUI.SuspendScannerForModal();
            }

            inputLock.Lock(InputLockReason);
            ownInputLockHeld = true;
            cameraRig?.SetLookEnabled(false);
            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled = false;
            }

            gameplayCamera.enabled = false;
            SetAuxiliaryCamerasActive(
                Chapter2BrokenDoorInspection.None);
            activePlayer.SetActive(false);
        }

        private static void ApplyInspectionCursor(
            Chapter2BrokenDoorInspection inspection)
        {
            bool keypadMode =
                inspection ==
                Chapter2BrokenDoorInspection.Keypad;
            Cursor.lockState = keypadMode
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = keypadMode;
        }

        private void SetAuxiliaryCamerasActive(
            Chapter2BrokenDoorInspection inspection)
        {
            SetCameraActive(
                contractCamera,
                contractAudioListener,
                inspection ==
                Chapter2BrokenDoorInspection.Contract);
            SetCameraActive(
                keypadCamera,
                keypadAudioListener,
                inspection ==
                Chapter2BrokenDoorInspection.Keypad);
        }

        private static void SetCameraActive(
            Camera camera,
            AudioListener listener,
            bool active)
        {
            if (camera != null)
            {
                camera.enabled = active;
            }

            if (listener != null)
            {
                listener.enabled = active;
            }
        }

        private void ConfigureCopiedKeypad()
        {
            if (keypad != null &&
                keypad.KeypadCombo != CorrectKeypadCombo)
            {
                keypad.SetCombo(CorrectKeypadCombo);
            }
        }

        private void SubscribeToKeypad()
        {
            if (keypad != null)
            {
                keypad.OnAccessGranted.RemoveListener(
                    HandleAccessGranted);
                keypad.OnAccessGranted.AddListener(
                    HandleAccessGranted);
            }
        }

        private void UnsubscribeFromKeypad()
        {
            if (keypad != null)
            {
                keypad.OnAccessGranted.RemoveListener(
                    HandleAccessGranted);
            }
        }

        private void SynchronizeProgress(bool force)
        {
            ResolvePhoneReference();
            if (saveManager == null)
            {
                SetInteractionColliders(false, false);
                return;
            }

            int hash = CalculateProgressHash();
            if (!force && hash == lastProgressHash)
            {
                return;
            }

            bool available = InteractionsAvailable;
            bool unlocked =
                saveManager.CurrentData
                    .Mission05BrokenDoorUnlocked;
            SetInteractionColliders(
                available,
                available && !unlocked);

            if (!available && IsInspecting)
            {
                EndInspection();
            }
            else if (unlocked &&
                     activeInspection ==
                     Chapter2BrokenDoorInspection.Keypad)
            {
                EndInspection();
            }

            StopDoorAnimation();
            SetDoorOpenImmediate(unlocked);
            lastProgressHash = hash;
        }

        private int CalculateProgressHash()
        {
            if (saveManager == null)
            {
                return 0;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            int hash = 17;
            hash = hash * 31 +
                   (data.Mission05ScannerActivated ? 1 : 0);
            hash = hash * 31 +
                   (data.Mission05BrokenDoorUnlocked ? 1 : 0);
            return hash;
        }

        private void SetInteractionColliders(
            bool contractEnabled,
            bool keypadEnabled)
        {
            if (contractCollider != null)
            {
                contractCollider.enabled =
                    contractEnabled;
            }

            if (keypadCollider != null)
            {
                keypadCollider.enabled = keypadEnabled;
            }
        }

        private void CaptureClosedDoorPositions()
        {
            if (closedPositionsCaptured ||
                doorOnePanel == null ||
                doorTwoPanel == null)
            {
                return;
            }

            doorOneClosedPosition = doorOnePanel.position;
            doorTwoClosedPosition = doorTwoPanel.position;
            closedPositionsCaptured = true;
        }

        private void StartDoorOpening()
        {
            StopDoorAnimation();
            if (!closedPositionsCaptured)
            {
                return;
            }

            SetDoorwayBlocked(false);
            doorAnimation = StartCoroutine(
                AnimateDoorOpening());
        }

        private IEnumerator AnimateDoorOpening()
        {
            Vector3 firstTarget =
                doorOneClosedPosition +
                Vector3.up * DefaultDoorOpenHeight;
            Vector3 secondTarget =
                doorTwoClosedPosition +
                Vector3.up * DefaultDoorOpenHeight;

            while (doorOnePanel != null &&
                   doorTwoPanel != null &&
                   (doorOnePanel.position != firstTarget ||
                    doorTwoPanel.position != secondTarget))
            {
                float step =
                    DefaultDoorOpenSpeed * Time.unscaledDeltaTime;
                doorOnePanel.position = Vector3.MoveTowards(
                    doorOnePanel.position,
                    firstTarget,
                    step);
                doorTwoPanel.position = Vector3.MoveTowards(
                    doorTwoPanel.position,
                    secondTarget,
                    step);
                yield return null;
            }

            SetDoorOpenImmediate(true);
            doorAnimation = null;
        }

        private void SetDoorOpenImmediate(bool open)
        {
            SetDoorwayBlocked(!open);
            if (!closedPositionsCaptured ||
                doorOnePanel == null ||
                doorTwoPanel == null)
            {
                return;
            }

            Vector3 offset = open
                ? Vector3.up * DefaultDoorOpenHeight
                : Vector3.zero;
            doorOnePanel.position =
                doorOneClosedPosition + offset;
            doorTwoPanel.position =
                doorTwoClosedPosition + offset;
        }

        private void SetDoorwayBlocked(bool blocked)
        {
            if (doorwayHeaderCollider != null)
            {
                doorwayHeaderCollider.enabled = blocked;
            }
        }

        private void StopDoorAnimation()
        {
            if (doorAnimation == null)
            {
                return;
            }

            StopCoroutine(doorAnimation);
            doorAnimation = null;
        }

        private void ResolvePhoneReference()
        {
            if (phoneUI != null)
            {
                return;
            }

            PhoneUIController[] candidates =
                FindObjectsByType<PhoneUIController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                PhoneUIController candidate = candidates[i];
                if (candidate != null &&
                    candidate.gameObject.scene ==
                    gameObject.scene)
                {
                    phoneUI = candidate;
                    return;
                }
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
                player.GetComponent<
                    Chapter1InteractionController>();
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

        private void ResolvePlayerReferences(
            InteractionContext context)
        {
            inputLock = context.PlayerObject
                .GetComponent<PlayerInputLock>();
            interactionController =
                context.InteractionController ??
                context.PlayerObject.GetComponent<
                    Chapter1InteractionController>();
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
                         contractCollider != null &&
                         contractZone != null &&
                         contractCamera != null &&
                         keypadCollider != null &&
                         keypadZone != null &&
                         keypad != null &&
                         keypadCamera != null &&
                         doorOnePanel != null &&
                         doorTwoPanel != null &&
                         doorwayHeaderCollider != null &&
                         inputReader != null &&
                         inputLock != null &&
                         interactionController != null &&
                         gameplayCamera != null;
            if (!valid)
            {
                Debug.LogError(
                    "[Chapter2BrokenDoor] Thiếu reference cho contract camera, keypad, cửa hoặc player.",
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
            if (!ownInputLockHeld)
            {
                return;
            }

            inputLock?.Unlock(InputLockReason);
            ownInputLockHeld = false;
        }
    }
}
