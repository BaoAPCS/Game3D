using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        private const float DefaultSingleLineHeight = 72f;
        private const float AdditionalLineHeight = 38f;
        private const string RuntimeCanvasName =
            "Chapter1InteractionPrompt_Runtime";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private float fadeDuration = 0.15f;

        private Chapter1InteractionController interactionController;
        private PlayerInputLock inputLock;
        private Coroutine fadeRoutine;
        private string currentPrompt = string.Empty;
        private RectTransform promptRect;
        private float singleLineHeight;
        private bool layoutInitialized;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePromptAfterInitialSceneLoad()
        {
            EnsureRuntimePrompt();
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            EnsureRuntimePrompt();
        }

        private static void EnsureRuntimePrompt()
        {
            Chapter1InteractionController controller =
                FindAnyObjectByType<Chapter1InteractionController>();
            if (controller == null)
            {
                return;
            }

            InteractionPromptUI promptUI =
                FindAnyObjectByType<InteractionPromptUI>(
                    FindObjectsInactive.Include);
            if (promptUI == null)
            {
                promptUI = CreateRuntimePrompt();
            }

            PlayerInputLock inputLock =
                controller.GetComponent<PlayerInputLock>() ??
                FindAnyObjectByType<PlayerInputLock>();
            promptUI.Bind(controller, inputLock);
        }

        private static InteractionPromptUI CreateRuntimePrompt()
        {
            GameObject canvasObject = new GameObject(
                RuntimeCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject(
                "InteractionPrompt",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 155f);
            panelRect.sizeDelta =
                new Vector2(640f, DefaultSingleLineHeight);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.58f);
            panelImage.raycastTarget = false;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            GameObject textObject = new GameObject(
                "PromptText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 30f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            InteractionPromptUI promptUI =
                panelObject.AddComponent<InteractionPromptUI>();
            promptUI.canvasGroup = group;
            promptUI.promptText = text;
            return promptUI;
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyVisible(false, true);
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Bind(Chapter1InteractionController controller, PlayerInputLock lockReference)
        {
            Unbind();
            interactionController = controller;
            inputLock = lockReference;

            if (interactionController != null)
            {
                interactionController.PromptChanged += SetPrompt;
                SetPrompt(interactionController.CurrentPrompt);
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged += HandleLockStateChanged;
            }

            RefreshVisibility();
        }

        public void SetPrompt(string prompt)
        {
            currentPrompt = prompt ?? string.Empty;
            if (promptText != null)
            {
                promptText.text = currentPrompt;
            }

            RefreshPromptHeight();
            RefreshVisibility();
        }

        private void Unbind()
        {
            if (interactionController != null)
            {
                interactionController.PromptChanged -= SetPrompt;
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged -= HandleLockStateChanged;
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (promptText == null)
            {
                promptText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (!layoutInitialized)
            {
                promptRect = transform as RectTransform;
                if (promptRect != null)
                {
                    singleLineHeight = Mathf.Max(
                        DefaultSingleLineHeight,
                        promptRect.sizeDelta.y);
                }
                else
                {
                    singleLineHeight = DefaultSingleLineHeight;
                }

                layoutInitialized = true;
            }
        }

        private void RefreshPromptHeight()
        {
            ResolveReferences();
            if (promptRect == null)
            {
                return;
            }

            int lineCount = 1;
            for (int i = 0; i < currentPrompt.Length; i++)
            {
                if (currentPrompt[i] == '\n')
                {
                    lineCount++;
                }
            }

            Vector2 size = promptRect.sizeDelta;
            size.y = singleLineHeight +
                     Mathf.Max(0, lineCount - 1) *
                     AdditionalLineHeight;
            promptRect.sizeDelta = size;
        }

        private void HandleLockStateChanged(bool locked)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool visible = !string.IsNullOrWhiteSpace(currentPrompt) && (inputLock == null || !inputLock.IsLocked);
            ApplyVisible(visible, false);
        }

        private void ApplyVisible(bool visible, bool immediate)
        {
            ResolveReferences();
            if (canvasGroup == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (immediate || fadeDuration <= 0f)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = false;
                return;
            }

            fadeRoutine = StartCoroutine(FadeTo(visible ? 1f : 0f));
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = targetAlpha > 0.5f;
            canvasGroup.blocksRaycasts = false;
            fadeRoutine = null;
        }
    }
}
