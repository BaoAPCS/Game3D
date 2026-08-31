using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2ServiceCardMissionUI : MonoBehaviour
    {
        private const float PromptHeight = 72f;

        private GameObject promptRoot;
        private CanvasGroup promptGroup;
        private TextMeshProUGUI promptText;
        private GameObject progressRoot;
        private RectTransform progressFill;
        private TextMeshProUGUI progressLabel;
        private string currentPrompt = string.Empty;

        public string CurrentPrompt => currentPrompt;

        public static Chapter2ServiceCardMissionUI Create(
            Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "Chapter2Mission01HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 260;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Chapter2ServiceCardMissionUI ui =
                canvasObject.AddComponent<
                    Chapter2ServiceCardMissionUI>();
            ui.BuildPrompt(canvasObject.transform);
            ui.BuildProgress(canvasObject.transform);
            ui.HideAll();
            return ui;
        }

        public void SetPrompt(string prompt)
        {
            string nextPrompt = prompt ?? string.Empty;
            currentPrompt = nextPrompt;
            bool visible =
                !string.IsNullOrWhiteSpace(currentPrompt);
            if (promptText != null)
            {
                promptText.text = currentPrompt;
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(visible);
            }

            if (promptGroup != null)
            {
                promptGroup.alpha = visible ? 1f : 0f;
                promptGroup.interactable = false;
                promptGroup.blocksRaycasts = false;
            }
        }

        public void SetPryProgress(float normalizedProgress)
        {
            float value = Mathf.Clamp01(normalizedProgress);
            if (progressRoot != null)
            {
                progressRoot.SetActive(true);
            }

            if (progressFill != null)
            {
                Vector2 anchorMax = progressFill.anchorMax;
                anchorMax.x = value;
                progressFill.anchorMax = anchorMax;
            }

            if (progressLabel != null)
            {
                progressLabel.text =
                    $"CẠY TOILET  {Mathf.RoundToInt(value * 100f)}%";
            }
        }

        public void HideProgress()
        {
            if (progressRoot != null)
            {
                progressRoot.SetActive(false);
            }
        }

        public void HideAll()
        {
            SetPrompt(string.Empty);
            HideProgress();
        }

        private void BuildPrompt(Transform canvasTransform)
        {
            promptRoot = CreateImageObject(
                canvasTransform,
                "MissionPrompt",
                new Color(0f, 0f, 0f, 0.68f));
            RectTransform panelRect =
                promptRoot.GetComponent<RectTransform>();
            SetBottomCenter(
                panelRect,
                new Vector2(760f, PromptHeight),
                new Vector2(0f, 155f));

            promptGroup = promptRoot.AddComponent<CanvasGroup>();
            promptGroup.blocksRaycasts = false;

            promptText = CreateText(
                promptRoot.transform,
                "PromptText",
                30f,
                TextAlignmentOptions.Center);
            Stretch(promptText.rectTransform, 18f, 8f);
        }

        private void BuildProgress(Transform canvasTransform)
        {
            progressRoot = CreateImageObject(
                canvasTransform,
                "PryProgress",
                new Color(0.035f, 0.035f, 0.045f, 0.92f));
            RectTransform rootRect =
                progressRoot.GetComponent<RectTransform>();
            SetBottomCenter(
                rootRect,
                new Vector2(720f, 126f),
                new Vector2(0f, 250f));

            progressLabel = CreateText(
                progressRoot.transform,
                "ProgressLabel",
                25f,
                TextAlignmentOptions.Center);
            RectTransform labelRect = progressLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.offsetMin = new Vector2(20f, -58f);
            labelRect.offsetMax = new Vector2(-20f, -10f);

            GameObject track = CreateImageObject(
                progressRoot.transform,
                "ProgressTrack",
                new Color(0.18f, 0.18f, 0.2f, 1f));
            RectTransform trackRect =
                track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.offsetMin = new Vector2(30f, 25f);
            trackRect.offsetMax = new Vector2(-30f, 59f);

            GameObject fill = CreateImageObject(
                track.transform,
                "ProgressFill",
                new Color(0.82f, 0.14f, 0.08f, 1f));
            progressFill = fill.GetComponent<RectTransform>();
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = Vector2.one;
            progressFill.pivot = new Vector2(0f, 0.5f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
        }

        private static GameObject CreateImageObject(
            Transform parent,
            string objectName,
            Color color)
        {
            GameObject result = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            result.transform.SetParent(parent, false);
            Image image = result.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return result;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void SetBottomCenter(
            RectTransform rect,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(
            RectTransform rect,
            float horizontalInset,
            float verticalInset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(
                horizontalInset,
                verticalInset);
            rect.offsetMax = new Vector2(
                -horizontalInset,
                -verticalInset);
        }
    }
}
