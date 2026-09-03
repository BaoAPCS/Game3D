using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2RouterInspectionUI : MonoBehaviour
    {
        private const string RuntimeObjectName =
            "Chapter2_Mission05_RouterInspectionUI";

        [SerializeField] private Canvas canvas;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI promptText;

        public bool IsVisible => canvas != null && canvas.enabled;

        public static Chapter2RouterInspectionUI Create(
            Transform owner)
        {
            GameObject root = new GameObject(
                RuntimeObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            if (owner != null)
            {
                root.transform.SetParent(owner, false);
            }

            Canvas rootCanvas = root.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 560;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject titlePanel = CreatePanel(
                root.transform,
                "RouterTitlePanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -54f),
                new Vector2(560f, 64f),
                new Color(0.015f, 0.055f, 0.08f, 0.86f));
            TextMeshProUGUI title = CreateText(
                titlePanel.transform,
                "RouterTitle",
                "THIẾT BỊ WI-FI — TÀI LIỆU BỊ CHE GIẤU",
                27f,
                new Color(0.37f, 0.9f, 1f, 1f));

            GameObject promptPanel = CreatePanel(
                root.transform,
                "RouterPromptPanel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 58f),
                new Vector2(820f, 76f),
                new Color(0.01f, 0.035f, 0.055f, 0.9f));
            TextMeshProUGUI prompt = CreateText(
                promptPanel.transform,
                "RouterInspectionPrompt",
                "[E] Lấy tài liệu mật     [ESC] Quay lại",
                29f,
                Color.white);

            Chapter2RouterInspectionUI ui =
                root.AddComponent<Chapter2RouterInspectionUI>();
            ui.canvas = rootCanvas;
            ui.titleText = title;
            ui.promptText = prompt;
            ui.Hide();
            return ui;
        }

        public void Show(bool documentAvailable)
        {
            if (titleText != null)
            {
                titleText.text = documentAvailable
                    ? "THIẾT BỊ WI-FI — PHÁT HIỆN TÀI LIỆU BỊ CHE GIẤU"
                    : "THIẾT BỊ WI-FI — ĐÃ KIỂM TRA";
            }

            if (promptText != null)
            {
                promptText.text = documentAvailable
                    ? "[E] Lấy tài liệu mật     [ESC] Quay lại"
                    : "[ESC] Quay lại";
            }

            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        public void Hide()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private static GameObject CreatePanel(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject panel = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            string value,
            float fontSize,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}
