using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Menu.Editor
{
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/Menu/MenuScene.unity";
        public const string GeneratedRootName = "MainMenuUI";
        private const string BackgroundPath = "Assets/Menu/Sprites/background.png";
        private const string TitlePath = "Assets/Menu/Sprites/title.png";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        [MenuItem("Tools/Dormitory Mystery/Build Menu Scene")]
        public static void BuildMenuScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building the menu scene.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("The existing MenuScene asset could not be found.");

            Sprite background = ImportUISprite(BackgroundPath);
            Sprite title = ImportUISprite(TitlePath);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
                throw new InvalidOperationException("The project's LiberationSans font is missing.");

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);
                foreach (GameObject candidate in scene.GetRootGameObjects())
                {
                    if (candidate.name != GeneratedRootName)
                        continue;
                    if (candidate.GetComponent<MainMenuController>() == null)
                        throw new InvalidOperationException("A user object already uses the reserved MainMenuUI name.");
                    Object.DestroyImmediate(candidate);
                }

                GameObject root = new GameObject(GeneratedRootName);
                MainMenuController controller = root.AddComponent<MainMenuController>();
                GameObject canvasObject = CreateUI(root.transform, "MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject backgroundObject = CreateUI(canvasObject.transform, "Background", typeof(Image), typeof(AspectRatioFitter));
                Image backgroundImage = backgroundObject.GetComponent<Image>();
                backgroundImage.sprite = background;
                backgroundImage.raycastTarget = false;
                AspectRatioFitter backgroundAspect = backgroundObject.GetComponent<AspectRatioFitter>();
                backgroundAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                backgroundAspect.aspectRatio = background.rect.width / background.rect.height;

                GameObject dimmerObject = CreateUI(canvasObject.transform, "BackgroundDimmer", typeof(Image));
                Stretch(dimmerObject.GetComponent<RectTransform>());
                Image dimmer = dimmerObject.GetComponent<Image>();
                dimmer.color = new Color(0f, 0.005f, 0.015f, 0.32f);
                dimmer.raycastTarget = false;

                GameObject titleObject = CreateUI(canvasObject.transform, "Title", typeof(Image));
                Place(titleObject.GetComponent<RectTransform>(), new Vector2(0.15f, 0.86f), new Vector2(-20f, 0f), new Vector2(550f, 275f));
                Image titleImage = titleObject.GetComponent<Image>();
                titleImage.sprite = title;
                titleImage.preserveAspect = true;
                titleImage.raycastTarget = false;

                Button start = CreateButton(canvasObject.transform, "StartButton", "Bắt đầu", 0.537f, font);
                Button resume = CreateButton(canvasObject.transform, "ContinueButton", "Tiếp tục", 0.47f, font);
                Button quit = CreateButton(canvasObject.transform, "QuitButton", "Thoát", 0.403f, font);

                Text status = CreateText(canvasObject.transform, "Status", font, 23);
                Place(status.rectTransform, new Vector2(0.15f, 0.30f), new Vector2(56f, 0f), new Vector2(550f, 110f));
                status.alignment = TextAnchor.UpperLeft;
                status.color = new Color(0.85f, 0.78f, 0.72f);
                status.text = string.Empty;

                EventSystem eventSystem = FindInScene<EventSystem>(scene);
                if (eventSystem == null)
                {
                    GameObject events = new GameObject("MenuEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                    events.transform.SetParent(root.transform, false);
                    eventSystem = events.GetComponent<EventSystem>();
                }

                foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                    if (!(module is InputSystemUIInputModule)) module.enabled = false;
                InputSystemUIInputModule input = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (input == null) input = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                input.enabled = true;
                input.AssignDefaultActions();
                input.deselectOnBackgroundClick = false;
                eventSystem.firstSelectedGameObject = start.gameObject;
                controller.Configure(start, resume, quit, status, eventSystem);

                Camera camera = FindInScene<Camera>(scene);
                if (camera == null)
                    camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.018f, 0.025f, 1f);
                camera.cullingMask = 0;

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Unity could not save the menu scene.");
                Debug.Log("MenuScene built with background, title, three buttons, and Input System UI navigation.");
            }
            finally
            {
                // Never save or replace another open scene, including dirty user scenes.
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static Sprite ImportUISprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Menu image is missing: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("Menu image did not import as a sprite: " + path);
            return sprite;
        }

        private static Button CreateButton(Transform parent, string name, string label, float topAnchor, Font font)
        {
            GameObject result = CreateUI(parent, name, typeof(MenuSelectionGraphic), typeof(Button));
            Place(result.GetComponent<RectTransform>(), new Vector2(0.15f, topAnchor), Vector2.zero, new Vector2(370f, 58f));
            MenuSelectionGraphic graphic = result.GetComponent<MenuSelectionGraphic>();
            if (result.GetComponent<CanvasRenderer>() == null)
                result.AddComponent<CanvasRenderer>();
            graphic.color = Color.white;
            graphic.raycastTarget = true;
            Button button = result.GetComponent<Button>();
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

            Text text = CreateText(result.transform, "Label", font, 32);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(56f, 0f);
            text.rectTransform.offsetMax = new Vector2(-12f, 0f);
            text.text = label;
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(1f, -2f);
            return button;
        }

        private static Text CreateText(Transform parent, string name, Font font, int size)
        {
            Text text = CreateUI(parent, name, typeof(Text)).GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = new Color(0.93f, 0.92f, 0.9f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static GameObject CreateUI(Transform parent, string name, params Type[] components)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            result.transform.SetParent(parent, false);
            foreach (Type component in components)
                result.AddComponent(component);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
