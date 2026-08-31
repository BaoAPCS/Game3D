using System;
using System.IO;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    public sealed class JsonChapter2SaveService : IChapter2SaveService
    {
        private const string SaveFileName = "chapter2_save.json";
        private readonly string savePathOverride;

        public JsonChapter2SaveService()
        {
        }

        public JsonChapter2SaveService(string savePathOverride)
        {
            this.savePathOverride = savePathOverride;
        }

        public string SavePath =>
            string.IsNullOrWhiteSpace(savePathOverride)
                ? Path.Combine(
                    Application.persistentDataPath,
                    SaveFileName)
                : savePathOverride;

        public void Save(Chapter2SaveData data)
        {
            try
            {
                Chapter2SaveData snapshot =
                    data?.DeepCopy() ??
                    Chapter2SaveData.CreateDefault();
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    SavePath,
                    JsonUtility.ToJson(snapshot, true));
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[JsonChapter2SaveService] Không thể lưu dữ liệu Chương 2 tại '{SavePath}'. Lỗi: {exception.Message}");
            }
        }

        public Chapter2SaveData Load()
        {
            try
            {
                if (!HasSave())
                {
                    return Chapter2SaveData.CreateDefault();
                }

                string json = File.ReadAllText(SavePath);
                SaveHeader header =
                    JsonUtility.FromJson<SaveHeader>(json);
                if (header == null ||
                    header.SaveVersion !=
                    Chapter2SaveData.CurrentSaveVersion)
                {
                    return DeleteInvalidSaveAndCreateDefault(
                        "File save Chương 2 dùng schema cũ hoặc không hợp lệ");
                }

                Chapter2SaveData data =
                    JsonUtility.FromJson<Chapter2SaveData>(json);
                if (data == null)
                {
                    return DeleteInvalidSaveAndCreateDefault(
                        "File save Chương 2 không đọc được dữ liệu");
                }

                data.EnsureValidDefaults();
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[JsonChapter2SaveService] Không thể đọc file save Chương 2 tại '{SavePath}'. Sử dụng dữ liệu mặc định. Lỗi: {exception.Message}");
                DeleteSave();
                return Chapter2SaveData.CreateDefault();
            }
        }

        public bool HasSave()
        {
            try
            {
                return File.Exists(SavePath);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[JsonChapter2SaveService] Không thể kiểm tra file save Chương 2 tại '{SavePath}'. Lỗi: {exception.Message}");
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
                Debug.LogError(
                    $"[JsonChapter2SaveService] Không thể xóa file save Chương 2 tại '{SavePath}'. Lỗi: {exception.Message}");
            }
        }

        private Chapter2SaveData DeleteInvalidSaveAndCreateDefault(
            string reason)
        {
            Debug.LogWarning(
                $"[JsonChapter2SaveService] {reason} tại '{SavePath}'. File sẽ bị xóa và Mission 01 được khởi tạo lại.");
            DeleteSave();
            return Chapter2SaveData.CreateDefault();
        }

        [Serializable]
        private sealed class SaveHeader
        {
            public int SaveVersion = 0;
        }
    }
}
