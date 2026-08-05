using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class MinhDialogueInteractable : Chapter1Interactable
    {
        [Header("Camera")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Camera minhCamera;
        [SerializeField, Min(0.5f)] private float conversationDistance = 1.65f;
        [SerializeField, Min(1f)] private float standingEyeHeight = 1.62f;
        [SerializeField, Range(0.65f, 0.98f)] private float faceHeightRatio = 0.88f;
        [SerializeField, Range(35f, 80f)] private float dialogueFieldOfView = 55f;

        [Header("Dialogue UI")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private TMP_Text advanceHintText;

        [Header("Dialogue")]
        [SerializeField] private string playerSpeaker = "Player";
        [SerializeField] private string minhSpeaker = "Minh";

        [Header("Second Dialogue")]
        [SerializeField, TextArea] private string secondPlayerOpeningLine =
            "Tôi đã tách được đoạn ghi âm rồi đây";
        [SerializeField, TextArea] private string secondMinhAiLine =
            "Tiếng còi này tôi sẽ phải train model AI để nhận biết nó tiếng còi gì";
        [SerializeField, TextArea] private string secondMinhTrainingTimeLine =
            "Nhưng quá trình train sẽ tầm 1 tháng bạn có đợi được không ?";
        [SerializeField, TextArea] private string secondPlayerUrgencyLine =
            "1 tháng hả, lâu quá chị tôi sẽ chết mất. Có cách nào đẩy nhanh quá trình train không ?";
        [SerializeField, TextArea] private string secondMinhEquipmentLine =
            "Có nhưng bạn phải tìm cho tôi một PSU và UPS";
        [SerializeField, TextArea] private string secondMinhScrapyardLine =
            "Sáng nay tôi thấy 2 thứ đó ngoài bãi rác có vẻ còn dùng được";
        [SerializeField, TextArea] private string secondMinhBatteryLine =
            "Nhưng UPS thì cần ắc quy, bạn thử hỏi mượn ắc quy của ông Henry chủ quán burger đối diện xem sao";
        [SerializeField, TextArea] private string secondPlayerAcceptLine =
            "Được, tôi sẽ lấy những thứ bạn yêu cầu.";

        [SerializeField] private Mission01AudioSeparatorManager mission01Manager;
        [SerializeField, Range(10f, 80f)] private float charactersPerSecond =
            34f;
        [SerializeField, Range(1f, 6f)] private float punctuationPauseMultiplier =
            2.5f;

        private Chapter1InteractionController interactionController;
        private PlayerInputLock inputLock;
        private bool dialogueRunning;
        private bool interactionControllerWasEnabled;
        private bool dialogueLockHeld;
        private bool generatedDialogueCanvas;
        private GameObject dialoguePlayer;
        private Renderer[] hiddenPlayerRenderers;
        private bool[] playerRendererEnabledStates;
        private List<DialogueLine> activeMissionLines;
        private MinhMissionDialogueMode activeMissionMode;
        private PlayerInventory activeInventory;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        protected override void Awake()
        {
            base.Awake();
            SanitizeSettings();
            ConfigureInteractionTrigger();
            ResolveMinhCamera();
            EnsureDialogueUI();
            SetDialogueVisible(false);
            SetCamera(minhCamera, false);
        }

        protected override void OnDisable()
        {
            if (dialogueRunning)
            {
                StopAllCoroutines();
                RestoreGameplayState();
            }

            base.OnDisable();
        }

        private void OnDestroy()
        {
            if (generatedDialogueCanvas && dialogueCanvas != null)
            {
                Destroy(dialogueCanvas.gameObject);
            }
        }

        private void OnValidate()
        {
            SanitizeSettings();
        }

        public override string GetInteractionPrompt(InteractionContext context)
        {
            return "[E] Trò chuyện với Minh";
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            if (dialogueRunning || context.PlayerObject == null)
            {
                return InteractionResult.Ignored();
            }

            ResolveMinhCamera();
            interactionController = context.InteractionController;
            inputLock = context.PlayerObject.GetComponent<PlayerInputLock>();

            if (gameplayCamera == null &&
                context.InteractionController != null)
            {
                gameplayCamera = context.InteractionController.GameplayCamera;
            }

            if (minhCamera == null || gameplayCamera == null)
            {
                return InteractionResult.Failed(
                    "Chưa thiết lập đủ camera cho hội thoại với Minh.");
            }

            dialoguePlayer = context.PlayerObject;
            activeInventory = context.Inventory != null
                ? context.Inventory
                : context.PlayerObject.GetComponent<PlayerInventory>();
            ResolveMission01Manager();
            activeMissionMode = DetermineMissionDialogueMode(activeInventory);
            activeMissionLines = BuildMissionDialogue(activeMissionMode);
            StartCoroutine(PlayDialogue());
            return InteractionResult.Succeeded();
        }

        private IEnumerator PlayDialogue()
        {
            dialogueRunning = true;

            if (inputLock != null)
            {
                inputLock.Lock(PlayerInputLock.DialogueReason);
                dialogueLockHeld = true;
            }

            interactionControllerWasEnabled =
                interactionController != null && interactionController.enabled;
            if (interactionControllerWasEnabled)
            {
                interactionController.enabled = false;
            }

            HidePlayerVisuals();
            PositionDialogueCamera();
            SetCamera(gameplayCamera, false);
            SetCamera(minhCamera, true);
            SetDialogueVisible(true);

            yield return WaitForAdvanceRelease();
            if (activeMissionLines != null && activeMissionLines.Count > 0)
            {
                for (int i = 0; i < activeMissionLines.Count; i++)
                {
                    DialogueLine line = activeMissionLines[i];
                    yield return StreamLine(line.Speaker, line.Text);
                }
            }
            ApplyMissionDialogueOutcome();
            RestoreGameplayState();
        }

        private MinhMissionDialogueMode DetermineMissionDialogueMode(PlayerInventory inventory)
        {
            Mission2HeistProgress.EnsureScene(gameObject.scene);
            if (mission01Manager == null)
            {
                return MinhMissionDialogueMode.Default;
            }

            FirstMissionState state = mission01Manager.State;
            Chapter1SaveData missionData = mission01Manager.Data;

            // Normalize sessions created before Task 1 ended immediately
            // after all six separated stems were saved. This also protects
            // against a missing mixer reference: Minh can recover the state
            // from the six recordings already present in the phone.
            if (state >= FirstMissionState.ProcessAudio &&
                state < FirstMissionState.ReturnToMinh &&
                missionData.AreAllLanAudioStemsSaved())
            {
                mission01Manager.NotifyAllLanAudioStemsSaved();
                state = mission01Manager.State;
            }

            if (state < FirstMissionState.MessageDung &&
                !missionData.Mission01MinhIntroDialoguePlayed)
            {
                // The legacy branch could reach Minh before it synchronized
                // the downloaded recording with Mission 01. Normalize that
                // state here so finishing the first main dialogue always
                // unlocks the Dung message choice.
                if (state < FirstMissionState.GoToMinhRoom)
                {
                    mission01Manager.NotifyLanRecordingSaved();
                }

                if (mission01Manager.State ==
                    FirstMissionState.GoToMinhRoom)
                {
                    mission01Manager.TryStartMinhIntroDialogue();
                }

                return MinhMissionDialogueMode.Intro;
            }

            if (state == FirstMissionState.ReturnToMinh &&
                !missionData.Mission01CompletionDialoguePlayed &&
                missionData.Mission01LanRecordingSeparated &&
                missionData.AreAllLanAudioStemsSaved())
            {
                return MinhMissionDialogueMode.Completion;
            }

            if (state >= FirstMissionState.MessageDung &&
                state < FirstMissionState.ReturnToMinh)
            {
                return MinhMissionDialogueMode.Reminder;
            }

            if (state == FirstMissionState.Completed)
            {
                return Mission2HeistProgress.IsStarted
                    ? MinhMissionDialogueMode.AlreadyCompleted
                    : MinhMissionDialogueMode.Task2Briefing;
            }

            return MinhMissionDialogueMode.Default;
        }

        private List<DialogueLine> BuildMissionDialogue(MinhMissionDialogueMode mode)
        {
            List<DialogueLine> lines = new List<DialogueLine>();
            switch (mode)
            {
                case MinhMissionDialogueMode.Default:
                case MinhMissionDialogueMode.Intro:
                    AddMainTask1IntroLines(lines);
                    break;
                case MinhMissionDialogueMode.Reminder:
                    lines.Add(new DialogueLine(minhSpeaker, "Nhắn hỏi Dũng về cái máy tách âm đi."));
                    break;
                case MinhMissionDialogueMode.Completion:
                case MinhMissionDialogueMode.Task2Briefing:
                    AddTask2BriefingLines(lines);
                    break;
                case MinhMissionDialogueMode.AlreadyCompleted:
                    lines.Add(new DialogueLine(minhSpeaker, "Tớ đang nghe lại phần ghi âm đã được tách."));
                    break;
            }

            return lines;
        }

        private void AddMainTask1IntroLines(List<DialogueLine> lines)
        {
            lines.Add(new DialogueLine(
                minhSpeaker,
                "Đoạn ghi âm này nhiều tạp âm quá. Nghe thế này thì không thể biết chị Lan đang nói gì được."));
            lines.Add(new DialogueLine(
                minhSpeaker,
                "Phải dùng máy tách âm để lọc giọng nói ra."));
            lines.Add(new DialogueLine(
                minhSpeaker,
                "Dũng có một cái. Cậu nhắn hỏi mượn thử xem."));
        }

        private void ApplyMissionDialogueOutcome()
        {
            if (mission01Manager != null &&
                activeMissionMode == MinhMissionDialogueMode.Intro)
            {
                mission01Manager.CompleteMinhIntroDialogue();
            }
            else if (mission01Manager != null &&
                     activeMissionMode == MinhMissionDialogueMode.Completion)
            {
                if (mission01Manager.TryCompleteWithMinh(activeInventory))
                {
                    Mission2HeistProgress.BeginMission(gameObject.scene);
                }
            }
            else if (activeMissionMode ==
                     MinhMissionDialogueMode.Task2Briefing)
            {
                Mission2HeistProgress.BeginMission(gameObject.scene);
            }

            activeMissionMode = MinhMissionDialogueMode.Default;
            activeMissionLines = null;
            activeInventory = null;
        }

        private void AddTask2BriefingLines(List<DialogueLine> lines)
        {
            lines.Add(new DialogueLine(
                playerSpeaker,
                secondPlayerOpeningLine));
            lines.Add(new DialogueLine(
                minhSpeaker,
                secondMinhAiLine));
            lines.Add(new DialogueLine(
                minhSpeaker,
                secondMinhTrainingTimeLine));
            lines.Add(new DialogueLine(
                playerSpeaker,
                secondPlayerUrgencyLine));
            lines.Add(new DialogueLine(
                minhSpeaker,
                secondMinhEquipmentLine));
            lines.Add(new DialogueLine(
                minhSpeaker,
                secondMinhScrapyardLine));
            lines.Add(new DialogueLine(
                minhSpeaker,
                secondMinhBatteryLine));
            lines.Add(new DialogueLine(
                playerSpeaker,
                secondPlayerAcceptLine));
        }

        private IEnumerator StreamLine(string speaker, string line)
        {
            string safeLine = line ?? string.Empty;

            if (speakerText != null)
            {
                speakerText.text = speaker;
            }

            if (lineText != null)
            {
                lineText.text = safeLine;
                lineText.maxVisibleCharacters = 0;
            }

            if (speakerText != null)
            {
                speakerText.color = string.Equals(speaker, minhSpeaker)
                    ? new Color(0.45f, 0.95f, 0.68f)
                    : new Color(0.45f, 0.78f, 1f);
            }

            if (lineText == null)
            {
                yield return WaitForNextLine();
                yield break;
            }

            lineText.ForceMeshUpdate();
            int characterCount = lineText.textInfo.characterCount;
            bool skippedTyping = false;

            for (int i = 0; i < characterCount; i++)
            {
                if (IsAdvancePressed())
                {
                    skippedTyping = true;
                    break;
                }

                lineText.maxVisibleCharacters = i + 1;
                char visibleCharacter =
                    lineText.textInfo.characterInfo[i].character;
                float delay = GetCharacterDelay(visibleCharacter);
                float elapsed = 0f;

                while (elapsed < delay)
                {
                    if (IsAdvancePressed())
                    {
                        skippedTyping = true;
                        break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (skippedTyping)
                {
                    break;
                }
            }

            lineText.maxVisibleCharacters = int.MaxValue;
            if (skippedTyping)
            {
                yield return WaitForAdvanceRelease();
            }

            yield return WaitForNextLine();
        }

        private float GetCharacterDelay(char character)
        {
            float baseDelay = 1f / Mathf.Max(1f, charactersPerSecond);
            if (character == '.' ||
                character == '!' ||
                character == '?')
            {
                return baseDelay * punctuationPauseMultiplier;
            }

            if (character == ',' ||
                character == ';' ||
                character == ':')
            {
                return baseDelay *
                       Mathf.Lerp(1f, punctuationPauseMultiplier, 0.5f);
            }

            return baseDelay;
        }

        private static IEnumerator WaitForNextLine()
        {
            while (!IsAdvancePressed())
            {
                yield return null;
            }

            yield return WaitForAdvanceRelease();
        }

        private static IEnumerator WaitForAdvanceRelease()
        {
            while (IsAdvanceHeld())
            {
                yield return null;
            }

            yield return null;
        }

        private static bool IsAdvancePressed()
        {
            Keyboard keyboard = Keyboard.current;

            return keyboard != null &&
                   (keyboard.eKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame);
        }

        private static bool IsAdvanceHeld()
        {
            Keyboard keyboard = Keyboard.current;

            return keyboard != null &&
                   (keyboard.eKey.isPressed ||
                    keyboard.spaceKey.isPressed ||
                    keyboard.enterKey.isPressed);
        }

        private void RestoreGameplayState()
        {
            SetDialogueVisible(false);
            SetCamera(minhCamera, false);
            RestorePlayerVisuals();
            SetCamera(gameplayCamera, true);

            if (interactionController != null && interactionControllerWasEnabled)
            {
                interactionController.enabled = true;
            }

            if (inputLock != null && dialogueLockHeld)
            {
                inputLock.Unlock(PlayerInputLock.DialogueReason);
            }

            interactionControllerWasEnabled = false;
            dialogueLockHeld = false;
            dialogueRunning = false;
            dialoguePlayer = null;
        }

        private void HidePlayerVisuals()
        {
            RestorePlayerVisuals();

            if (dialoguePlayer == null)
            {
                return;
            }

            hiddenPlayerRenderers =
                dialoguePlayer.GetComponentsInChildren<Renderer>(true);
            playerRendererEnabledStates =
                new bool[hiddenPlayerRenderers.Length];

            for (int i = 0; i < hiddenPlayerRenderers.Length; i++)
            {
                Renderer playerRenderer = hiddenPlayerRenderers[i];
                if (playerRenderer == null)
                {
                    continue;
                }

                playerRendererEnabledStates[i] = playerRenderer.enabled;
                playerRenderer.enabled = false;
            }
        }

        private void RestorePlayerVisuals()
        {
            if (hiddenPlayerRenderers == null ||
                playerRendererEnabledStates == null)
            {
                return;
            }

            int rendererCount = Mathf.Min(
                hiddenPlayerRenderers.Length,
                playerRendererEnabledStates.Length);

            for (int i = 0; i < rendererCount; i++)
            {
                Renderer playerRenderer = hiddenPlayerRenderers[i];
                if (playerRenderer != null)
                {
                    playerRenderer.enabled =
                        playerRendererEnabledStates[i];
                }
            }

            hiddenPlayerRenderers = null;
            playerRendererEnabledStates = null;
        }

        private void ResolveMinhCamera()
        {
            if (minhCamera != null)
            {
                return;
            }

            Transform searchRoot = transform.parent != null
                ? transform.parent
                : transform;

            Camera[] cameras = searchRoot.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null &&
                    string.Equals(cameras[i].name, "Minh_camera"))
                {
                    minhCamera = cameras[i];
                    return;
                }
            }
        }

        private void ResolveMission01Manager()
        {
            if (mission01Manager == null)
            {
                mission01Manager = Mission01AudioSeparatorManager.Instance;
            }
        }

        private void SanitizeSettings()
        {
            if (conversationDistance < 0.5f)
            {
                conversationDistance = 1.65f;
            }

            if (standingEyeHeight < 1f)
            {
                standingEyeHeight = 1.62f;
            }

            if (faceHeightRatio < 0.65f || faceHeightRatio > 0.98f)
            {
                faceHeightRatio = 0.88f;
            }

            if (dialogueFieldOfView < 35f ||
                dialogueFieldOfView > 80f)
            {
                dialogueFieldOfView = 55f;
            }

            if (charactersPerSecond < 10f ||
                charactersPerSecond > 80f)
            {
                charactersPerSecond = 34f;
            }

            if (punctuationPauseMultiplier < 1f ||
                punctuationPauseMultiplier > 6f)
            {
                punctuationPauseMultiplier = 2.5f;
            }
        }

        private void ConfigureInteractionTrigger()
        {
            SphereCollider interactionCollider =
                GetComponent<SphereCollider>();
            if (interactionCollider != null)
            {
                interactionCollider.isTrigger = true;
            }

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }
        }

        private void PositionDialogueCamera()
        {
            if (minhCamera == null)
            {
                return;
            }

            Transform npcRoot = minhCamera.transform.parent != null
                ? minhCamera.transform.parent
                : transform;

            Bounds npcBounds = GetNpcBounds(npcRoot);
            Vector3 facePosition = new Vector3(
                npcBounds.center.x,
                Mathf.Lerp(npcBounds.min.y, npcBounds.max.y, faceHeightRatio),
                npcBounds.center.z);

            Vector3 frontDirection = Vector3.ProjectOnPlane(
                minhCamera.transform.position - facePosition,
                Vector3.up);

            if (frontDirection.sqrMagnitude < 0.01f)
            {
                frontDirection = Vector3.ProjectOnPlane(
                    npcRoot.forward,
                    Vector3.up);
            }

            if (frontDirection.sqrMagnitude < 0.01f)
            {
                frontDirection = Vector3.forward;
            }

            frontDirection.Normalize();

            Vector3 cameraPosition =
                facePosition + frontDirection * conversationDistance;
            cameraPosition.y = npcBounds.min.y + standingEyeHeight;

            minhCamera.transform.position = cameraPosition;
            minhCamera.transform.LookAt(facePosition, Vector3.up);
            minhCamera.fieldOfView = dialogueFieldOfView;
            minhCamera.nearClipPlane = 0.1f;

            if (gameplayCamera != null)
            {
                minhCamera.depth = gameplayCamera.depth + 1f;
            }
        }

        private static Bounds GetNpcBounds(Transform npcRoot)
        {
            Renderer[] renderers =
                npcRoot.GetComponentsInChildren<Renderer>(true);

            bool hasBounds = false;
            Bounds bounds = new Bounds(npcRoot.position, Vector3.one * 1.5f);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer currentRenderer = renderers[i];
                if (currentRenderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = currentRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(currentRenderer.bounds);
                }
            }

            return bounds;
        }

        private void EnsureDialogueUI()
        {
            if (dialogueCanvas != null &&
                speakerText != null &&
                lineText != null)
            {
                if (advanceHintText != null)
                {
                    advanceHintText.text =
                        "E / Space / Enter: hiện nhanh / tiếp tục";
                }

                return;
            }

            GameObject canvasObject = new GameObject(
                "MinhDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            dialogueCanvas = canvasObject.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject(
                "DialoguePanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(48f, 32f);
            panelRect.offsetMax = new Vector2(-48f, 232f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
            panelImage.raycastTarget = false;

            speakerText = CreateText(
                "SpeakerText",
                panelRect,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -16f),
                new Vector2(-48f, 44f),
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            lineText = CreateText(
                "LineText",
                panelRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f),
                new Vector2(-48f, -98f),
                34f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);

            advanceHintText = CreateText(
                "AdvanceHintText",
                panelRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 12f),
                new Vector2(-48f, 32f),
                18f,
                FontStyles.Italic,
                TextAlignmentOptions.MidlineRight);

            advanceHintText.text =
                "E / Space / Enter: hiện nhanh / tiếp tục";
            advanceHintText.color = new Color(0.72f, 0.76f, 0.82f, 0.9f);
            generatedDialogueCanvas = true;
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            return text;
        }

        private void SetDialogueVisible(bool visible)
        {
            if (dialogueCanvas != null)
            {
                dialogueCanvas.gameObject.SetActive(visible);
            }
        }

        private static void SetCamera(Camera camera, bool enabled)
        {
            if (camera == null)
            {
                return;
            }

            camera.enabled = enabled;

            AudioListener listener =
                camera.GetComponent<AudioListener>() ??
                camera.GetComponentInChildren<AudioListener>(true);

            if (listener != null)
            {
                listener.enabled = enabled;
            }
        }

        private enum MinhMissionDialogueMode
        {
            Default,
            Intro,
            Reminder,
            Completion,
            Task2Briefing,
            AlreadyCompleted
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(string speaker, string text)
            {
                Speaker = speaker ?? string.Empty;
                Text = text ?? string.Empty;
            }

            public string Speaker { get; }
            public string Text { get; }
        }
    }
}
