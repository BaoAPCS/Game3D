using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    public static class BackpackPhoneRuntimeUIFactory
    {
        public static InventoryUIController EnsureInventoryUI(
            Canvas canvas,
            InventoryController inventory,
            PhoneUIController phone,
            PlayerInputLock inputLock)
        {
            InventoryUIController existing = Object.FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(inventory, phone, inputLock);
                return existing;
            }

            GameObject root = CreatePanelRoot(canvas.transform, "InventoryPanel");
            root.AddComponent<AudioSource>().playOnAwake = false;

            RectTransform main = CreateImage(root.transform, "MainPanel", new Color(0.06f, 0.06f, 0.065f, 0.98f)).rectTransform;
            SetCentered(main, new Vector2(1180f, 720f));

            CreateText(main, "Title", "BALO", 46f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(420f, 72f));
            CreateButton(main, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -42f), new Vector2(54f, 54f));

            RectTransform grid = CreateEmpty(main, "SlotGrid");
            SetRect(grid, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -140f), new Vector2(520f, 430f), new Vector2(0f, 1f));
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(112f, 112f);
            layout.spacing = new Vector2(18f, 18f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;

            for (int i = 0; i < 12; i++)
            {
                CreateSlot(grid, $"InventorySlot_{i + 1:00}");
            }

            RectTransform detail = CreateImage(main, "DetailPanel", new Color(0.095f, 0.095f, 0.105f, 0.96f)).rectTransform;
            SetRect(detail, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330f, -12f), new Vector2(430f, 520f), new Vector2(0.5f, 0.5f));
            CreateImage(detail, "DetailIcon", new Color(1f, 1f, 1f, 1f), false).rectTransform.sizeDelta = new Vector2(156f, 156f);
            RectTransform detailIcon = detail.Find("DetailIcon").GetComponent<RectTransform>();
            SetRect(detailIcon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(156f, 156f), new Vector2(0.5f, 0.5f));
            CreateText(detail, "DetailName", string.Empty, 34f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(360f, 52f));
            CreateText(detail, "DetailDescription", string.Empty, 22f, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(342f, 150f));
            CreateText(detail, "DetailQuantity", string.Empty, 20f, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(260f, 34f));
            CreateButton(detail, "UseButton", "S\u1eec D\u1ee4NG", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(260f, 62f));

            InventoryUIController controller = root.AddComponent<InventoryUIController>();
            controller.Configure(inventory, phone, inputLock);
            root.SetActive(false);
            return controller;
        }

        public static PhoneUIController EnsurePhoneUI(Canvas canvas, PlayerInputLock inputLock)
        {
            PhoneUIController existing = Object.FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(inputLock);
                return existing;
            }

            GameObject root = CreatePanelRoot(canvas.transform, "PhonePanel");
            root.AddComponent<AudioSource>().playOnAwake = false;

            RectTransform frame = CreateImage(root.transform, "PhoneFrame", new Color(0.025f, 0.025f, 0.03f, 0.99f)).rectTransform;
            SetCentered(frame, new Vector2(460f, 780f));
            CreateText(frame, "Title", "\u0110I\u1ec6N THO\u1ea0I", 36f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(360f, 56f));
            CreateButton(frame, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -38f), new Vector2(48f, 48f));

            CreatePhoneHomeScreen(frame);
            RectTransform appContent = CreatePhoneAppContent(frame);
            appContent.gameObject.SetActive(false);

            PhoneUIController controller = root.AddComponent<PhoneUIController>();
            controller.Configure(inputLock);
            root.SetActive(false);
            return controller;
        }

        private static GameObject CreatePanelRoot(Transform parent, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image overlay = root.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.62f);
            return root;
        }

        private static InventorySlotUI CreateSlot(Transform parent, string name)
        {
            GameObject slot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InventorySlotUI));
            slot.transform.SetParent(parent, false);
            Image background = slot.GetComponent<Image>();
            background.color = new Color(0.18f, 0.18f, 0.19f, 0.92f);
            Button button = slot.GetComponent<Button>();
            button.targetGraphic = background;

            RectTransform highlight = CreateImage(slot.transform, "SelectedHighlight", new Color(0.45f, 0.03f, 0.03f, 0.78f)).rectTransform;
            highlight.anchorMin = Vector2.zero;
            highlight.anchorMax = Vector2.one;
            highlight.offsetMin = Vector2.zero;
            highlight.offsetMax = Vector2.zero;
            highlight.GetComponent<Image>().enabled = false;

            RectTransform icon = CreateImage(slot.transform, "Icon", Color.white, false).rectTransform;
            SetRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78f, 78f), new Vector2(0.5f, 0.5f));
            CreateText(slot.transform, "Quantity", string.Empty, 20f, TextAlignmentOptions.BottomRight, Vector2.zero, Vector2.one, new Vector2(-8f, 6f), new Vector2(-16f, -12f));
            return slot.GetComponent<InventorySlotUI>();
        }

        private static void CreateTabButton(RectTransform parent, string buttonName, string label, string highlightName)
        {
            Button button = CreateButton(parent, buttonName, label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform highlight = CreateImage(button.transform, highlightName, new Color(0.46f, 0.04f, 0.04f, 0.85f)).rectTransform;
            highlight.anchorMin = Vector2.zero;
            highlight.anchorMax = Vector2.one;
            highlight.offsetMin = Vector2.zero;
            highlight.offsetMax = Vector2.zero;
            highlight.SetAsFirstSibling();
            highlight.GetComponent<Image>().enabled = false;
        }

        private static void CreatePhoneHomeScreen(RectTransform parent)
        {
            RectTransform screen = CreateEmpty(parent, "HomeScreen");
            Stretch(screen, new Vector2(42f, 68f), new Vector2(-42f, -126f));

            CreateText(
                screen,
                "HomeTitle",
                "Home",
                22f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(260f, 34f));

            RectTransform grid = CreateEmpty(screen, "AppGrid");
            SetRect(
                grid,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(332f, 346f),
                new Vector2(0.5f, 0.5f));

            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(146f, 146f);
            layout.spacing = new Vector2(34f, 34f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;

            CreateAppButton(grid, "MessengerButton", "Messenger", "M", new Color(0.05f, 0.48f, 0.95f, 1f));
            CreateAppButton(grid, "RecorderButton", "Ghi \u00e2m", "REC", new Color(0.82f, 0.12f, 0.17f, 1f));
            CreateAppButton(grid, "CameraButton", "Camera", "CAM", new Color(0.13f, 0.62f, 0.37f, 1f));
            CreateAppButton(grid, "GoogleButton", "Google", "G", new Color(0.96f, 0.72f, 0.12f, 1f));
        }

        private static RectTransform CreatePhoneAppContent(RectTransform parent)
        {
            RectTransform content = CreateImage(parent, "AppContent", new Color(0.075f, 0.077f, 0.083f, 0.98f)).rectTransform;
            SetRect(
                content,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 314f),
                new Vector2(370f, 520f),
                new Vector2(0.5f, 0.5f));

            CreateButton(
                content,
                "HomeButton",
                "<",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -36f),
                new Vector2(54f, 48f));

            CreateText(
                content,
                "AppTitleText",
                string.Empty,
                28f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(18f, -38f),
                new Vector2(250f, 44f));

            RectTransform bodyPanel = CreateImage(content, "AppBodyPanel", new Color(0.11f, 0.115f, 0.125f, 0.98f)).rectTransform;
            Stretch(bodyPanel, new Vector2(26f, 28f), new Vector2(-26f, -94f));
            CreateText(
                bodyPanel,
                "AppBodyText",
                string.Empty,
                24f,
                TextAlignmentOptions.TopLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, -2f),
                new Vector2(-34f, -28f));

            return content;
        }

        private static Button CreateAppButton(Transform parent, string name, string label, string glyph, Color iconColor)
        {
            Button button = CreateButton(parent, name, string.Empty, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.13f, 0.135f, 0.15f, 0.95f);
            }

            RectTransform icon = CreateImage(button.transform, "Icon", iconColor).rectTransform;
            SetRect(
                icon,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -48f),
                new Vector2(72f, 72f),
                new Vector2(0.5f, 0.5f));

            TextMeshProUGUI glyphText = CreateText(
                icon,
                "Glyph",
                glyph,
                glyph.Length > 1 ? 22f : 34f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            glyphText.color = Color.white;

            CreateText(
                button.transform,
                "Label",
                label,
                18f,
                TextAlignmentOptions.Center,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 24f),
                new Vector2(0f, 36f));

            return button;
        }

        private static void CreatePhoneContent(RectTransform parent, string objectName, string textName, string text)
        {
            RectTransform content = CreateEmpty(parent, objectName);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(26f, 26f);
            content.offsetMax = new Vector2(-26f, -26f);
            CreateText(content, textName, text, 24f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, size, new Vector2(0.5f, 0.5f));
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.22f, 0.22f, 0.23f, 0.96f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                CreateText(buttonObject.transform, "Label", label, 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 rectSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, rectSize, new Vector2(0.5f, 0.5f));
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = new Color(0.91f, 0.88f, 0.8f, 1f);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static Image CreateImage(Transform parent, string name, Color color, bool enabled = true)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.enabled = enabled;
            return image;
        }

        private static RectTransform CreateEmpty(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void SetCentered(RectTransform rect, Vector2 size)
        {
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
