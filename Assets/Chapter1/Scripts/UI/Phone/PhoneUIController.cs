using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class PhoneUIController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private GameObject homeScreen;
        [SerializeField] private GameObject appContent;
        [SerializeField] private Button messengerButton;
        [SerializeField] private Button recorderButton;
        [SerializeField] private Button cameraButton;
        [SerializeField] private Button googleButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private TextMeshProUGUI appTitleText;
        [SerializeField] private TextMeshProUGUI appBodyText;
        [SerializeField] private Button messagesButton;
        [SerializeField] private Button recordingsButton;
        [SerializeField] private Button cluesButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject messagesContent;
        [SerializeField] private GameObject recordingsContent;
        [SerializeField] private GameObject cluesContent;
        [SerializeField] private TextMeshProUGUI messagesText;
        [SerializeField] private TextMeshProUGUI recordingsText;
        [SerializeField] private TextMeshProUGUI cluesText;
        [SerializeField] private Image messagesHighlight;
        [SerializeField] private Image recordingsHighlight;
        [SerializeField] private Image cluesHighlight;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip tabClip;
        [SerializeField] private LanRecordingMissionController missionController;
        [SerializeField] private Mission01AudioSeparatorManager firstMissionManager;
        [SerializeField] private AudioSeparatorMixerController mixerController;

        private bool isOpen;
        private bool motherChatRead;
        private bool lanMessageReceived;
        private bool lanChatRead;
        private bool lanRecordingDownloaded;
        private string activeMessengerContact = string.Empty;
        private Coroutine dungReplyRoutine;
        private string currentRecordingId = string.Empty;
        private AudioClip currentVoiceClip;
        private Slider currentVoiceSlider;
        private TextMeshProUGUI currentVoiceTimeText;
        private Button currentVoicePlayButton;

        private const string DynamicMessengerRootName = "MessengerDynamicRoot";
        private const float ContactRowHeight = 76f;
        private const float ConversationBackButtonHeight = 38f;
        private const float ConversationScrollHeight = 342f;
        private const float AudioMessageRowHeight = 154f;

        public bool IsOpen => isOpen;

        private void Update()
        {
            UpdateVoiceProgress();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsurePhoneScreenStructure();
            BindButtonListeners();
            if (!isOpen)
            {
                SetOpenState(false, false);
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsurePhoneScreenStructure();
            BindButtonListeners();
        }

        public void Configure(PlayerInputLock lockReference)
        {
            inputLock = lockReference;
        }

        public void SetAudio(AudioSource source, AudioClip open, AudioClip close, AudioClip tab)
        {
            audioSource = source;
            openClip = open;
            closeClip = close;
            tabClip = tab;
        }

        public void OpenPhone()
        {
            ResolveReferences();
            EnsurePhoneScreenStructure();
            BindButtonListeners();
            if (isOpen)
            {
                return;
            }

            inputLock?.AcquireInputLock(PlayerInputLock.PhoneReason);
            Chapter1UICursorLock.ApplyForOpenUi();
            SetOpenState(true, true);
            AdvanceMissionState(LanRecordingMissionState.OpenPhone);
            ShowHomeScreen(false);
            PlayClip(openClip);
        }

        public void ClosePhone()
        {
            if (!isOpen)
            {
                return;
            }

            SetOpenState(false, false);
            StopVoicePlayback();
            inputLock?.ReleaseInputLock(PlayerInputLock.PhoneReason);
            Chapter1UICursorLock.ApplyAfterClose(inputLock);
            PlayClip(closeClip);
        }

        public void OpenMessages()
        {
            OpenMessenger();
        }

        public void OpenRecordings()
        {
            OpenRecorder();
        }

        public void OpenClues()
        {
            OpenGoogle();
        }

        public void ShowHomeScreen()
        {
            ShowHomeScreen(true);
        }

        public void OpenMessenger()
        {
            SyncMessengerStateFromMission();
            SyncFirstMissionReferences();
            ReceiveLanMessageIfReady();
            activeMessengerContact = string.Empty;
            ShowApp("Messenger", string.Empty);
            ShowMessengerContactList();
        }

        public void OpenRecorder()
        {
            SyncMessengerStateFromMission();
            SyncFirstMissionReferences();
            ShowApp("Ghi âm", string.Empty);
            ShowRecorderLibrary();
        }

        public void OpenCamera()
        {
            ShowApp("Camera", "Camera đang sẵn sàng.");
        }

        public void OpenGoogle()
        {
            ShowApp("Google", "Không có kết nối mạng.");
        }

        private void ShowHomeScreen(bool playSound)
        {
            EnsurePhoneScreenStructure();
            SetActive(homeScreen, true);
            SetActive(appContent, false);
            SetLegacyPhoneUiActive(false);
            ClearDynamicAppBody();
            SetHomeButtonAction(ShowHomeScreen);

            if (playSound)
            {
                PlayClip(tabClip);
            }
        }

        private void ShowApp(string title, string body)
        {
            EnsurePhoneScreenStructure();
            SetActive(homeScreen, false);
            SetActive(appContent, true);
            SetLegacyPhoneUiActive(false);
            ClearDynamicAppBody();
            if (appBodyText != null)
            {
                appBodyText.gameObject.SetActive(true);
            }

            SetHomeButtonAction(ShowHomeScreen);
            SetContentText(appTitleText, title);
            SetContentText(appBodyText, body);
            PlayClip(tabClip);
        }

        private void SetOpenState(bool open, bool makeInteractable)
        {
            isOpen = open;
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            if (root.activeSelf != open)
            {
                root.SetActive(open);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = open ? 1f : 0f;
                canvasGroup.interactable = makeInteractable;
                canvasGroup.blocksRaycasts = makeInteractable;
            }
        }

        private void BindButtonListeners()
        {
            Bind(messengerButton, OpenMessenger);
            Bind(recorderButton, OpenRecorder);
            Bind(cameraButton, OpenCamera);
            Bind(googleButton, OpenGoogle);
            Bind(homeButton, ShowHomeScreen);
            Bind(messagesButton, OpenMessages);
            Bind(recordingsButton, OpenRecordings);
            Bind(cluesButton, OpenClues);
            Bind(closeButton, ClosePhone);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (inputLock == null)
            {
                inputLock = FindAnyObjectByType<PlayerInputLock>();
            }

            homeScreen ??= FindChild("HomeScreen");
            appContent ??= FindChild("AppContent");

            if (messengerButton == null)
            {
                messengerButton = FindChildComponent<Button>("MessengerButton");
            }

            if (recorderButton == null)
            {
                recorderButton = FindChildComponent<Button>("RecorderButton");
            }

            if (cameraButton == null)
            {
                cameraButton = FindChildComponent<Button>("CameraButton");
            }

            if (googleButton == null)
            {
                googleButton = FindChildComponent<Button>("GoogleButton");
            }

            if (homeButton == null)
            {
                homeButton = FindChildComponent<Button>("HomeButton");
            }

            appTitleText ??= FindChildComponent<TextMeshProUGUI>("AppTitleText");
            appBodyText ??= FindChildComponent<TextMeshProUGUI>("AppBodyText");

            if (messagesButton == null)
            {
                messagesButton = FindChildComponent<Button>("MessagesButton");
            }

            if (recordingsButton == null)
            {
                recordingsButton = FindChildComponent<Button>("RecordingsButton");
            }

            if (cluesButton == null)
            {
                cluesButton = FindChildComponent<Button>("CluesButton");
            }

            if (closeButton == null)
            {
                closeButton = FindChildComponent<Button>("CloseButton");
            }

            messagesContent ??= FindChild("MessagesContent");
            recordingsContent ??= FindChild("RecordingsContent");
            cluesContent ??= FindChild("CluesContent");

            messagesText ??= FindChildComponent<TextMeshProUGUI>("MessagesText");
            recordingsText ??= FindChildComponent<TextMeshProUGUI>("RecordingsText");
            cluesText ??= FindChildComponent<TextMeshProUGUI>("CluesText");

            messagesHighlight ??= FindChildComponent<Image>("MessagesHighlight");
            recordingsHighlight ??= FindChildComponent<Image>("RecordingsHighlight");
            cluesHighlight ??= FindChildComponent<Image>("CluesHighlight");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (missionController == null)
            {
                missionController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            if (mixerController == null)
            {
                mixerController = FindAnyObjectByType<AudioSeparatorMixerController>(FindObjectsInactive.Include);
            }

            SyncFirstMissionReferences();
        }

        private void EnsurePhoneScreenStructure()
        {
            ResolveReferences();

            Transform frame = FindChild("PhoneFrame")?.transform ?? transform;
            SetLegacyPhoneUiActive(false);

            if (homeScreen == null)
            {
                homeScreen = CreateHomeScreen(frame).gameObject;
            }

            if (appContent == null)
            {
                appContent = CreateAppContent(frame).gameObject;
                appContent.SetActive(false);
            }

            ResolveReferences();
        }

        private RectTransform CreateHomeScreen(Transform parent)
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
            CreateAppButton(grid, "RecorderButton", "Ghi âm", "REC", new Color(0.82f, 0.12f, 0.17f, 1f));
            CreateAppButton(grid, "CameraButton", "Camera", "CAM", new Color(0.13f, 0.62f, 0.37f, 1f));
            CreateAppButton(grid, "GoogleButton", "Google", "G", new Color(0.96f, 0.72f, 0.12f, 1f));

            return screen;
        }

        private void ShowMessengerContactList()
        {
            SetContentText(appTitleText, "Messenger");
            SetHomeButtonAction(ShowHomeScreen);

            RectTransform root = PrepareDynamicMessengerRoot();
            ConfigureVerticalStack(root, 8f, new RectOffset(8, 8, 8, 8));

            CreateContactButton(
                root,
                "Mẹ",
                motherChatRead ? "Đã đọc" : "2 tin nhắn mới",
                motherChatRead ? string.Empty : "2",
                OpenMotherConversation);

            CreateContactButton(
                root,
                "Chị Lan",
                GetLanContactStatus(),
                lanMessageReceived && !lanChatRead ? "1" : string.Empty,
                OpenLanConversation);

            CreateContactButton(root, "Dũng", GetDungContactStatus(), GetDungContactBadge(), OpenDungConversation);
            CreateContactButton(root, "Minh", "Không có tin mới", string.Empty, () => OpenSimpleConversation("Minh"));
            ForceMessengerLayout(root);
        }

        private string GetLanContactStatus()
        {
            if (lanMessageReceived && !lanChatRead)
            {
                return "Tin nhắn thoại mới";
            }

            return lanChatRead ? "Đã đọc" : "Không có tin mới";
        }

        private void OpenMotherConversation()
        {
            activeMessengerContact = "mother";
            SetContentText(appTitleText, "Mẹ");
            SetHomeButtonAction(OpenMessenger);

            RectTransform root = PrepareDynamicMessengerRoot();
            ConfigureVerticalStack(root, 8f, new RectOffset(0, 0, 0, 0));

            CreateSmallButton(root, "BackToContacts", "< Messenger", OpenMessenger, ConversationBackButtonHeight);
            RectTransform content = CreateConversationScroll(root);
            CreateMessageBubble(content, "Mẹ", "Chị con đã bỏ nhà đi đâu rồi Nam", false);
            CreateMessageBubble(content, "Mẹ", "Con có liên lạc với chị dạo gần đây không?", false);
            CreateMessageBubble(content, "Nam", "Dạ con không. Mẹ đã báo cảnh sát chưa?", true);
            CreateMessageBubble(content, "Mẹ", "Bố mẹ báo rồi. Cảnh sát điều tra thì nói chị đang gặp vấn đề về tâm lý nên đã bỏ nhà ra đi.", false, 116f);
            CreateMessageBubble(content, "Mẹ", "Thôi mẹ đi làm đây. Có liên lạc được với chị thì nói mẹ nhé.", false, 98f);

            motherChatRead = true;
            AdvanceMissionState(LanRecordingMissionState.ReadMotherChat);
            ReceiveLanMessageIfReady();
            ForceMessengerLayout(root);
        }

        private void OpenLanConversation()
        {
            activeMessengerContact = "lan";
            SetContentText(appTitleText, "Chị Lan");
            SetHomeButtonAction(OpenMessenger);

            RectTransform root = PrepareDynamicMessengerRoot();
            ConfigureVerticalStack(root, 8f, new RectOffset(0, 0, 0, 0));

            CreateSmallButton(root, "BackToContacts", "< Messenger", OpenMessenger, ConversationBackButtonHeight);
            RectTransform content = CreateConversationScroll(root);
            CreateMessageBubble(content, "Chị Lan", "Dạo này học hành thế nào rồi?", false);
            CreateMessageBubble(content, "Nam", "Vẫn ổn chị ạ. Dạo này em hơi bận làm bài với Minh.", true, 92f);
            CreateMessageBubble(content, "Chị Lan", "Nhớ ăn uống đầy đủ nhé, đừng thức khuya quá.", false, 92f);
            CreateMessageBubble(content, "Nam", "Chị cũng vậy. Khi nào rảnh về nhà chơi với em.", true, 92f);
            CreateMessageBubble(content, "Chị Lan", "Ừ, chị biết rồi.", false);

            if (lanMessageReceived)
            {
                CreateAudioMessageBubble(content);
                lanChatRead = true;
                AdvanceMissionState(LanRecordingMissionState.RecordingOpened);
            }

            ForceMessengerLayout(root);
        }

        private void OpenSimpleConversation(string contactName)
        {
            activeMessengerContact = contactName;
            SetContentText(appTitleText, contactName);
            SetHomeButtonAction(OpenMessenger);

            RectTransform root = PrepareDynamicMessengerRoot();
            ConfigureVerticalStack(root, 8f, new RectOffset(0, 0, 0, 0));

            CreateSmallButton(root, "BackToContacts", "< Messenger", OpenMessenger, ConversationBackButtonHeight);
            RectTransform content = CreateConversationScroll(root);
            CreateMessageBubble(content, contactName, "Chưa có tin nhắn mới.", false);
            ForceMessengerLayout(root);
        }

        private void OpenDungConversation()
        {
            activeMessengerContact = Mission01DungConversation.DungContactId;
            SyncFirstMissionReferences();
            SetContentText(appTitleText, Mission01DungConversation.DungDisplayName);
            SetHomeButtonAction(OpenMessenger);

            RectTransform root = PrepareDynamicMessengerRoot();
            ConfigureVerticalStack(root, 8f, new RectOffset(0, 0, 0, 0));

            CreateSmallButton(root, "BackToContacts", "< Messenger", OpenMessenger, ConversationBackButtonHeight);
            RectTransform content = CreateConversationScroll(root);

            Chapter1SaveData data = firstMissionManager != null ? firstMissionManager.Data : null;
            List<Mission01DungMessage> messages = Mission01DungConversation.BuildMessages(data);
            for (int i = 0; i < messages.Count; i++)
            {
                Mission01DungMessage message = messages[i];
                CreateMessageBubble(content, message.Sender, message.Text, message.FromPlayer, GetMessageHeight(message.Text));
            }

            if (messages.Count == 0)
            {
                CreateMessageBubble(content, Mission01DungConversation.DungDisplayName, "Chưa có tin nhắn mới.", false);
            }

            List<Mission01DungChoice> choices = firstMissionManager != null
                ? Mission01DungConversation.BuildChoices(firstMissionManager.State, firstMissionManager.Data)
                : new List<Mission01DungChoice>();
            for (int i = 0; i < choices.Count; i++)
            {
                Mission01DungChoice choice = choices[i];
                CreateSmallButton(
                    root,
                    "DungChoice_" + choice,
                    Mission01DungConversation.GetChoiceText(choice),
                    () => HandleDungChoice(choice),
                    58f);
            }

            firstMissionManager?.ClearDungUnread();
            ForceMessengerLayout(root);
        }

        private string GetDungContactStatus()
        {
            SyncFirstMissionReferences();
            if (firstMissionManager == null)
            {
                return "Không có tin mới";
            }

            Chapter1SaveData data = firstMissionManager.Data;
            if (data.Mission01DungHasUnread)
            {
                return "Tin nhắn mới";
            }

            if (Mission01DungConversation.BuildChoices(firstMissionManager.State, data).Count > 0)
            {
                return "Có thể nhắn tin";
            }

            List<Mission01DungMessage> messages = Mission01DungConversation.BuildMessages(data);
            if (messages.Count > 0)
            {
                return messages[messages.Count - 1].Text;
            }

            return "Không có tin mới";
        }

        private string GetDungContactBadge()
        {
            SyncFirstMissionReferences();
            if (firstMissionManager == null)
            {
                return string.Empty;
            }

            return firstMissionManager.Data.Mission01DungHasUnread ? "1" : string.Empty;
        }

        private void HandleDungChoice(Mission01DungChoice choice)
        {
            SyncFirstMissionReferences();
            if (firstMissionManager == null)
            {
                Chapter1EventBus.RaiseNotification("Chưa tìm thấy Mission 01 Manager.");
                return;
            }

            switch (choice)
            {
                case Mission01DungChoice.BorrowAudioSeparator:
                    firstMissionManager.SendDungBorrowRequest();
                    break;
                case Mission01DungChoice.AskRoomPassword:
                    firstMissionManager.SendDungPasswordQuestion();
                    break;
                case Mission01DungChoice.AskBirthday:
                    firstMissionManager.SendDungBirthdayQuestion();
                    break;
            }

            OpenDungConversation();
            StartDungReplyRoutine(choice);
        }

        private void StartDungReplyRoutine(Mission01DungChoice choice)
        {
            if (dungReplyRoutine != null)
            {
                return;
            }

            dungReplyRoutine = StartCoroutine(ReceiveDungReplyAfterDelay(choice));
        }

        private IEnumerator ReceiveDungReplyAfterDelay(Mission01DungChoice choice)
        {
            yield return new WaitForSecondsRealtime(1f);

            if (firstMissionManager != null)
            {
                switch (choice)
                {
                    case Mission01DungChoice.BorrowAudioSeparator:
                        firstMissionManager.ReceiveDungBorrowReply();
                        break;
                    case Mission01DungChoice.AskRoomPassword:
                        firstMissionManager.ReceiveDungPasswordHint();
                        break;
                    case Mission01DungChoice.AskBirthday:
                        firstMissionManager.ReceiveDungBirthdayHint();
                        break;
                }
            }

            dungReplyRoutine = null;
            if (isOpen && string.Equals(activeMessengerContact, Mission01DungConversation.DungContactId, System.StringComparison.Ordinal))
            {
                OpenDungConversation();
            }
            else
            {
                PlayClip(tabClip);
            }
        }

        private static float GetMessageHeight(string text)
        {
            int length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            if (length > 95)
            {
                return 118f;
            }

            return length > 58 ? 94f : 74f;
        }

        private void ReceiveLanMessageIfReady()
        {
            if (!motherChatRead || lanMessageReceived)
            {
                return;
            }

            lanMessageReceived = true;
            AdvanceMissionState(LanRecordingMissionState.LanMessageReceived);
            Chapter1EventBus.RaiseNotification("Bạn có tin nhắn mới từ Chị Lan");
            PlayClip(tabClip);
        }

        private void ShowRecorderLibrary()
        {
            SetContentText(appTitleText, "Ghi âm");
            SetHomeButtonAction(ShowHomeScreen);

            RectTransform root = PrepareDynamicMessengerRoot();
            ConfigureVerticalStack(root, 10f, new RectOffset(8, 8, 8, 8));
            RectTransform listRoot = CreateConversationScroll(root);

            Chapter1SaveData data = GetCurrentSaveData();
            data?.EnsureValidDefaults();
            List<string> recordingIds = data != null ? data.SavedPhoneRecordingIds : new List<string>();
            if (recordingIds.Count == 0)
            {
                CreateRecorderEmptyState(listRoot);
                ForceMessengerLayout(root);
                return;
            }

            HashSet<string> renderedRecordingIds = new HashSet<string>();
            for (int i = 0; i < recordingIds.Count; i++)
            {
                if (LanAudioRecordingCatalog.IsKnownRecordingId(recordingIds[i]) && renderedRecordingIds.Add(recordingIds[i]))
                {
                    CreateRecordingCard(listRoot, recordingIds[i]);
                }
            }

            if (renderedRecordingIds.Count == 0)
            {
                CreateRecorderEmptyState(listRoot);
            }

            ForceMessengerLayout(root);
        }

        private void CreateRecorderEmptyState(RectTransform parent)
        {
            GameObject row = new GameObject("RecorderEmptyState", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = 96f;
            layout.minHeight = 96f;
            layout.flexibleHeight = 0f;

            RectTransform panel = CreateImage(row.transform, "EmptyPanel", new Color(0.12f, 0.125f, 0.14f, 0.92f)).rectTransform;
            Stretch(panel, Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = CreateText(panel, "EmptyText", "Chưa có bản ghi âm đã tải.", 18f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.color = new Color(0.82f, 0.82f, 0.86f, 1f);
        }

        private void CreateRecordingCard(RectTransform parent, string recordingId)
        {
            GameObject row = new GameObject("SavedRecordingRow_" + recordingId, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 138f;
            rowLayout.minHeight = 138f;
            rowLayout.flexibleHeight = 0f;

            RectTransform card = CreateImage(row.transform, "RecordingCard", new Color(0.135f, 0.14f, 0.155f, 0.98f)).rectTransform;
            Stretch(card, Vector2.zero, Vector2.zero);

            TextMeshProUGUI title = CreateText(card, "RecordingTitle", LanAudioRecordingCatalog.GetRecordingDisplayName(recordingId), 18f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(title.rectTransform, 16f, 86f, 16f, 14f);
            title.color = Color.white;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI subtitle = CreateText(card, "RecordingSubtitle", GetRecordingSubtitle(recordingId), 14f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(subtitle.rectTransform, 16f, 64f, 16f, 44f);
            subtitle.color = new Color(0.72f, 0.72f, 0.76f, 1f);
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;

            Button playButton = CreateButton(card, "RecordingPlayButton", "Play", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 32f), new Vector2(76f, 34f));
            StyleButtonLabel(playButton, 15f);
            playButton.onClick.RemoveAllListeners();

            GameObject sliderObject = new GameObject("RecordingProgress", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(card, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            Inset(sliderRect, 102f, 26f, 16f, 86f);
            Image track = CreateImage(sliderObject.transform, "Track", new Color(0.34f, 0.35f, 0.38f, 0.78f));
            Stretch(track.rectTransform, Vector2.zero, Vector2.zero);
            RectTransform fillArea = CreateEmpty(sliderObject.transform, "FillArea");
            Stretch(fillArea, Vector2.zero, Vector2.zero);
            Image fill = CreateImage(fillArea, "Fill", new Color(0.80f, 0.12f, 0.14f, 0.95f));
            Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = false;
            slider.targetGraphic = track;
            slider.fillRect = fill.rectTransform;

            TextMeshProUGUI timeText = CreateText(card, "RecordingTime", GetRecordingDurationText(recordingId), 13f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(timeText.rectTransform, 102f, 6f, 16f, 112f);
            timeText.color = new Color(0.78f, 0.78f, 0.82f, 1f);
            timeText.textWrappingMode = TextWrappingModes.NoWrap;

            playButton.onClick.AddListener(() => ToggleRecordingPlayback(recordingId, playButton, slider, timeText));
        }

        private RectTransform PrepareDynamicMessengerRoot()
        {
            ClearDynamicAppBody();
            if (appBodyText != null)
            {
                appBodyText.gameObject.SetActive(false);
            }

            Transform bodyPanel = appBodyText != null ? appBodyText.transform.parent : FindChild("AppBodyPanel")?.transform;
            if (bodyPanel == null)
            {
                bodyPanel = appContent != null ? appContent.transform : transform;
            }

            RectTransform root = CreateEmpty(bodyPanel, DynamicMessengerRootName);
            Stretch(root, new Vector2(0f, 0f), new Vector2(0f, 0f));
            return root;
        }

        private static void ConfigureVerticalStack(RectTransform root, float spacing, RectOffset padding)
        {
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }

        private static void ForceMessengerLayout(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private void ClearDynamicAppBody()
        {
            Transform bodyPanel = appBodyText != null ? appBodyText.transform.parent : FindChild("AppBodyPanel")?.transform;
            if (bodyPanel == null)
            {
                return;
            }

            for (int i = bodyPanel.childCount - 1; i >= 0; i--)
            {
                Transform child = bodyPanel.GetChild(i);
                if (child == null || !string.Equals(child.name, DynamicMessengerRootName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private Button CreateContactButton(RectTransform parent, string contactName, string preview, string badge, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(parent, "Contact_" + contactName, string.Empty, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = ContactRowHeight;
            layoutElement.minHeight = ContactRowHeight;
            layoutElement.flexibleHeight = 0f;

            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.color = string.IsNullOrEmpty(badge)
                    ? new Color(0.135f, 0.14f, 0.155f, 0.98f)
                    : new Color(0.16f, 0.165f, 0.185f, 0.98f);
            }

            RectTransform avatar = CreateImage(button.transform, "Avatar", new Color(0.34f, 0.36f, 0.42f, 1f)).rectTransform;
            SetRect(avatar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(44f, 44f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI avatarText = CreateText(avatar, "AvatarText", contactName.Substring(0, 1), 20f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            avatarText.color = Color.white;
            avatarText.textWrappingMode = TextWrappingModes.NoWrap;

            TextMeshProUGUI nameText = CreateText(button.transform, "Name", contactName, 19f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(nameText.rectTransform, 72f, 38f, 48f, 10f);
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.fontStyle = string.IsNullOrEmpty(badge) ? FontStyles.Normal : FontStyles.Bold;

            TextMeshProUGUI previewText = CreateText(button.transform, "Preview", preview, 14f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(previewText.rectTransform, 72f, 12f, 48f, 38f);
            previewText.color = new Color(0.72f, 0.72f, 0.76f, 1f);
            previewText.textWrappingMode = TextWrappingModes.NoWrap;
            previewText.overflowMode = TextOverflowModes.Ellipsis;

            if (!string.IsNullOrEmpty(badge))
            {
                RectTransform badgeRect = CreateImage(button.transform, "Badge", new Color(0.78f, 0.04f, 0.05f, 1f)).rectTransform;
                SetRect(badgeRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(26f, 26f), new Vector2(0.5f, 0.5f));
                TextMeshProUGUI badgeText = CreateText(badgeRect, "BadgeText", badge, 15f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                badgeText.color = Color.white;
                badgeText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            return button;
        }

        private Button CreateSmallButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction action, float height)
        {
            Button button = CreateButton(parent, name, label, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            layoutElement.flexibleHeight = 0f;
            TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (labelText != null)
            {
                labelText.fontSize = 18f;
                labelText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            return button;
        }

        private RectTransform CreateConversationScroll(RectTransform parent)
        {
            GameObject scrollObject = new GameObject("ConversationScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.preferredHeight = ConversationScrollHeight;
            scrollLayout.minHeight = ConversationScrollHeight;
            scrollLayout.flexibleHeight = 0f;
            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(0.08f, 0.085f, 0.095f, 0.35f);

            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.sizeDelta = Vector2.zero;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, Vector2.zero, Vector2.zero);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.02f);
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateEmpty(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalNormalizedPosition = 0f;
            return content;
        }

        private void CreateMessageBubble(RectTransform parent, string sender, string message, bool fromPlayer, float height = 74f)
        {
            GameObject row = new GameObject("MessageRow", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = height;
            rowLayout.minHeight = height;
            rowLayout.flexibleHeight = 0f;

            RectTransform bubble = CreateImage(row.transform, "Bubble", fromPlayer ? new Color(0.17f, 0.28f, 0.48f, 0.98f) : new Color(0.16f, 0.16f, 0.18f, 0.98f)).rectTransform;
            bubble.anchorMin = fromPlayer ? new Vector2(0.28f, 0f) : new Vector2(0f, 0f);
            bubble.anchorMax = fromPlayer ? new Vector2(1f, 1f) : new Vector2(0.72f, 1f);
            bubble.offsetMin = new Vector2(0f, 2f);
            bubble.offsetMax = new Vector2(0f, -2f);

            TextMeshProUGUI text = CreateText(bubble, "MessageText", sender + "\n" + message, 15f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(text.rectTransform, 12f, 10f, 12f, 10f);
            text.color = Color.white;
        }

        private void CreateAudioMessageBubble(RectTransform parent)
        {
            GameObject row = new GameObject("AudioMessageRow", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = AudioMessageRowHeight;
            rowLayout.minHeight = AudioMessageRowHeight;
            rowLayout.flexibleHeight = 0f;

            RectTransform bubble = CreateImage(row.transform, "AudioBubble", new Color(0.16f, 0.16f, 0.18f, 0.98f)).rectTransform;
            bubble.anchorMin = new Vector2(0f, 0f);
            bubble.anchorMax = new Vector2(0.96f, 1f);
            bubble.offsetMin = new Vector2(0f, 2f);
            bubble.offsetMax = new Vector2(0f, -2f);

            TextMeshProUGUI title = CreateText(bubble, "AudioTitle", "Chị Lan\nTin nhắn thoại", 15f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(title.rectTransform, 12f, 102f, 12f, 12f);
            title.color = Color.white;

            currentVoicePlayButton = CreateButton(bubble, "VoicePlayButton", "Play", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 74f), new Vector2(68f, 32f));
            StyleButtonLabel(currentVoicePlayButton, 15f);
            currentVoicePlayButton.onClick.RemoveAllListeners();
            currentVoicePlayButton.onClick.AddListener(() => ToggleRecordingPlayback(LanAudioRecordingCatalog.MixedRecordingId, currentVoicePlayButton, currentVoiceSlider, currentVoiceTimeText));

            GameObject sliderObject = new GameObject("VoiceProgress", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(bubble, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            Inset(sliderRect, 90f, 66f, 16f, 72f);
            Image track = CreateImage(sliderObject.transform, "Track", new Color(0.35f, 0.36f, 0.39f, 0.75f));
            Stretch(track.rectTransform, Vector2.zero, Vector2.zero);
            RectTransform fillArea = CreateEmpty(sliderObject.transform, "FillArea");
            Stretch(fillArea, Vector2.zero, Vector2.zero);
            Image fill = CreateImage(fillArea, "Fill", new Color(0.78f, 0.10f, 0.12f, 0.95f));
            Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);
            currentVoiceSlider = sliderObject.GetComponent<Slider>();
            currentVoiceSlider.minValue = 0f;
            currentVoiceSlider.maxValue = 1f;
            currentVoiceSlider.direction = Slider.Direction.LeftToRight;
            currentVoiceSlider.interactable = false;
            currentVoiceSlider.targetGraphic = track;
            currentVoiceSlider.fillRect = fill.rectTransform;

            currentVoiceTimeText = CreateText(bubble, "VoiceTime", GetLanVoiceDurationText(), 13f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Inset(currentVoiceTimeText.rectTransform, 12f, 18f, 112f, 98f);
            currentVoiceTimeText.textWrappingMode = TextWrappingModes.NoWrap;

            Button downloadButton = CreateButton(bubble, "VoiceDownloadButton", lanRecordingDownloaded ? "Đã tải" : "Tải về", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 28f), new Vector2(92f, 32f));
            StyleButtonLabel(downloadButton, 15f);
            downloadButton.interactable = !lanRecordingDownloaded;
            downloadButton.onClick.RemoveAllListeners();
            downloadButton.onClick.AddListener(() => DownloadLanRecording(downloadButton));
        }

        private RectTransform CreateAppContent(Transform parent)
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

        private Button CreateAppButton(Transform parent, string name, string label, string glyph, Color iconColor)
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

        private void SetLegacyPhoneUiActive(bool active)
        {
            SetActive(FindChild("Tabs"), active);
            SetActive(FindChild("ContentPanel"), active);
            SetActive(messagesContent, active);
            SetActive(recordingsContent, active);
            SetActive(cluesContent, active);
            SetActive(messagesHighlight, false);
            SetActive(recordingsHighlight, false);
            SetActive(cluesHighlight, false);
        }

        private void SyncMessengerStateFromMission()
        {
            if (missionController == null)
            {
                missionController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            SyncFirstMissionReferences();

            if (firstMissionManager != null)
            {
                Chapter1SaveData data = firstMissionManager.Data;
                data.EnsureValidDefaults();
                if (firstMissionManager.State >= FirstMissionState.GoToMinhRoom)
                {
                    if (data.AddPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId))
                    {
                        Chapter1Manager.Instance?.SaveChapter();
                    }
                }

                lanRecordingDownloaded |= data.HasPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId);
            }

            if (missionController == null)
            {
                return;
            }

            LanRecordingMissionState state = missionController.State;
            motherChatRead |= (int)state >= (int)LanRecordingMissionState.ReadMotherChat;
            lanMessageReceived |= (int)state >= (int)LanRecordingMissionState.LanMessageReceived;
            lanChatRead |= (int)state >= (int)LanRecordingMissionState.RecordingOpened;
            lanRecordingDownloaded |= (int)state >= (int)LanRecordingMissionState.RecordingDownloaded;
        }

        private void AdvanceMissionState(LanRecordingMissionState state)
        {
            if (missionController == null)
            {
                missionController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            if (missionController != null)
            {
                missionController.SetState(state);
            }
        }

        private void SyncFirstMissionReferences()
        {
            if (firstMissionManager == null)
            {
                firstMissionManager = Mission01AudioSeparatorManager.Instance;
            }

            if (firstMissionManager == null)
            {
                firstMissionManager = FindAnyObjectByType<Mission01AudioSeparatorManager>(FindObjectsInactive.Include);
            }

            if (mixerController == null)
            {
                mixerController = FindAnyObjectByType<AudioSeparatorMixerController>(FindObjectsInactive.Include);
            }
        }

        private Chapter1SaveData GetCurrentSaveData()
        {
            SyncFirstMissionReferences();
            Chapter1Manager chapterManager = Chapter1Manager.Instance;
            Chapter1SaveData data = firstMissionManager != null ? firstMissionManager.Data : chapterManager != null ? chapterManager.CurrentData : null;
            data?.EnsureValidDefaults();
            return data;
        }

        private string GetRecordingSubtitle(string recordingId)
        {
            if (string.Equals(recordingId, LanAudioRecordingCatalog.MixedRecordingId, System.StringComparison.Ordinal))
            {
                return "Bản ghi âm gốc đã tải từ Messenger";
            }

            return LanAudioRecordingCatalog.TryGetStemFromRecordingId(recordingId, out _)
                ? "Âm thanh đã tách bằng máy tách âm"
                : "Bản ghi âm trong điện thoại";
        }

        private AudioClip ResolveRecordingClip(string recordingId)
        {
            if (missionController == null)
            {
                missionController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            if (mixerController == null)
            {
                mixerController = FindAnyObjectByType<AudioSeparatorMixerController>(FindObjectsInactive.Include);
            }

            return LanAudioRecordingCatalog.ResolveClip(recordingId, missionController, mixerController);
        }

        private void ToggleRecordingPlayback(
            string recordingId,
            Button playButton,
            Slider progressSlider,
            TextMeshProUGUI timeText)
        {
            AudioClip clip = ResolveRecordingClip(recordingId);
            if (clip == null)
            {
                Chapter1EventBus.RaiseNotification("Chưa tìm thấy file ghi âm.");
                Debug.LogWarning($"[PhoneUIController] Missing AudioClip for recording '{recordingId}'. No fake AudioClip will be played.", this);
                return;
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (!string.Equals(currentRecordingId, recordingId, System.StringComparison.Ordinal))
            {
                StopVoicePlayback();
            }

            currentRecordingId = recordingId;
            currentVoiceClip = clip;
            currentVoiceSlider = progressSlider;
            currentVoiceTimeText = timeText;
            currentVoicePlayButton = playButton;

            if (audioSource.clip == clip && audioSource.isPlaying)
            {
                audioSource.Pause();
                SetButtonLabel(currentVoicePlayButton, "Play");
                return;
            }

            if (audioSource.clip == clip && audioSource.time > 0f)
            {
                audioSource.UnPause();
            }
            else
            {
                audioSource.clip = clip;
                audioSource.time = 0f;
                audioSource.Play();
            }

            NotifyIfVoiceRecordingListened(recordingId);
            SetButtonLabel(currentVoicePlayButton, "Pause");
            UpdateVoiceProgress();
        }

        private void NotifyIfVoiceRecordingListened(string recordingId)
        {
            if (string.Equals(recordingId, LanAudioRecordingCatalog.GetOutputRecordingId(LanAudioStemId.Voice), System.StringComparison.Ordinal))
            {
                SyncFirstMissionReferences();
                firstMissionManager?.NotifyLanVoiceRecordingListened();
            }
        }

        private string GetRecordingDurationText(string recordingId)
        {
            AudioClip clip = ResolveRecordingClip(recordingId);
            return clip != null ? "0:00 / " + FormatTime(clip.length) : "0:00 / --:--";
        }

        private AudioClip GetLanVoiceClip()
        {
            if (missionController == null)
            {
                missionController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            return missionController != null ? missionController.LanRecordingClip : null;
        }

        private void ToggleLanVoicePlayback()
        {
            AudioClip clip = GetLanVoiceClip();
            if (clip == null)
            {
                Chapter1EventBus.RaiseNotification("Chưa tìm thấy file ghi âm của Chị Lan.");
                Debug.LogWarning("[PhoneUIController] Missing Lan recording AudioClip; no fake AudioClip will be played.", this);
                return;
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (audioSource.clip == clip && audioSource.isPlaying)
            {
                audioSource.Pause();
                SetButtonLabel(currentVoicePlayButton, "Play");
                return;
            }

            if (audioSource.clip == clip && audioSource.time > 0f)
            {
                audioSource.UnPause();
            }
            else
            {
                audioSource.clip = clip;
                audioSource.time = 0f;
                audioSource.Play();
            }

            currentVoiceClip = clip;
            SetButtonLabel(currentVoicePlayButton, "Pause");
            UpdateVoiceProgress();
        }

        private void DownloadLanRecording(Button downloadButton)
        {
            lanRecordingDownloaded = true;
            Chapter1Manager chapterManager = Chapter1Manager.Instance;
            if (chapterManager != null)
            {
                chapterManager.CurrentData.EnsureValidDefaults();
                chapterManager.CurrentData.AddPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId);
                chapterManager.SaveChapter();
            }

            if (missionController != null)
            {
                missionController.MarkRecordingDownloaded();
            }
            else
            {
                Mission01AudioSeparatorManager.Instance?.NotifyLanRecordingSaved();
            }

            SetButtonLabel(downloadButton, "Đã tải");
            downloadButton.interactable = false;
            Chapter1EventBus.RaiseNotification("Đã tải đoạn ghi âm của Chị Lan");
        }

        private void UpdateVoiceProgress()
        {
            if (!isOpen || currentVoiceClip == null || currentVoiceSlider == null || audioSource == null)
            {
                return;
            }

            float length = Mathf.Max(0.01f, currentVoiceClip.length);
            currentVoiceSlider.value = Mathf.Clamp01(audioSource.time / length);
            if (currentVoiceTimeText != null)
            {
                currentVoiceTimeText.text = FormatTime(audioSource.time) + " / " + FormatTime(currentVoiceClip.length);
            }

            if (!audioSource.isPlaying && audioSource.clip == currentVoiceClip && audioSource.time >= currentVoiceClip.length - 0.05f)
            {
                SetButtonLabel(currentVoicePlayButton, "Play");
                audioSource.time = 0f;
            }
        }

        private void StopVoicePlayback()
        {
            if (audioSource != null && audioSource.clip == currentVoiceClip)
            {
                audioSource.Stop();
            }

            SetButtonLabel(currentVoicePlayButton, "Play");
            currentRecordingId = string.Empty;
            currentVoiceClip = null;
            currentVoiceSlider = null;
            currentVoiceTimeText = null;
            currentVoicePlayButton = null;
        }

        private string GetLanVoiceDurationText()
        {
            return GetRecordingDurationText(LanAudioRecordingCatalog.MixedRecordingId);
        }

        private static string FormatTime(float seconds)
        {
            int safeSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (safeSeconds / 60) + ":" + (safeSeconds % 60).ToString("00");
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TextMeshProUGUI text = GetButtonLabel(button);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void StyleButtonLabel(Button button, float fontSize)
        {
            TextMeshProUGUI text = GetButtonLabel(button);
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static TextMeshProUGUI GetButtonLabel(Button button)
        {
            if (button == null)
            {
                return null;
            }

            return button.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void SetHomeButtonAction(UnityEngine.Events.UnityAction action)
        {
            if (homeButton == null)
            {
                return;
            }

            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(action);
        }

        private GameObject FindChild(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && string.Equals(children[i].name, childName, System.StringComparison.Ordinal))
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && string.Equals(components[i].name, childName, System.StringComparison.Ordinal))
                {
                    return components[i];
                }
            }

            return null;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetActive(Behaviour target, bool active)
        {
            if (target != null)
            {
                target.enabled = active;
            }
        }

        private static void SetContentText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
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
                CreateText(
                    buttonObject.transform,
                    "Label",
                    label,
                    22f,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
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

        private static void Inset(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
