using System;
using System.IO;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class JsonChapter1SaveService : IChapter1SaveService
    {
        private const string SaveFileName = "chapter1_save.json";

        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public void Save(Chapter1SaveData data)
        {
            try
            {
                Chapter1SaveData safeData = data ?? Chapter1SaveData.CreateDefault();
                safeData.EnsureValidDefaults();

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
                Chapter1SaveData data = JsonUtility.FromJson<Chapter1SaveData>(json);
                if (data == null)
                {
                    Debug.LogError($"[JsonChapter1SaveService] File save Chương 1 không hợp lệ tại '{SavePath}'. Sử dụng dữ liệu mặc định.");
                    return Chapter1SaveData.CreateDefault();
                }

                data.EnsureValidDefaults();
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonChapter1SaveService] Không thể đọc file save Chương 1 tại '{SavePath}'. Sử dụng dữ liệu mặc định. Lỗi: {exception.Message}");
                return Chapter1SaveData.CreateDefault();
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
    }
}
