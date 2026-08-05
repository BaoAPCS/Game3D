using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class Mission01KeypadUIController : MonoBehaviour
    {
        private const string InputLockReason = "Mission01Keypad";
        private const int MaxDigits = 4;

        [SerializeField] private string correctPassword = Mission01AudioSeparatorManager.CorrectDoorPassword;
        [SerializeField] private Canvas canvas;
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private TextMeshProUGUI statusText;

        private Mission01DungDoorInteractable activeDoor;
        private PlayerInputLock activeInputLock;
        private string currentInput = string.Empty;
        private bool isOpen;

        public string CorrectPassword => correctPassword;
        public bool IsOpen => isOpen;
        public string CurrentInput => currentInput;

        private void Awake()
        {
            EnsureUi();
            SetVisible(false);
        }

        private void Update()
        {
            if (!isOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public InteractionResult Open(Mission01DungDoorInteractable door, InteractionContext context)
        {
            if (isOpen)
            {
                return InteractionResult.Ignored();
            }

            if (door == null)
            {
                return InteractionResult.Failed("Không xác định được cửa phòng Dũng.");
            }

            EnsureUi();
            activeDoor = door;
            activeInputLock = context.PlayerObject != null ? context.PlayerObject.GetComponent<PlayerInputLock>() : null;
            activeInputLock?.Lock(InputLockReason);
            currentInput = string.Empty;
            RefreshDisplay();
            SetStatus(string.Empty);
            SetVisible(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isOpen = true;
            return InteractionResult.Succeeded();
        }

        public void Close()
        {
            if (!isOpen)
            {
                SetVisible(false);
                return;
            }

            SetVisible(false);
            activeInputLock?.Unlock(InputLockReason);
            activeInputLock = null;
            activeDoor = null;
            currentInput = string.Empty;
            isOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void AppendDigit(string digit)
        {
            if (!isOpen || string.IsNullOrEmpty(digit) || currentInput.Length >= MaxDigits)
            {
                return;
            }

            char character = digit[0];
            if (character < '0' || character > '9')
            {
                return;
            }

            currentInput += character;
            SetStatus(string.Empty);
            RefreshDisplay();
        }

        public void Backspace()
        {
            if (!isOpen || currentInput.Length == 0)
            {
                return;
            }

            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            RefreshDisplay();
        }

        public void ClearInput()
        {
            currentInput = string.Empty;
            RefreshDisplay();
            SetStatus(string.Empty);
        }

        public void Submit()
        {
            if (!isOpen || activeDoor == null)
            {
                return;
            }

            if (!string.Equals(currentInput, correctPassword, System.StringComparison.Ordinal))
            {
                SetStatus("Mật khẩu không đúng.");
                currentInput = string.Empty;
                RefreshDisplay();
                return;
            }

            Mission01DungDoorInteractable door = activeDoor;
            Close();
            door.SubmitPassword(correctPassword);
            Chapter1EventBus.RaiseNotification("Khóa cửa đã mở.");
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            EnsureEventSystem();

            GameObject canvasObject = new GameObject("Mission01_KeypadCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreatePanel(canvasObject.transform);
            displayText = CreateText(panel, "Display", "----", 44f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(displayText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(260f, 64f), new Vector2(0.5f, 0.5f));

            statusText = CreateText(panel, "Status", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Center);
            statusText.color = new Color(1f, 0.42f, 0.36f, 1f);
            SetRect(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(300f, 36f), new Vector2(0.5f, 0.5f));

            RectTransform grid = CreateGrid(panel);
            string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "C" };
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                Button button = CreateButton(grid, label);
                if (label == "<")
                {
                    button.onClick.AddListener(Backspace);
                }
                else if (label == "C")
                {
                    button.onClick.AddListener(ClearInput);
                }
                else
                {
                    string digit = label;
                    button.onClick.AddListener(() => AppendDigit(digit));
                }
            }

            RectTransform footer = CreateHorizontal(panel);
            Button confirm = CreateButton(footer, "OK");
            confirm.onClick.AddListener(Submit);
            Button close = CreateButton(footer, "Đóng");
            close.onClick.AddListener(Close);
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("KeypadPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 560f), new Vector2(0.5f, 0.5f));
            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.045f, 0.047f, 0.055f, 0.98f);
            return rect;
        }

        private static RectTransform CreateGrid(Transform parent)
        {
            GameObject gridObject = new GameObject("DigitGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(parent, false);
            RectTransform rect = gridObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(282f, 300f), new Vector2(0.5f, 0.5f));
            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(82f, 64f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            return rect;
        }

        private static RectTransform CreateHorizontal(Transform parent)
        {
            GameObject rowObject = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(parent, false);
            RectTransform rect = rowObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(282f, 56f), new Vector2(0.5f, 0.5f));
            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            return rect;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            GameObject buttonObject = new GameObject("Button_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.17f, 0.19f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private void RefreshDisplay()
        {
            if (displayText == null)
            {
                return;
            }

            displayText.text = string.IsNullOrEmpty(currentInput) ? "----" : currentInput.PadRight(MaxDigits, '-');
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text ?? string.Empty;
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.pivot = pivot;
        }
    }
}
