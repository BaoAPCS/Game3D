using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class MissionHintController : MonoBehaviour
    {
        private const string RuntimeCanvasName = "Chapter1MissionHint_Runtime";
        private const string BackpackHintText = "[B] M\u1edf balo";

        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private LanRecordingMissionController missionController;
        [SerializeField] private bool showBackpackHintOnStart = true;
        [SerializeField] private float showDelaySeconds = 0.25f;

        private CanvasGroup canvasGroup;
        private TextMeshProUGUI hintText;
        private bool backpackHintDismissed;
        private bool subscribed;
        private Coroutine showRoutine;

        private void Awake()
        {
            ResolveReferences();
            EnsureRuntimeUi();
            ApplyVisible(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureRuntimeUi();
            Subscribe();

            if (showBackpackHintOnStart && !backpackHintDismissed)
            {
                showRoutine = StartCoroutine(ShowBackpackHintAfterDelay());
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }
        }

        public void ShowHint(string text)
        {
            EnsureRuntimeUi();
            if (hintText != null)
            {
                hintText.text = text ?? string.Empty;
            }

            ApplyVisible(!string.IsNullOrWhiteSpace(text) && (inputLock == null || !inputLock.IsLocked));
        }

        public void ClearHint()
        {
            if (hintText != null)
            {
                hintText.text = string.Empty;
            }

            ApplyVisible(false);
        }

        private IEnumerator ShowBackpackHintAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, showDelaySeconds));

            if (!backpackHintDismissed)
            {
                ShowHint(BackpackHintText);
            }

            showRoutine = null;
        }

        private void HandleInventoryPressed()
        {
            backpackHintDismissed = true;
            ClearHint();

            if (missionController != null && (int)missionController.State < (int)LanRecordingMissionState.OpenBackpack)
            {
                missionController.SetState(LanRecordingMissionState.OpenBackpack);
            }
        }

        private void HandleLockStateChanged(bool locked)
        {
            if (locked)
            {
                ApplyVisible(false);
            }
            else if (!backpackHintDismissed && hintText != null && !string.IsNullOrWhiteSpace(hintText.text))
            {
                ApplyVisible(true);
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (inputReader != null)
            {
                inputReader.InventoryPressed += HandleInventoryPressed;
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged += HandleLockStateChanged;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (inputReader != null)
            {
                inputReader.InventoryPressed -= HandleInventoryPressed;
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged -= HandleLockStateChanged;
            }

            subscribed = false;
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
            {
                inputReader = FindAnyObjectByType<Chapter1InputReader>(FindObjectsInactive.Include);
            }

            if (inputLock == null)
            {
                inputLock = inputReader != null
                    ? inputReader.GetComponent<PlayerInputLock>()
                    : FindAnyObjectByType<PlayerInputLock>(FindObjectsInactive.Include);
            }

            if (missionController == null)
            {
                missionController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }
        }

        private void EnsureRuntimeUi()
        {
            if (canvasGroup != null && hintText != null)
            {
                return;
            }

            Canvas existingCanvas = null;
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.name == RuntimeCanvasName)
                {
                    existingCanvas = canvases[i];
                    break;
                }
            }

            Canvas canvas = existingCanvas != null ? existingCanvas : CreateCanvas();
            Transform panel = canvas.transform.Find("MissionHint");
            if (panel == null)
            {
                panel = CreatePanel(canvas.transform).transform;
            }

            canvasGroup = panel.GetComponent<CanvasGroup>();
            hintText = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(RuntimeCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 165;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("MissionHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(parent, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 155f);
            panelRect.sizeDelta = new Vector2(640f, 72f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.58f);
            panelImage.raycastTarget = false;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 30f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return panelObject;
        }

        private void ApplyVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
