using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Menu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuRuntimeBootstrap : MonoBehaviour
    {
        private const string MenuRootName = "MainMenuUI";
        private const float MenuAnchorX = 0.15f;

        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField] private Texture2D titleTexture;
        [SerializeField] private Font menuFont;

        private void Awake()
        {
            if (FindSceneComponent<MainMenuController>() != null)
            {
                return;
            }

            if (backgroundTexture == null ||
                titleTexture == null ||
                menuFont == null)
            {
                Debug.LogError(
                    "[MainMenu] Thiếu background, title hoặc font cho menu.",
                    this);
                enabled = false;
                return;
            }

            BuildMenu();
        }

        private void BuildMenu()
        {
            GameObject root = new GameObject(
                MenuRootName,
                typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            root.SetActive(false);

            GameObject canvasObject = CreateUiObject(
                root.transform,
                "MenuCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateBackground(canvasObject.transform);
            CreateDimmer(canvasObject.transform);
            CreateTitle(canvasObject.transform);

            Button startButton = CreateButton(
                canvasObject.transform,
                "StartButton",
                "Bắt đầu",
                0.537f);
            Button continueButton = CreateButton(
                canvasObject.transform,
                "ContinueButton",
                "Tiếp tục",
                0.47f);
            Button quitButton = CreateButton(
                canvasObject.transform,
                "QuitButton",
                "Thoát",
                0.403f);
            Text statusText = CreateStatusText(canvasObject.transform);
            EventSystem eventSystem = GetOrCreateEventSystem(root.transform);
            eventSystem.firstSelectedGameObject = startButton.gameObject;
            ConfigureSceneCamera();

            MainMenuController controller =
                root.AddComponent<MainMenuController>();
            controller.Configure(
                startButton,
                continueButton,
                quitButton,
                statusText,
                eventSystem);
            root.SetActive(true);
        }

        private void CreateBackground(Transform parent)
        {
            GameObject backgroundObject = CreateUiObject(
                parent,
                "Background",
                typeof(RawImage),
                typeof(AspectRatioFitter));
            RawImage background =
                backgroundObject.GetComponent<RawImage>();
            Stretch(background.rectTransform);
            background.texture = backgroundTexture;
            background.color = Color.white;
            background.raycastTarget = false;

            AspectRatioFitter aspect =
                backgroundObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio =
                (float)backgroundTexture.width / backgroundTexture.height;
        }

        private static void CreateDimmer(Transform parent)
        {
            GameObject dimmerObject = CreateUiObject(
                parent,
                "BackgroundDimmer",
                typeof(Image));
            Image dimmer = dimmerObject.GetComponent<Image>();
            Stretch(dimmer.rectTransform);
            dimmer.color = new Color(0f, 0.005f, 0.015f, 0.32f);
            dimmer.raycastTarget = false;
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObject = CreateUiObject(
                parent,
                "Title",
                typeof(RawImage));
            RawImage title = titleObject.GetComponent<RawImage>();
            Place(
                title.rectTransform,
                new Vector2(MenuAnchorX, 0.86f),
                new Vector2(-20f, 0f),
                new Vector2(550f, 275f));
            title.texture = titleTexture;
            title.color = Color.white;
            title.raycastTarget = false;
        }

        private Button CreateButton(
            Transform parent,
            string objectName,
            string label,
            float topAnchor)
        {
            GameObject buttonObject = CreateUiObject(
                parent,
                objectName,
                typeof(MenuSelectionGraphic),
                typeof(Button));
            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            Place(
                buttonRect,
                new Vector2(MenuAnchorX, topAnchor),
                Vector2.zero,
                new Vector2(370f, 58f));

            MenuSelectionGraphic graphic =
                buttonObject.GetComponent<MenuSelectionGraphic>();
            if (buttonObject.GetComponent<CanvasRenderer>() == null)
            {
                buttonObject.AddComponent<CanvasRenderer>();
            }
            graphic.color = Color.white;
            graphic.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Vertical;
            button.navigation = navigation;

            Text buttonText = CreateText(
                buttonObject.transform,
                "Label",
                32);
            Stretch(buttonText.rectTransform);
            buttonText.rectTransform.offsetMin = new Vector2(56f, 0f);
            buttonText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            buttonText.text = label;

            Shadow shadow = buttonText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(1f, -2f);
            return button;
        }

        private Text CreateStatusText(Transform parent)
        {
            Text status = CreateText(parent, "Status", 23);
            Place(
                status.rectTransform,
                new Vector2(MenuAnchorX, 0.30f),
                new Vector2(56f, 0f),
                new Vector2(550f, 110f));
            status.alignment = TextAnchor.UpperLeft;
            status.color = new Color(0.85f, 0.78f, 0.72f);
            status.text = string.Empty;
            return status;
        }

        private void ConfigureSceneCamera()
        {
            Camera camera = FindSceneComponent<Camera>();
            if (camera == null)
            {
                Debug.LogWarning(
                    "[MainMenu] Không tìm thấy camera cho MenuScene.",
                    this);
                return;
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.015f, 0.018f, 0.025f, 1f);
            camera.cullingMask = 0;
        }

        private Text CreateText(
            Transform parent,
            string objectName,
            int fontSize)
        {
            GameObject textObject = CreateUiObject(
                parent,
                objectName,
                typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = menuFont;
            text.fontSize = fontSize;
            text.color = new Color(0.93f, 0.92f, 0.9f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private EventSystem GetOrCreateEventSystem(Transform parent)
        {
            EventSystem eventSystem = FindSceneComponent<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject(
                    "MenuEventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                eventSystemObject.transform.SetParent(parent, false);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            BaseInputModule[] modules =
                eventSystem.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if (!(modules[i] is InputSystemUIInputModule))
                {
                    modules[i].enabled = false;
                }
            }

            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<
                    InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
            inputModule.AssignDefaultActions();
            inputModule.deselectOnBackgroundClick = false;
            return eventSystem;
        }

        private static GameObject CreateUiObject(
            Transform parent,
            string objectName,
            params Type[] componentTypes)
        {
            GameObject result = new GameObject(
                objectName,
                typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                result.layer = uiLayer;
            }

            result.transform.SetParent(parent, false);
            for (int i = 0; i < componentTypes.Length; i++)
            {
                result.AddComponent(componentTypes[i]);
            }

            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private T FindSceneComponent<T>() where T : Component
        {
            T[] candidates = FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null &&
                    candidates[i].gameObject.scene == gameObject.scene)
                {
                    return candidates[i];
                }
            }

            return null;
        }
    }
}
