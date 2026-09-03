using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using NUnit.Framework;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2PhoneDataEditModeTests
    {
        [Test]
        public void Chapter1PhoneSnapshotCopiesEveryPhoneFieldAndDetaches()
        {
            Chapter1SaveData chapter1 =
                Chapter1SaveData.CreateDefault();
            chapter1.PhoneLanRecordingStateValue =
                (int)LanRecordingMissionState.SendRecordingToMinh;
            chapter1.HasLanRecording = true;
            chapter1.Mission01LanVoiceRecordingListened = true;
            chapter1.Mission01DungBorrowRequestSent = true;
            chapter1.Mission01DungBorrowReplyReceived = true;
            chapter1.Mission01DungPasswordQuestionSent = true;
            chapter1.Mission01DungPasswordHintReceived = true;
            chapter1.Mission01DungBirthdayQuestionSent = true;
            chapter1.Mission01DungBirthdayHintReceived = true;
            chapter1.Mission01DungHasUnread = true;
            chapter1.AddPhoneRecording(
                LanAudioRecordingCatalog.MixedRecordingId);
            for (int i = 0;
                 i < LanAudioRecordingCatalog.StemOrder.Length;
                 i++)
            {
                chapter1.AddSavedStem(
                    LanAudioRecordingCatalog.StemOrder[i]);
            }

            chapter1.AudioSeparatorFaderValues = new List<float>
            {
                0f,
                0.2f,
                0.4f,
                0.6f,
                0.8f,
                1f
            };

            Chapter2PhoneData snapshot =
                Chapter2PhoneData.FromChapter1(chapter1);

            chapter1.SavedPhoneRecordingIds.Clear();
            chapter1.SavedLanAudioStemIds.Clear();
            chapter1.AudioSeparatorFaderValues[0] = 1f;
            chapter1.Mission01DungHasUnread = false;

            Assert.AreEqual(
                (int)LanRecordingMissionState.SendRecordingToMinh,
                snapshot.PhoneLanRecordingStateValue);
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
            Assert.AreEqual(0f, snapshot.AudioSeparatorFaderValues[0]);
        }

        [Test]
        public void PhoneSnapshotRoundTripsIntoPhoneCompatibleChapter1Data()
        {
            Chapter2PhoneData snapshot =
                Chapter2PhoneData.CreateDefault();
            snapshot.PhoneLanRecordingStateValue =
                (int)LanRecordingMissionState.RecordingDownloaded;
            snapshot.HasLanRecording = true;
            snapshot.Mission01LanVoiceRecordingListened = true;
            snapshot.Mission01DungPasswordHintReceived = true;
            snapshot.Mission01DungHasUnread = true;
            snapshot.EnsureValidDefaults();

            Chapter1SaveData phoneData =
                snapshot.ToChapter1SaveData();

            Assert.AreEqual(
                snapshot.PhoneLanRecordingStateValue,
                phoneData.PhoneLanRecordingStateValue);
            Assert.IsTrue(phoneData.HasPhoneRecording(
                LanAudioRecordingCatalog.MixedRecordingId));
            Assert.IsTrue(
                phoneData.Mission01LanVoiceRecordingListened);
            Assert.IsTrue(
                phoneData.Mission01DungPasswordHintReceived);
            Assert.IsTrue(phoneData.Mission01DungHasUnread);
        }

        [Test]
        public void Chapter2SaveDeepCopyOwnsDetachedPhonePayload()
        {
            Chapter2SaveData save = Chapter2SaveData.CreateDefault();
            save.Chapter1PhoneDataImported = true;
            save.PhoneData.HasLanRecording = true;
            save.PhoneData.EnsureValidDefaults();

            Chapter2SaveData copy = save.DeepCopy();
            save.PhoneData.SavedPhoneRecordingIds.Clear();

            Assert.AreEqual(1, Chapter2SaveData.CurrentSaveVersion);
            Assert.AreEqual(1, copy.SaveVersion);
            Assert.IsTrue(copy.Chapter1PhoneDataImported);
            Assert.IsTrue(copy.PhoneData.HasLanRecording);
            Assert.IsTrue(copy.PhoneData.SavedPhoneRecordingIds.Contains(
                LanAudioRecordingCatalog.MixedRecordingId));
        }
    }
}
