using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter2.Editor
{
    public static class Chapter2SaveTools
    {
        [MenuItem("Tools/Chapter 2/Create Fresh Chapter 2 Test Save")]
        public static void CreateFreshChapter2TestSave()
        {
            if (!EditorUtility.DisplayDialog(
                    "Create Fresh Chapter 2 Test Save",
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

        [MenuItem("Tools/Chapter 2/Delete Chapter 2 Test Save")]
        public static void DeleteChapter2TestSave()
        {
            if (!EditorUtility.DisplayDialog(
                    "Delete Chapter 2 Test Save",
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
    }
}
