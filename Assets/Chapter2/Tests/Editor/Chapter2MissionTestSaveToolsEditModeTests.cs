using System;
using DormitoryMystery.Chapter1;
using NUnit.Framework;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2MissionTestSaveToolsEditModeTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void CreatePresetStartsExactlyBeforeRequestedMission(
            int missionNumber)
        {
            Chapter2SaveData current = CreateCompletedSave();

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionTestData(
                        missionNumber,
                        current,
                        null);

            AssertCanonicalMissionStart(prepared, missionNumber);
            Assert.IsTrue(prepared.Chapter1PhoneDataImported);
            Assert.IsTrue(prepared.PhoneData.HasLanRecording);
            Assert.AreNotSame(current.PhoneData, prepared.PhoneData);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void DeleteProgressClearsTargetAndLaterMissionsOnly(
            int missionNumber)
        {
            Chapter2SaveData current = CreateCompletedSave();

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionResetData(
                        current,
                        missionNumber);

            AssertCanonicalMissionStart(prepared, missionNumber);
            Assert.IsTrue(prepared.Chapter1PhoneDataImported);
            Assert.IsTrue(prepared.PhoneData.HasLanRecording);
            Assert.AreNotSame(current.PhoneData, prepared.PhoneData);
        }

        [Test]
        public void DeleteLaterMissionPreservesPartialEarlierProgress()
        {
            Chapter2SaveData current = Chapter2SaveData.CreateDefault();
            current.Mission01CrowbarCollected = true;
            current.Mission01ToiletPried = false;
            current.Mission01ServiceCardCollected = false;

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionResetData(current, 2);

            Assert.IsTrue(prepared.Mission01CrowbarCollected);
            Assert.IsFalse(prepared.Mission01ToiletPried);
            Assert.IsFalse(prepared.Mission01ServiceCardCollected);
            Assert.IsFalse(prepared.Mission02JailObstacleDisabled);
            Assert.IsFalse(prepared.Mission03PhoneRecovered);
            Assert.IsFalse(prepared.Mission03PoliceKeyRecovered);
            Assert.IsFalse(prepared.Mission04ComputerUnlocked);
            Assert.IsFalse(prepared.Mission05RouterInspected);
        }

        [Test]
        public void ResetMission3ConfiscatesItemsButPreservesPhonePayload()
        {
            Chapter2SaveData current = CreateCompletedSave();

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionResetData(current, 3);

            Assert.IsFalse(prepared.HasPhone);
            Assert.IsFalse(prepared.HasPoliceStationKey);
            Assert.IsFalse(prepared.Mission03PhoneRecovered);
            Assert.IsFalse(prepared.Mission03PoliceKeyRecovered);
            Assert.IsTrue(prepared.PhoneData.HasLanRecording);
            Assert.IsTrue(prepared.Chapter1PhoneDataImported);
        }

        [Test]
        public void Mission1PresetImportsChapter1PhoneWithoutReturningPhone()
        {
            Chapter1SaveData chapter1 = Chapter1SaveData.CreateDefault();
            chapter1.HasLanRecording = true;
            chapter1.EnsureValidDefaults();

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionTestData(
                        1,
                        Chapter2SaveData.CreateDefault(),
                        chapter1);

            Assert.IsTrue(prepared.Chapter1PhoneDataImported);
            Assert.IsTrue(prepared.PhoneData.HasLanRecording);
            Assert.IsFalse(prepared.HasPhone);
            Assert.IsFalse(prepared.HasPoliceStationKey);
        }

        [Test]
        public void MissionNumberOutsideOneToFiveIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionTestData(0, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMissionResetData(null, 6));
        }

        private static Chapter2SaveData CreateCompletedSave()
        {
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();
            data.Mission01CrowbarCollected = true;
            data.Mission01ToiletPried = true;
            data.Mission01ServiceCardCollected = true;
            data.Mission02JailObstacleDisabled = true;
            data.Mission03PhoneRecovered = true;
            data.Mission03PoliceKeyRecovered = true;
            data.Mission04ComputerUnlocked = true;
            data.Mission04WifiPasswordDiscovered = true;
            data.Mission04PoliceWifiConnected = true;
            data.Mission04MinhMessagesRead = true;
            data.Mission05RouterInspected = true;
            data.Mission05SecretDocumentCollected = true;
            data.Chapter1PhoneDataImported = true;
            data.PhoneData.HasLanRecording = true;
            data.EnsureValidDefaults();
            return data;
        }

        private static void AssertCanonicalMissionStart(
            Chapter2SaveData data,
            int missionNumber)
        {
            bool mission01Completed = missionNumber > 1;
            bool mission02Completed = missionNumber > 2;
            bool mission03Completed = missionNumber > 3;
            bool mission04Completed = missionNumber > 4;

            Assert.AreEqual(
                mission01Completed,
                data.Mission01CrowbarCollected);
            Assert.AreEqual(
                mission01Completed,
                data.Mission01ToiletPried);
            Assert.AreEqual(
                mission01Completed,
                data.Mission01ServiceCardCollected);
            Assert.AreEqual(
                mission02Completed,
                data.Mission02JailObstacleDisabled);
            Assert.AreEqual(
                mission03Completed,
                data.Mission03PhoneRecovered);
            Assert.AreEqual(
                mission03Completed,
                data.Mission03PoliceKeyRecovered);
            Assert.AreEqual(mission03Completed, data.HasPhone);
            Assert.AreEqual(
                mission03Completed,
                data.HasPoliceStationKey);
            Assert.AreEqual(
                mission04Completed,
                data.Mission04ComputerUnlocked);
            Assert.AreEqual(
                mission04Completed,
                data.Mission04WifiPasswordDiscovered);
            Assert.AreEqual(
                mission04Completed,
                data.Mission04PoliceWifiConnected);
            Assert.AreEqual(
                mission04Completed,
                data.Mission04MinhMessagesRead);
            Assert.IsFalse(data.Mission05RouterInspected);
            Assert.IsFalse(data.Mission05SecretDocumentCollected);
        }
    }
}
