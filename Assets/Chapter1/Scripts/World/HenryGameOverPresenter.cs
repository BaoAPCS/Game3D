using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryGameOverPresenter : MonoBehaviour
    {
        private const float EscapeMessageDuration = 2.5f;
        private const int OverlaySortingOrder = 32000;

        private Canvas runtimeCanvas;
        private GameObject gameOverPanel;
        private GameObject escapeMessagePanel;
        private PlayerInputLock lockedPlayerInput;
        private Coroutine escapeMessageRoutine;
        private bool isGameOver;
        private bool isReloading;

        public static HenryGameOverPresenter Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForLoadedScene(scene);
        }

        private static void InstallForLoadedScene(Scene scene)
        {
            if (Instance != null ||
                !scene.IsValid() ||
                !scene.isLoaded)
            {
                return;
            }

            bool sceneContainsHenry = false;
            GameObject[] sceneRoots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < sceneRoots.Length;
                 rootIndex++)
            {
                HenryGameOverPresenter existing =
                    sceneRoots[rootIndex]
                        .GetComponentInChildren<HenryGameOverPresenter>(true);
                if (existing != null)
                {
                    Instance = existing;
                    return;
                }

                Transform[] hierarchy =
                    sceneRoots[rootIndex]
                        .GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0;
                     childIndex < hierarchy.Length;
                     childIndex++)
                {
                    if (string.Equals(
                            hierarchy[childIndex].name,
                            HenryTheftInteractable.HenryObjectName,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        sceneContainsHenry = true;
                        break;
                    }
                }
            }

            if (!sceneContainsHenry)
            {
                return;
            }

            GameObject presenterObject = new GameObject(nameof(HenryGameOverPresenter));
            SceneManager.MoveGameObjectToScene(presenterObject, scene);
            presenterObject.AddComponent<HenryGameOverPresenter>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Chapter1EventBus.PlayerCaught += HandlePlayerCaught;
        }

        private void Update()
        {
            if (!isGameOver || isReloading || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartCurrentScene();
            }
        }

        private void RestartCurrentScene()
        {
            if (!isGameOver || isReloading)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return;
            }

            isReloading = true;
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }

        private void OnDestroy()
        {
            Chapter1EventBus.PlayerCaught -= HandlePlayerCaught;

            if (lockedPlayerInput != null)
            {
                lockedPlayerInput.Unlock(PlayerInputLock.RespawnReason);
                lockedPlayerInput = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ShowEscapeMessage()
        {
            if (isGameOver)
            {
                return;
            }

            EnsureRuntimeCanvas();

            if (escapeMessageRoutine != null)
            {
                StopCoroutine(escapeMessageRoutine);
            }

            escapeMessageRoutine = StartCoroutine(ShowEscapeMessageTemporarily());
        }

        private void HandlePlayerCaught()
        {
            if (isGameOver)
            {
                return;
            }

            isGameOver = true;
            LockPlayerInput();
            EnsureRuntimeCanvas();

            if (escapeMessageRoutine != null)
            {
                StopCoroutine(escapeMessageRoutine);
                escapeMessageRoutine = null;
            }

            escapeMessagePanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        private void LockPlayerInput()
        {
            Chapter1PlayerMotor playerMotor =
                Object.FindAnyObjectByType<Chapter1PlayerMotor>(
                    FindObjectsInactive.Include);
            if (playerMotor == null)
            {
                Debug.LogWarning("[HenryGameOverPresenter] Không tìm thấy Chapter1PlayerMotor để khóa input.");
                return;
            }

            lockedPlayerInput = playerMotor.GetComponent<PlayerInputLock>();
            if (lockedPlayerInput == null)
            {
                Debug.LogWarning("[HenryGameOverPresenter] Người chơi không có PlayerInputLock.", playerMotor);
                return;
            }

            lockedPlayerInput.Lock(PlayerInputLock.RespawnReason);
        }

        private void EnsureRuntimeCanvas()
        {
            if (runtimeCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "HenryRuntimeUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            runtimeCanvas = canvasObject.GetComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameOverPanel = CreateGameOverPanel(canvasObject.transform);
            escapeMessagePanel = CreateEscapeMessagePanel(canvasObject.transform);
            gameOverPanel.SetActive(false);
            escapeMessagePanel.SetActive(false);
        }

        private static GameObject CreateGameOverPanel(Transform parent)
        {
            GameObject panel = CreatePanel(
                "GameOverPanel",
                parent,
                new Color(0.02f, 0.02f, 0.02f, 0.9f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            StretchToParent(panelRect);

            CreateText(
                "Title",
                panel.transform,
                "BẠN ĐÃ THUA",
                58f,
                FontStyles.Bold,
                new Vector2(900f, 90f),
                new Vector2(0f, 100f));
            CreateText(
                "CaughtMessage",
                panel.transform,
                "Henry đã bắt được bạn",
                30f,
                FontStyles.Normal,
                new Vector2(900f, 60f),
                new Vector2(0f, 20f));
            CreateText(
                "RestartHint",
                panel.transform,
                "Nhấn [R] để chơi lại",
                25f,
                FontStyles.Normal,
                new Vector2(900f, 60f),
                new Vector2(0f, -55f));

            return panel;
        }

        private static GameObject CreateEscapeMessagePanel(Transform parent)
        {
            GameObject panel = CreatePanel(
                "EscapeMessagePanel",
                parent,
                new Color(0.2f, 0.2f, 0.2f, 0.9f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 95f);
            panelRect.sizeDelta = new Vector2(650f, 72f);

            TextMeshProUGUI message = CreateText(
                "EscapeMessage",
                panel.transform,
                "Bạn đã trốn thoát khỏi Henry.",
                26f,
                FontStyles.Normal,
                Vector2.zero,
                Vector2.zero);
            StretchToParent(message.rectTransform);

            return panel;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            Image background = panel.GetComponent<Image>();
            background.color = color;
            background.raycastTarget = false;
            return panel;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            FontStyles fontStyle,
            Vector2 size,
            Vector2 position)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private IEnumerator ShowEscapeMessageTemporarily()
        {
            escapeMessagePanel.SetActive(true);
            yield return new WaitForSecondsRealtime(EscapeMessageDuration);
            escapeMessagePanel.SetActive(false);
            escapeMessageRoutine = null;
        }
    }
}
