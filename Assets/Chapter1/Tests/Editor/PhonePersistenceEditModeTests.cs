using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class PhonePersistenceEditModeTests
    {
        private const string PhonePrefabPath =
            "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";

        [Test]
        public void Mission1Checkpoint_PreservesAllPhoneData_AsDeepCopy()
        {
            Chapter1SaveData runtime = CreateCompletePhoneData();
            runtime.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission1Start;

            List<string> expectedRecordingIds = new List<string>(
                runtime.SavedPhoneRecordingIds);
            List<string> expectedStemIds = new List<string>(
                runtime.SavedLanAudioStemIds);

            Chapter1SaveData snapshot =
                Chapter1CheckpointPolicy.CreateSnapshot(runtime);

            Assert.AreEqual(
                (int)LanRecordingMissionState.RecordingDownloaded,
                snapshot.PhoneLanRecordingStateValue);
            Assert.AreEqual(
                LanRecordingMissionState.RecordingDownloaded,
                snapshot.GetPhoneLanRecordingState());
            Assert.IsTrue(snapshot.HasLanRecording);
            Assert.IsTrue(snapshot.Mission01LanVoiceRecordingListened);

            Assert.IsTrue(snapshot.Mission01DungBorrowRequestSent);
            Assert.IsTrue(snapshot.Mission01DungBorrowReplyReceived);
            Assert.IsTrue(snapshot.Mission01DungPasswordQuestionSent);
            Assert.IsTrue(snapshot.Mission01DungPasswordHintReceived);
            Assert.IsTrue(snapshot.Mission01DungBirthdayQuestionSent);
            Assert.IsTrue(snapshot.Mission01DungBirthdayHintReceived);
            Assert.IsTrue(snapshot.Mission01DungHasUnread);

            Assert.AreEqual(7, snapshot.SavedPhoneRecordingIds.Count);
            Assert.AreEqual(
                LanAudioRecordingCatalog.StemCount,
                snapshot.SavedLanAudioStemIds.Count);
            CollectionAssert.AreEqual(
                expectedRecordingIds,
                snapshot.SavedPhoneRecordingIds);
            CollectionAssert.AreEqual(
                expectedStemIds,
                snapshot.SavedLanAudioStemIds);

            Assert.AreNotSame(
                runtime.SavedPhoneRecordingIds,
                snapshot.SavedPhoneRecordingIds);
            Assert.AreNotSame(
                runtime.SavedLanAudioStemIds,
                snapshot.SavedLanAudioStemIds);

            snapshot.SavedPhoneRecordingIds.Clear();
            snapshot.SavedLanAudioStemIds.Clear();
            Assert.AreEqual(7, runtime.SavedPhoneRecordingIds.Count);
            Assert.AreEqual(
                LanAudioRecordingCatalog.StemCount,
                runtime.SavedLanAudioStemIds.Count);

            runtime.SavedPhoneRecordingIds.Add("runtime-only-recording");
            runtime.SavedLanAudioStemIds.Add("runtime-only-stem");
            Assert.IsEmpty(snapshot.SavedPhoneRecordingIds);
            Assert.IsEmpty(snapshot.SavedLanAudioStemIds);
        }

        [Test]
        public void LegacyVersion8_WithMixedRecording_MigratesToDownloaded()
        {
            string directory = Path.Combine(
                Application.temporaryCachePath,
                "phone-save-migration-tests",
                Guid.NewGuid().ToString("N"));
            string savePath = Path.Combine(
                directory,
                "chapter1_save.json");
            Directory.CreateDirectory(directory);

            try
            {
                Chapter1SaveData legacy = Chapter1SaveData.CreateDefault();
                legacy.SaveVersion = 8;
                legacy.CurrentCheckpointId =
                    Chapter1MissionCheckpoint.Mission1Start;
                legacy.HasLanRecording = false;
                legacy.PhoneLanRecordingStateValue =
                    (int)LanRecordingMissionState.NotStarted;
                legacy.SavedPhoneRecordingIds.Add(
                    LanAudioRecordingCatalog.MixedRecordingId);

                File.WriteAllText(savePath, JsonUtility.ToJson(legacy));

                JsonChapter1SaveService service =
                    new JsonChapter1SaveService(savePath);
                Chapter1SaveData loaded = service.Load();

                Assert.AreEqual(
                    Chapter1SaveData.CurrentSaveVersion,
                    loaded.SaveVersion);
                Assert.AreEqual(
                    LanRecordingMissionState.RecordingDownloaded,
                    loaded.GetPhoneLanRecordingState());
                Assert.AreEqual(
                    (int)LanRecordingMissionState.RecordingDownloaded,
                    loaded.PhoneLanRecordingStateValue);
                Assert.IsTrue(loaded.HasPhoneRecording(
                    LanAudioRecordingCatalog.MixedRecordingId));
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
        public void CarriedPhone_OfflineMessenger_ShowsOfflineAndRaisesNoMessage()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhonePrefabPath);
            Assert.NotNull(prefab, "Missing main PhonePanel prefab.");

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab)
                as GameObject;
            Assert.NotNull(instance);

            List<string> notifications = new List<string>();
            Action<string> captureNotification =
                message => notifications.Add(message);
            Chapter1EventBus.NotificationRequested += captureNotification;

            try
            {
                Chapter1SaveData carried = Chapter1SaveData.CreateDefault();
                carried.PhoneLanRecordingStateValue =
                    (int)LanRecordingMissionState.ReadMotherChat;

                PhoneUIController controller =
                    instance.GetComponent<PhoneUIController>();
                Assert.NotNull(controller);

                controller.ConfigureCarriedPhoneData(
                    carried,
                    isMessengerOnline: false);
                controller.OpenMessenger();

                TextMeshProUGUI title = FindChild(
                        instance,
                        "AppTitleText")
                    ?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI body = FindChild(
                        instance,
                        "AppBodyText")
                    ?.GetComponent<TextMeshProUGUI>();

                Assert.NotNull(title);
                Assert.NotNull(body);
                Assert.AreEqual("Messenger", title.text);
                Assert.AreEqual(
                    "Kh\u00F4ng c\u00F3 k\u1EBFt n\u1ED1i m\u1EA1ng.",
                    PhoneUIController.OfflineMessengerMessage);
                Assert.AreEqual(
                    PhoneUIController.OfflineMessengerMessage,
                    body.text);
                Assert.IsFalse(controller.MessengerOnline);
                Assert.IsEmpty(notifications,
                    "Offline Messenger must not announce Lan's message.");
                Assert.AreEqual(
                    LanRecordingMissionState.ReadMotherChat,
                    controller.CurrentPhoneData.GetPhoneLanRecordingState(),
                    "Opening offline Messenger must not advance carried data.");
            }
            finally
            {
                Chapter1EventBus.NotificationRequested -=
                    captureNotification;
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static Chapter1SaveData CreateCompletePhoneData()
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.PhoneLanRecordingStateValue =
                (int)LanRecordingMissionState.RecordingDownloaded;
            data.Mission01LanVoiceRecordingListened = true;

            data.Mission01DungBorrowRequestSent = true;
            data.Mission01DungBorrowReplyReceived = true;
            data.Mission01DungPasswordQuestionSent = true;
            data.Mission01DungPasswordHintReceived = true;
            data.Mission01DungBirthdayQuestionSent = true;
            data.Mission01DungBirthdayHintReceived = true;
            data.Mission01DungHasUnread = true;

            Assert.IsTrue(data.AddPhoneRecording(
                LanAudioRecordingCatalog.MixedRecordingId));
            for (int i = 0;
                 i < LanAudioRecordingCatalog.StemOrder.Length;
                 i++)
            {
                Assert.IsTrue(data.AddSavedStem(
                    LanAudioRecordingCatalog.StemOrder[i]));
            }

            return data;
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
    }
}
