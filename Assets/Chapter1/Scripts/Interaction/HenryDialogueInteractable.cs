using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class HenryDialogueInteractable : Chapter1Interactable
    {
        private const string InteractionTriggerName =
            "Henry_Dialogue_Interaction";
        private const string HenryCameraName = "HenryCamera";

        [Header("Henry")]
        [SerializeField] private Transform henryRoot;
        [SerializeField] private HenryChaseController chaseController;
        [SerializeField] private SphereCollider interactionTrigger;
        [SerializeField, Min(0.5f)] private float conversationRange = 2.8f;
        [SerializeField, Range(-1f, 1f)] private float minimumFrontDot = 0.35f;

        [Header("Camera")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Camera henryCamera;

        [Header("Dialogue UI")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private TMP_Text advanceHintText;

        [Header("Dialogue")]
        [SerializeField] private string playerSpeaker = "Player";
        [SerializeField] private string henrySpeaker = "Henry";
        [SerializeField, TextArea] private string playerLine =
            "Ch\u00E0o \u00F4ng Henry, ch\u00E1u c\u00F3 th\u1EC3 m\u01B0\u1EE3n \u1EAFc quy c\u1EE7a \u00F4ng kh\u00F4ng ?";
        [SerializeField, TextArea] private string henryRefusalLine =
            "Kh\u00F4ng, tao r\u1EA5t k\u00EC th\u1ECB nh\u1EEFng \u0111\u1EE9a sinh vi\u00EAn b\u1ECDn m\u00E0y.";
        [SerializeField, TextArea] private string henryBackstoryLine =
            "V\u00EC ng\u00E0y x\u01B0a tao h\u1ECDc d\u1ED1t n\u00EAn thi r\u1EDBt \u0111\u1EA1i h\u1ECDc";
        [SerializeField, TextArea] private string henryDismissLine =
            "C\u00FAt \u0111i !!";
        [SerializeField, Range(10f, 80f)] private float charactersPerSecond =
            34f;
        [SerializeField, Range(1f, 6f)] private float punctuationPauseMultiplier =
            2.5f;

        private Chapter1InteractionController interactionController;
        private PlayerInputLock inputLock;
        private GameObject dialoguePlayer;
        private Renderer[] hiddenPlayerRenderers;
        private bool[] playerRendererEnabledStates;
        private bool dialogueRunning;
        private bool interactionControllerWasEnabled;
        private bool dialogueLockHeld;
        private bool generatedDialogueCanvas;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            SanitizeSettings();
            ConfigureInteractionTrigger();
            ResolveHenryCamera();
            EnsureDialogueUI();
            SetDialogueVisible(false);
            SetCamera(henryCamera, false);
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
            return "[E] Tr\u00F2 chuy\u1EC7n v\u1EDBi Henry";
        }

        public override bool CanInteract(InteractionContext context)
        {
            return base.CanInteract(context) &&
                   IsConversationAvailable(context.PlayerTransform);
        }

        public override Transform GetInteractionTransform()
        {
            ResolveReferences();
            return interactionTrigger != null
                ? interactionTrigger.transform
                : transform;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            ResolveReferences();
            if (context.PlayerObject == null ||
                !IsConversationAvailable(context.PlayerTransform))
            {
                return InteractionResult.Ignored();
            }

            interactionController = context.InteractionController;
            inputLock = context.PlayerObject.GetComponent<PlayerInputLock>();

            if (gameplayCamera == null && interactionController != null)
            {
                gameplayCamera = interactionController.GameplayCamera;
            }

            ResolveHenryCamera();
            if (gameplayCamera == null || henryCamera == null)
            {
                return InteractionResult.Failed(
                    "Ch\u01B0a thi\u1EBFt l\u1EADp \u0111\u1EE7 camera cho h\u1ED9i tho\u1EA1i v\u1EDBi Henry.");
            }

            dialoguePlayer = context.PlayerObject;
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
            SetCamera(gameplayCamera, false);
            SetCamera(henryCamera, true);
            SetDialogueVisible(true);

            yield return WaitForAdvanceRelease();
            yield return StreamLine(playerSpeaker, playerLine);
            yield return StreamLine(henrySpeaker, henryRefusalLine);
            yield return StreamLine(henrySpeaker, henryBackstoryLine);
            yield return StreamLine(henrySpeaker, henryDismissLine);

            RestoreGameplayState();
        }

        private IEnumerator StreamLine(string speaker, string line)
        {
            string safeLine = line ?? string.Empty;

            if (speakerText != null)
            {
                speakerText.text = speaker;
                speakerText.color = string.Equals(
                    speaker,
                    henrySpeaker,
                    StringComparison.Ordinal)
                    ? new Color(1f, 0.58f, 0.32f)
                    : new Color(0.45f, 0.78f, 1f);
            }

            if (lineText == null)
            {
                yield return WaitForNextLine();
                yield break;
            }

            lineText.text = safeLine;
            lineText.maxVisibleCharacters = 0;
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
                float elapsed = 0f;
                float delay = GetCharacterDelay(visibleCharacter);
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
            if (character == '.' || character == '!' || character == '?')
            {
                return baseDelay * punctuationPauseMultiplier;
            }

            if (character == ',' || character == ';' || character == ':')
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

        private bool IsConversationAvailable(Transform playerTransform)
        {
            if (dialogueRunning || playerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            if (henryRoot == null ||
                chaseController == null ||
                !chaseController.CanStartDistraction)
            {
                return false;
            }

            Vector3 toPlayer = Vector3.ProjectOnPlane(
                playerTransform.position - henryRoot.position,
                Vector3.up);
            float distance = toPlayer.magnitude;
            if (distance <= 0.01f || distance > conversationRange)
            {
                return false;
            }

            Vector3 front = GetHenryFront();
            return Vector3.Dot(front, toPlayer / distance) >=
                   minimumFrontDot;
        }

        private Vector3 GetHenryFront()
        {
            Vector3 front = henryRoot != null
                ? Vector3.ProjectOnPlane(henryRoot.forward, Vector3.up)
                : Vector3.forward;
            return front.sqrMagnitude > 0.001f
                ? front.normalized
                : Vector3.forward;
        }

        private void RestoreGameplayState()
        {
            SetDialogueVisible(false);
            SetCamera(henryCamera, false);
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

        private void ResolveReferences()
        {
            if (henryRoot == null)
            {
                henryRoot = transform.parent != null
                    ? transform.parent
                    : transform;
            }

            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<SphereCollider>();
            }

            if (chaseController == null && henryRoot != null)
            {
                chaseController =
                    henryRoot.GetComponent<HenryChaseController>();
            }
        }

        private void ConfigureInteractionTrigger()
        {
            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<SphereCollider>();
            }

            if (interactionTrigger != null)
            {
                interactionTrigger.isTrigger = true;
            }

            int interactableLayer = LayerMask.NameToLayer(
                HenryTheftInteractable.InteractableLayerName);
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }
        }

        private void SanitizeSettings()
        {
            conversationRange = Mathf.Max(0.5f, conversationRange);
            minimumFrontDot = Mathf.Clamp(minimumFrontDot, -1f, 1f);
            charactersPerSecond = Mathf.Clamp(
                charactersPerSecond,
                10f,
                80f);
            punctuationPauseMultiplier = Mathf.Clamp(
                punctuationPauseMultiplier,
                1f,
                6f);
        }

        private void ResolveHenryCamera()
        {
            if (henryCamera != null)
            {
                return;
            }

            ResolveReferences();
            var targetScene = henryRoot != null
                ? henryRoot.gameObject.scene
                : gameObject.scene;

            Camera[] cameras = FindObjectsByType<Camera>(
                FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate != null &&
                    candidate.gameObject.scene == targetScene &&
                    string.Equals(
                        candidate.name,
                        HenryCameraName,
                        StringComparison.Ordinal))
                {
                    henryCamera = candidate;
                    return;
                }
            }
        }

        private void EnsureDialogueUI()
        {
            if (dialogueCanvas != null &&
                speakerText != null &&
                lineText != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "HenryDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            dialogueCanvas = canvasObject.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 201;

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

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
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
                "E / Space / Enter: hi\u1EC7n nhanh / ti\u1EBFp t\u1EE5c";
            advanceHintText.color =
                new Color(0.72f, 0.76f, 0.82f, 0.9f);
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

        internal static void InstallOnHenry(
            GameObject henry,
            int interactableLayer,
            HenryChaseController chase)
        {
            if (henry == null)
            {
                return;
            }

            Transform triggerTransform = henry.transform.Find(
                InteractionTriggerName);
            if (triggerTransform == null)
            {
                GameObject triggerObject = new GameObject(
                    InteractionTriggerName);
                triggerTransform = triggerObject.transform;
                triggerTransform.SetParent(henry.transform, true);
            }

            Vector3 front = Vector3.ProjectOnPlane(
                henry.transform.forward,
                Vector3.up);
            if (front.sqrMagnitude < 0.001f)
            {
                front = Vector3.forward;
            }

            front.Normalize();
            triggerTransform.position =
                henry.transform.position +
                front * 1.15f +
                Vector3.up * 1.35f;
            triggerTransform.rotation = henry.transform.rotation;
            triggerTransform.localScale = Vector3.one;
            triggerTransform.gameObject.layer = interactableLayer;

            SphereCollider trigger =
                triggerTransform.GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = triggerTransform.gameObject
                    .AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.radius = 0.35f;

            HenryDialogueInteractable dialogue =
                triggerTransform.GetComponent<HenryDialogueInteractable>();
            if (dialogue == null)
            {
                dialogue = triggerTransform.gameObject
                    .AddComponent<HenryDialogueInteractable>();
            }

            dialogue.henryRoot = henry.transform;
            dialogue.chaseController = chase;
            dialogue.interactionTrigger = trigger;
            dialogue.ConfigureInteractionTrigger();
            dialogue.ResolveHenryCamera();
        }
    }
}
