using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter3
{
    [DisallowMultipleComponent]
    public sealed class Chapter3RescueHUD : MonoBehaviour
    {
        private GameObject doorVoiceRoot;
        private GameObject doorPromptRoot;
        private GameObject dialogueRoot;
        private TextMeshProUGUI speakerText;
        private TextMeshProUGUI dialogueText;
        private GameObject fadeRoot;
        private CanvasGroup fadeGroup;
        private GameObject endingRoot;
        private bool endingVisible;

        public string CurrentDialogue { get; private set; } = string.Empty;
        public float FadeAlpha => fadeGroup != null ? fadeGroup.alpha : 0f;

        public static Chapter3RescueHUD Create(Transform parent)
        {
            Chapter3RescueHUD existing = parent != null
                ? parent.GetComponentInChildren<Chapter3RescueHUD>(true)
                : null;
            if (existing != null)
            {
                return existing;
            }

            GameObject canvasObject = new GameObject(
                "Chapter3RescueHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent, false);
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 280;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Chapter3RescueHUD hud = canvasObject.AddComponent<Chapter3RescueHUD>();
            hud.BuildDoorVoice(canvasObject.transform);
            hud.BuildDoorPrompt(canvasObject.transform);
            hud.BuildDialogue(canvasObject.transform);
            hud.BuildEnding(canvasObject.transform);
            hud.HideAll();
            return hud;
        }

        public void SetDoorVoice(bool visible)
        {
            if (doorVoiceRoot != null)
            {
                doorVoiceRoot.SetActive(visible && !endingVisible);
            }
        }

        public void SetDoorPrompt(bool visible)
        {
            if (doorPromptRoot != null)
            {
                doorPromptRoot.SetActive(visible && !endingVisible);
            }
        }

        public void ShowDialogue(string speaker, string line)
        {
            if (endingVisible)
            {
                return;
            }

            SetDoorVoice(false);
            SetDoorPrompt(false);
            CurrentDialogue = line ?? string.Empty;
            if (speakerText != null)
            {
                speakerText.text = speaker ?? string.Empty;
            }

            if (dialogueText != null)
            {
                dialogueText.text = CurrentDialogue;
            }

            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(true);
            }
        }

        public void HideDialogue()
        {
            CurrentDialogue = string.Empty;
            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(false);
            }
        }

        public void SetFade(float alpha)
        {
            float value = endingVisible ? 1f : Mathf.Clamp01(alpha);
            if (fadeGroup != null)
            {
                fadeGroup.alpha = value;
            }

            if (fadeRoot != null)
            {
                fadeRoot.SetActive(value > 0f);
                fadeRoot.transform.SetAsLastSibling();
            }
        }

        public void ShowEnding()
        {
            SetDoorVoice(false);
            SetDoorPrompt(false);
            HideDialogue();
            endingVisible = true;
            if (endingRoot != null)
            {
                endingRoot.SetActive(true);
            }

            SetFade(1f);
        }

        public void HideAll()
        {
            endingVisible = false;
            SetDoorVoice(false);
            SetDoorPrompt(false);
            HideDialogue();
            if (endingRoot != null)
            {
                endingRoot.SetActive(false);
            }

            SetFade(0f);
        }

        private void BuildDoorVoice(Transform canvasTransform)
        {
            doorVoiceRoot = CreateImageObject(
                canvasTransform,
                "VoiceBehindDoor",
                new Color(0.025f, 0.025f, 0.035f, 0.72f));
            SetPosition(
                doorVoiceRoot.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f),
                new Vector2(820f, 116f),
                new Vector2(0f, -72f));

            TextMeshProUGUI label = CreateText(
                doorVoiceRoot.transform, "VoiceLabel", 21f, TextAlignmentOptions.Center);
            label.text = "Tiếng nói sau cánh cửa";
            label.color = new Color(0.73f, 0.74f, 0.78f);
            SetRegion(label.rectTransform, new Vector2(0f, 0.6f), Vector2.one, 18f, 6f);

            TextMeshProUGUI voice = CreateText(
                doorVoiceRoot.transform, "VoiceText", 32f, TextAlignmentOptions.Center);
            voice.text = "\"Có ai không, cứu tôi với!\"";
            voice.fontStyle = FontStyles.Italic;
            SetRegion(voice.rectTransform, Vector2.zero, new Vector2(1f, 0.64f), 18f, 8f);
        }

        private void BuildDoorPrompt(Transform canvasTransform)
        {
            doorPromptRoot = CreateImageObject(
                canvasTransform, "DoorPrompt", new Color(0f, 0f, 0f, 0.7f));
            SetPosition(
                doorPromptRoot.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(620f, 72f),
                new Vector2(0f, 155f));

            TextMeshProUGUI prompt = CreateText(
                doorPromptRoot.transform, "PromptText", 30f, TextAlignmentOptions.Center);
            prompt.text = "[F] Mở cửa";
            SetRegion(prompt.rectTransform, Vector2.zero, Vector2.one, 18f, 8f);
        }

        private void BuildDialogue(Transform canvasTransform)
        {
            dialogueRoot = CreateImageObject(
                canvasTransform,
                "RescueDialogue",
                new Color(0.025f, 0.025f, 0.035f, 0.92f));
            RectTransform panel = dialogueRoot.GetComponent<RectTransform>();
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.offsetMin = new Vector2(220f, 78f);
            panel.offsetMax = new Vector2(-220f, 312f);

            speakerText = CreateText(
                dialogueRoot.transform, "Speaker", 29f, TextAlignmentOptions.MidlineLeft);
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = new Color(0.93f, 0.8f, 0.61f);
            SetRegion(speakerText.rectTransform, new Vector2(0f, 0.71f), Vector2.one, 34f, 8f);

            dialogueText = CreateText(
                dialogueRoot.transform, "DialogueText", 31f, TextAlignmentOptions.TopLeft);
            SetRegion(dialogueText.rectTransform, new Vector2(0f, 0.22f), new Vector2(1f, 0.72f), 34f, 8f);

            TextMeshProUGUI next = CreateText(
                dialogueRoot.transform, "ContinuePrompt", 22f, TextAlignmentOptions.MidlineRight);
            next.text = "[Enter] Tiếp tục";
            next.color = new Color(0.73f, 0.74f, 0.78f);
            SetRegion(next.rectTransform, Vector2.zero, new Vector2(1f, 0.24f), 34f, 8f);
        }

        private void BuildEnding(Transform canvasTransform)
        {
            fadeRoot = CreateImageObject(canvasTransform, "RescueFadeAndEnding", Color.black);
            SetRegion(fadeRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f, 0f);
            Canvas fadeCanvas = fadeRoot.AddComponent<Canvas>();
            fadeCanvas.overrideSorting = true;
            fadeCanvas.sortingOrder = 32000;
            fadeGroup = fadeRoot.AddComponent<CanvasGroup>();
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;

            endingRoot = new GameObject("GameEnding", typeof(RectTransform));
            endingRoot.transform.SetParent(fadeRoot.transform, false);
            SetRegion(endingRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f, 0f);

            TextMeshProUGUI ending = CreateText(
                endingRoot.transform, "EndingMessage", 42f, TextAlignmentOptions.Center);
            ending.text = "Nam đã cứu được Lan khỏi bệnh viện.";
            SetPosition(ending.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1160f, 130f), new Vector2(0f, 36f));

            TextMeshProUGUI completion = CreateText(
                endingRoot.transform, "ChapterComplete", 22f, TextAlignmentOptions.Center);
            completion.text = "CHAPTER 3 HOÀN THÀNH • HẾT GAME";
            completion.color = new Color(0.64f, 0.65f, 0.69f);
            completion.characterSpacing = 2f;
            SetPosition(completion.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 60f), new Vector2(0f, -64f));
        }

        private static GameObject CreateImageObject(Transform parent, string objectName, Color color)
        {
            GameObject result = new GameObject(
                objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            result.transform.SetParent(parent, false);
            Image image = result.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return result;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string objectName, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject result = new GameObject(
                objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            result.transform.SetParent(parent, false);
            TextMeshProUGUI text = result.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static void SetPosition(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetRegion(
            RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float horizontalInset, float verticalInset)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        }
    }
}
