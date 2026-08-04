using System.Collections.Generic;
using DormitoryMystery.Chapter1.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public void SavingAllSeparatedStemsUnlocksVoiceListenObjective()
        {
            using TestRig rig = new TestRig();
            MoveToSolveBirthdayPassword(rig.Mission);
            rig.Mission.TryUnlockDungDoor("2502");
            Assert.IsTrue(rig.Mission.StartAudioSeparatorProcessing());

            SaveAllLanStems(rig.Data);
            rig.Mission.NotifyAllLanAudioStemsSaved();

            Assert.AreEqual(FirstMissionState.ListenToLanVoice, rig.Mission.State);
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
            rig.Mission.NotifyLanVoiceRecordingListened();

            Assert.IsTrue(rig.Mission.TryCompleteWithMinh(rig.Inventory));
            Assert.AreEqual(FirstMissionState.Completed, rig.Mission.State);

            Assert.IsFalse(rig.Mission.TryCompleteWithMinh(rig.Inventory));
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
    }
}
