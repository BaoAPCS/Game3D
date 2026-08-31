using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DormitoryMystery.Chapter1.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class Mission01AudioSeparatorEditModeTests
    {
        [Test]
        public void RecordingSavedSetsObjectiveToVisitMinh()
        {
            using TestRig rig = new TestRig();

            rig.Mission.NotifyLanRecordingSaved();

            Assert.AreEqual(FirstMissionState.GoToMinhRoom, rig.Mission.State);
            Assert.AreEqual(
                "Qua phòng Minh để nhờ Minh kiểm tra đoạn ghi âm.",
                rig.Mission.CurrentObjective);
        }

        [Test]
        public void MinhIntroMovesMissionToDungMessageStep()
        {
            using TestRig rig = new TestRig();
            rig.Mission.NotifyLanRecordingSaved();

            Assert.IsTrue(rig.Mission.TryStartMinhIntroDialogue());
            Assert.AreEqual(FirstMissionState.TalkToMinh, rig.Mission.State);

            rig.Mission.CompleteMinhIntroDialogue();

            Assert.AreEqual(FirstMissionState.MessageDung, rig.Mission.State);
            Assert.IsTrue(rig.Data.Mission01MinhIntroDialoguePlayed);
        }

        [Test]
        public void BorrowChoiceSendsOnceAndDungRepliesWithFloorTwoRoom()
        {
            using TestRig rig = new TestRig();
            MoveToMessageDung(rig.Mission);

            rig.Mission.SendDungBorrowRequest();
            rig.Mission.SendDungBorrowRequest();
            List<Mission01DungMessage> sentMessages = Mission01DungConversation.BuildMessages(rig.Data);

            Assert.AreEqual(1, CountMessages(sentMessages, "Nam", Mission01DungConversation.BorrowQuestion));

            rig.Mission.ReceiveDungBorrowReply();
            List<Mission01DungMessage> repliedMessages = Mission01DungConversation.BuildMessages(rig.Data);

            Assert.AreEqual(FirstMissionState.GoToDungRoom, rig.Mission.State);
            Assert.IsTrue(ContainsText(repliedMessages, "giữa lầu 2"));
        }

        [Test]
        public void LockedDoorUnlocksPasswordQuestionChoice()
        {
            using TestRig rig = new TestRig();
            MoveToGoToDungRoom(rig.Mission);

            rig.Mission.DiscoverLockedDoor();

            Assert.AreEqual(FirstMissionState.DiscoverLockedDoor, rig.Mission.State);
            List<Mission01DungChoice> choices = Mission01DungConversation.BuildChoices(rig.Mission.State, rig.Data);
            Assert.Contains(Mission01DungChoice.AskRoomPassword, choices);
        }

        [Test]
        public void PasswordAndBirthdayHintsNeverRevealDirectAnswer()
        {
            using TestRig rig = new TestRig();
            MoveToGoToDungRoom(rig.Mission);
            rig.Mission.DiscoverLockedDoor();
            rig.Mission.SendDungPasswordQuestion();
            rig.Mission.ReceiveDungPasswordHint();
            rig.Mission.SendDungBirthdayQuestion();
            rig.Mission.ReceiveDungBirthdayHint();

            Assert.AreEqual(FirstMissionState.SolveBirthdayPassword, rig.Mission.State);
            List<Mission01DungMessage> messages = Mission01DungConversation.BuildMessages(rig.Data);
            for (int i = 0; i < messages.Count; i++)
            {
                Assert.IsFalse(
                    Mission01DungConversation.ContainsForbiddenDirectAnswer(messages[i].Text),
                    "Message leaked direct password answer: " + messages[i].Text);
            }

            Assert.IsFalse(Mission01DungConversation.ContainsForbiddenDirectAnswer(rig.Mission.CurrentObjective));
        }

        [Test]
        public void WrongPasswordDoesNotUnlockAndCorrectPasswordEntersDungRoom()
        {
            using TestRig rig = new TestRig();
            MoveToSolveBirthdayPassword(rig.Mission);

            Assert.IsFalse(rig.Mission.TryUnlockDungDoor("1234"));
            Assert.IsFalse(rig.Mission.DoorUnlocked);
            Assert.AreEqual(FirstMissionState.SolveBirthdayPassword, rig.Mission.State);

            Assert.IsTrue(rig.Mission.TryUnlockDungDoor("2502"));
            Assert.IsTrue(rig.Mission.DoorUnlocked);
            Assert.AreEqual(FirstMissionState.EnterDungRoom, rig.Mission.State);
            Assert.AreEqual("Dùng máy tách âm để xử lý đoạn ghi âm của Chị Lan.", rig.Mission.CurrentObjective);
        }

        [Test]
        public void CorrectPasswordDoesNotSkipMissionBeforeBirthdayHint()
        {
            using TestRig rig = new TestRig();
            MoveToGoToDungRoom(rig.Mission);

            Assert.IsFalse(rig.Mission.TryUnlockDungDoor("2502"));
            Assert.IsFalse(rig.Mission.DoorUnlocked);
            Assert.AreEqual(FirstMissionState.GoToDungRoom, rig.Mission.State);

            rig.Mission.DiscoverLockedDoor();
            Assert.IsFalse(rig.Mission.TryUnlockDungDoor("2502"));
            Assert.AreEqual(FirstMissionState.DiscoverLockedDoor, rig.Mission.State);
        }

        [Test]
        public void SavingAllSeparatedStemsUnlocksReturnToMinhObjective()
        {
            using TestRig rig = new TestRig();
            MoveToSolveBirthdayPassword(rig.Mission);
            rig.Mission.TryUnlockDungDoor("2502");
            Assert.IsTrue(rig.Mission.StartAudioSeparatorProcessing());

            SaveAllLanStems(rig.Data);
            rig.Mission.NotifyAllLanAudioStemsSaved();

            Assert.AreEqual(FirstMissionState.ReturnToMinh, rig.Mission.State);
            Assert.IsTrue(rig.Mission.LanRecordingSeparated);
            Assert.IsFalse(rig.Data.Mission01AudioSeparatorCollected);
        }

        [Test]
        public void ReturningDeviceToMinhCompletesMissionOnce()
        {
            using TestRig rig = new TestRig();
            MoveToSolveBirthdayPassword(rig.Mission);
            rig.Mission.TryUnlockDungDoor("2502");
            rig.Mission.StartAudioSeparatorProcessing();
            SaveAllLanStems(rig.Data);
            rig.Mission.NotifyAllLanAudioStemsSaved();

            Assert.IsTrue(rig.Mission.TryCompleteWithMinh(rig.Inventory));
            Assert.AreEqual(FirstMissionState.Completed, rig.Mission.State);

            Assert.IsFalse(rig.Mission.TryCompleteWithMinh(rig.Inventory));
        }

        [Test]
        public void ReturningToMinhAfterSixSavedStemsUsesTask2Dialogue()
        {
            using TestRig rig = new TestRig();
            MoveToSolveBirthdayPassword(rig.Mission);
            rig.Mission.TryUnlockDungDoor("2502");
            rig.Mission.StartAudioSeparatorProcessing();
            SaveAllLanStems(rig.Data);
            rig.Mission.NotifyAllLanAudioStemsSaved();

            GameObject minhObject = new GameObject("MinhTask2DialogueTest");
            try
            {
                MinhDialogueInteractable dialogue =
                    minhObject.AddComponent<MinhDialogueInteractable>();
                System.Type dialogueType = typeof(MinhDialogueInteractable);
                SetPrivateField(dialogue, "mission01Manager", rig.Mission);

                MethodInfo determineMode = dialogueType.GetMethod(
                    "DetermineMissionDialogueMode",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo buildDialogue = dialogueType.GetMethod(
                    "BuildMissionDialogue",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(determineMode);
                Assert.NotNull(buildDialogue);

                object mode = determineMode.Invoke(
                    dialogue,
                    new object[] { rig.Inventory });
                Assert.AreEqual("Completion", mode.ToString());

                IEnumerable lines = buildDialogue.Invoke(
                    dialogue,
                    new[] { mode }) as IEnumerable;
                Assert.NotNull(lines);

                List<string> texts = new List<string>();
                foreach (object line in lines)
                {
                    PropertyInfo textProperty = line.GetType().GetProperty(
                        "Text",
                        BindingFlags.Instance | BindingFlags.Public);
                    Assert.NotNull(textProperty);
                    texts.Add(textProperty.GetValue(line) as string);
                }

                Assert.AreEqual(8, texts.Count);
                StringAssert.Contains("PSU", texts[4]);
                StringAssert.Contains("UPS", texts[4]);
                StringAssert.Contains("ắc quy", texts[6]);
            }
            finally
            {
                Object.DestroyImmediate(minhObject);
            }
        }

        [Test]
        public void MainPhonePrefabBuildsAndOpensEveryApp()
        {
            const string phonePrefabPath =
                "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                phonePrefabPath);
            Assert.NotNull(prefab, "Missing main PhonePanel prefab.");

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab)
                as GameObject;
            Assert.NotNull(instance);

            try
            {
                PhoneUIController controller =
                    instance.GetComponent<PhoneUIController>();
                Assert.NotNull(controller);

                controller.OpenPhone();
                Assert.IsTrue(controller.IsOpen);

                CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
                Assert.NotNull(canvasGroup);
                Assert.IsTrue(canvasGroup.interactable);
                Assert.IsTrue(canvasGroup.blocksRaycasts);

                AssertPhoneAppOpens(
                    instance,
                    controller,
                    "MessengerButton",
                    "Messenger");
                AssertPhoneAppOpens(
                    instance,
                    controller,
                    "RecorderButton",
                    "Ghi \u00E2m");
                AssertPhoneAppOpens(
                    instance,
                    controller,
                    "CameraButton",
                    "Camera");
                AssertPhoneAppOpens(
                    instance,
                    controller,
                    "GoogleButton",
                    "Google");

                controller.ClosePhone();
                Assert.IsFalse(controller.IsOpen);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void MinhFirstDialogueAlwaysUsesMainTaskOneVersion()
        {
            GameObject minhObject = new GameObject("MinhDialogueTest");
            try
            {
                MinhDialogueInteractable dialogue =
                    minhObject.AddComponent<MinhDialogueInteractable>();
                System.Type dialogueType =
                    typeof(MinhDialogueInteractable);
                System.Type modeType = dialogueType.GetNestedType(
                    "MinhMissionDialogueMode",
                    BindingFlags.NonPublic);
                MethodInfo buildDialogue = dialogueType.GetMethod(
                    "BuildMissionDialogue",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(modeType);
                Assert.NotNull(buildDialogue);

                object defaultMode = System.Enum.Parse(
                    modeType,
                    "Default");
                IEnumerable lines = buildDialogue.Invoke(
                    dialogue,
                    new[] { defaultMode }) as IEnumerable;
                Assert.NotNull(lines);

                List<string> texts = new List<string>();
                foreach (object line in lines)
                {
                    PropertyInfo textProperty = line.GetType().GetProperty(
                        "Text",
                        BindingFlags.Instance | BindingFlags.Public);
                    Assert.NotNull(textProperty);
                    texts.Add(textProperty.GetValue(line) as string);
                }

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "Đoạn ghi âm này nhiều tạp âm quá. Nghe thế này thì không thể biết chị Lan đang nói gì được.",
                        "Phải dùng máy tách âm để lọc giọng nói ra.",
                        "Dũng có một cái. Cậu nhắn hỏi mượn thử xem."
                    },
                    texts);
                CollectionAssert.DoesNotContain(
                    texts,
                    "Tôi cần bạn tách đoạn ghi âm này");
            }
            finally
            {
                Object.DestroyImmediate(minhObject);
            }
        }

        [Test]
        public void FirstMinhDialogueUnlocksDungMessageButton()
        {
            using TestRig rig = new TestRig();
            GameObject minhObject = new GameObject("MinhDialogueStateTest");
            GameObject phoneInstance = null;

            try
            {
                MinhDialogueInteractable dialogue =
                    minhObject.AddComponent<MinhDialogueInteractable>();
                System.Type dialogueType =
                    typeof(MinhDialogueInteractable);
                SetPrivateField(
                    dialogue,
                    "mission01Manager",
                    rig.Mission);

                MethodInfo determineMode = dialogueType.GetMethod(
                    "DetermineMissionDialogueMode",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyOutcome = dialogueType.GetMethod(
                    "ApplyMissionDialogueOutcome",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(determineMode);
                Assert.NotNull(applyOutcome);

                object introMode = determineMode.Invoke(
                    dialogue,
                    new object[] { rig.Inventory });
                Assert.AreEqual("Intro", introMode.ToString());
                Assert.AreEqual(
                    FirstMissionState.TalkToMinh,
                    rig.Mission.State);

                SetPrivateField(dialogue, "activeMissionMode", introMode);
                SetPrivateField(
                    dialogue,
                    "activeInventory",
                    rig.Inventory);
                applyOutcome.Invoke(dialogue, null);

                Assert.AreEqual(
                    FirstMissionState.MessageDung,
                    rig.Mission.State);
                CollectionAssert.Contains(
                    Mission01DungConversation.BuildChoices(
                        rig.Mission.State,
                        rig.Data),
                    Mission01DungChoice.BorrowAudioSeparator);

                GameObject phonePrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab");
                Assert.NotNull(phonePrefab);
                phoneInstance = PrefabUtility.InstantiatePrefab(phonePrefab)
                    as GameObject;
                Assert.NotNull(phoneInstance);

                PhoneUIController phone =
                    phoneInstance.GetComponent<PhoneUIController>();
                Assert.NotNull(phone);
                phone.OpenPhone();

                MethodInfo openDungConversation =
                    typeof(PhoneUIController).GetMethod(
                        "OpenDungConversation",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(openDungConversation);
                openDungConversation.Invoke(phone, null);

                GameObject sendButton = FindChild(
                    phoneInstance,
                    "DungChoice_BorrowAudioSeparator");
                Assert.NotNull(
                    sendButton,
                    "Dung chat did not create the send-message button.");
                Assert.IsTrue(sendButton.GetComponent<Button>().interactable);

                phone.ClosePhone();
            }
            finally
            {
                if (phoneInstance != null)
                {
                    Object.DestroyImmediate(phoneInstance);
                }

                Object.DestroyImmediate(minhObject);
            }
        }

        [Test]
        public void SetupToolCanRunTwiceWithoutDuplicateMissionObjects()
        {
            Mission01AudioSeparatorSetupTool.SetupResult first =
                Mission01AudioSeparatorSetupTool.RunSetup(false);
            Mission01AudioSeparatorSetupTool.SetupResult second =
                Mission01AudioSeparatorSetupTool.RunSetup(false);

            Assert.AreEqual(0, first.FailCount, first.ToMarkdown());
            Assert.AreEqual(0, second.FailCount, second.ToMarkdown());

            Scene scene = GetLoadedScene(Mission01AudioSeparatorSetupTool.ScenePath);
            Assert.IsTrue(scene.IsValid() && scene.isLoaded, "Mission scene was not loaded by setup.");
            Assert.LessOrEqual(CountSceneObjects(scene, "Mission01AudioSeparatorManager"), 1);
            Assert.LessOrEqual(CountSceneObjects(scene, "DungRoom"), 1);
            Assert.LessOrEqual(CountSceneObjects(scene, "AudioSeparator_Device"), 1);
        }

        private static void MoveToMessageDung(Mission01AudioSeparatorManager mission)
        {
            mission.NotifyLanRecordingSaved();
            mission.TryStartMinhIntroDialogue();
            mission.CompleteMinhIntroDialogue();
        }

        private static void MoveToGoToDungRoom(Mission01AudioSeparatorManager mission)
        {
            MoveToMessageDung(mission);
            mission.SendDungBorrowRequest();
            mission.ReceiveDungBorrowReply();
        }

        private static void MoveToSolveBirthdayPassword(Mission01AudioSeparatorManager mission)
        {
            MoveToGoToDungRoom(mission);
            mission.DiscoverLockedDoor();
            mission.SendDungPasswordQuestion();
            mission.ReceiveDungPasswordHint();
            mission.SendDungBirthdayQuestion();
            mission.ReceiveDungBirthdayHint();
        }

        private static void SaveAllLanStems(Chapter1SaveData data)
        {
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                data.AddSavedStem(LanAudioRecordingCatalog.StemOrder[i]);
            }
        }

        private static void AssertPhoneAppOpens(
            GameObject phoneRoot,
            PhoneUIController controller,
            string buttonName,
            string expectedTitle)
        {
            controller.ShowHomeScreen();

            GameObject homeScreen = FindChild(phoneRoot, "HomeScreen");
            GameObject appContent = FindChild(phoneRoot, "AppContent");
            Button button = FindChild(phoneRoot, buttonName)
                ?.GetComponent<Button>();
            TextMeshProUGUI title = FindChild(phoneRoot, "AppTitleText")
                ?.GetComponent<TextMeshProUGUI>();

            Assert.NotNull(homeScreen);
            Assert.NotNull(appContent);
            Assert.NotNull(button, $"Missing runtime button {buttonName}.");
            Assert.NotNull(title);
            Assert.IsTrue(button.interactable);

            button.onClick.Invoke();

            Assert.IsFalse(homeScreen.activeSelf);
            Assert.IsTrue(appContent.activeSelf);
            Assert.AreEqual(expectedTitle, title.text);
        }

        private static GameObject FindChild(
            GameObject root,
            string objectName)
        {
            Transform[] hierarchy =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < hierarchy.Length; i++)
            {
                if (hierarchy[i] != null &&
                    hierarchy[i].name == objectName)
                {
                    return hierarchy[i].gameObject;
                }
            }

            return null;
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Missing private field {fieldName}.");
            field.SetValue(target, value);
        }

        private static int CountMessages(List<Mission01DungMessage> messages, string sender, string text)
        {
            int count = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Sender == sender && messages[i].Text == text)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ContainsText(List<Mission01DungMessage> messages, string text)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Text.Contains(text))
                {
                    return true;
                }
            }

            return false;
        }

        private static Scene GetLoadedScene(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.path == path)
                {
                    return scene;
                }
            }

            return default;
        }

        private static int CountSceneObjects(Scene scene, string objectName)
        {
            int count = 0;
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private sealed class TestRig : System.IDisposable
        {
            private readonly GameObject root;

            public TestRig()
            {
                root = new GameObject("Mission01_TestRig");
                ChapterManager = root.AddComponent<Chapter1Manager>();
                Mission = root.AddComponent<Mission01AudioSeparatorManager>();
                Inventory = root.AddComponent<PlayerInventory>();

                SetBool(ChapterManager, "autoLoadOnAwake", false);
                SetBool(ChapterManager, "autoSaveOnMilestones", false);
                SetPrivateField(
                    ChapterManager,
                    "saveService",
                    new NoOpChapter1SaveService());
                SetBool(Mission, "autoSaveOnChange", false);
                SetBool(Inventory, "autoSaveOnChange", false);

                Mission.SetChapterManager(ChapterManager);
                Inventory.SetChapterManager(ChapterManager);
            }

            public Chapter1Manager ChapterManager { get; }
            public Mission01AudioSeparatorManager Mission { get; }
            public PlayerInventory Inventory { get; }
            public Chapter1SaveData Data => ChapterManager.CurrentData;

            public void Dispose()
            {
                Object.DestroyImmediate(root);
            }

            private static void SetBool(Object target, string propertyName, bool value)
            {
                SerializedObject serialized = new SerializedObject(target);
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property != null)
                {
                    property.boolValue = value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private sealed class NoOpChapter1SaveService : IChapter1SaveService
        {
            public string SavePath => string.Empty;

            public void Save(Chapter1SaveData data)
            {
            }

            public Chapter1SaveData Load()
            {
                return Chapter1SaveData.CreateDefault();
            }

            public bool HasSave()
            {
                return false;
            }

            public void DeleteSave()
            {
            }
        }
    }
}
