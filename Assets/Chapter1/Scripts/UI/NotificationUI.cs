using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    public sealed class NotificationUI : MonoBehaviour
    {
        private const string RuntimeCanvasName =
            "Chapter1Notification_Runtime";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float displaySeconds = 2f;
        [SerializeField] private float fadeDuration = 0.2f;

        private readonly Queue<string> messageQueue = new Queue<string>();
        private Coroutine queueRoutine;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureNotificationAfterInitialSceneLoad()
        {
            EnsureRuntimeNotification();
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            EnsureRuntimeNotification();
        }

        private static void EnsureRuntimeNotification()
        {
            if (FindAnyObjectByType<Chapter1InteractionController>() == null)
            {
                return;
            }

            NotificationUI notificationUI =
                FindAnyObjectByType<NotificationUI>(
                    FindObjectsInactive.Include);
            if (notificationUI == null)
            {
                CreateRuntimeNotification();
            }
        }

        private static NotificationUI CreateRuntimeNotification()
        {
            GameObject canvasObject = new GameObject(
                RuntimeCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject(
                "Notification",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -80f);
            panelRect.sizeDelta = new Vector2(900f, 72f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.62f);
            panelImage.raycastTarget = false;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            GameObject textObject = new GameObject(
                "NotificationText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 8f);
            textRect.offsetMax = new Vector2(-20f, -8f);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 27f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            NotificationUI notificationUI =
                panelObject.AddComponent<NotificationUI>();
            notificationUI.canvasGroup = group;
            notificationUI.notificationText = text;
            return notificationUI;
        }

        private void Awake()
        {
            ResolveReferences();
            SetVisible(false);
        }

        private void OnEnable()
        {
            Chapter1EventBus.NotificationRequested += ShowMessage;
        }

        private void OnDisable()
        {
            Chapter1EventBus.NotificationRequested -= ShowMessage;
            if (queueRoutine != null)
            {
                StopCoroutine(queueRoutine);
                queueRoutine = null;
            }

            messageQueue.Clear();
            SetVisible(false);
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            messageQueue.Enqueue(message);
            if (queueRoutine == null && isActiveAndEnabled)
            {
                queueRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (notificationText == null)
            {
                notificationText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                if (notificationText != null)
                {
                    notificationText.text = message;
                }

                yield return FadeTo(1f);
                yield return new WaitForSecondsRealtime(displaySeconds);
                yield return FadeTo(0f);
            }

            queueRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            ResolveReferences();
            if (canvasGroup == null)
            {
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
