using System;
using System.IO;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class JsonChapter1SaveService : IChapter1SaveService
    {
        private const string SaveFileName = "chapter1_save.json";
        private readonly string savePathOverride;

        public JsonChapter1SaveService()
        {
        }

        public JsonChapter1SaveService(string savePathOverride)
        {
            this.savePathOverride = savePathOverride;
        }

        public string SavePath =>
            string.IsNullOrWhiteSpace(savePathOverride)
                ? Path.Combine(Application.persistentDataPath, SaveFileName)
                : savePathOverride;

        public void Save(Chapter1SaveData data)
        {
            try
            {
                Chapter1SaveData safeData =
                    Chapter1CheckpointPolicy.CreateSnapshot(data);

                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(safeData, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonChapter1SaveService] Không thể lưu dữ liệu Chương 1 tại '{SavePath}'. Lỗi: {exception.Message}");
            }
        }

        public Chapter1SaveData Load()
        {
            try
            {
                if (!HasSave())
                {
                    return Chapter1SaveData.CreateDefault();
                }

                string json = File.ReadAllText(SavePath);
                SaveHeader header = JsonUtility.FromJson<SaveHeader>(json);
                if (header == null ||
                    header.SaveVersion !=
                    Chapter1SaveData.CurrentSaveVersion)
                {
                    return DeleteInvalidSaveAndCreateDefault(
                        "File save Chương 1 dùng schema cũ hoặc không hợp lệ");
                }

                Chapter1SaveData data = JsonUtility.FromJson<Chapter1SaveData>(json);
                if (data == null ||
                    !Chapter1CheckpointPolicy.IsValidCheckpoint(
                        data.CurrentCheckpointId))
                {
                    return DeleteInvalidSaveAndCreateDefault(
                        "File save Chương 1 có checkpoint không hợp lệ");
                }

                return Chapter1CheckpointPolicy.CreateSnapshot(data);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonChapter1SaveService] Không thể đọc file save Chương 1 tại '{SavePath}'. Sử dụng dữ liệu mặc định. Lỗi: {exception.Message}");
                DeleteSave();
                return Chapter1SaveData.CreateDefault();
            }
        }

        private Chapter1SaveData DeleteInvalidSaveAndCreateDefault(
            string reason)
        {
            Debug.LogWarning(
                $"[JsonChapter1SaveService] {reason} tại '{SavePath}'. File sẽ bị xóa và Task 1 được khởi tạo lại.");
            DeleteSave();
            return Chapter1SaveData.CreateDefault();
        }

        public bool HasSave()
        {
            try
            {
                return File.Exists(SavePath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonChapter1SaveService] Không thể kiểm tra file save Chương 1 tại '{SavePath}'. Lỗi: {exception.Message}");
                return false;
            }
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonChapter1SaveService] Không thể xóa file save Chương 1 tại '{SavePath}'. Lỗi: {exception.Message}");
            }
        }

        [Serializable]
        private sealed class SaveHeader
        {
            public int SaveVersion = 0;
        }
    }
}
