using System;
using DormitoryMystery.Chapter1;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter2.Editor
{
    public static class Chapter2SaveTools
    {
        private const int FirstMission = 1;
        private const int LastMission = 5;

        [MenuItem("Tools/Chapter 2/Create Fresh Chapter 2 Test Save")]
        public static void CreateFreshChapter2TestSave()
        {
            const string title = "Create Fresh Chapter 2 Test Save";
            if (!CanEditSave(title) ||
                !EditorUtility.DisplayDialog(
                    title,
                    "Create a fresh Chapter 2 save? Any existing Chapter 2 save will be overwritten. The Chapter 1 save is not changed.",
                    "Create",
                    "Cancel"))
            {
                return;
            }

            JsonChapter2SaveService saveService =
                new JsonChapter2SaveService();
            saveService.Save(Chapter2SaveData.CreateDefault());
            Debug.Log(
                $"[Chapter2 Save Tools] Created fresh Chapter 2 test save: {saveService.SavePath}");
        }

        [MenuItem("Tools/Chapter 2/Create Mission 1 Test Save")]
        public static void CreateMission1TestSave()
        {
            CreateMissionTestSave(1);
        }

        [MenuItem("Tools/Chapter 2/Create Mission 2 Test Save")]
        public static void CreateMission2TestSave()
        {
            CreateMissionTestSave(2);
        }

        [MenuItem("Tools/Chapter 2/Create Mission 3 Test Save")]
        public static void CreateMission3TestSave()
        {
            CreateMissionTestSave(3);
        }

        [MenuItem("Tools/Chapter 2/Create Mission 4 Test Save")]
        public static void CreateMission4TestSave()
        {
            CreateMissionTestSave(4);
        }

        [MenuItem("Tools/Chapter 2/Create Mission 5 Test Save")]
        public static void CreateMission5TestSave()
        {
            CreateMissionTestSave(5);
        }

        [MenuItem("Tools/Chapter 2/Delete Mission 1 Progress")]
        public static void DeleteMission1Progress()
        {
            DeleteMissionProgress(1);
        }

        [MenuItem("Tools/Chapter 2/Delete Mission 2 Progress")]
        public static void DeleteMission2Progress()
        {
            DeleteMissionProgress(2);
        }

        [MenuItem("Tools/Chapter 2/Delete Mission 3 Progress")]
        public static void DeleteMission3Progress()
        {
            DeleteMissionProgress(3);
        }

        [MenuItem("Tools/Chapter 2/Delete Mission 4 Progress")]
        public static void DeleteMission4Progress()
        {
            DeleteMissionProgress(4);
        }

        [MenuItem("Tools/Chapter 2/Delete Mission 5 Progress")]
        public static void DeleteMission5Progress()
        {
            DeleteMissionProgress(5);
        }

        internal static Chapter2SaveData PrepareMissionTestData(
            int missionNumber,
            Chapter2SaveData currentChapter2,
            Chapter1SaveData chapter1Data)
        {
            ValidateMissionNumber(missionNumber);

            Chapter2SaveData data = currentChapter2?.DeepCopy() ??
                Chapter2SaveData.CreateDefault();
            ApplyChapter1PhoneDataIfNeeded(data, chapter1Data);

            bool mission01Completed = missionNumber > 1;
            bool mission02Completed = missionNumber > 2;
            bool mission03Completed = missionNumber > 3;
            bool mission04Completed = missionNumber > 4;

            data.Mission01CrowbarCollected = mission01Completed;
            data.Mission01ToiletPried = mission01Completed;
            data.Mission01ServiceCardCollected = mission01Completed;
            data.Mission02JailObstacleDisabled = mission02Completed;
            data.Mission03PhoneRecovered = mission03Completed;
            data.Mission03PoliceKeyRecovered = true;
            data.Mission03ClosetUnlocked = mission03Completed;
            data.Chapter1CarryOverInventoryApplied = true;
            data.Mission04ComputerUnlocked = mission04Completed;
            data.Mission04WifiPasswordDiscovered = mission04Completed;
            data.Mission04PoliceWifiConnected = mission04Completed;
            data.Mission04MinhMessagesRead = mission04Completed;
            data.Mission05ScannerActivated = false;
            data.Mission05RouterInspected = false;
            data.Mission05SecretDocumentCollected = false;
            data.Mission05BrokenDoorUnlocked = false;
            data.Mission05SecretDocumentViewed = false;
            data.Mission05MinhConversationAvailable = false;
            data.Mission05MinhConversationOpened = false;
            data.Mission05MinhConversationStep = 0;
            data.Chapter2Completed = false;
            data.ChapterEndInventory?.Clear();
            data.EnsureValidDefaults();
            return data;
        }

        internal static Chapter2SaveData PrepareMission1TestData(
            Chapter2SaveData currentChapter2,
            Chapter1SaveData chapter1Data)
        {
            return PrepareMissionTestData(
                1,
                currentChapter2,
                chapter1Data);
        }

        internal static Chapter2SaveData PrepareMission2TestData(
            Chapter2SaveData currentChapter2,
            Chapter1SaveData chapter1Data)
        {
            return PrepareMissionTestData(
                2,
                currentChapter2,
                chapter1Data);
        }

        internal static Chapter2SaveData PrepareMission3TestData(
            Chapter2SaveData currentChapter2,
            Chapter1SaveData chapter1Data)
        {
            return PrepareMissionTestData(
                3,
                currentChapter2,
                chapter1Data);
        }

        internal static Chapter2SaveData PrepareMission4TestData(
            Chapter2SaveData currentChapter2,
            Chapter1SaveData chapter1Data)
        {
            return PrepareMissionTestData(
                4,
                currentChapter2,
                chapter1Data);
        }

        internal static Chapter2SaveData PrepareMission5TestData(
            Chapter2SaveData currentChapter2,
            Chapter1SaveData chapter1Data)
        {
            return PrepareMissionTestData(
                5,
                currentChapter2,
                chapter1Data);
        }

        internal static Chapter2SaveData PrepareMissionResetData(
            Chapter2SaveData currentChapter2,
            int missionNumber,
            Chapter1SaveData chapter1Data = null)
        {
            ValidateMissionNumber(missionNumber);

            Chapter2SaveData data = currentChapter2?.DeepCopy() ??
                Chapter2SaveData.CreateDefault();
            ApplyChapter1PhoneDataIfNeeded(data, chapter1Data);

            if (missionNumber <= 1)
            {
                data.Mission01CrowbarCollected = false;
                data.Mission01ToiletPried = false;
                data.Mission01ServiceCardCollected = false;
            }

            if (missionNumber <= 2)
            {
                data.Mission02JailObstacleDisabled = false;
            }

            if (missionNumber <= 3)
            {
                data.Mission03PhoneRecovered = false;
                data.Mission03PoliceKeyRecovered = true;
                data.Mission03ClosetUnlocked = false;
                data.HasPhone = false;
                data.HasPoliceStationKey = true;
                data.Chapter1CarryOverInventoryApplied = true;
            }

            if (missionNumber <= 4)
            {
                data.Mission04ComputerUnlocked = false;
                data.Mission04WifiPasswordDiscovered = false;
                data.Mission04PoliceWifiConnected = false;
                data.Mission04MinhMessagesRead = false;
            }

            data.Mission05ScannerActivated = false;
            data.Mission05RouterInspected = false;
            data.Mission05SecretDocumentCollected = false;
            data.Mission05BrokenDoorUnlocked = false;
            data.Mission05SecretDocumentViewed = false;
            data.Mission05MinhConversationAvailable = false;
            data.Mission05MinhConversationOpened = false;
            data.Mission05MinhConversationStep = 0;
            data.Chapter2Completed = false;
            data.ChapterEndInventory?.Clear();
            data.EnsureValidDefaults();
            return data;
        }

        [MenuItem("Tools/Chapter 2/Delete Chapter 2 Test Save")]
        public static void DeleteChapter2TestSave()
        {
            const string title = "Delete Chapter 2 Test Save";
            if (!CanEditSave(title) ||
                !EditorUtility.DisplayDialog(
                    title,
                    "Delete only the Chapter 2 JSON save file from persistentDataPath? The Chapter 1 save is not changed.",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            JsonChapter2SaveService saveService =
                new JsonChapter2SaveService();
            saveService.DeleteSave();
            Debug.Log(
                $"[Chapter2 Save Tools] Deleted Chapter 2 test save if present: {saveService.SavePath}");
        }

        private static void CreateMissionTestSave(int missionNumber)
        {
            ValidateMissionNumber(missionNumber);

            string title = $"Create Mission {missionNumber} Test Save";
            string completedDescription = missionNumber == FirstMission
                ? "All Mission 1-5 progress will be reset."
                : $"Mission 1-{missionNumber - 1} will be marked complete and Mission {missionNumber}-5 will be reset.";
            if (!CanEditSave(title) ||
                !EditorUtility.DisplayDialog(
                    title,
                    $"Create a Chapter 2 save positioned immediately before Mission {missionNumber}? {completedDescription} Chapter 1 is not changed.",
                    "Create",
                    "Cancel"))
            {
                return;
            }

            JsonChapter2SaveService saveService =
                new JsonChapter2SaveService();
            Chapter2SaveData currentChapter2 = saveService.HasSave()
                ? saveService.Load()
                : Chapter2SaveData.CreateDefault();
            Chapter1SaveData chapter1Data =
                LoadChapter1PhoneDataIfNeeded(currentChapter2);

            Chapter2SaveData data = PrepareMissionTestData(
                missionNumber,
                currentChapter2,
                chapter1Data);
            saveService.Save(data);
            Debug.Log(
                $"[Chapter2 Save Tools] Created Mission {missionNumber} test save: {saveService.SavePath}");
        }

        private static void DeleteMissionProgress(int missionNumber)
        {
            ValidateMissionNumber(missionNumber);

            string title = $"Delete Mission {missionNumber} Progress";
            if (!CanEditSave(title))
            {
                return;
            }

            JsonChapter2SaveService saveService =
                new JsonChapter2SaveService();
            if (!saveService.HasSave())
            {
                EditorUtility.DisplayDialog(
                    title,
                    "No Chapter 2 save exists yet, so there is no mission progress to delete.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    title,
                    $"Reset Mission {missionNumber} and every later mission in the shared Chapter 2 save? Progress before Mission {missionNumber} and all Chapter 1 data will be kept.",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            Chapter2SaveData currentChapter2 = saveService.Load();
            Chapter1SaveData chapter1Data =
                LoadChapter1PhoneDataIfNeeded(currentChapter2);
            Chapter2SaveData data = PrepareMissionResetData(
                currentChapter2,
                missionNumber,
                chapter1Data);
            saveService.Save(data);
            Debug.Log(
                $"[Chapter2 Save Tools] Deleted Mission {missionNumber}-5 progress: {saveService.SavePath}");
        }

        private static Chapter1SaveData LoadChapter1PhoneDataIfNeeded(
            Chapter2SaveData currentChapter2)
        {
            if (currentChapter2 != null &&
                currentChapter2.Chapter1PhoneDataImported)
            {
                return null;
            }

            JsonChapter1SaveService chapter1Save =
                new JsonChapter1SaveService();
            return chapter1Save.HasSave()
                ? chapter1Save.Load()
                : null;
        }

        private static void ApplyChapter1PhoneDataIfNeeded(
            Chapter2SaveData data,
            Chapter1SaveData chapter1Data)
        {
            if (data.Chapter1PhoneDataImported ||
                chapter1Data == null)
            {
                return;
            }

            data.PhoneData = Chapter2PhoneData.FromChapter1(
                chapter1Data);
            data.Chapter1PhoneDataImported = true;
        }

        private static bool CanEditSave(string title)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return true;
            }

            EditorUtility.DisplayDialog(
                title,
                "Stop Play Mode before changing the Chapter 2 save. The running Chapter2SaveManager could otherwise overwrite the test state.",
                "OK");
            return false;
        }

        private static void ValidateMissionNumber(int missionNumber)
        {
            if (missionNumber < FirstMission ||
                missionNumber > LastMission)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(missionNumber),
                    missionNumber,
                    $"Mission number must be between {FirstMission} and {LastMission}.");
            }
        }
    }
}
