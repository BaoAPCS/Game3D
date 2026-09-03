using System;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2ConfiscatedItemsUI : MonoBehaviour
    {
        private static readonly Color Cyan =
            new Color(0.13f, 0.78f, 1f, 1f);
        private static readonly Color PaleCyan =
            new Color(0.69f, 0.90f, 1f, 1f);
        private static readonly Color Green =
            new Color(0.25f, 0.91f, 0.52f, 1f);
        private static readonly Color Muted =
            new Color(0.55f, 0.66f, 0.72f, 1f);

        private sealed class ItemRow
        {
            public Image Background;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Description;
            public TextMeshProUGUI Status;
            public Button ReceiveButton;
            public TextMeshProUGUI ButtonLabel;
        }

        private ItemDefinition phoneDefinition;
        private ItemDefinition keyDefinition;
        private Action receivePhoneRequested;
        private Action receiveKeyRequested;
        private Action closeRequested;
        private ItemRow phoneRow;
        private ItemRow keyRow;
        private TextMeshProUGUI summaryText;

        public bool IsVisible => gameObject.activeSelf;

        public static Chapter2ConfiscatedItemsUI Create(
            Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "Chapter2_Mission03_ConfiscatedItemsUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.SetActive(false);
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent, false);
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 730;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect, Vector2.zero, Vector2.zero);

            Chapter2ConfiscatedItemsUI ui =
                canvasObject.AddComponent<
                    Chapter2ConfiscatedItemsUI>();
            ui.Build();
            ui.Refresh(false, false);
            return ui;
        }

        public void Configure(
            ItemDefinition phone,
            ItemDefinition key,
            Action receivePhone,
            Action receiveKey,
            Action close)
        {
            phoneDefinition = phone;
            keyDefinition = key;
            receivePhoneRequested = receivePhone;
            receiveKeyRequested = receiveKey;
            closeRequested = close;

            ApplyDefinition(phoneRow, phoneDefinition, "Điện thoại");
            ApplyDefinition(keyRow, keyDefinition, "Chìa khóa của James");
        }

        public void Refresh(
            bool phoneRecovered,
            bool keyRecovered)
        {
            ApplyRecoveryState(phoneRow, phoneRecovered);
            ApplyRecoveryState(keyRow, keyRecovered);

            if (summaryText == null)
            {
                return;
            }

            int recoveredCount =
                (phoneRecovered ? 1 : 0) +
                (keyRecovered ? 1 : 0);
            summaryText.text = recoveredCount == 2
                ? "ĐÃ THU HỒI TOÀN BỘ TÀI SẢN"
                : $"ĐÃ THU HỒI {recoveredCount}/2 VẬT PHẨM";
            summaryText.color = recoveredCount == 2
                ? Green
                : PaleCyan;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Build()
        {
            RectTransform backdrop = CreateImage(
                transform,
                "Backdrop",
                new Color(0f, 0.025f, 0.05f, 0.90f),
                true);
            Stretch(backdrop, Vector2.zero, Vector2.zero);

            RectTransform frame = CreateImage(
                transform,
                "EvidenceLockerPanel",
                new Color(0.025f, 0.075f, 0.115f, 0.99f),
                true);
            SetRect(
                frame,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1120f, 760f),
                new Vector2(0.5f, 0.5f));

            RectTransform outerBorder = CreateImage(
                frame,
                "OuterBorder",
                new Color(0.05f, 0.40f, 0.58f, 1f));
            Stretch(
                outerBorder,
                new Vector2(-3f, -3f),
                new Vector2(3f, 3f));
            outerBorder.SetAsFirstSibling();

            RectTransform innerPanel = CreateImage(
                frame,
                "InnerPanel",
                new Color(0.025f, 0.075f, 0.115f, 1f));
            Stretch(
                innerPanel,
                new Vector2(3f, 3f),
                new Vector2(-3f, -3f));

            TextMeshProUGUI chapterLabel = CreateText(
                innerPanel,
                "ChapterLabel",
                "CHƯƠNG 2  •  NHIỆM VỤ 3",
                20f,
                TextAlignmentOptions.Center);
            SetRect(
                chapterLabel.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -35f),
                new Vector2(620f, 32f),
                new Vector2(0.5f, 1f));
            chapterLabel.color = Cyan;
            chapterLabel.fontStyle = FontStyles.Bold;

            TextMeshProUGUI title = CreateText(
                innerPanel,
                "Title",
                "DANH SÁCH TÀI SẢN TỊCH THU",
                38f,
                TextAlignmentOptions.Center);
            SetRect(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -76f),
                new Vector2(850f, 58f),
                new Vector2(0.5f, 1f));
            title.color = PaleCyan;
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI subtitle = CreateText(
                innerPanel,
                "Subtitle",
                "TỦ LƯU GIỮ VẬT CHỨNG  •  HỒ SƠ CỦA NAM",
                17f,
                TextAlignmentOptions.Center);
            SetRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -137f),
                new Vector2(680f, 30f),
                new Vector2(0.5f, 1f));
            subtitle.color = Muted;

            Button closeButton = CreateButton(
                innerPanel,
                "CloseButton",
                "×",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(56f, 56f),
                new Color(0.10f, 0.24f, 0.32f, 1f),
                new Vector2(1f, 1f),
                out _);
            closeButton.onClick.AddListener(
                () => closeRequested?.Invoke());

            phoneRow = CreateItemRow(
                innerPanel,
                "PhoneRow",
                -195f,
                () => receivePhoneRequested?.Invoke());
            keyRow = CreateItemRow(
                innerPanel,
                "JamesKeyRow",
                -405f,
                () => receiveKeyRequested?.Invoke());

            summaryText = CreateText(
                innerPanel,
                "RecoverySummary",
                string.Empty,
                18f,
                TextAlignmentOptions.Center);
            SetRect(
                summaryText.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 83f),
                new Vector2(520f, 32f),
                new Vector2(0.5f, 0f));
            summaryText.fontStyle = FontStyles.Bold;

            RectTransform footer = CreateImage(
                innerPanel,
                "Footer",
                new Color(0.015f, 0.045f, 0.07f, 0.98f));
            SetRect(
                footer,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 0f),
                new Vector2(1114f, 62f),
                new Vector2(0.5f, 0f));

            TextMeshProUGUI footerText = CreateText(
                footer,
                "Controls",
                "[LMB] Chọn vật phẩm    [ESC] Đóng",
                19f,
                TextAlignmentOptions.Center);
            Stretch(
                footerText.rectTransform,
                new Vector2(20f, 8f),
                new Vector2(-20f, -8f));
            footerText.color = new Color(0.80f, 0.90f, 0.96f, 1f);
        }

        private static ItemRow CreateItemRow(
            Transform parent,
            string name,
            float anchoredY,
            Action receive)
        {
            RectTransform rowRect = CreateImage(
                parent,
                name,
                new Color(0.045f, 0.115f, 0.16f, 0.99f),
                true);
            SetRect(
                rowRect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, anchoredY),
                new Vector2(960f, 178f),
                new Vector2(0.5f, 1f));

            RectTransform accent = CreateImage(
                rowRect,
                "Accent",
                Cyan);
            SetRect(
                accent,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(5f, 0f),
                new Vector2(0f, 0.5f));

            RectTransform iconFrame = CreateImage(
                rowRect,
                "IconFrame",
                new Color(0.02f, 0.055f, 0.08f, 1f));
            SetRect(
                iconFrame,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(86f, 0f),
                new Vector2(126f, 126f),
                new Vector2(0.5f, 0.5f));

            RectTransform iconRect = CreateImage(
                iconFrame,
                "ItemIcon",
                Color.white);
            Stretch(
                iconRect,
                new Vector2(13f, 13f),
                new Vector2(-13f, -13f));
            Image icon = iconRect.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;

            TextMeshProUGUI itemName = CreateText(
                rowRect,
                "ItemName",
                "Vật phẩm",
                29f,
                TextAlignmentOptions.Left);
            SetRect(
                itemName.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(176f, -28f),
                new Vector2(410f, 44f),
                new Vector2(0f, 1f));
            itemName.color = PaleCyan;
            itemName.fontStyle = FontStyles.Bold;

            TextMeshProUGUI description = CreateText(
                rowRect,
                "Description",
                string.Empty,
                17f,
                TextAlignmentOptions.TopLeft);
            SetRect(
                description.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(176f, -74f),
                new Vector2(430f, 72f),
                new Vector2(0f, 1f));
            description.color = new Color(0.70f, 0.78f, 0.82f, 1f);
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI status = CreateText(
                rowRect,
                "Status",
                "ĐANG BỊ TỊCH THU",
                16f,
                TextAlignmentOptions.Center);
            SetRect(
                status.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-173f, -31f),
                new Vector2(260f, 34f),
                new Vector2(0.5f, 1f));
            status.color = Muted;
            status.fontStyle = FontStyles.Bold;

            Button receiveButton = CreateButton(
                rowRect,
                "ReceiveButton",
                "NHẬN LẠI",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-42f, -25f),
                new Vector2(260f, 64f),
                new Color(0.04f, 0.38f, 0.53f, 1f),
                new Vector2(1f, 0.5f),
                out TextMeshProUGUI buttonLabel);
            receiveButton.onClick.AddListener(
                () => receive?.Invoke());

            return new ItemRow
            {
                Background = rowRect.GetComponent<Image>(),
                Icon = icon,
                Name = itemName,
                Description = description,
                Status = status,
                ReceiveButton = receiveButton,
                ButtonLabel = buttonLabel
            };
        }

        private static void ApplyDefinition(
            ItemRow row,
            ItemDefinition definition,
            string fallbackName)
        {
            if (row == null)
            {
                return;
            }

            bool hasDefinition = definition != null;
            row.Name.text = hasDefinition &&
                            !string.IsNullOrWhiteSpace(
                                definition.DisplayName)
                ? definition.DisplayName
                : fallbackName;
            row.Description.text = hasDefinition
                ? definition.Description ?? string.Empty
                : string.Empty;
            row.Icon.sprite = hasDefinition
                ? definition.Icon
                : null;
            row.Icon.enabled = row.Icon.sprite != null;
        }

        private static void ApplyRecoveryState(
            ItemRow row,
            bool recovered)
        {
            if (row == null)
            {
                return;
            }

            row.Background.color = recovered
                ? new Color(0.035f, 0.20f, 0.15f, 0.99f)
                : new Color(0.045f, 0.115f, 0.16f, 0.99f);
            row.Status.text = recovered
                ? "ĐÃ NHẬN LẠI"
                : "ĐANG BỊ TỊCH THU";
            row.Status.color = recovered ? Green : Muted;
            row.ReceiveButton.interactable = !recovered;
            row.ButtonLabel.text = recovered
                ? "ĐÃ NHẬN LẠI"
                : "NHẬN LẠI";
        }

        private static RectTransform CreateImage(
            Transform parent,
            string name,
            Color color,
            bool raycastTarget = false)
        {
            GameObject target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return target.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Color color,
            Vector2 pivot,
            out TextMeshProUGUI buttonLabel)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            SetRect(
                rect,
                anchorMin,
                anchorMax,
                position,
                size,
                pivot);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(0.72f, 0.94f, 1f, 1f);
            colors.pressedColor =
                new Color(0.48f, 0.80f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor =
                new Color(0.35f, 0.46f, 0.50f, 0.75f);
            button.colors = colors;

            buttonLabel = CreateText(
                buttonObject.transform,
                "Label",
                label,
                18f,
                TextAlignmentOptions.Center);
            Stretch(
                buttonLabel.rectTransform,
                new Vector2(8f, 4f),
                new Vector2(-8f, -4f));
            buttonLabel.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
