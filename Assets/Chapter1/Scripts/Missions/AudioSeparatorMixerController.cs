using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class AudioSeparatorMixerController : MonoBehaviour
    {
        private const string InputLockReason = "AudioSeparatorMixer";

        [Header("Mission")]
        [SerializeField] private Mission01AudioSeparatorManager missionManager;
        [SerializeField] private LanRecordingMissionController lanRecordingController;
        [SerializeField] private bool requireMixedRecordingSaved = true;

        [Header("Audio")]
        [SerializeField] private AudioStemFader[] faders = new AudioStemFader[0];
        [SerializeField] private AudioStemPlaybackController playbackController;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private AudioClip policeSirenClip;
        [SerializeField] private AudioClip rainClip;
        [SerializeField] private AudioClip trafficHornClip;
        [SerializeField] private AudioClip windClip;
        [SerializeField] private AudioClip thunderClip;

        [Header("Controls")]
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private Transform cameraFocusPoint;
        [SerializeField] private AudioSeparatorMixerButton saveButton;
        [SerializeField] private AudioSeparatorMixerButton playStopButton;
        [SerializeField] private AudioSeparatorMixerButton resetButton;
        [SerializeField] private AudioSeparatorMixerButton[] controlButtons = new AudioSeparatorMixerButton[0];

        [Header("Rules")]
        [SerializeField, Range(0f, 1f)] private float targetMinimum = 0.75f;
        [SerializeField, Range(0f, 1f)] private float otherMaximum = 0.15f;
        [SerializeField, Min(1f)] private float firstHintSeconds = 10f;
        [SerializeField, Min(1f)] private float secondHintSeconds = 20f;

        [Header("Runtime UI")]
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI hoverText;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private TextMeshProUGUI phoneStatusText;
        [SerializeField] private Slider[] uiFaderSliders = new Slider[0];
        [SerializeField] private TextMeshProUGUI[] uiFaderValueTexts = new TextMeshProUGUI[0];
        [SerializeField] private Button uiPlayStopButton;
        [SerializeField] private Button uiSaveButton;
        [SerializeField] private Button uiResetButton;
        [SerializeField] private Button uiCloseButton;

        private PlayerInputLock activeInputLock;
        private Chapter1InputReader activeInputReader;
        private ThirdPersonCameraRig activeCameraRig;
        private Camera activeCamera;
        private bool cameraRigWasEnabled;
        private Vector3 cameraPositionBeforeSession;
        private Quaternion cameraRotationBeforeSession;
        private CursorLockMode cursorLockBeforeSession;
        private bool cursorVisibleBeforeSession;
        private bool inputReaderWasEnabled = true;
        private bool isSessionOpen;
        private float sessionOpenedAt;
        private bool firstHintShown;
        private bool secondHintShown;
        private Coroutine introRoutine;
        private bool suppressUiFaderCallbacks;

        public bool IsSessionOpen => isSessionOpen;
        public Camera ActiveCamera => activeCamera != null ? activeCamera : Camera.main;
        public float TargetMinimum => targetMinimum;
        public float OtherMaximum => otherMaximum;
        public AudioStemFader[] Faders => faders;
        public AudioStemPlaybackController PlaybackController => playbackController;

        private void Awake()
        {
            ResolveReferences();
            ConfigureFaders();
            LoadFaderValuesFromSave();
            EnsureUi();
            SetUiVisible(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            ConfigureFaders();
            LoadFaderValuesFromSave();
        }

        private void Update()
        {
            if (!isSessionOpen)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                EndSession();
                return;
            }

            float elapsed = Time.unscaledTime - sessionOpenedAt;
            if (!firstHintShown && elapsed >= firstHintSeconds)
            {
                firstHintShown = true;
                SetStatus("Kéo một cần xuống để bỏ lớp âm đó khỏi bản phát, rồi bấm PLAY để nghe phần còn lại.");
            }

            if (!secondHintShown && elapsed >= secondHintSeconds)
            {
                secondHintShown = true;
                saveButton?.SetAttention(true);
                if (faders.Length > 0)
                {
                    faders[0].SetNormalizedValue(Mathf.Max(faders[0].NormalizedValue, faders[0].NormalizedValue));
                }
            }

            RefreshPlayButtonLabel();
        }

        private void OnDisable()
        {
            if (isSessionOpen)
            {
                EndSession();
            }
        }

        public InteractionResult TryBeginSession(InteractionContext context)
        {
            ResolveReferences();
            if (isSessionOpen)
            {
                return InteractionResult.Ignored();
            }

            if (!CanBeginSession(out string message))
            {
                return InteractionResult.Failed(message);
            }

            if (!missionManager.StartAudioSeparatorProcessing())
            {
                return InteractionResult.Failed("Chưa đến bước xử lý âm thanh.");
            }

            isSessionOpen = true;
            sessionOpenedAt = Time.unscaledTime;
            firstHintShown = false;
            secondHintShown = false;

            ClearGameplayNotifications();
            CaptureAndLockInput(context);
            CaptureAndMoveCamera(context);
            EnsureUi();
            SetUiVisible(true);
            LoadFaderValuesFromSave();
            ConfigureFaders();
            playbackController?.StopAll();
            RefreshProgressText();
            RefreshIsolationStatus();
            RefreshUiFaderValues();
            RefreshPlayButtonLabel();

            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
            }

            introRoutine = StartCoroutine(IntroRoutine());
            return InteractionResult.Succeeded();
        }

        public bool CanBeginSession(out string message)
        {
            ResolveReferences();
            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance != null ? Chapter1Manager.Instance.CurrentData : null;
            data?.EnsureValidDefaults();

            if (missionManager == null)
            {
                message = "Chưa tìm thấy Mission 01 Manager.";
                return false;
            }

            if (missionManager.State < FirstMissionState.EnterDungRoom)
            {
                message = "Chưa đến lúc dùng máy tách âm.";
                return false;
            }

            if (requireMixedRecordingSaved && (data == null || !data.HasPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId)))
            {
                message = "Hãy tải đoạn ghi âm của Chị Lan vào ứng dụng Ghi âm trước.";
                return false;
            }

            if (lanRecordingController == null || lanRecordingController.LanRecordingClip == null)
            {
                message = "Thiếu bản ghi âm hỗn hợp của Chị Lan.";
                return false;
            }

            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                if (GetStemClip(LanAudioRecordingCatalog.StemOrder[i]) == null)
                {
                    message = "Thiếu stem audio: " + LanAudioRecordingCatalog.GetStemDisplayName(LanAudioRecordingCatalog.StemOrder[i]);
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        public AudioClip GetStemClip(LanAudioStemId stem)
        {
            switch (stem)
            {
                case LanAudioStemId.Voice:
                    return voiceClip;
                case LanAudioStemId.Police:
                    return policeSirenClip;
                case LanAudioStemId.Rain:
                    return rainClip;
                case LanAudioStemId.Horns:
                    return trafficHornClip;
                case LanAudioStemId.Wind:
                    return windClip;
                case LanAudioStemId.Thunder:
                    return thunderClip;
                default:
                    return null;
            }
        }

        public void SetStemClip(LanAudioStemId stem, AudioClip clip)
        {
            switch (stem)
            {
                case LanAudioStemId.Voice:
                    voiceClip = clip;
                    break;
                case LanAudioStemId.Police:
                    policeSirenClip = clip;
                    break;
                case LanAudioStemId.Rain:
                    rainClip = clip;
                    break;
                case LanAudioStemId.Horns:
                    trafficHornClip = clip;
                    break;
                case LanAudioStemId.Wind:
                    windClip = clip;
                    break;
                case LanAudioStemId.Thunder:
                    thunderClip = clip;
                    break;
            }

            ConfigureFaders();
        }

        public void NotifyFaderChanged(AudioStemFader fader)
        {
            RefreshUiFaderValues();
            SaveFaderValuesToSave(false);
            RefreshIsolationStatus();
        }

        public void ShowHoverLabel(string text)
        {
            if (hoverText != null)
            {
                hoverText.text = text ?? string.Empty;
            }
        }

        public void ClearHoverLabel(string text)
        {
            if (hoverText != null && string.Equals(hoverText.text, text, System.StringComparison.Ordinal))
            {
                hoverText.text = string.Empty;
            }
        }

        public void HandleMixerButton(AudioSeparatorMixerButtonType buttonType)
        {
            if (!isSessionOpen)
            {
                return;
            }

            switch (buttonType)
            {
                case AudioSeparatorMixerButtonType.PlayStop:
                    if (playbackController != null && playbackController.IsPlaying)
                    {
                        playbackController.StopAll();
                        SetStatus("Đã dừng phát.");
                    }
                    else
                    {
                        playbackController?.PlayAll();
                        SetStatus("Đang phát bản ghi theo vị trí 6 cần gạt.");
                    }

                    RefreshPlayButtonLabel();
                    break;
                case AudioSeparatorMixerButtonType.Save:
                    SaveCurrentIsolation();
                    break;
                case AudioSeparatorMixerButtonType.Reset:
                    ResetFaders();
                    break;
            }
        }

        public bool TryGetIsolatedStem(out LanAudioStemId stem, out string message)
        {
            stem = default;
            int loudCount = 0;
            int targetIndex = -1;
            int mutedCount = 0;
            int mutedIndex = -1;
            for (int i = 0; i < faders.Length; i++)
            {
                if (faders[i] == null)
                {
                    continue;
                }

                if (faders[i].NormalizedValue >= targetMinimum)
                {
                    loudCount++;
                    targetIndex = i;
                }

                if (faders[i].NormalizedValue <= otherMaximum)
                {
                    mutedCount++;
                    mutedIndex = i;
                }
            }

            if (mutedCount == 1 && mutedIndex >= 0)
            {
                bool othersAreAudible = true;
                for (int i = 0; i < faders.Length; i++)
                {
                    if (i == mutedIndex || faders[i] == null)
                    {
                        continue;
                    }

                    if (faders[i].NormalizedValue < targetMinimum)
                    {
                        othersAreAudible = false;
                        break;
                    }
                }

                if (othersAreAudible)
                {
                    stem = faders[mutedIndex].StemId;
                    message = "Đã tách lớp: " + LanAudioRecordingCatalog.GetStemDisplayName(stem) + ". PLAY sẽ phát bản ghi thiếu lớp này; nút đỏ sẽ lưu lớp đã tách.";
                    return true;
                }
            }

            if (loudCount > 1)
            {
                message = "Kéo xuống đúng một cần để bỏ lớp âm đó, hoặc kéo chỉ một cần lên để nghe riêng lớp đó.";
                return false;
            }

            if (loudCount != 1 || targetIndex < 0)
            {
                message = "Kéo một cần xuống thấp để tách lớp đó khỏi bản phát.";
                return false;
            }

            for (int i = 0; i < faders.Length; i++)
            {
                if (i == targetIndex || faders[i] == null)
                {
                    continue;
                }

                if (faders[i].NormalizedValue > otherMaximum)
                {
                    message = "Nếu muốn nghe riêng một lớp, hãy kéo năm cần còn lại xuống thấp.";
                    return false;
                }
            }

            stem = faders[targetIndex].StemId;
            message = "Đã cô lập: " + LanAudioRecordingCatalog.GetStemDisplayName(stem) + ". Nhấn nút đỏ để lưu vào điện thoại.";
            return true;
        }

        public void SaveCurrentIsolation()
        {
            ResolveReferences();
            if (!TryGetIsolatedStem(out LanAudioStemId stem, out string message))
            {
                saveButton?.SetAttention(false);
                SetStatus("Chưa thể lưu. Hãy kéo một cần xuống thấp hoặc cô lập đúng một lớp âm thanh trước.");
                return;
            }

            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance?.CurrentData;
            if (data == null)
            {
                SetStatus("Không thể lưu tiến trình lúc này.");
                return;
            }

            data.EnsureValidDefaults();
            if (data.HasSavedStem(stem))
            {
                SetStatus("Âm thanh này đã được lưu trước đó.");
                return;
            }

            data.AddSavedStem(stem);
            SaveFaderValuesToSave(false);
            RefreshProgressText();
            Chapter1Manager.Instance?.SaveChapter();
            SetStatus("Đã lưu " + LanAudioRecordingCatalog.GetStemDisplayName(stem) + " vào mục Ghi âm.");

            if (data.AreAllLanAudioStemsSaved())
            {
                data.Mission01AudioSeparatorMixerCompleted = true;
                missionManager?.NotifyAllLanAudioStemsSaved();
                SetStatus("Đã tách xong toàn bộ 6 lớp âm thanh.");
            }
        }

        public void ResetFaders()
        {
            playbackController?.StopAll();
            for (int i = 0; i < faders.Length; i++)
            {
                faders[i]?.SetNormalizedValue(1f);
            }

            SaveFaderValuesToSave(true);
            saveButton?.SetAttention(false);
            RefreshUiFaderValues();
            SetStatus("Đã đưa các cần gạt về mức ban đầu.");
        }

        public void EndSession()
        {
            if (!isSessionOpen)
            {
                return;
            }

            SaveFaderValuesToSave(true);
            playbackController?.StopAll();
            RefreshPlayButtonLabel();
            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            SetUiVisible(false);
            RestoreCamera();
            RestoreInput();
            isSessionOpen = false;
        }

        private IEnumerator IntroRoutine()
        {
            SetStatus("Đang kết nối điện thoại...");
            yield return new WaitForSecondsRealtime(0.8f);
            SetStatus("Đã kết nối điện thoại.");
            yield return new WaitForSecondsRealtime(0.8f);
            SetStatus("Đã tải đoạn ghi âm của chị Lan vào máy.");

            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance?.CurrentData;
            if (data != null && !data.Mission01AudioSeparatorTutorialSeen)
            {
                data.Mission01AudioSeparatorTutorialSeen = true;
                Chapter1Manager.Instance?.SaveChapter();
                if (tutorialText != null)
                {
                    tutorialText.text =
                        "1. Cắm điện thoại vào dock trên máy.\n" +
                        "2. Mỗi cần gạt là một lớp âm thanh.\n" +
                        "3. Kéo một cần xuống để lớp đó biến mất khi PLAY.\n" +
                        "4. Nút đỏ lưu lớp vừa tách vào Ghi âm.";
                }
            }

            RefreshIsolationStatus();
        }

        private void RefreshIsolationStatus()
        {
            if (!isSessionOpen)
            {
                return;
            }

            if (TryGetIsolatedStem(out _, out string message))
            {
                saveButton?.SetAttention(true);
                SetStatus(message);
            }
            else
            {
                saveButton?.SetAttention(false);
                SetStatus(message);
            }
        }

        private void RefreshProgressText()
        {
            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance?.CurrentData;
            data?.EnsureValidDefaults();
            if (progressText == null || data == null)
            {
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                builder.Append(data.HasSavedStem(stem) ? "[x] " : "[ ] ");
                builder.AppendLine(LanAudioRecordingCatalog.GetStemDisplayName(stem));
            }

            builder.Append("Đã tách ");
            builder.Append(data.GetSavedStemCount());
            builder.Append("/6");
            progressText.text = builder.ToString();

            if (phoneStatusText != null)
            {
                phoneStatusText.text = data.AreAllLanAudioStemsSaved()
                    ? "Đã lưu đủ 6 lớp âm vào Ghi âm"
                    : "Đang kết nối điện thoại: " + data.GetSavedStemCount() + "/6";
            }
        }

        private void RefreshUiFaderValues()
        {
            if (uiFaderSliders == null || uiFaderSliders.Length == 0)
            {
                return;
            }

            suppressUiFaderCallbacks = true;
            for (int i = 0; i < uiFaderSliders.Length && i < faders.Length; i++)
            {
                if (uiFaderSliders[i] == null || faders[i] == null)
                {
                    continue;
                }

                uiFaderSliders[i].SetValueWithoutNotify(faders[i].NormalizedValue);
                if (uiFaderValueTexts != null && i < uiFaderValueTexts.Length && uiFaderValueTexts[i] != null)
                {
                    uiFaderValueTexts[i].text = Mathf.RoundToInt(faders[i].NormalizedValue * 100f) + "%";
                }
            }

            suppressUiFaderCallbacks = false;
        }

        private void OnUiFaderValueChanged(int index, float value)
        {
            if (suppressUiFaderCallbacks || index < 0 || index >= faders.Length || faders[index] == null)
            {
                return;
            }

            faders[index].SetNormalizedValue(value);
            if (uiFaderValueTexts != null && index < uiFaderValueTexts.Length && uiFaderValueTexts[index] != null)
            {
                uiFaderValueTexts[index].text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        private void RefreshPlayButtonLabel()
        {
            SetButtonText(uiPlayStopButton, playbackController != null && playbackController.IsPlaying ? "Dừng" : "Play");
        }

        private void SaveFaderValuesToSave(bool saveChapter)
        {
            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance?.CurrentData;
            if (data == null)
            {
                return;
            }

            data.EnsureValidDefaults();
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                float value = i < faders.Length && faders[i] != null ? faders[i].NormalizedValue : 1f;
                data.AudioSeparatorFaderValues[i] = value;
            }

            if (saveChapter)
            {
                Chapter1Manager.Instance?.SaveChapter();
            }
        }

        private void LoadFaderValuesFromSave()
        {
            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance?.CurrentData;
            if (data == null)
            {
                return;
            }

            data.EnsureValidDefaults();
            for (int i = 0; i < faders.Length && i < data.AudioSeparatorFaderValues.Count; i++)
            {
                faders[i]?.SetNormalizedValue(data.AudioSeparatorFaderValues[i]);
            }
        }

        private void CaptureAndLockInput(InteractionContext context)
        {
            activeInputLock = context.PlayerObject != null ? context.PlayerObject.GetComponent<PlayerInputLock>() : FindAnyObjectByType<PlayerInputLock>();
            activeInputReader = context.PlayerObject != null ? context.PlayerObject.GetComponent<Chapter1InputReader>() : FindAnyObjectByType<Chapter1InputReader>();
            inputReaderWasEnabled = activeInputReader == null || activeInputReader.enabled;
            activeInputLock?.Lock(InputLockReason);
            activeInputReader?.SetGameplayInputEnabled(false);

            PhoneUIController phone = FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            if (phone != null && phone.IsOpen)
            {
                phone.ClosePhone();
            }

            InventoryUIController inventory = FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
            if (inventory != null && inventory.IsOpen)
            {
                inventory.CloseInventory();
            }

            cursorLockBeforeSession = Cursor.lockState;
            cursorVisibleBeforeSession = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreInput()
        {
            activeInputReader?.SetGameplayInputEnabled(inputReaderWasEnabled);
            activeInputLock?.Unlock(InputLockReason);
            Cursor.lockState = cursorLockBeforeSession;
            Cursor.visible = cursorVisibleBeforeSession;
            activeInputLock = null;
            activeInputReader = null;
        }

        private void CaptureAndMoveCamera(InteractionContext context)
        {
            activeCamera = context.InteractionController != null ? context.InteractionController.GameplayCamera : Camera.main;
            if (activeCamera == null)
            {
                return;
            }

            cameraPositionBeforeSession = activeCamera.transform.position;
            cameraRotationBeforeSession = activeCamera.transform.rotation;
            activeCameraRig = activeCamera.GetComponentInParent<ThirdPersonCameraRig>();
            if (activeCameraRig != null)
            {
                cameraRigWasEnabled = activeCameraRig.enabled;
                activeCameraRig.enabled = false;
                activeCameraRig.SetLookEnabled(false);
            }

            if (cameraFocusPoint != null)
            {
                activeCamera.transform.SetPositionAndRotation(cameraFocusPoint.position, cameraFocusPoint.rotation);
            }
        }

        private void RestoreCamera()
        {
            if (activeCamera != null)
            {
                activeCamera.transform.SetPositionAndRotation(cameraPositionBeforeSession, cameraRotationBeforeSession);
            }

            if (activeCameraRig != null)
            {
                activeCameraRig.enabled = cameraRigWasEnabled;
                activeCameraRig.SetLookEnabled(true);
            }

            activeCamera = null;
            activeCameraRig = null;
        }

        private void ConfigureFaders()
        {
            if (faders == null)
            {
                faders = new AudioStemFader[0];
            }

            for (int i = 0; i < faders.Length; i++)
            {
                if (faders[i] == null)
                {
                    continue;
                }

                LanAudioStemId stem = faders[i].StemId;
                faders[i].Configure(this, stem, LanAudioRecordingCatalog.GetStemDisplayName(stem), faders[i].AudioSource, stem == LanAudioStemId.Voice);
                faders[i].SetClip(GetStemClip(stem));
            }

            playbackController?.Configure(faders);
            if (controlButtons != null)
            {
                for (int i = 0; i < controlButtons.Length; i++)
                {
                    if (controlButtons[i] != null)
                    {
                        controlButtons[i].Configure(this, controlButtons[i].ButtonType);
                    }
                }
            }
        }

        private void ResolveReferences()
        {
            if (missionManager == null)
            {
                missionManager = Mission01AudioSeparatorManager.Instance;
            }

            if (missionManager == null)
            {
                missionManager = FindAnyObjectByType<Mission01AudioSeparatorManager>(FindObjectsInactive.Include);
            }

            if (lanRecordingController == null)
            {
                lanRecordingController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            if (playbackController == null)
            {
                playbackController = GetComponent<AudioStemPlaybackController>();
            }

            if (playbackController == null)
            {
                playbackController = gameObject.AddComponent<AudioStemPlaybackController>();
            }

            if (faders == null || faders.Length == 0)
            {
                faders = GetComponentsInChildren<AudioStemFader>(true);
            }

            if (saveButton == null || playStopButton == null || resetButton == null)
            {
                AudioSeparatorMixerButton[] buttons = GetComponentsInChildren<AudioSeparatorMixerButton>(true);
                controlButtons = buttons;
                for (int i = 0; i < buttons.Length; i++)
                {
                    switch (buttons[i].ButtonType)
                    {
                        case AudioSeparatorMixerButtonType.Save:
                            saveButton = saveButton != null ? saveButton : buttons[i];
                            break;
                        case AudioSeparatorMixerButtonType.PlayStop:
                            playStopButton = playStopButton != null ? playStopButton : buttons[i];
                            break;
                        case AudioSeparatorMixerButtonType.Reset:
                            resetButton = resetButton != null ? resetButton : buttons[i];
                            break;
                    }
                }
            }
        }

        private void EnsureUi()
        {
            if (tutorialCanvas == null)
            {
                GameObject canvasObject = new GameObject("MixerTutorialPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                tutorialCanvas = canvasObject.GetComponent<Canvas>();
            }

            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = 650;

            CanvasScaler scaler = tutorialCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = tutorialCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (tutorialCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                tutorialCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            RectTransform canvasRect = tutorialCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.offsetMin = Vector2.zero;
                canvasRect.offsetMax = Vector2.zero;
            }

            ClearGeneratedUi(tutorialCanvas.transform);
            BuildMixerUi(tutorialCanvas.transform);
            RefreshUiFaderValues();
            RefreshPlayButtonLabel();
        }

        private void ClearGeneratedUi(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void BuildMixerUi(Transform canvasRoot)
        {
            RectTransform backdrop = CreateImageRect(canvasRoot, "MixerBackdrop", new Color(0f, 0f, 0f, 0.55f));
            Stretch(backdrop, Vector2.zero, Vector2.zero);

            RectTransform board = CreateImageRect(canvasRoot, "AudioSeparatorBoard", new Color(0.11f, 0.13f, 0.15f, 0.98f));
            SetRect(board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 690f), new Vector2(0.5f, 0.5f));

            RectTransform face = CreateImageRect(board, "MetalFace", new Color(0.18f, 0.20f, 0.22f, 1f));
            Stretch(face, new Vector2(24f, 24f), new Vector2(-24f, -24f));

            TextMeshProUGUI title = CreateText(face, "BoardTitle", "STEREO MIXER SA-100", 32f, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -36f), new Vector2(-64f, 42f), new Vector2(0f, 0.5f));
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.93f, 0.91f, 0.84f, 1f);

            uiCloseButton = CreateUiButton(face, "CloseMixerButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -34f), new Vector2(48f, 44f), new Color(0.23f, 0.22f, 0.22f, 1f));
            uiCloseButton.onClick.AddListener(EndSession);

            statusText = CreateText(face, "StatusText", string.Empty, 20f, TextAlignmentOptions.Left);
            SetRect(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -82f), new Vector2(-92f, 30f), new Vector2(0f, 0.5f));
            statusText.color = Color.white;

            RectTransform phoneDock = CreateImageRect(face, "PhoneDock", new Color(0.05f, 0.055f, 0.065f, 0.94f));
            SetRect(phoneDock, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(300f, 152f), new Vector2(0.5f, 0.5f));

            RectTransform phone = CreateImageRect(phoneDock, "PhoneInserted", new Color(0.78f, 0.80f, 0.82f, 0.82f));
            SetRect(phone, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(112f, 142f), new Vector2(0.5f, 0f));
            RectTransform phoneScreen = CreateImageRect(phone, "PhoneScreen", new Color(0.12f, 0.16f, 0.22f, 0.88f));
            Stretch(phoneScreen, new Vector2(14f, 18f), new Vector2(-14f, -28f));

            phoneStatusText = CreateText(phoneDock, "PhoneStatusText", "Đang kết nối điện thoại: 0/6", 16f, TextAlignmentOptions.Center);
            SetRect(phoneStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(-20f, 30f), new Vector2(0.5f, 0.5f));
            phoneStatusText.color = new Color(0.86f, 0.91f, 0.96f, 1f);

            RectTransform channelRoot = CreateEmptyRect(face, "MixerChannels");
            SetRect(channelRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 82f), new Vector2(980f, 330f), new Vector2(0.5f, 0.5f));

            uiFaderSliders = new Slider[LanAudioRecordingCatalog.StemCount];
            uiFaderValueTexts = new TextMeshProUGUI[LanAudioRecordingCatalog.StemCount];
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                float x = Mathf.Lerp(-410f, 410f, i / 5f);
                RectTransform channel = CreateImageRect(channelRoot, "Channel_" + stem, new Color(0.12f, 0.13f, 0.145f, 0.96f));
                SetRect(channel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(132f, 308f), new Vector2(0.5f, 0.5f));

                TextMeshProUGUI label = CreateText(
                    channel,
                    "Label",
                    LanAudioRecordingCatalog.GetStemDisplayName(stem),
                    15f,
                    TextAlignmentOptions.Center);
                SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(-14f, 42f), new Vector2(0.5f, 0.5f));
                label.color = Color.white;
                label.textWrappingMode = TextWrappingModes.Normal;

                Slider slider = CreateVerticalFader(channel, stem == LanAudioStemId.Voice);
                int capturedIndex = i;
                slider.onValueChanged.AddListener(value => OnUiFaderValueChanged(capturedIndex, value));
                uiFaderSliders[i] = slider;

                uiFaderValueTexts[i] = CreateText(channel, "Value", "100%", 14f, TextAlignmentOptions.Center);
                SetRect(uiFaderValueTexts[i].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(-12f, 26f), new Vector2(0.5f, 0.5f));
                uiFaderValueTexts[i].color = new Color(0.86f, 0.86f, 0.88f, 1f);
            }

            RectTransform buttonRow = CreateEmptyRect(face, "ButtonRow");
            SetRect(buttonRow, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-230f, 62f), new Vector2(390f, 72f), new Vector2(0.5f, 0.5f));
            uiPlayStopButton = CreateUiButton(buttonRow, "PlayStopUiButton", "Play", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(100f, 52f), new Color(0.05f, 0.25f, 0.58f, 1f));
            uiPlayStopButton.onClick.AddListener(() => HandleMixerButton(AudioSeparatorMixerButtonType.PlayStop));
            uiResetButton = CreateUiButton(buttonRow, "ResetUiButton", "Reset", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 52f), new Color(0.22f, 0.22f, 0.23f, 1f));
            uiResetButton.onClick.AddListener(() => HandleMixerButton(AudioSeparatorMixerButtonType.Reset));
            uiSaveButton = CreateUiButton(buttonRow, "SaveUiButton", "Lưu", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, 0f), new Vector2(104f, 52f), new Color(0.75f, 0.03f, 0.04f, 1f));
            uiSaveButton.onClick.AddListener(() => HandleMixerButton(AudioSeparatorMixerButtonType.Save));

            RectTransform progressPanel = CreateImageRect(face, "ProgressPanel", new Color(0.07f, 0.075f, 0.085f, 0.92f));
            SetRect(progressPanel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(172f, 102f), new Vector2(300f, 176f), new Vector2(0.5f, 0.5f));
            progressText = CreateText(progressPanel, "ProgressText", string.Empty, 16f, TextAlignmentOptions.TopLeft);
            Stretch(progressText.rectTransform, new Vector2(18f, 14f), new Vector2(-18f, -14f));
            progressText.color = new Color(0.90f, 0.91f, 0.93f, 1f);

            RectTransform tutorialPanel = CreateImageRect(face, "TutorialPanel", new Color(0.07f, 0.075f, 0.085f, 0.92f));
            SetRect(tutorialPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-260f, 164f), new Vector2(480f, 96f), new Vector2(0.5f, 0.5f));
            tutorialText = CreateText(tutorialPanel, "TutorialText", string.Empty, 15f, TextAlignmentOptions.TopLeft);
            Stretch(tutorialText.rectTransform, new Vector2(16f, 12f), new Vector2(-16f, -12f));
            tutorialText.color = new Color(0.88f, 0.88f, 0.82f, 1f);
            tutorialText.text =
                "Kéo một cần xuống để loại lớp âm đó khỏi bản phát.\n" +
                "Play nghe bản ghi theo cần gạt. Lưu đưa lớp đã tách vào Ghi âm.";

            hoverText = CreateText(face, "HoverText", string.Empty, 20f, TextAlignmentOptions.Center);
            SetRect(hoverText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(420f, 34f), new Vector2(0.5f, 0.5f));
            hoverText.color = new Color(0.90f, 0.95f, 1f, 1f);
        }

        private void SetUiVisible(bool visible)
        {
            if (tutorialCanvas != null)
            {
                tutorialCanvas.gameObject.SetActive(visible);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        private static void ClearGameplayNotifications()
        {
            NotificationUI notificationUI =
                FindAnyObjectByType<NotificationUI>(
                    FindObjectsInactive.Include);
            notificationUI?.ClearMessages();
        }

        private static RectTransform CreateEmptyRect(Transform parent, string name)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static RectTransform CreateImageRect(Transform parent, string name, Color color)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.color = color;
            return target.GetComponent<RectTransform>();
        }

        private static Slider CreateVerticalFader(Transform parent, bool isVoice)
        {
            GameObject sliderObject = new GameObject("FaderSlider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetRect(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(78f, 220f), new Vector2(0.5f, 0.5f));

            RectTransform track = CreateImageRect(sliderObject.transform, "Track", new Color(0.32f, 0.34f, 0.36f, 1f));
            SetRect(track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 210f), new Vector2(0.5f, 0.5f));

            RectTransform fillArea = CreateEmptyRect(sliderObject.transform, "Fill Area");
            Stretch(fillArea, new Vector2(30f, 4f), new Vector2(-30f, -4f));
            RectTransform fill = CreateImageRect(fillArea, "Fill", isVoice ? new Color(0.76f, 0.08f, 0.10f, 1f) : new Color(0.95f, 0.46f, 0.12f, 1f));
            Stretch(fill, Vector2.zero, Vector2.zero);

            RectTransform handleArea = CreateEmptyRect(sliderObject.transform, "Handle Slide Area");
            Stretch(handleArea, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            RectTransform handle = CreateImageRect(handleArea, "Handle", isVoice ? new Color(0.93f, 0.08f, 0.10f, 1f) : new Color(0.12f, 0.14f, 0.16f, 1f));
            SetRect(handle, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(58f, 26f), new Vector2(0.5f, 0.5f));

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.BottomToTop;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            return slider;
        }

        private static Button CreateUiButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, size, new Vector2(0.5f, 0.5f));
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 20f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        private static void SetButtonText(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.045f, 0.047f, 0.055f, 0.88f);
            return rect;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = new Color(0.92f, 0.90f, 0.84f, 1f);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static void SetInset(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
