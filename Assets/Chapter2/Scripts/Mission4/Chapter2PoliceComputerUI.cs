using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2PoliceComputerUI : MonoBehaviour
    {
        public const string ComputerPassword = "12345";
        public const string WifiSsid = "Police_Station_Wifi";
        public const string WifiPassword = "abcd@@@12345";

        private static readonly Color Navy =
            new Color(0.018f, 0.075f, 0.145f, 1f);
        private static readonly Color DeepNavy =
            new Color(0.008f, 0.026f, 0.055f, 1f);
        private static readonly Color Blue =
            new Color(0.075f, 0.36f, 0.78f, 1f);
        private static readonly Color Cyan =
            new Color(0.15f, 0.76f, 1f, 1f);
        private static readonly Color PaleBlue =
            new Color(0.76f, 0.90f, 1f, 1f);
        private static readonly Color Red =
            new Color(0.95f, 0.28f, 0.31f, 1f);
        private static readonly Color Page =
            new Color(0.87f, 0.90f, 0.94f, 1f);
        private static readonly Color Ink =
            new Color(0.035f, 0.065f, 0.11f, 1f);
        private static readonly Color MutedInk =
            new Color(0.28f, 0.34f, 0.42f, 1f);

        private GameObject loginScreen;
        private GameObject menuScreen;
        private TMP_InputField passwordInput;
        private TextMeshProUGUI loginErrorText;
        private TextMeshProUGUI wifiPasswordValue;
        private TextMeshProUGUI wifiStatusText;
        private TextMeshProUGUI revealButtonLabel;
        private Button revealButton;

        private Action unlocked;
        private Action passwordRevealed;
        private bool computerUnlocked;
        private bool passwordVisible;
        private bool built;

        public bool IsVisible => gameObject.activeSelf;
        public bool LoginVisible =>
            IsVisible && loginScreen != null && loginScreen.activeSelf;
        public bool MenuVisible =>
            IsVisible && menuScreen != null && menuScreen.activeSelf;
        public bool PasswordVisible => MenuVisible && passwordVisible;

        public static Chapter2PoliceComputerUI Create(Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "Chapter2_Mission04_PoliceComputerUI",
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
            canvas.sortingOrder = 740;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect, Vector2.zero, Vector2.zero);

            Chapter2PoliceComputerUI ui =
                canvasObject.AddComponent<Chapter2PoliceComputerUI>();
            ui.Build();
            return ui;
        }

        public void Configure(
            Action onUnlocked,
            Action onPasswordRevealed)
        {
            unlocked = onUnlocked;
            passwordRevealed = onPasswordRevealed;
        }

        public void Show(
            bool isComputerUnlocked,
            bool isPasswordDiscovered)
        {
            EnsureBuilt();
            computerUnlocked = isComputerUnlocked;
            passwordVisible = isPasswordDiscovered;
            gameObject.SetActive(true);

            if (computerUnlocked)
            {
                ShowMenuScreen();
            }
            else
            {
                ShowLoginScreen();
            }
        }

        public void Hide()
        {
            if (passwordInput != null)
            {
                passwordInput.DeactivateInputField();
            }

            gameObject.SetActive(false);
        }

        public bool TryLogin(string candidatePassword)
        {
            if (!LoginVisible)
            {
                return false;
            }

            if (!string.Equals(
                    candidatePassword ?? string.Empty,
                    ComputerPassword,
                    StringComparison.Ordinal))
            {
                SetLoginError(
                    "Mật khẩu không chính xác. Vui lòng thử lại.");
                FocusPasswordInput(selectAll: true);
                return false;
            }

            computerUnlocked = true;
            SetLoginError(string.Empty);
            unlocked?.Invoke();
            ShowMenuScreen();
            return true;
        }

        public void RevealWifiPassword()
        {
            if (!MenuVisible || !computerUnlocked || passwordVisible)
            {
                return;
            }

            passwordVisible = true;
            RefreshWifiCredentials();
            passwordRevealed?.Invoke();
        }

        private void EnsureBuilt()
        {
            if (!built)
            {
                Build();
            }
        }

        private void Build()
        {
            if (built)
            {
                return;
            }

            built = true;

            RectTransform backdrop = CreateImage(
                transform,
                "Backdrop",
                new Color(0f, 0.012f, 0.025f, 0.92f),
                true);
            Stretch(backdrop, Vector2.zero, Vector2.zero);

            TextMeshProUGUI missionHeader = CreateText(
                transform,
                "MissionHeader",
                "CHƯƠNG 2  /  NHIỆM VỤ 4 — KHÔI PHỤC KẾT NỐI",
                21f,
                TextAlignmentOptions.Left);
            SetRect(
                missionHeader.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -24f),
                new Vector2(760f, 36f),
                new Vector2(0f, 1f));
            missionHeader.color = Cyan;
            missionHeader.fontStyle = FontStyles.Bold;
            missionHeader.characterSpacing = 1.2f;

            RectTransform shadow = CreateImage(
                transform,
                "MonitorShadow",
                new Color(0f, 0f, 0f, 0.72f));
            SetRect(
                shadow,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -14f),
                new Vector2(1572f, 922f),
                new Vector2(0.5f, 0.5f));

            RectTransform monitorFrame = CreateImage(
                transform,
                "MonitorFrame",
                new Color(0.075f, 0.085f, 0.095f, 1f));
            SetRect(
                monitorFrame,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1540f, 900f),
                new Vector2(0.5f, 0.5f));

            RectTransform bezelAccent = CreateImage(
                monitorFrame,
                "BezelAccent",
                new Color(0.19f, 0.22f, 0.24f, 1f));
            Stretch(
                bezelAccent,
                new Vector2(10f, 10f),
                new Vector2(-10f, -10f));

            RectTransform display = CreateImage(
                bezelAccent,
                "Display",
                DeepNavy);
            Stretch(
                display,
                new Vector2(12f, 12f),
                new Vector2(-12f, -12f));

            BuildHeader(display);
            BuildTaskbar(display);

            RectTransform content = CreateImage(
                display,
                "Content",
                DeepNavy);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.offsetMin = new Vector2(0f, 58f);
            content.offsetMax = new Vector2(0f, -88f);
            content.localScale = Vector3.one;

            BuildLoginScreen(content);
            BuildMenuScreen(content);

            TextMeshProUGUI bezelLabel = CreateText(
                monitorFrame,
                "BezelLabel",
                "RPD SECURE TERMINAL",
                12f,
                TextAlignmentOptions.Center);
            SetRect(
                bezelLabel.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 4f),
                new Vector2(300f, 22f),
                new Vector2(0.5f, 0f));
            bezelLabel.color = new Color(0.45f, 0.49f, 0.51f, 1f);
            bezelLabel.characterSpacing = 2f;

            loginScreen.SetActive(true);
            menuScreen.SetActive(false);
        }

        private void BuildHeader(RectTransform display)
        {
            RectTransform header = CreateImage(
                display,
                "Header",
                Navy);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 88f);

            RectTransform crest = CreateImage(
                header,
                "Crest",
                new Color(0.70f, 0.58f, 0.22f, 1f));
            SetRect(
                crest,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(48f, 0f),
                new Vector2(54f, 54f),
                new Vector2(0f, 0.5f));

            TextMeshProUGUI crestText = CreateText(
                crest,
                "CrestText",
                "RPD",
                15f,
                TextAlignmentOptions.Center);
            Stretch(crestText.rectTransform, Vector2.zero, Vector2.zero);
            crestText.color = DeepNavy;
            crestText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI department = CreateText(
                header,
                "Department",
                "SỞ CẢNH SÁT RPD",
                24f,
                TextAlignmentOptions.Left);
            SetRect(
                department.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(122f, 14f),
                new Vector2(420f, 34f),
                new Vector2(0f, 0.5f));
            department.fontStyle = FontStyles.Bold;

            TextMeshProUGUI system = CreateText(
                header,
                "SystemName",
                "HỆ THỐNG NỘI BỘ",
                15f,
                TextAlignmentOptions.Left);
            SetRect(
                system.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(122f, -18f),
                new Vector2(360f, 26f),
                new Vector2(0f, 0.5f));
            system.color = new Color(0.64f, 0.71f, 0.80f, 1f);
            system.characterSpacing = 1.2f;

            TextMeshProUGUI account = CreateText(
                header,
                "Account",
                "Đăng nhập: OFFICER_2417    |    BẢO MẬT",
                17f,
                TextAlignmentOptions.Right);
            SetRect(
                account.rectTransform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-42f, 0f),
                new Vector2(600f, 36f),
                new Vector2(1f, 0.5f));
            account.color = new Color(0.72f, 0.80f, 0.89f, 1f);
        }

        private void BuildTaskbar(RectTransform display)
        {
            RectTransform taskbar = CreateImage(
                display,
                "Taskbar",
                new Color(0.012f, 0.055f, 0.105f, 1f));
            taskbar.anchorMin = Vector2.zero;
            taskbar.anchorMax = new Vector2(1f, 0f);
            taskbar.pivot = new Vector2(0.5f, 0f);
            taskbar.anchoredPosition = Vector2.zero;
            taskbar.sizeDelta = new Vector2(0f, 58f);

            TextMeshProUGUI start = CreateText(
                taskbar,
                "Start",
                "RPD    |    HỆ THỐNG     WI-FI",
                16f,
                TextAlignmentOptions.Left);
            SetRect(
                start.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(28f, 0f),
                new Vector2(480f, 34f),
                new Vector2(0f, 0.5f));
            start.color = PaleBlue;
            start.fontStyle = FontStyles.Bold;

            TextMeshProUGUI controls = CreateText(
                taskbar,
                "Controls",
                "[ENTER] Xác nhận     [ESC] Quay lại",
                16f,
                TextAlignmentOptions.Right);
            SetRect(
                controls.rectTransform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-28f, 0f),
                new Vector2(520f, 34f),
                new Vector2(1f, 0.5f));
            controls.color = new Color(0.66f, 0.78f, 0.88f, 1f);
        }

        private void BuildLoginScreen(RectTransform content)
        {
            loginScreen = new GameObject(
                "LoginScreen",
                typeof(RectTransform));
            loginScreen.transform.SetParent(content, false);
            Stretch(
                loginScreen.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.zero);

            RectTransform background = CreateImage(
                loginScreen.transform,
                "LoginBackground",
                new Color(0.018f, 0.055f, 0.10f, 1f));
            Stretch(background, Vector2.zero, Vector2.zero);

            RectTransform leftAccent = CreateImage(
                background,
                "LeftAccent",
                new Color(0.025f, 0.14f, 0.24f, 1f));
            SetRect(
                leftAccent,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(470f, 0f),
                new Vector2(0f, 0.5f));

            TextMeshProUGUI secureTitle = CreateText(
                leftAccent,
                "SecureTitle",
                "RPD\nSECURE\nACCESS",
                47f,
                TextAlignmentOptions.Left);
            SetRect(
                secureTitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(58f, -85f),
                new Vector2(350f, 200f),
                new Vector2(0f, 1f));
            secureTitle.fontStyle = FontStyles.Bold;
            secureTitle.color = PaleBlue;
            secureTitle.characterSpacing = 1.5f;

            TextMeshProUGUI secureCopy = CreateText(
                leftAccent,
                "SecureCopy",
                "Thiết bị thuộc mạng nội bộ của Sở Cảnh sát.\nMọi truy cập đều được ghi lại trong nhật ký.",
                17f,
                TextAlignmentOptions.TopLeft,
                true);
            SetRect(
                secureCopy.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(58f, -30f),
                new Vector2(350f, 105f),
                new Vector2(0f, 0.5f));
            secureCopy.color = new Color(0.62f, 0.75f, 0.84f, 1f);

            TextMeshProUGUI classified = CreateText(
                leftAccent,
                "Classification",
                "INTERNAL / AUTHORIZED PERSONNEL ONLY",
                12f,
                TextAlignmentOptions.Left);
            SetRect(
                classified.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(58f, 42f),
                new Vector2(360f, 28f),
                new Vector2(0f, 0f));
            classified.color = Cyan;
            classified.characterSpacing = 1.1f;

            RectTransform cardShadow = CreateImage(
                background,
                "LoginCardShadow",
                new Color(0f, 0f, 0f, 0.42f));
            SetRect(
                cardShadow,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(260f, -8f),
                new Vector2(640f, 500f),
                new Vector2(0.5f, 0.5f));

            RectTransform card = CreateImage(
                background,
                "LoginCard",
                new Color(0.055f, 0.09f, 0.135f, 1f),
                true);
            SetRect(
                card,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(260f, 0f),
                new Vector2(640f, 500f),
                new Vector2(0.5f, 0.5f));

            RectTransform cardAccent = CreateImage(
                card,
                "CardAccent",
                Cyan);
            SetRect(
                cardAccent,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(5f, 0f),
                new Vector2(0f, 0.5f));

            TextMeshProUGUI loginTitle = CreateText(
                card,
                "LoginTitle",
                "XÁC THỰC NGƯỜI DÙNG",
                29f,
                TextAlignmentOptions.Left);
            SetRect(
                loginTitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -42f),
                new Vector2(530f, 44f),
                new Vector2(0f, 1f));
            loginTitle.fontStyle = FontStyles.Bold;
            loginTitle.color = PaleBlue;

            TextMeshProUGUI accountLabel = CreateText(
                card,
                "AccountLabel",
                "TÀI KHOẢN",
                14f,
                TextAlignmentOptions.Left);
            SetRect(
                accountLabel.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -116f),
                new Vector2(190f, 25f),
                new Vector2(0f, 1f));
            accountLabel.color = new Color(0.56f, 0.67f, 0.76f, 1f);
            accountLabel.fontStyle = FontStyles.Bold;

            RectTransform accountValue = CreateImage(
                card,
                "AccountValue",
                new Color(0.025f, 0.055f, 0.085f, 1f));
            SetRect(
                accountValue,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -146f),
                new Vector2(544f, 54f),
                new Vector2(0f, 1f));

            TextMeshProUGUI accountName = CreateText(
                accountValue,
                "AccountName",
                "OFFICER_2417",
                19f,
                TextAlignmentOptions.Left);
            Stretch(
                accountName.rectTransform,
                new Vector2(18f, 6f),
                new Vector2(-18f, -6f));
            accountName.color = PaleBlue;

            TextMeshProUGUI passwordLabel = CreateText(
                card,
                "PasswordLabel",
                "MẬT KHẨU MÁY TÍNH",
                14f,
                TextAlignmentOptions.Left);
            SetRect(
                passwordLabel.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -224f),
                new Vector2(260f, 25f),
                new Vector2(0f, 1f));
            passwordLabel.color = new Color(0.56f, 0.67f, 0.76f, 1f);
            passwordLabel.fontStyle = FontStyles.Bold;

            passwordInput = CreatePasswordInput(
                card,
                "ComputerPasswordInput",
                "Nhập mật khẩu...");
            SetRect(
                passwordInput.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -254f),
                new Vector2(544f, 62f),
                new Vector2(0f, 1f));
            passwordInput.onSubmit.AddListener(HandlePasswordSubmitted);
            passwordInput.onValueChanged.AddListener(HandlePasswordChanged);

            Button loginButton = CreateButton(
                card,
                "LoginButton",
                "ĐĂNG NHẬP",
                Blue,
                out _);
            SetRect(
                loginButton.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -342f),
                new Vector2(544f, 64f),
                new Vector2(0f, 1f));
            loginButton.onClick.AddListener(
                () => TryLogin(passwordInput.text));

            loginErrorText = CreateText(
                card,
                "LoginError",
                string.Empty,
                15f,
                TextAlignmentOptions.Left);
            SetRect(
                loginErrorText.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -420f),
                new Vector2(544f, 34f),
                new Vector2(0f, 1f));
            loginErrorText.color = Red;
            loginErrorText.fontStyle = FontStyles.Bold;
        }

        private void BuildMenuScreen(RectTransform content)
        {
            menuScreen = new GameObject(
                "MenuScreen",
                typeof(RectTransform));
            menuScreen.transform.SetParent(content, false);
            Stretch(
                menuScreen.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.zero);

            RectTransform page = CreateImage(
                menuScreen.transform,
                "Page",
                Page);
            Stretch(page, Vector2.zero, Vector2.zero);

            RectTransform sidebar = CreateImage(
                page,
                "Sidebar",
                new Color(0.035f, 0.065f, 0.105f, 1f));
            SetRect(
                sidebar,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(318f, 0f),
                new Vector2(0f, 0.5f));

            TextMeshProUGUI navTitle = CreateText(
                sidebar,
                "NavigationTitle",
                "CÀI ĐẶT HỆ THỐNG",
                16f,
                TextAlignmentOptions.Left);
            SetRect(
                navTitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -34f),
                new Vector2(250f, 32f),
                new Vector2(0f, 1f));
            navTitle.color = new Color(0.54f, 0.66f, 0.75f, 1f);
            navTitle.fontStyle = FontStyles.Bold;

            CreateNavigationRow(sidebar, "SystemNav", "HỆ THỐNG", -93f, false);
            CreateNavigationRow(sidebar, "WifiNav", "WI-FI", -159f, true);
            CreateNavigationRow(sidebar, "SecurityNav", "BẢO MẬT", -225f, false);
            CreateNavigationRow(sidebar, "LogsNav", "NHẬT KÝ", -291f, false);
            CreateNavigationRow(sidebar, "AccountNav", "TÀI KHOẢN", -357f, false);

            TextMeshProUGUI sidebarFooter = CreateText(
                sidebar,
                "SidebarFooter",
                "RACCOON POLICE\nTO PROTECT AND TO SERVE",
                13f,
                TextAlignmentOptions.BottomLeft);
            SetRect(
                sidebarFooter.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(32f, 34f),
                new Vector2(250f, 70f),
                new Vector2(0f, 0f));
            sidebarFooter.color = new Color(0.28f, 0.38f, 0.46f, 1f);
            sidebarFooter.fontStyle = FontStyles.Bold;

            RectTransform main = CreateImage(
                page,
                "WifiSettings",
                Page);
            main.anchorMin = Vector2.zero;
            main.anchorMax = Vector2.one;
            main.offsetMin = new Vector2(318f, 0f);
            main.offsetMax = Vector2.zero;
            main.localScale = Vector3.one;

            TextMeshProUGUI title = CreateText(
                main,
                "WifiTitle",
                "Cài đặt Wi-Fi",
                36f,
                TextAlignmentOptions.Left);
            SetRect(
                title.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(66f, -43f),
                new Vector2(560f, 54f),
                new Vector2(0f, 1f));
            title.color = Ink;
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI subtitle = CreateText(
                main,
                "WifiSubtitle",
                "Thông tin mạng không dây dành cho thiết bị được cấp quyền.",
                18f,
                TextAlignmentOptions.Left);
            SetRect(
                subtitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(66f, -101f),
                new Vector2(860f, 34f),
                new Vector2(0f, 1f));
            subtitle.color = MutedInk;

            RectTransform divider = CreateImage(
                main,
                "TitleDivider",
                new Color(0.43f, 0.49f, 0.57f, 0.55f));
            SetRect(
                divider,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -142f),
                new Vector2(-132f, 2f),
                new Vector2(0.5f, 1f));

            TextMeshProUGUI availableLabel = CreateText(
                main,
                "AvailableNetworkLabel",
                "MẠNG NỘI BỘ",
                17f,
                TextAlignmentOptions.Left);
            SetRect(
                availableLabel.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(66f, -171f),
                new Vector2(320f, 28f),
                new Vector2(0f, 1f));
            availableLabel.color = new Color(0.08f, 0.25f, 0.48f, 1f);
            availableLabel.fontStyle = FontStyles.Bold;

            RectTransform networkCard = CreateImage(
                main,
                "PoliceWifiNetwork",
                new Color(0.82f, 0.87f, 0.94f, 1f),
                true);
            SetRect(
                networkCard,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -211f),
                new Vector2(-132f, 108f),
                new Vector2(0.5f, 1f));
            networkCard.offsetMin = new Vector2(66f, networkCard.offsetMin.y);
            networkCard.offsetMax = new Vector2(-66f, networkCard.offsetMax.y);

            RectTransform networkAccent = CreateImage(
                networkCard,
                "NetworkAccent",
                Blue);
            SetRect(
                networkAccent,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(5f, 0f),
                new Vector2(0f, 0.5f));

            RectTransform wifiBadge = CreateImage(
                networkCard,
                "WifiBadge",
                Blue);
            SetRect(
                wifiBadge,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(34f, 0f),
                new Vector2(64f, 64f),
                new Vector2(0f, 0.5f));
            TextMeshProUGUI wifiBadgeText = CreateText(
                wifiBadge,
                "WifiBadgeText",
                "WI",
                18f,
                TextAlignmentOptions.Center);
            Stretch(wifiBadgeText.rectTransform, Vector2.zero, Vector2.zero);
            wifiBadgeText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI networkName = CreateText(
                networkCard,
                "NetworkName",
                WifiSsid,
                25f,
                TextAlignmentOptions.Left);
            SetRect(
                networkName.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(124f, 17f),
                new Vector2(560f, 38f),
                new Vector2(0f, 0.5f));
            networkName.color = Ink;
            networkName.fontStyle = FontStyles.Bold;

            wifiStatusText = CreateText(
                networkCard,
                "NetworkStatus",
                "CÓ SẴN — MẠNG BẢO MẬT",
                15f,
                TextAlignmentOptions.Left);
            SetRect(
                wifiStatusText.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(124f, -19f),
                new Vector2(520f, 28f),
                new Vector2(0f, 0.5f));
            wifiStatusText.color = new Color(0.10f, 0.35f, 0.64f, 1f);

            RectTransform credentialPanel = CreateImage(
                main,
                "NetworkCredentials",
                new Color(0.94f, 0.96f, 0.98f, 1f));
            SetRect(
                credentialPanel,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -347f),
                new Vector2(-132f, 292f),
                new Vector2(0.5f, 1f));
            credentialPanel.offsetMin =
                new Vector2(66f, credentialPanel.offsetMin.y);
            credentialPanel.offsetMax =
                new Vector2(-66f, credentialPanel.offsetMax.y);

            TextMeshProUGUI credentialsTitle = CreateText(
                credentialPanel,
                "CredentialsTitle",
                "THÔNG TIN KẾT NỐI",
                17f,
                TextAlignmentOptions.Left);
            SetRect(
                credentialsTitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, -28f),
                new Vector2(360f, 30f),
                new Vector2(0f, 1f));
            credentialsTitle.color = MutedInk;
            credentialsTitle.fontStyle = FontStyles.Bold;

            TextMeshProUGUI ssidLabel = CreateText(
                credentialPanel,
                "SsidLabel",
                "Wi-Fi",
                18f,
                TextAlignmentOptions.Left);
            SetRect(
                ssidLabel.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, -82f),
                new Vector2(150f, 34f),
                new Vector2(0f, 1f));
            ssidLabel.color = MutedInk;

            TextMeshProUGUI ssidValue = CreateText(
                credentialPanel,
                "SsidValue",
                WifiSsid,
                20f,
                TextAlignmentOptions.Left);
            SetRect(
                ssidValue.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(195f, -82f),
                new Vector2(530f, 34f),
                new Vector2(0f, 1f));
            ssidValue.color = Ink;
            ssidValue.fontStyle = FontStyles.Bold;

            TextMeshProUGUI passwordLabel = CreateText(
                credentialPanel,
                "WifiPasswordLabel",
                "Mật khẩu",
                18f,
                TextAlignmentOptions.Left);
            SetRect(
                passwordLabel.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, -137f),
                new Vector2(150f, 34f),
                new Vector2(0f, 1f));
            passwordLabel.color = MutedInk;

            RectTransform passwordField = CreateImage(
                credentialPanel,
                "WifiPasswordField",
                new Color(0.83f, 0.87f, 0.92f, 1f));
            SetRect(
                passwordField,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(195f, -132f),
                new Vector2(470f, 52f),
                new Vector2(0f, 1f));

            wifiPasswordValue = CreateText(
                passwordField,
                "WifiPasswordValue",
                string.Empty,
                20f,
                TextAlignmentOptions.Left);
            Stretch(
                wifiPasswordValue.rectTransform,
                new Vector2(18f, 7f),
                new Vector2(-18f, -7f));
            wifiPasswordValue.color = Ink;
            wifiPasswordValue.fontStyle = FontStyles.Bold;
            wifiPasswordValue.characterSpacing = 1.3f;

            revealButton = CreateButton(
                credentialPanel,
                "RevealWifiPasswordButton",
                "HIỆN MẬT KHẨU",
                Blue,
                out revealButtonLabel);
            SetRect(
                revealButton.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(695f, -132f),
                new Vector2(268f, 52f),
                new Vector2(0f, 1f));
            revealButton.onClick.AddListener(RevealWifiPassword);

            TextMeshProUGUI warning = CreateText(
                credentialPanel,
                "AuthorizationWarning",
                "Chỉ sử dụng thông tin này trên thiết bị được cấp quyền.",
                15f,
                TextAlignmentOptions.Left);
            SetRect(
                warning.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(34f, 27f),
                new Vector2(760f, 32f),
                new Vector2(0f, 0f));
            warning.color = MutedInk;
            warning.fontStyle = FontStyles.Italic;
        }

        private static void CreateNavigationRow(
            Transform parent,
            string name,
            string label,
            float anchoredY,
            bool selected)
        {
            RectTransform row = CreateImage(
                parent,
                name,
                selected
                    ? new Color(0.075f, 0.25f, 0.49f, 1f)
                    : Color.clear);
            SetRect(
                row,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, anchoredY),
                new Vector2(0f, 58f),
                new Vector2(0.5f, 1f));

            if (selected)
            {
                RectTransform accent = CreateImage(
                    row,
                    "SelectedAccent",
                    Cyan);
                SetRect(
                    accent,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    Vector2.zero,
                    new Vector2(5f, 0f),
                    new Vector2(0f, 0.5f));
            }

            TextMeshProUGUI symbol = CreateText(
                row,
                "Symbol",
                selected ? "●" : "○",
                17f,
                TextAlignmentOptions.Center);
            SetRect(
                symbol.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(30f, 0f),
                new Vector2(40f, 40f),
                new Vector2(0f, 0.5f));
            symbol.color = selected
                ? Cyan
                : new Color(0.52f, 0.63f, 0.72f, 1f);

            TextMeshProUGUI rowLabel = CreateText(
                row,
                "Label",
                label,
                18f,
                TextAlignmentOptions.Left);
            SetRect(
                rowLabel.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(82f, 0f),
                new Vector2(205f, 36f),
                new Vector2(0f, 0.5f));
            rowLabel.color = selected
                ? Color.white
                : new Color(0.70f, 0.77f, 0.83f, 1f);
            rowLabel.fontStyle = selected
                ? FontStyles.Bold
                : FontStyles.Normal;
        }

        private void ShowLoginScreen()
        {
            loginScreen.SetActive(true);
            menuScreen.SetActive(false);
            SetLoginError(string.Empty);
            if (passwordInput != null)
            {
                passwordInput.SetTextWithoutNotify(string.Empty);
            }

            FocusPasswordInput(selectAll: false);
        }

        private void ShowMenuScreen()
        {
            if (passwordInput != null)
            {
                passwordInput.DeactivateInputField();
            }

            loginScreen.SetActive(false);
            menuScreen.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            RefreshWifiCredentials();
        }

        private void RefreshWifiCredentials()
        {
            if (wifiPasswordValue != null)
            {
                wifiPasswordValue.text = passwordVisible
                    ? WifiPassword
                    : "••••••••••••";
                wifiPasswordValue.color = passwordVisible
                    ? new Color(0.02f, 0.31f, 0.61f, 1f)
                    : Ink;
            }

            if (wifiStatusText != null)
            {
                wifiStatusText.text = passwordVisible
                    ? "THÔNG TIN ĐÃ ĐƯỢC XÁC THỰC"
                    : "CÓ SẴN — MẠNG BẢO MẬT";
                wifiStatusText.color = passwordVisible
                    ? new Color(0.08f, 0.48f, 0.27f, 1f)
                    : new Color(0.10f, 0.35f, 0.64f, 1f);
            }

            if (revealButton != null)
            {
                revealButton.interactable = !passwordVisible;
            }

            if (revealButtonLabel != null)
            {
                revealButtonLabel.text = passwordVisible
                    ? "ĐÃ HIỂN THỊ"
                    : "HIỆN MẬT KHẨU";
            }
        }

        private void HandlePasswordSubmitted(string value)
        {
            TryLogin(value);
        }

        private void HandlePasswordChanged(string value)
        {
            if (loginErrorText != null &&
                !string.IsNullOrEmpty(loginErrorText.text))
            {
                SetLoginError(string.Empty);
            }
        }

        private void SetLoginError(string message)
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = message ?? string.Empty;
            }
        }

        private void FocusPasswordInput(bool selectAll)
        {
            if (passwordInput == null || !passwordInput.isActiveAndEnabled)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    passwordInput.gameObject);
            }

            passwordInput.ActivateInputField();
            if (selectAll)
            {
                passwordInput.Select();
            }
        }

        private static TMP_InputField CreatePasswordInput(
            Transform parent,
            string name,
            string placeholderValue)
        {
            GameObject inputObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);

            Image background = inputObject.GetComponent<Image>();
            background.color = new Color(0.018f, 0.038f, 0.065f, 1f);

            GameObject viewportObject = new GameObject(
                "Text Area",
                typeof(RectTransform),
                typeof(RectMask2D));
            viewportObject.transform.SetParent(inputObject.transform, false);
            RectTransform viewport =
                viewportObject.GetComponent<RectTransform>();
            Stretch(
                viewport,
                new Vector2(16f, 5f),
                new Vector2(-16f, -5f));

            TextMeshProUGUI placeholder = CreateText(
                viewport,
                "Placeholder",
                placeholderValue,
                18f,
                TextAlignmentOptions.Left);
            Stretch(placeholder.rectTransform, Vector2.zero, Vector2.zero);
            placeholder.color = new Color(0.43f, 0.52f, 0.60f, 1f);
            placeholder.fontStyle = FontStyles.Italic;

            TextMeshProUGUI valueText = CreateText(
                viewport,
                "Text",
                string.Empty,
                20f,
                TextAlignmentOptions.Left);
            Stretch(valueText.rectTransform, Vector2.zero, Vector2.zero);
            valueText.color = Color.white;

            TMP_InputField input =
                inputObject.GetComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = viewport;
            input.textComponent = valueText;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.Password;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 32;
            input.restoreOriginalTextOnEscape = false;
            input.customCaretColor = true;
            input.caretColor = Cyan;
            input.selectionColor = new Color(0.12f, 0.50f, 0.84f, 0.62f);
            input.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            return input;
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
            TextAlignmentOptions alignment,
            bool wrap = false)
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
            text.textWrappingMode = wrap
                ? TextWrappingModes.Normal
                : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Color color,
            out TextMeshProUGUI labelText)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

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
            colors.highlightedColor = new Color(0.76f, 0.91f, 1f, 1f);
            colors.pressedColor = new Color(0.50f, 0.76f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.52f, 0.57f, 0.58f);
            button.colors = colors;

            labelText = CreateText(
                buttonObject.transform,
                "Label",
                label,
                17f,
                TextAlignmentOptions.Center);
            Stretch(
                labelText.rectTransform,
                new Vector2(10f, 4f),
                new Vector2(-10f, -4f));
            labelText.fontStyle = FontStyles.Bold;
            labelText.characterSpacing = 0.8f;
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
