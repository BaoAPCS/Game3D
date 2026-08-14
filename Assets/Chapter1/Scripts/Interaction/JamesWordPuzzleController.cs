using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class JamesWordPuzzleController : MonoBehaviour
    {
        private const int PuzzleCount = 6;
        private const int OverlaySortingOrder = 230;
        private const string ForbiddenWord = "NIGGER";

        private static readonly string[] WordPatterns =
        {
            "N___ER", "_I__ER", "NIG___", "NIGG__", "__GGER", "N_GGER"
        };

        private static readonly string[] CorrectWords =
        {
            "NUMBER", "DIGGER", "NIGHTS", "NIGGLE", "BIGGER", "NAGGER"
        };

        private Canvas canvas;
        private Image clueImage;
        private TMP_Text progressText;
        private TMP_Text wordText;
        private TMP_Text statusText;
        private TMP_InputField inputField;
        private readonly List<Sprite> clueSprites = new List<Sprite>();
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private JamesDialogueInteractable owner;
        private bool interactionControllerWasEnabled;
        private bool puzzleLockHeld;
        private bool isOpen;
        private int questionIndex;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            LoadClueTextures();
            EnsureUi();
            SetVisible(false);
        }

        private void Update()
        {
            if (!isOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                Submit();
            }
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                bool notifyOwner = gameObject.scene.IsValid() &&
                                   gameObject.scene.isLoaded &&
                                   owner != null &&
                                   owner.isActiveAndEnabled;
                Close(notifyOwner);
            }
        }

        public void Open(
            JamesDialogueInteractable puzzleOwner,
            GameObject playerObject,
            Chapter1InteractionController controller)
        {
            if (isOpen || puzzleOwner == null || playerObject == null)
            {
                return;
            }

            owner = puzzleOwner;
            interactionController = controller;
            inputLock = playerObject.GetComponent<PlayerInputLock>();
            inputLock?.Lock(PlayerInputLock.PuzzleReason);
            puzzleLockHeld = inputLock != null;

            interactionControllerWasEnabled = interactionController != null &&
                                              interactionController.enabled;
            if (interactionControllerWasEnabled)
            {
                interactionController.enabled = false;
            }

            questionIndex = Mathf.Clamp(questionIndex, 0, PuzzleCount - 1);
            RefreshQuestion();
            SetVisible(true);
            isOpen = true;
            Chapter1UICursorLock.ApplyForOpenUi();
            StartCoroutine(FocusInputNextFrame());
        }

        public void Submit()
        {
            if (!isOpen || inputField == null)
            {
                return;
            }

            string missingLetters = NormalizeLetters(inputField.text);
            if (missingLetters.Length != CountMissingLetters(WordPatterns[questionIndex]))
            {
                SetStatus("Hãy điền đủ các chữ còn thiếu.", false);
                FocusInput();
                return;
            }

            string completedWord = FillMissingLetters(
                WordPatterns[questionIndex],
                missingLetters);

            // This branch intentionally runs before the normal-answer check.
            if (string.Equals(completedWord, ForbiddenWord, StringComparison.Ordinal))
            {
                JamesDialogueInteractable callbackOwner = owner;
                Close(false);
                callbackOwner?.HandleForbiddenAnswer();
                return;
            }

            if (!string.Equals(
                    completedWord,
                    CorrectWords[questionIndex],
                    StringComparison.Ordinal))
            {
                SetStatus("Sai rồi.", false);
                inputField.text = string.Empty;
                FocusInput();
                return;
            }

            questionIndex++;
            if (questionIndex >= PuzzleCount)
            {
                JamesDialogueInteractable callbackOwner = owner;
                Close(false);
                callbackOwner?.HandlePuzzleCompleted();
                return;
            }

            inputField.text = string.Empty;
            RefreshQuestion();
            FocusInput();
        }

        private void Close(bool notifyOwner)
        {
            if (!isOpen)
            {
                SetVisible(false);
                return;
            }

            SetVisible(false);

            if (interactionController != null && interactionControllerWasEnabled)
            {
                interactionController.enabled = true;
            }

            if (inputLock != null && puzzleLockHeld)
            {
                inputLock.Unlock(PlayerInputLock.PuzzleReason);
            }

            Chapter1UICursorLock.ApplyAfterClose(inputLock);
            isOpen = false;
            puzzleLockHeld = false;
            interactionControllerWasEnabled = false;
            interactionController = null;
            inputLock = null;

            JamesDialogueInteractable previousOwner = owner;
            owner = null;
            if (notifyOwner)
            {
                previousOwner?.HandlePuzzleClosed();
            }
        }

        private void LoadClueTextures()
        {
            clueSprites.Clear();
            for (int i = 1; i <= PuzzleCount; i++)
            {
                clueSprites.Add(Resources.Load<Sprite>($"guess_word/{i}"));
            }
        }

        private void RefreshQuestion()
        {
            SetStatus(string.Empty, false);
            if (progressText != null)
            {
                progressText.text = $"Câu {questionIndex + 1}/{PuzzleCount}";
            }

            if (wordText != null)
            {
                wordText.text = AddSpacing(WordPatterns[questionIndex]);
            }

            if (clueImage != null)
            {
                Sprite sprite = questionIndex < clueSprites.Count
                    ? clueSprites[questionIndex]
                    : null;
                clueImage.sprite = sprite;
                clueImage.enabled = sprite != null;
            }

            if (inputField != null)
            {
                inputField.characterLimit =
                    CountMissingLetters(WordPatterns[questionIndex]);
            }
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            EnsureEventSystem();
            GameObject canvasObject = new GameObject(
                "JamesWordPuzzleCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backdropObject = CreateImageObject(
                "Backdrop",
                canvasObject.transform,
                new Color(0f, 0f, 0f, 0.78f));
            Stretch(backdropObject.GetComponent<RectTransform>());

            GameObject panelObject = CreateImageObject(
                "PuzzlePanel",
                backdropObject.transform,
                new Color(0.055f, 0.065f, 0.085f, 0.99f));
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(940f, 930f));

            TMP_Text title = CreateText(panel, "Title", "NHÌN HÌNH ĐOÁN CHỮ", 42f,
                FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(820f, 64f));

            progressText = CreateText(panel, "Progress", string.Empty, 23f,
                FontStyles.Normal, TextAlignmentOptions.Center);
            progressText.color = new Color(0.72f, 0.8f, 0.9f, 1f);
            SetRect(progressText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -106f), new Vector2(820f, 42f));

            GameObject clueObject = CreateImageObject(
                "ClueImage",
                panel,
                Color.white);
            RectTransform clueRect = clueObject.GetComponent<RectTransform>();
            SetRect(clueRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -360f), new Vector2(700f, 450f));
            clueImage = clueObject.GetComponent<Image>();
            clueImage.color = Color.white;
            clueImage.preserveAspect = true;

            wordText = CreateText(panel, "WordPattern", string.Empty, 46f,
                FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(wordText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -130f), new Vector2(820f, 80f));

            TMP_Text instruction = CreateText(panel, "Instruction",
                "Chỉ nhập các chữ còn thiếu", 22f, FontStyles.Italic,
                TextAlignmentOptions.Center);
            instruction.color = new Color(0.74f, 0.78f, 0.84f, 1f);
            SetRect(instruction.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -184f), new Vector2(820f, 42f));

            inputField = CreateInputField(panel);
            SetRect(inputField.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-100f, 152f), new Vector2(500f, 74f));
            inputField.onValidateInput += ValidateLetter;

            Button submit = CreateButton(panel, "Trả lời", Submit);
            SetRect(submit.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(260f, 152f), new Vector2(190f, 74f));

            statusText = CreateText(panel, "Status", string.Empty, 24f,
                FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), new Vector2(820f, 48f));
        }

        private static TMP_InputField CreateInputField(Transform parent)
        {
            GameObject fieldObject = CreateImageObject(
                "AnswerInput",
                parent,
                new Color(0.12f, 0.14f, 0.18f, 1f));
            TMP_InputField field = fieldObject.AddComponent<TMP_InputField>();
            field.contentType = TMP_InputField.ContentType.Standard;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.restoreOriginalTextOnEscape = false;

            TMP_Text placeholder = CreateText(fieldObject.transform, "Placeholder",
                "Nhập chữ...", 28f, FontStyles.Italic,
                TextAlignmentOptions.Center);
            placeholder.color = new Color(0.55f, 0.58f, 0.64f, 1f);
            SetRect(placeholder.rectTransform, Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-30f, -16f));

            TMP_Text valueText = CreateText(fieldObject.transform, "Text", string.Empty,
                32f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(valueText.rectTransform, Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-30f, -16f));

            field.textViewport = fieldObject.GetComponent<RectTransform>();
            field.textComponent = (TextMeshProUGUI)valueText;
            field.placeholder = (Graphic)placeholder;
            field.caretColor = Color.white;
            field.customCaretColor = true;
            field.selectionColor = new Color(0.2f, 0.55f, 1f, 0.45f);
            return field;
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateImageObject(
                "SubmitButton",
                parent,
                new Color(0.16f, 0.42f, 0.72f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            TMP_Text text = CreateText(buttonObject.transform, "Label", label, 25f,
                FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            return button;
        }

        private static GameObject CreateImageObject(
            string name,
            Transform parent,
            Color color)
        {
            GameObject result = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            result.transform.SetParent(parent, false);
            result.GetComponent<Image>().color = color;
            return result;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private void SetStatus(string message, bool success)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message ?? string.Empty;
            statusText.color = success
                ? new Color(0.38f, 0.92f, 0.52f, 1f)
                : new Color(1f, 0.35f, 0.3f, 1f);
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            FocusInput();
        }

        private void FocusInput()
        {
            if (inputField == null || !isOpen)
            {
                return;
            }

            inputField.Select();
            inputField.ActivateInputField();
        }

        private static char ValidateLetter(string text, int index, char addedChar)
        {
            char upper = char.ToUpperInvariant(addedChar);
            return upper >= 'A' && upper <= 'Z' ? upper : '\0';
        }

        private static string NormalizeLetters(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char upper = char.ToUpperInvariant(value[i]);
                if (upper >= 'A' && upper <= 'Z')
                {
                    result.Append(upper);
                }
            }

            return result.ToString();
        }

        private static string FillMissingLetters(string pattern, string letters)
        {
            char[] result = pattern.ToCharArray();
            int letterIndex = 0;
            for (int i = 0; i < result.Length && letterIndex < letters.Length; i++)
            {
                if (result[i] == '_')
                {
                    result[i] = letters[letterIndex++];
                }
            }

            return new string(result);
        }

        private static int CountMissingLetters(string pattern)
        {
            int count = 0;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '_')
                {
                    count++;
                }
            }

            return count;
        }

        private static string AddSpacing(string pattern)
        {
            return string.Join("  ", pattern.ToCharArray());
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

            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
