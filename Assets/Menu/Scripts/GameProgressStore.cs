using System;
using System.IO;
using DormitoryMystery.Chapter1;
using DormitoryMystery.Chapter2;
using DormitoryMystery.Chapter3;
using UnityEngine;

namespace DormitoryMystery.Menu
{
    /// <summary>Reads menu eligibility without repairing or deleting an existing save.</summary>
    public sealed class GameProgressStore
    {
        private readonly string chapter1Path;
        private readonly string chapter2Path;
        private readonly Func<bool> isGameCompleted;
        private readonly Action clearGameCompleted;

        public GameProgressStore() : this(
            new JsonChapter1SaveService().SavePath,
            new JsonChapter2SaveService().SavePath,
            () => PlayerPrefs.GetInt(Chapter3RescueController.CompletionPreferenceKey, 0) != 0,
            () =>
            {
                PlayerPrefs.DeleteKey(Chapter3RescueController.CompletionPreferenceKey);
                PlayerPrefs.Save();
            })
        {
        }

        public GameProgressStore(string chapter1Path, string chapter2Path,
            Func<bool> isGameCompleted = null, Action clearGameCompleted = null)
        {
            this.chapter1Path = chapter1Path;
            this.chapter2Path = chapter2Path;
            this.isGameCompleted = isGameCompleted;
            this.clearGameCompleted = clearGameCompleted;
        }

        public bool TryGetContinueScene(out string scenePath)
        {
            scenePath = null;
            if (isGameCompleted != null && isGameCompleted())
                return false;

            if (TryReadChapter2(out Chapter2SaveData chapter2))
            {
                scenePath = chapter2.Chapter2Completed
                    ? GameSessionFlow.Chapter3ScenePath : GameSessionFlow.Chapter2ScenePath;
                return true;
            }

            if (TryReadChapter1(out Chapter1SaveData chapter1))
            {
                scenePath = chapter1.ChapterCompleted && chapter1.Mission03PoliceArrestCompleted
                    ? GameSessionFlow.Chapter2ScenePath : GameSessionFlow.Chapter1ScenePath;
                return true;
            }

            return false;
        }

        public bool TryResetProgress(out string error)
        {
            error = null;
            try
            {
                // Deliberately target only our two saves, never the persistent-data folder
                // or unrelated PlayerPrefs (volume, display preferences, other games).
                if (File.Exists(chapter1Path)) File.Delete(chapter1Path);
                if (File.Exists(chapter2Path)) File.Delete(chapter2Path);
                clearGameCompleted?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                error = "Không thể xóa dữ liệu đã lưu. Hãy kiểm tra quyền truy cập file và thử lại.";
                Debug.LogError($"[GameProgressStore] Reset failed: {exception.Message}");
                return false;
            }
        }

        private bool TryReadChapter1(out Chapter1SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(chapter1Path)) return false;
                string json = File.ReadAllText(chapter1Path);
                SaveHeader header = JsonUtility.FromJson<SaveHeader>(json);
                if (header == null ||
                    header.SaveVersion < JsonChapter1SaveService.MinimumCompatibleSaveVersion ||
                    header.SaveVersion > Chapter1SaveData.CurrentSaveVersion)
                    return false;

                Chapter1SaveData loaded = JsonUtility.FromJson<Chapter1SaveData>(json);
                if (loaded == null || !Chapter1CheckpointPolicy.IsValidCheckpoint(loaded.CurrentCheckpointId))
                    return false;
                // Match the actual loader's checkpoint normalization, on a detached object.
                loaded.EnsureValidDefaults();
                data = Chapter1CheckpointPolicy.CreateSnapshot(loaded);
                return true;
            }
            catch (Exception)
            {
                // Merely displaying the menu must not delete a damaged save.
                return false;
            }
        }

        private bool TryReadChapter2(out Chapter2SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(chapter2Path)) return false;
                string json = File.ReadAllText(chapter2Path);
                SaveHeader header = JsonUtility.FromJson<SaveHeader>(json);
                if (header == null || header.SaveVersion != Chapter2SaveData.CurrentSaveVersion)
                    return false;
                data = JsonUtility.FromJson<Chapter2SaveData>(json);
                if (data == null) return false;
                data.EnsureValidDefaults();
                return true;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        [Serializable]
        private sealed class SaveHeader
        {
            public int SaveVersion = 0;
        }
    }
}
