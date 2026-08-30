using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class Chapter1CheckpointPolicyEditModeTests
    {
        [Test]
        public void BuildSettings_StartAtChapter1_AndIncludeChapter2()
        {
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 2);
            Assert.IsTrue(scenes[0].enabled);
            Assert.AreEqual(
                "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity",
                scenes[0].path);
            Assert.IsTrue(scenes[1].enabled);
            Assert.AreEqual(
                "Assets/Chapter2/Scenes/Police_Station.unity",
                scenes[1].path);
        }

        [Test]
        public void PoliceStationKey_IsCarryOverStoryItem_NotUsable()
        {
            ItemDefinition key = AssetDatabase.LoadAssetAtPath<
                ItemDefinition>(
                "Assets/Chapter1/Resources/Inventory/" +
                "PoliceStationKeyItem.asset");

            Assert.IsNotNull(key);
            Assert.AreEqual(
                Chapter1SaveData.PoliceKeyCarryOverItemId,
                key.ItemId);
            Assert.IsFalse(key.IsUsable);
            StringAssert.Contains(
                "cất giấu thật kĩ",
                key.Description);
            StringAssert.DoesNotContain(
                "phòng giam",
                key.Description);
        }

        [Test]
        public void Mission1Snapshot_ResetsAllRuntimeProgressWithoutMutation()
        {
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission1Start;

            Chapter1SaveData snapshot =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);

            Assert.AreEqual(
                Chapter1SaveData.CurrentSaveVersion,
                snapshot.SaveVersion);
            Assert.AreEqual(
                Chapter1MissionCheckpoint.Mission1Start,
                snapshot.CurrentCheckpointId);
            Assert.AreEqual(FirstMissionState.None,
                (FirstMissionState)snapshot.FirstMissionStateValue);
            Assert.IsFalse(snapshot.Mission02Started);
            Assert.IsFalse(snapshot.Mission03PoliceKeyReceived);
            Assert.IsEmpty(snapshot.SavedLanAudioStemIds);
            Assert.IsEmpty(snapshot.CollectedUniqueItemIds);

            Assert.IsTrue(runtime.Mission03PoliceKeyReceived);
            Assert.AreEqual("runtime-item",
                runtime.CollectedUniqueItemIds[0]);
        }

        [Test]
        public void Mission2Snapshot_KeepsSeparatedAudioAndRollsBackLaterTasks()
        {
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission2Start;

            Chapter1SaveData snapshot =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);

            Assert.AreEqual(FirstMissionState.ReturnToMinh,
                (FirstMissionState)snapshot.FirstMissionStateValue);
            Assert.IsTrue(snapshot.AreAllLanAudioStemsSaved());
            Assert.IsTrue(snapshot.Mission01LanRecordingSeparated);
            Assert.IsTrue(snapshot.Mission01AudioSeparatorMixerCompleted);
            Assert.IsTrue(snapshot.Mission01DungDoorUnlocked);
            Assert.IsTrue(snapshot.Mission01AudioSeparatorTutorialSeen);
            Assert.IsFalse(snapshot.Mission01CompletionDialoguePlayed);
            Assert.IsFalse(snapshot.Mission01Completed);
            Assert.IsFalse(snapshot.Mission02Started);
            Assert.IsFalse(snapshot.Mission02HasPsu);
            Assert.IsFalse(snapshot.Mission03JamesIntroPlayed);
            Assert.IsFalse(snapshot.Mission03PoliceKeyReceived);
        }

        [Test]
        public void Mission3Snapshot_KeepsEquipmentAndClearsJamesHenryProgress()
        {
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission3Start;

            Chapter1SaveData snapshot =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);

            Assert.IsTrue(snapshot.Mission02Started);
            Assert.IsTrue(snapshot.Mission02HasPsu);
            Assert.IsTrue(snapshot.Mission02HasUps);
            Assert.IsTrue(snapshot.Mission02HasHenryBattery);
            Assert.IsFalse(snapshot.Mission02HasBrokenBattery);
            Assert.IsFalse(snapshot.Mission02EquipmentDelivered);
            Assert.IsFalse(snapshot.Mission03JamesIntroPlayed);
            Assert.IsFalse(snapshot.Mission03GangHostile);
            Assert.IsFalse(snapshot.Mission03PoliceKeyReceived);
            Assert.IsFalse(snapshot.Mission03HenryConfrontationCompleted);
            Assert.IsFalse(snapshot.Mission03HenryDefeated);
            Assert.IsFalse(snapshot.Mission03PoliceArrestCompleted);
            Assert.IsEmpty(snapshot.CollectedUniqueItemIds);
        }

        [Test]
        public void Snapshot_IsDeepCopiedAndIdempotent()
        {
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission2Start;

            Chapter1SaveData first =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);
            Chapter1SaveData second =
                Chapter1CheckpointPolicy.CreateSnapshot(first);

            Assert.AreEqual(
                JsonUtility.ToJson(first),
                JsonUtility.ToJson(second));
            Assert.AreNotSame(
                first.SavedPhoneRecordingIds,
                second.SavedPhoneRecordingIds);
            Assert.AreNotSame(
                first.SavedLanAudioStemIds,
                second.SavedLanAudioStemIds);
            Assert.AreNotSame(
                first.AudioSeparatorFaderValues,
                second.AudioSeparatorFaderValues);
        }

        [Test]
        public void CompletedSnapshot_RetainsCarryOverAndChapter2State()
        {
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission3Start;
            runtime.ChapterCompleted = true;
            runtime.Mission03PoliceArrestCompleted = true;

            Chapter1SaveData snapshot =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);
            Chapter1SaveData second =
                Chapter1CheckpointPolicy.CreateSnapshot(snapshot);

            Assert.IsTrue(snapshot.ChapterCompleted);
            Assert.IsTrue(snapshot.Mission03PoliceArrestCompleted);
            Assert.IsTrue(snapshot.Mission03PoliceKeyReceived);
            Assert.IsTrue(snapshot.HasChapterCarryOverItem(
                Chapter1SaveData.PhoneCarryOverItemId));
            Assert.IsTrue(snapshot.HasChapterCarryOverItem(
                Chapter1SaveData.PoliceKeyCarryOverItemId));
            Assert.AreEqual(
                JsonUtility.ToJson(snapshot),
                JsonUtility.ToJson(second));
            Assert.AreNotSame(
                snapshot.ChapterCarryOverItemIds,
                second.ChapterCarryOverItemIds);
        }

        [Test]
        public void Manager_CommitIsMonotonic_AndDeleteSuppressesPersistence()
        {
            GameObject owner = new GameObject("CheckpointManagerTest");
            owner.SetActive(false);
            Chapter1Manager manager = owner.AddComponent<Chapter1Manager>();
            FakeSaveService service = new FakeSaveService();
            Chapter1SaveData runtime = Chapter1SaveData.CreateDefault();
            SetPrivateField(manager, "saveService", service);
            SetPrivateField(manager, "currentData", runtime);

            try
            {
                Assert.IsFalse(manager.CommitMissionCheckpoint(
                    Chapter1MissionCheckpoint.Mission3Start),
                    "Mission 3 must not be fabricated from a fresh save.");

                runtime.FirstMissionStateValue =
                    (int)FirstMissionState.ReturnToMinh;
                runtime.Mission01LanRecordingSeparated = true;
                runtime.Mission01AudioSeparatorMixerCompleted = true;
                SaveAllLanStems(runtime);
                Assert.IsTrue(manager.CommitMissionCheckpoint(
                    Chapter1MissionCheckpoint.Mission2Start));

                runtime.Mission02Started = true;
                runtime.Mission02HasPsu = true;
                runtime.Mission02HasUps = true;
                runtime.Mission02HasHenryBattery = true;
                Assert.IsTrue(manager.CommitMissionCheckpoint(
                    Chapter1MissionCheckpoint.Mission3Start));
                Assert.IsFalse(manager.CommitMissionCheckpoint(
                    Chapter1MissionCheckpoint.Mission2Start));
                Assert.AreEqual(
                    Chapter1MissionCheckpoint.Mission3Start,
                    runtime.CurrentCheckpointId);

                manager.DeleteTestSaveForNextSession();
                Assert.IsTrue(manager.PersistenceSuppressedForSession);
                Assert.AreSame(runtime, manager.CurrentData);

                int savesBeforeSuppression = service.SaveCount;
                manager.SaveChapter();
                Assert.AreEqual(savesBeforeSuppression, service.SaveCount);
                Assert.IsFalse(manager.CommitMissionCheckpoint(
                    Chapter1MissionCheckpoint.Mission3Start));
                Assert.AreEqual(1, service.DeleteCount);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Manager_SavePersistsDetachedCanonicalData()
        {
            GameObject owner = new GameObject("CheckpointSaveCopyTest");
            owner.SetActive(false);
            Chapter1Manager manager = owner.AddComponent<Chapter1Manager>();
            FakeSaveService service = new FakeSaveService();
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission1Start;
            SetPrivateField(manager, "saveService", service);
            SetPrivateField(manager, "currentData", runtime);

            try
            {
                manager.SaveChapter();

                Assert.AreSame(runtime, manager.CurrentData);
                Assert.IsTrue(runtime.Mission03PoliceKeyReceived);
                Assert.AreNotSame(runtime, service.LastSavedData);
                Assert.AreNotSame(
                    runtime.CollectedUniqueItemIds,
                    service.LastSavedData.CollectedUniqueItemIds);
                Assert.IsFalse(
                    service.LastSavedData.Mission03PoliceKeyReceived);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Manager_Mission2StartObjective_ReturnsToMinh()
        {
            GameObject owner = new GameObject("CheckpointObjectiveTest");
            owner.SetActive(false);
            Chapter1Manager manager = owner.AddComponent<Chapter1Manager>();
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission2Start;
            Chapter1SaveData checkpoint =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);
            SetPrivateField(manager, "currentData", checkpoint);

            try
            {
                Assert.AreEqual(
                    Mission01AudioSeparatorManager.GetObjective(
                        FirstMissionState.ReturnToMinh),
                    manager.GetCurrentObjective());
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Manager_DeleteFallbackOverwritesSurvivingSaveWithTask1()
        {
            GameObject owner = new GameObject("CheckpointDeleteFallbackTest");
            owner.SetActive(false);
            Chapter1Manager manager = owner.AddComponent<Chapter1Manager>();
            FakeSaveService service = new FakeSaveService
            {
                SaveSurvivesDelete = true
            };
            Chapter1SaveData runtime = CreateDirtyRuntimeData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission3Start;
            SetPrivateField(manager, "saveService", service);
            SetPrivateField(manager, "currentData", runtime);

            try
            {
                manager.DeleteTestSaveForNextSession();

                Assert.IsTrue(manager.PersistenceSuppressedForSession);
                Assert.AreSame(runtime, manager.CurrentData);
                Assert.AreEqual(1, service.DeleteCount);
                Assert.AreEqual(1, service.SaveCount);
                Assert.AreEqual(
                    Chapter1MissionCheckpoint.Mission1Start,
                    service.LastSavedData.CurrentCheckpointId);
                Assert.IsFalse(
                    service.LastSavedData.Mission03PoliceKeyReceived);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(
            "{\"SaveVersion\":6,\"CurrentCheckpointId\":\"ChapterStart\",\"Mission03PoliceKeyReceived\":true}")]
        [TestCase(
            "{\"SaveVersion\":7,\"CurrentCheckpointId\":999,\"Mission03PoliceKeyReceived\":true}")]
        [TestCase("{ malformed-json")]
        public void JsonService_InvalidOrLegacySave_DeletesFileAndLoadsTask1(
            string json)
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "Chapter1CheckpointTests_" +
                System.Guid.NewGuid().ToString("N"));
            string savePath = Path.Combine(directory, "chapter1_save.json");
            Directory.CreateDirectory(directory);
            File.WriteAllText(savePath, json);

            try
            {
                if (json.StartsWith("{ malformed", System.StringComparison.Ordinal))
                {
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex(
                            "\\[JsonChapter1SaveService\\] Không thể đọc file save Chương 1.*"));
                }

                JsonChapter1SaveService service =
                    new JsonChapter1SaveService(savePath);

                Chapter1SaveData loaded = service.Load();

                Assert.AreEqual(
                    Chapter1SaveData.CurrentSaveVersion,
                    loaded.SaveVersion);
                Assert.AreEqual(
                    Chapter1MissionCheckpoint.Mission1Start,
                    loaded.CurrentCheckpointId);
                Assert.IsFalse(loaded.Mission03PoliceKeyReceived);
                Assert.IsFalse(File.Exists(savePath));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void JsonService_VersionSevenCheckpoint_MigratesWithoutDeletion()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "Chapter1V7MigrationTests_" +
                System.Guid.NewGuid().ToString("N"));
            string savePath = Path.Combine(directory, "chapter1_save.json");
            Directory.CreateDirectory(directory);

            Chapter1SaveData legacy = Chapter1SaveData.CreateDefault();
            legacy.SaveVersion = 7;
            legacy.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission2Start;
            File.WriteAllText(savePath, JsonUtility.ToJson(legacy));

            try
            {
                JsonChapter1SaveService service =
                    new JsonChapter1SaveService(savePath);
                Chapter1SaveData loaded = service.Load();

                Assert.AreEqual(
                    Chapter1SaveData.CurrentSaveVersion,
                    loaded.SaveVersion);
                Assert.AreEqual(
                    Chapter1MissionCheckpoint.Mission2Start,
                    loaded.CurrentCheckpointId);
                Assert.IsTrue(File.Exists(savePath));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void JsonService_CompletedChapter_RoundTripsIntoChapter2()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "Chapter1CompletedRoundTripTests_" +
                System.Guid.NewGuid().ToString("N"));
            string savePath = Path.Combine(directory, "chapter1_save.json");

            try
            {
                Chapter1SaveData completed = CreateDirtyRuntimeData();
                completed.CurrentCheckpointId =
                    Chapter1MissionCheckpoint.Mission3Start;
                completed.ChapterCompleted = true;
                completed.Mission03PoliceArrestCompleted = true;

                JsonChapter1SaveService service =
                    new JsonChapter1SaveService(savePath);
                service.Save(completed);
                Chapter1SaveData loaded = service.Load();

                Assert.IsTrue(loaded.ChapterCompleted);
                Assert.IsTrue(loaded.Mission03PoliceArrestCompleted);
                Assert.IsTrue(loaded.Mission03PoliceKeyReceived);
                Assert.IsTrue(loaded.HasChapterCarryOverItem(
                    Chapter1SaveData.PhoneCarryOverItemId));
                Assert.IsTrue(loaded.HasChapterCarryOverItem(
                    Chapter1SaveData.PoliceKeyCarryOverItemId));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private static Chapter1SaveData CreateDirtyRuntimeData()
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.FirstMissionStateValue = (int)FirstMissionState.Completed;
            data.Mission01MinhIntroDialoguePlayed = true;
            data.Mission01DungDoorDiscovered = true;
            data.Mission01DungDoorUnlocked = true;
            data.Mission01AudioSeparatorMixerStarted = true;
            data.Mission01AudioSeparatorTutorialSeen = true;
            data.Mission01CompletionDialoguePlayed = true;
            data.Mission01Completed = true;
            data.Mission02Started = true;
            data.Mission02HasPsu = true;
            data.Mission02HasUps = true;
            data.Mission02HasBrokenBattery = true;
            data.Mission02HasHenryBattery = true;
            data.Mission02EquipmentDelivered = true;
            data.Mission03JamesIntroPlayed = true;
            data.Mission03ChallengePassed = true;
            data.Mission03GangHostile = true;
            data.Mission03PoliceKeyReceived = true;
            data.Mission03HenryConfrontationCompleted = true;
            data.Mission03HenryDefeated = true;
            data.Mission03PoliceArrestCompleted = true;
            data.CollectedUniqueItemIds.Add("runtime-item");
            data.SavedPhoneRecordingIds.Add(
                LanAudioRecordingCatalog.MixedRecordingId);
            return data;
        }

        private static void SaveAllLanStems(Chapter1SaveData data)
        {
            for (int i = 0;
                 i < LanAudioRecordingCatalog.StemOrder.Length;
                 i++)
            {
                data.AddSavedStem(
                    LanAudioRecordingCatalog.StemOrder[i]);
            }
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }

        private sealed class FakeSaveService : IChapter1SaveService
        {
            public string SavePath => string.Empty;
            public int SaveCount { get; private set; }
            public int DeleteCount { get; private set; }
            public Chapter1SaveData LastSavedData { get; private set; }
            public bool SaveSurvivesDelete { get; set; }

            public void Save(Chapter1SaveData data)
            {
                SaveCount++;
                LastSavedData = data;
            }

            public Chapter1SaveData Load()
            {
                return Chapter1SaveData.CreateDefault();
            }

            public bool HasSave()
            {
                return SaveSurvivesDelete;
            }

            public void DeleteSave()
            {
                DeleteCount++;
            }
        }
    }
}
