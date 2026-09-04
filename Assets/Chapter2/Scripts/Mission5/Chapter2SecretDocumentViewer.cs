using System;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2SecretDocumentViewer : MonoBehaviour
    {
        public const string RuntimeObjectName =
            "Chapter2_SecretDocumentViewer";
        public const string InputLockReason =
            "Chapter2.SecretDocumentViewer";

        private CanvasGroup canvasGroup;
        private Image documentImage;
        private Button closeButton;
        private PlayerInputLock inputLock;
        private bool visible;

        public event Action Closed;

        public bool IsVisible => visible;
        public Sprite DisplayedSprite => documentImage != null
            ? documentImage.sprite
            : null;

        public static Chapter2SecretDocumentViewer Create(
            Transform parent,
            PlayerInputLock playerInputLock)
        {
            GameObject owner = new GameObject(
                RuntimeObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            if (parent != null)
            {
                owner.transform.SetParent(parent, false);
            }

            Canvas canvas = owner.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            CanvasScaler scaler = owner.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = CreateImage(
                owner.transform,
                "Backdrop",
                new Color(0.015f, 0.025f, 0.04f, 0.94f));
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);

            GameObject panelObject = new GameObject(
                "DocumentPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(owner.transform, false);
            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 990f);
            panelObject.GetComponent<Image>().color =
                new Color(0.035f, 0.055f, 0.075f, 1f);

            TextMeshProUGUI title = CreateText(
                panelRect,
                "Title",
                "TÀI LIỆU MẬT",
                30f,
                TextAlignmentOptions.Center);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(-120f, 52f);
            title.color = new Color(0.75f, 0.91f, 1f, 1f);

            Image paper = CreateImage(
                panelRect,
                "SecretFileImage",
                Color.white);
            RectTransform paperRect = paper.rectTransform;
            paperRect.anchorMin = new Vector2(0.5f, 0.5f);
            paperRect.anchorMax = new Vector2(0.5f, 0.5f);
            paperRect.pivot = new Vector2(0.5f, 0.5f);
            paperRect.anchoredPosition = new Vector2(0f, -15f);
            paperRect.sizeDelta = new Vector2(650f, 867f);
            paper.preserveAspect = true;
            paper.raycastTarget = false;

            Button close = CreateCloseButton(panelRect);

            TextMeshProUGUI hint = CreateText(
                owner.transform,
                "CloseHint",
                "[ESC] Đóng tài liệu",
                22f,
                TextAlignmentOptions.Center);
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 18f);
            hintRect.sizeDelta = new Vector2(420f, 44f);
            hint.color = new Color(0.74f, 0.82f, 0.88f, 1f);

            Chapter2SecretDocumentViewer viewer =
                owner.AddComponent<Chapter2SecretDocumentViewer>();
            viewer.canvasGroup = owner.GetComponent<CanvasGroup>();
            viewer.documentImage = paper;
            viewer.closeButton = close;
            viewer.inputLock = playerInputLock;
            viewer.closeButton.onClick.AddListener(viewer.Close);
            viewer.SetVisible(false);
            return viewer;
        }

        private void Update()
        {
            if (!visible)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void OnDisable()
        {
            if (visible)
            {
                Close(false);
            }
        }

        private void OnDestroy()
        {
            inputLock?.ReleaseInputLock(InputLockReason);
        }

        public void Show(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogError(
                    "[Chapter2Ending] Thiếu ảnh secret_file.png để hiển thị tài liệu mật.",
                    this);
                return;
            }

            documentImage.sprite = sprite;
            inputLock?.AcquireInputLock(InputLockReason);
            SetVisible(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            Close(true);
        }

        private void Close(bool notify)
        {
            if (!visible)
            {
                return;
            }

            SetVisible(false);
            inputLock?.ReleaseInputLock(InputLockReason);
            Chapter1UICursorLock.ApplyAfterClose(inputLock);
            if (notify)
            {
                Closed?.Invoke();
            }
        }

        private void SetVisible(bool value)
        {
            visible = value;
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = value ? 1f : 0f;
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        private static Image CreateImage(
            Transform parent,
            string objectName,
            Color color)
        {
            GameObject owner = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            owner.transform.SetParent(parent, false);
            Image image = owner.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            string value,
            float size,
            TextAlignmentOptions alignment)
        {
            GameObject owner = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            owner.transform.SetParent(parent, false);
            TextMeshProUGUI text = owner.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateCloseButton(Transform parent)
        {
            Image image = CreateImage(
                parent,
                "CloseButton",
                new Color(0.45f, 0.08f, 0.08f, 0.96f));
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.sizeDelta = new Vector2(52f, 52f);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI label = CreateText(
                rect,
                "Label",
                "×",
                34f,
                TextAlignmentOptions.Center);
            Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
