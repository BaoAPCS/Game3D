using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter3
{
    [DisallowMultipleComponent]
    public sealed class Chapter3WoodenObstacleHUD : MonoBehaviour
    {
        private GameObject promptRoot;
        private TextMeshProUGUI promptText;
        private GameObject progressRoot;
        private RectTransform progressFill;
        private TextMeshProUGUI progressLabel;

        public static Chapter3WoodenObstacleHUD Create(
            Transform parent)
        {
            Chapter3WoodenObstacleHUD existing =
                parent != null
                    ? parent.GetComponentInChildren<
                        Chapter3WoodenObstacleHUD>(true)
                    : null;
            if (existing != null)
            {
                return existing;
            }

            GameObject canvasObject = new GameObject(
                "Chapter3WoodenObstacleHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent, false);
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 270;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Chapter3WoodenObstacleHUD hud =
                canvasObject.AddComponent<
                    Chapter3WoodenObstacleHUD>();
            hud.Build(canvasObject.transform);
            hud.HideAll();
            return hud;
        }

        public void SetPrompt(string prompt)
        {
            string value = prompt ?? string.Empty;
            bool visible = !string.IsNullOrWhiteSpace(value);
            if (promptText != null)
            {
                promptText.text = value;
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(visible);
            }
        }

        public void SetProgress(float normalizedProgress)
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
                    $"GỠ VẬT CẢN  {Mathf.RoundToInt(value * 100f)}%";
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

        private void Build(Transform canvasTransform)
        {
            promptRoot = CreateImageObject(
                canvasTransform,
                "ObstaclePrompt",
                new Color(0f, 0f, 0f, 0.7f));
            SetBottomCenter(
                promptRoot.GetComponent<RectTransform>(),
                new Vector2(720f, 72f),
                new Vector2(0f, 155f));

            promptText = CreateText(
                promptRoot.transform,
                "PromptText",
                30f);
            Stretch(promptText.rectTransform, 18f, 8f);

            progressRoot = CreateImageObject(
                canvasTransform,
                "ObstacleProgress",
                new Color(0.035f, 0.035f, 0.045f, 0.94f));
            SetBottomCenter(
                progressRoot.GetComponent<RectTransform>(),
                new Vector2(720f, 126f),
                new Vector2(0f, 250f));

            progressLabel = CreateText(
                progressRoot.transform,
                "ProgressLabel",
                25f);
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
            float fontSize)
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
            text.alignment = TextAlignmentOptions.Center;
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
