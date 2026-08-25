using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Runtime-only health display for the Nam versus Henry encounter.
    /// The encounter owns when this HUD is bound, shown, and hidden.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FightCombatHUD : MonoBehaviour
    {
        private const string RuntimeCanvasName =
            "Chapter1FightCombatHUD_Runtime";
        private const string HudRootName = "FightCombatHUD";
        private const int OverlaySortingOrder = 170;

        private static readonly Vector2 ReferenceResolution =
            new Vector2(1920f, 1080f);
        private static readonly Color PanelColor =
            new Color(0.035f, 0.04f, 0.05f, 0.82f);
        private static readonly Color BarBackgroundColor =
            new Color(0.055f, 0.06f, 0.07f, 0.95f);
        private static readonly Color NamHealthColor =
            new Color(0.12f, 0.78f, 0.25f, 1f);
        private static readonly Color HenryHealthColor =
            new Color(0.9f, 0.12f, 0.12f, 1f);

        private static Sprite runtimeWhiteSprite;

        [SerializeField] private Canvas runtimeCanvas;
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private Image namHealthFill;
        [SerializeField] private Image henryHealthFill;
        [SerializeField] private TextMeshProUGUI namHealthText;
        [SerializeField] private TextMeshProUGUI henryHealthText;

        private CombatHealth namHealth;
        private CombatHealth henryHealth;
        private bool isVisible;

        public bool IsVisible => isVisible;

        /// <summary>
        /// Returns the existing scene HUD or creates its dedicated overlay
        /// canvas. Repeated calls always return the same scene instance.
        /// </summary>
        public static FightCombatHUD EnsureRuntimeHUD()
        {
            FightCombatHUD[] existingHuds =
                FindObjectsByType<FightCombatHUD>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < existingHuds.Length; i++)
            {
                FightCombatHUD candidate = existingHuds[i];
                if (candidate == null || !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                candidate.EnsureRuntimeLayout();
                return candidate;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    candidate.name != RuntimeCanvasName)
                {
                    continue;
                }

                ConfigureCanvas(candidate);
                FightCombatHUD hud =
                    candidate.GetComponent<FightCombatHUD>();
                if (hud == null)
                {
                    hud = candidate.gameObject
                        .AddComponent<FightCombatHUD>();
                }

                hud.EnsureRuntimeLayout();
                return hud;
            }

            GameObject canvasObject = new GameObject(
                RuntimeCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            ConfigureCanvas(canvas);

            FightCombatHUD runtimeHud =
                canvasObject.AddComponent<FightCombatHUD>();
            runtimeHud.EnsureRuntimeLayout();
            return runtimeHud;
        }

        private void Awake()
        {
            EnsureRuntimeLayout();
            ApplyVisibility(false);
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Connects both bars to their CombatHealth sources and immediately
        /// paints the current values; no damage event is required first.
        /// </summary>
        public void Bind(CombatHealth nam, CombatHealth henry)
        {
            Unbind();
            EnsureRuntimeLayout();

            namHealth = nam;
            henryHealth = henry;

            if (namHealth != null)
            {
                namHealth.HealthChanged += HandleNamHealthChanged;
                namHealth.Died += HandleNamDied;
            }

            if (henryHealth != null)
            {
                henryHealth.HealthChanged += HandleHenryHealthChanged;
                henryHealth.Died += HandleHenryDied;
            }

            RefreshAll();
        }

        public void Unbind()
        {
            if (namHealth != null)
            {
                namHealth.HealthChanged -= HandleNamHealthChanged;
                namHealth.Died -= HandleNamDied;
            }

            if (henryHealth != null)
            {
                henryHealth.HealthChanged -= HandleHenryHealthChanged;
                henryHealth.Died -= HandleHenryDied;
            }

            namHealth = null;
            henryHealth = null;
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            EnsureRuntimeLayout();
            RefreshAll();
            ApplyVisibility(true);
        }

        public void Hide()
        {
            EnsureRuntimeLayout();
            ApplyVisibility(false);
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler =
                canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void EnsureRuntimeLayout()
        {
            runtimeCanvas = runtimeCanvas != null
                ? runtimeCanvas
                : GetComponent<Canvas>();
            if (runtimeCanvas == null)
            {
                runtimeCanvas = gameObject.AddComponent<Canvas>();
            }

            ConfigureCanvas(runtimeCanvas);

            RectTransform root = EnsureRectChild(transform, HudRootName);
            Stretch(root);
            hudGroup = EnsureComponent<CanvasGroup>(root.gameObject);
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;

            ConfigureHealthPanel(
                root,
                "NamPanel",
                true,
                "NAM",
                NamHealthColor,
                out namHealthFill,
                out namHealthText);
            ConfigureHealthPanel(
                root,
                "HenryPanel",
                false,
                "HENRY",
                HenryHealthColor,
                out henryHealthFill,
                out henryHealthText);
        }

        private static void ConfigureHealthPanel(
            RectTransform root,
            string panelName,
            bool alignLeft,
            string characterName,
            Color healthColor,
            out Image healthFill,
            out TextMeshProUGUI healthText)
        {
            RectTransform panel = EnsureRectChild(root, panelName);
            panel.anchorMin = alignLeft
                ? new Vector2(0f, 1f)
                : new Vector2(1f, 1f);
            panel.anchorMax = panel.anchorMin;
            panel.pivot = panel.anchorMin;
            panel.anchoredPosition = alignLeft
                ? new Vector2(48f, -42f)
                : new Vector2(-48f, -42f);
            panel.sizeDelta = new Vector2(570f, 112f);

            Image panelImage = EnsureImage(panel.gameObject);
            panelImage.color = PanelColor;

            RectTransform nameRect = EnsureRectChild(panel, "NameText");
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(18f, -10f);
            nameRect.sizeDelta = new Vector2(220f, 36f);
            TextMeshProUGUI nameText = EnsureText(nameRect.gameObject);
            ConfigureText(
                nameText,
                characterName,
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Left);

            RectTransform valueRect = EnsureRectChild(panel, "HealthText");
            valueRect.anchorMin = new Vector2(1f, 1f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 1f);
            valueRect.anchoredPosition = new Vector2(-18f, -10f);
            valueRect.sizeDelta = new Vector2(250f, 36f);
            healthText = EnsureText(valueRect.gameObject);
            ConfigureText(
                healthText,
                "100 / 100",
                23f,
                FontStyles.Normal,
                TextAlignmentOptions.Right);

            RectTransform barBackground =
                EnsureRectChild(panel, "HealthBarBackground");
            barBackground.anchorMin = new Vector2(0f, 0f);
            barBackground.anchorMax = new Vector2(1f, 0f);
            barBackground.pivot = new Vector2(0.5f, 0f);
            barBackground.anchoredPosition = new Vector2(0f, 18f);
            barBackground.sizeDelta = new Vector2(-36f, 36f);
            Image backgroundImage = EnsureImage(barBackground.gameObject);
            backgroundImage.color = BarBackgroundColor;

            RectTransform fillRect =
                EnsureRectChild(barBackground, "Fill");
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            healthFill = EnsureImage(fillRect.gameObject);
            healthFill.color = healthColor;
            if (healthFill.sprite == null)
            {
                healthFill.sprite = GetRuntimeWhiteSprite();
            }
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillClockwise = true;
            healthFill.fillAmount = 1f;
        }

        private void RefreshAll()
        {
            if (namHealth != null)
            {
                SetHealth(
                    namHealthFill,
                    namHealthText,
                    namHealth.CurrentHealth,
                    namHealth.MaxHealth);
            }
            else
            {
                SetHealth(namHealthFill, namHealthText, 0f, 100f);
            }

            if (henryHealth != null)
            {
                SetHealth(
                    henryHealthFill,
                    henryHealthText,
                    henryHealth.CurrentHealth,
                    henryHealth.MaxHealth);
            }
            else
            {
                SetHealth(henryHealthFill, henryHealthText, 0f, 100f);
            }
        }

        private static void SetHealth(
            Image fill,
            TextMeshProUGUI valueText,
            float current,
            float maximum)
        {
            float safeMaximum = Mathf.Max(0f, maximum);
            float safeCurrent = Mathf.Clamp(current, 0f, safeMaximum);
            float normalized = safeMaximum > 0f
                ? safeCurrent / safeMaximum
                : 0f;

            if (fill != null)
            {
                fill.fillAmount = normalized;
            }

            if (valueText != null)
            {
                valueText.text =
                    $"{Mathf.CeilToInt(safeCurrent)} / " +
                    $"{Mathf.CeilToInt(safeMaximum)}";
            }
        }

        private void HandleNamHealthChanged(float current, float maximum)
        {
            SetHealth(namHealthFill, namHealthText, current, maximum);
        }

        private void HandleHenryHealthChanged(float current, float maximum)
        {
            SetHealth(henryHealthFill, henryHealthText, current, maximum);
        }

        private void HandleNamDied()
        {
            if (namHealth != null)
            {
                SetHealth(
                    namHealthFill,
                    namHealthText,
                    namHealth.CurrentHealth,
                    namHealth.MaxHealth);
            }
        }

        private void HandleHenryDied()
        {
            if (henryHealth != null)
            {
                SetHealth(
                    henryHealthFill,
                    henryHealthText,
                    henryHealth.CurrentHealth,
                    henryHealth.MaxHealth);
            }
        }

        private void ApplyVisibility(bool visible)
        {
            isVisible = visible;
            if (hudGroup == null)
            {
                return;
            }

            hudGroup.alpha = visible ? 1f : 0f;
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;
        }

        private static RectTransform EnsureRectChild(
            Transform parent,
            string childName)
        {
            Transform existing = parent.Find(childName);
            RectTransform existingRect =
                existing != null ? existing as RectTransform : null;
            if (existingRect != null)
            {
                return existingRect;
            }

            GameObject child = new GameObject(
                childName,
                typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static T EnsureComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static Image EnsureImage(GameObject target)
        {
            EnsureComponent<CanvasRenderer>(target);
            Image image = EnsureComponent<Image>(target);
            image.raycastTarget = false;
            return image;
        }

        private static Sprite GetRuntimeWhiteSprite()
        {
            // Unity 6 removed the old UI/Skin/UISprite.psd built-in path.
            // Build the solid UI sprite from Unity's always-available white
            // texture so HUD creation cannot fail on an editor/version-specific
            // resource name.
            if (runtimeWhiteSprite != null)
            {
                return runtimeWhiteSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            runtimeWhiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeWhiteSprite.name = "FightCombatHUD_WhiteSprite";
            runtimeWhiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return runtimeWhiteSprite;
        }

        private static TextMeshProUGUI EnsureText(GameObject target)
        {
            EnsureComponent<CanvasRenderer>(target);
            return EnsureComponent<TextMeshProUGUI>(target);
        }

        private static void ConfigureText(
            TextMeshProUGUI text,
            string content,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
