using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class JamesDialogueInteractable : Chapter1Interactable
    {
        private const string JamesObjectName = "James";
        private const string JamesCameraName = "Black_guys_camera";
        private const string TriggerName = "James_Dialogue_Interaction";
        private const string InteractableLayerName = "Interactable";

        [SerializeField] private Transform jamesRoot;
        [SerializeField] private SphereCollider interactionTrigger;
        [SerializeField, Min(0.5f)] private float conversationRange = 3.2f;

        [Header("Camera")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Camera blackGuysCamera;

        [Header("Dialogue UI")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private TMP_Text advanceHintText;

        [Header("Dialogue")]
        [SerializeField, Range(10f, 80f)] private float charactersPerSecond = 34f;
        [SerializeField, Range(1f, 6f)] private float punctuationPauseMultiplier = 2.5f;

        private Chapter1InteractionController interactionController;
        private PlayerInputLock inputLock;
        private GameObject dialoguePlayer;
        private Renderer[] hiddenPlayerRenderers;
        private bool[] playerRendererEnabledStates;
        private Collider[] disabledPlayerColliders;
        private bool[] playerColliderEnabledStates;
        private bool dialogueRunning;
        private bool puzzleRunning;
        private bool interactionControllerWasEnabled;
        private bool dialogueLockHeld;
        private JamesWordPuzzleController puzzleController;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallForScene(scene);
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Transform james = FindSceneTransform(scene, JamesObjectName);
            if (james == null)
            {
                return;
            }

            int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogWarning($"[JamesDialogue] Layer '{InteractableLayerName}' does not exist.");
                return;
            }

            Camera blackCamera = FindSceneCamera(scene, JamesCameraName);
            Transform triggerTransform = james.Find(TriggerName);
            if (triggerTransform == null)
            {
                GameObject triggerObject = new GameObject(TriggerName);
                triggerTransform = triggerObject.transform;
                triggerTransform.SetParent(james, true);
            }

            Vector3 facing = Vector3.ProjectOnPlane(
                james.forward,
                Vector3.up);
            if (facing.sqrMagnitude <= 0.001f)
            {
                facing = Vector3.forward;
            }

            triggerTransform.SetPositionAndRotation(
                james.position + Vector3.up * 1.05f,
                Quaternion.LookRotation(facing.normalized, Vector3.up));
            triggerTransform.localScale = Vector3.one;
            triggerTransform.gameObject.layer = interactableLayer;

            SphereCollider trigger = triggerTransform.GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = triggerTransform.gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.radius = 0.75f;

            JamesDialogueInteractable dialogue =
                triggerTransform.GetComponent<JamesDialogueInteractable>();
            if (dialogue == null)
            {
                dialogue = triggerTransform.gameObject.AddComponent<JamesDialogueInteractable>();
            }

            dialogue.jamesRoot = james;
            dialogue.interactionTrigger = trigger;
            dialogue.blackGuysCamera = blackCamera;
            dialogue.ResolveReferences();
            SetCamera(blackCamera, false);
        }

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            EnsureDialogueUi();
            SetDialogueVisible(false);
        }

        protected override void OnDisable()
        {
            if (dialogueRunning || puzzleRunning)
            {
                StopAllCoroutines();
                RestoreGameplayState();
            }

            base.OnDisable();
        }

        public override string GetInteractionPrompt(InteractionContext context)
        {
            return "[E] Trò chuyện";
        }

        public override bool CanInteract(InteractionContext context)
        {
            return Mission3Progress.CanTalkToJames &&
                   !Mission3Progress.ChallengePassed &&
                   !Mission3Progress.GangHostile &&
                   base.CanInteract(context) &&
                   IsConversationAvailable(context.PlayerTransform);
        }

        public override Transform GetInteractionTransform()
        {
            return interactionTrigger != null ? interactionTrigger.transform : transform;
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            if (context.PlayerObject == null ||
                !Mission3Progress.CanTalkToJames ||
                Mission3Progress.ChallengePassed ||
                Mission3Progress.GangHostile ||
                !IsConversationAvailable(context.PlayerTransform))
            {
                return InteractionResult.Ignored();
            }

            interactionController = context.InteractionController;
            inputLock = context.PlayerObject.GetComponent<PlayerInputLock>();
            gameplayCamera = interactionController != null
                ? interactionController.GameplayCamera
                : Camera.main;
            dialoguePlayer = context.PlayerObject;
            ResolveReferences();

            if (gameplayCamera == null || blackGuysCamera == null)
            {
                return InteractionResult.Failed(
                    "Chưa thiết lập đủ camera cho hội thoại với James.");
            }

            if (!Mission3Progress.JamesIntroPlayed)
            {
                StartCoroutine(PlayIntroduction());
            }
            else
            {
                OpenPuzzle();
            }

            return InteractionResult.Succeeded();
        }

        private IEnumerator PlayIntroduction()
        {
            BeginDialogue(true);
            yield return WaitForAdvanceRelease();
            yield return StreamLine(
                "Player",
                "Tôi cần đột nhập được vào đồn cảnh sát, các anh có thể giúp tôi không?");
            yield return StreamLine(
                "James",
                "Được nhưng trước hết mày phải giải thử thách tụi anh đưa ra.");

            Mission3Progress.MarkJamesIntroPlayed();
            if (speakerText != null)
            {
                speakerText.text = string.Empty;
            }

            if (lineText != null)
            {
                lineText.text = string.Empty;
            }

            if (advanceHintText != null)
            {
                advanceHintText.text = "[E] Bắt đầu thử thách";
            }

            yield return WaitForAdvanceRelease();
            while (Keyboard.current == null ||
                   !Keyboard.current.eKey.wasPressedThisFrame)
            {
                yield return null;
            }

            yield return WaitForAdvanceRelease();
            if (advanceHintText != null)
            {
                advanceHintText.text =
                    "E / Space / Enter: hiện nhanh / tiếp tục";
            }

            SetDialogueVisible(false);
            dialogueRunning = false;
            if (inputLock != null && dialogueLockHeld)
            {
                inputLock.Unlock(PlayerInputLock.DialogueReason);
                dialogueLockHeld = false;
            }

            OpenPuzzleFromActiveDialogue();
        }

        private void OpenPuzzle()
        {
            puzzleRunning = true;
            HidePlayerVisuals();
            DisablePlayerColliders();
            SetCamera(gameplayCamera, false);
            SetCamera(blackGuysCamera, true);

            OpenPuzzleController();
        }

        private void OpenPuzzleFromActiveDialogue()
        {
            puzzleRunning = true;
            // The intro already owns the camera/player visibility and has
            // disabled the interaction controller. Reuse that state so there
            // is no one-frame return to gameplay between dialogue and puzzle.
            if (interactionController != null && interactionControllerWasEnabled)
            {
                interactionController.enabled = true;
            }
            OpenPuzzleController();
        }

        private void OpenPuzzleController()
        {

            if (puzzleController == null)
            {
                puzzleController = GetComponent<JamesWordPuzzleController>();
                if (puzzleController == null)
                {
                    puzzleController = gameObject.AddComponent<JamesWordPuzzleController>();
                }
            }

            puzzleController.Open(this, dialoguePlayer, interactionController);
        }

        internal void HandlePuzzleCompleted()
        {
            puzzleRunning = false;
            StartCoroutine(PlaySuccessLine());
        }

        internal void HandleForbiddenAnswer()
        {
            puzzleRunning = false;
            SetCamera(blackGuysCamera, false);
            RestorePlayerVisuals();
            RestorePlayerColliders();
            SetCamera(gameplayCamera, true);
            StartCoroutine(PlayHostileLine());
        }

        internal void HandlePuzzleClosed()
        {
            puzzleRunning = false;
            RestoreGameplayState();
        }

        private IEnumerator PlaySuccessLine()
        {
            BeginDialogue(false);
            yield return WaitForAdvanceRelease();
            yield return StreamLine(
                "James",
                "Khá đấy. Tụi tao sẽ giúp mày đột nhập vào đồn cảnh sát.");
            Mission3Progress.MarkChallengePassed();
            RestoreGameplayState();
        }

        private IEnumerator PlayHostileLine()
        {
            BeginDialogue(false);
            yield return WaitForAdvanceRelease();
            yield return StreamLine(
                "James",
                "Thằng phân biệt chủng tộc này, mày chết chắc rồi con!");
            Mission3Progress.MarkGangHostile();
            RestoreGameplayState();
            GangEncounterController.BeginHostileEncounter();
        }

        private void BeginDialogue(bool switchToBlackCamera)
        {
            dialogueRunning = true;
            if (inputLock != null)
            {
                inputLock.Lock(PlayerInputLock.DialogueReason);
                dialogueLockHeld = true;
            }

            interactionControllerWasEnabled = interactionController != null &&
                                              interactionController.enabled;
            if (interactionControllerWasEnabled)
            {
                interactionController.enabled = false;
            }

            if (switchToBlackCamera)
            {
                HidePlayerVisuals();
                DisablePlayerColliders();
                SetCamera(gameplayCamera, false);
                SetCamera(blackGuysCamera, true);
            }

            SetDialogueVisible(true);
        }

        private IEnumerator StreamLine(string speaker, string line)
        {
            speakerText.text = speaker;
            speakerText.color = string.Equals(speaker, "James", StringComparison.Ordinal)
                ? new Color(1f, 0.64f, 0.28f)
                : new Color(0.45f, 0.78f, 1f);
            lineText.text = line ?? string.Empty;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();

            bool skipped = false;
            for (int i = 0; i < lineText.textInfo.characterCount; i++)
            {
                if (IsAdvancePressed())
                {
                    skipped = true;
                    break;
                }

                lineText.maxVisibleCharacters = i + 1;
                char character = lineText.textInfo.characterInfo[i].character;
                float delay = GetCharacterDelay(character);
                float elapsed = 0f;
                while (elapsed < delay)
                {
                    if (IsAdvancePressed())
                    {
                        skipped = true;
                        break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (skipped)
                {
                    break;
                }
            }

            lineText.maxVisibleCharacters = int.MaxValue;
            if (skipped)
            {
                yield return WaitForAdvanceRelease();
            }

            while (!IsAdvancePressed())
            {
                yield return null;
            }

            yield return WaitForAdvanceRelease();
        }

        private float GetCharacterDelay(char character)
        {
            float baseDelay = 1f / Mathf.Max(1f, charactersPerSecond);
            if (character == '.' || character == '!' || character == '?')
            {
                return baseDelay * punctuationPauseMultiplier;
            }

            return baseDelay;
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

        private static IEnumerator WaitForAdvanceRelease()
        {
            while (IsAdvanceHeld())
            {
                yield return null;
            }

            yield return null;
        }

        private bool IsConversationAvailable(Transform playerTransform)
        {
            if (dialogueRunning || puzzleRunning || playerTransform == null || jamesRoot == null)
            {
                return false;
            }

            Vector3 toPlayer = Vector3.ProjectOnPlane(
                playerTransform.position - jamesRoot.position,
                Vector3.up);
            float distance = toPlayer.magnitude;
            if (distance <= 0.01f || distance > conversationRange)
            {
                return false;
            }

            return true;
        }

        private void RestoreGameplayState()
        {
            SetDialogueVisible(false);
            SetCamera(blackGuysCamera, false);
            RestorePlayerVisuals();
            RestorePlayerColliders();
            SetCamera(gameplayCamera, true);

            if (interactionController != null && interactionControllerWasEnabled)
            {
                interactionController.enabled = true;
            }

            if (inputLock != null && dialogueLockHeld)
            {
                inputLock.Unlock(PlayerInputLock.DialogueReason);
            }

            dialogueRunning = false;
            puzzleRunning = false;
            dialogueLockHeld = false;
            interactionControllerWasEnabled = false;
            dialoguePlayer = null;
        }

        private void HidePlayerVisuals()
        {
            RestorePlayerVisuals();
            if (dialoguePlayer == null)
            {
                return;
            }

            hiddenPlayerRenderers = dialoguePlayer.GetComponentsInChildren<Renderer>(true);
            playerRendererEnabledStates = new bool[hiddenPlayerRenderers.Length];
            for (int i = 0; i < hiddenPlayerRenderers.Length; i++)
            {
                Renderer renderer = hiddenPlayerRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                playerRendererEnabledStates[i] = renderer.enabled;
                renderer.enabled = false;
            }
        }

        private void RestorePlayerVisuals()
        {
            if (hiddenPlayerRenderers == null || playerRendererEnabledStates == null)
            {
                return;
            }

            int count = Mathf.Min(hiddenPlayerRenderers.Length, playerRendererEnabledStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (hiddenPlayerRenderers[i] != null)
                {
                    hiddenPlayerRenderers[i].enabled = playerRendererEnabledStates[i];
                }
            }

            hiddenPlayerRenderers = null;
            playerRendererEnabledStates = null;
        }

        private void DisablePlayerColliders()
        {
            RestorePlayerColliders();
            if (dialoguePlayer == null)
            {
                return;
            }

            disabledPlayerColliders =
                dialoguePlayer.GetComponentsInChildren<Collider>(true);
            playerColliderEnabledStates =
                new bool[disabledPlayerColliders.Length];
            for (int i = 0; i < disabledPlayerColliders.Length; i++)
            {
                Collider playerCollider = disabledPlayerColliders[i];
                if (playerCollider == null)
                {
                    continue;
                }

                playerColliderEnabledStates[i] = playerCollider.enabled;

                // PlayerMotor continues applying gravity while the dialogue
                // camera owns the view. Keeping the CharacterController active
                // avoids Unity warning about Move being called on a disabled
                // controller; the player is input-locked for the whole sequence.
                if (playerCollider is CharacterController)
                {
                    continue;
                }

                playerCollider.enabled = false;
            }
        }

        private void RestorePlayerColliders()
        {
            if (disabledPlayerColliders == null ||
                playerColliderEnabledStates == null)
            {
                return;
            }

            int count = Mathf.Min(
                disabledPlayerColliders.Length,
                playerColliderEnabledStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (disabledPlayerColliders[i] != null)
                {
                    disabledPlayerColliders[i].enabled =
                        playerColliderEnabledStates[i];
                }
            }

            disabledPlayerColliders = null;
            playerColliderEnabledStates = null;
        }

        private void ResolveReferences()
        {
            if (jamesRoot == null)
            {
                jamesRoot = transform.parent != null ? transform.parent : transform;
            }

            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<SphereCollider>();
            }

            if (blackGuysCamera == null)
            {
                blackGuysCamera = FindSceneCamera(gameObject.scene, JamesCameraName);
            }
        }

        private void EnsureDialogueUi()
        {
            if (dialogueCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "JamesDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            dialogueCanvas = canvasObject.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 231;

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
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.offsetMin = new Vector2(48f, 32f);
            panel.offsetMax = new Vector2(-48f, 232f);
            panelObject.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.94f);

            speakerText = CreateText(panel, "Speaker", 30f, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, -16f),
                new Vector2(-48f, 44f), true);
            lineText = CreateText(panel, "Line", 34f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, -8f),
                new Vector2(-48f, -98f), false);
            advanceHintText = CreateText(panel, "AdvanceHint", 18f, FontStyles.Italic,
                TextAlignmentOptions.MidlineRight, new Vector2(0f, 12f),
                new Vector2(-48f, 32f), false, true);
            advanceHintText.text = "E / Space / Enter: hiện nhanh / tiếp tục";
            advanceHintText.color = new Color(0.72f, 0.76f, 0.82f, 0.9f);
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string name,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool topAnchored,
            bool bottomAnchored = false)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = bottomAnchored ? new Vector2(0f, 0f) : new Vector2(0f, topAnchored ? 1f : 0f);
            rect.anchorMax = bottomAnchored ? new Vector2(1f, 0f) : Vector2.one;
            rect.pivot = new Vector2(0.5f, topAnchored ? 1f : (bottomAnchored ? 0f : 0.5f));
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
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

        private static Transform FindSceneTransform(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (string.Equals(transforms[i].name, objectName, StringComparison.Ordinal))
                    {
                        return transforms[i];
                    }
                }
            }

            return null;
        }

        private static Camera FindSceneCamera(Scene scene, string cameraName)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].gameObject.scene == scene &&
                    string.Equals(cameras[i].name, cameraName, StringComparison.Ordinal))
                {
                    return cameras[i];
                }
            }

            return null;
        }

        private static void SetCamera(Camera camera, bool enabled)
        {
            if (camera == null)
            {
                return;
            }

            camera.enabled = enabled;
            AudioListener listener = camera.GetComponent<AudioListener>() ??
                                     camera.GetComponentInChildren<AudioListener>(true);
            if (listener != null)
            {
                listener.enabled = enabled;
            }
        }
    }
}
