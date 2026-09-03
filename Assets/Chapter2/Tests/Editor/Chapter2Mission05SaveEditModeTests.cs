using System.Reflection;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2Mission05SaveEditModeTests
    {
        private const string ItemPath =
            "Assets/Chapter2/Resources/Inventory/ClassifiedDocumentItem.asset";
        private const string IconPath =
            "Assets/Chapter2/Sprites/classified_document.png";

        [Test]
        public void FreshSaveStartsWithMission05Locked()
        {
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();

            Assert.AreEqual(1, Chapter2SaveData.CurrentSaveVersion);
            Assert.IsFalse(data.Mission05RouterInspected);
            Assert.IsFalse(data.Mission05SecretDocumentCollected);
            Assert.IsFalse(data.Mission05Completed);
        }

        [Test]
        public void CollectedDocumentRepairsMission05AndAllPrerequisites()
        {
            Chapter2SaveData data = new Chapter2SaveData
            {
                Mission05SecretDocumentCollected = true
            };

            data.EnsureValidDefaults();

            Assert.IsTrue(data.Mission05RouterInspected);
            Assert.IsTrue(data.Mission05Completed);
            Assert.IsTrue(data.Mission04MinhMessagesRead);
            Assert.IsTrue(data.Mission04Completed);
            Assert.IsTrue(data.Mission03Completed);
            Assert.IsTrue(data.Mission02JailObstacleDisabled);
            Assert.IsTrue(data.Mission01ServiceCardCollected);
            Assert.IsTrue(data.HasPhone);
            Assert.IsTrue(data.HasPoliceStationKey);
        }

        [Test]
        public void DeepCopyPreservesMission05Progress()
        {
            Chapter2SaveData source = Chapter2SaveData.CreateDefault();
            source.Mission05RouterInspected = true;
            source.Mission05SecretDocumentCollected = true;

            Chapter2SaveData copy = source.DeepCopy();
            source.Mission05RouterInspected = false;
            source.Mission05SecretDocumentCollected = false;

            Assert.IsTrue(copy.Mission05RouterInspected);
            Assert.IsTrue(copy.Mission05SecretDocumentCollected);
            Assert.IsTrue(copy.Mission05Completed);
        }

        [Test]
        public void Mission04ResetCascadesMission05WhileMission05ResetDoesNot()
        {
            GameObject owner = new GameObject("Mission05SaveManagerTest");
            owner.SetActive(false);
            try
            {
                Chapter2SaveManager manager =
                    owner.AddComponent<Chapter2SaveManager>();
                Chapter2SaveData data = new Chapter2SaveData
                {
                    Mission04MinhMessagesRead = true,
                    Mission05RouterInspected = true,
                    Mission05SecretDocumentCollected = true
                };
                data.EnsureValidDefaults();
                SetPrivateField(manager, "currentData", data);
                SetPrivateField(
                    manager,
                    "saveService",
                    new MemorySaveService());

                manager.ResetMission05();
                Assert.IsTrue(data.Mission04MinhMessagesRead);
                Assert.IsFalse(data.Mission05RouterInspected);
                Assert.IsFalse(data.Mission05SecretDocumentCollected);

                data.Mission05RouterInspected = true;
                data.Mission05SecretDocumentCollected = true;
                manager.ResetMission04();
                Assert.IsFalse(data.Mission04MinhMessagesRead);
                Assert.IsFalse(data.Mission05RouterInspected);
                Assert.IsFalse(data.Mission05SecretDocumentCollected);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Mission5TestSaveCompletesMission4AndResetsMission5()
        {
            Chapter2SaveData current = Chapter2SaveData.CreateDefault();
            current.Chapter1PhoneDataImported = true;
            current.PhoneData.HasLanRecording = true;
            current.PhoneData.EnsureValidDefaults();
            current.Mission05RouterInspected = true;
            current.Mission05SecretDocumentCollected = true;

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMission5TestData(
                    current,
                    null);

            Assert.IsTrue(prepared.Mission04MinhMessagesRead);
            Assert.IsTrue(prepared.Mission04Completed);
            Assert.IsFalse(prepared.Mission05RouterInspected);
            Assert.IsFalse(prepared.Mission05Completed);
            Assert.IsTrue(prepared.Chapter1PhoneDataImported);
            Assert.IsTrue(prepared.PhoneData.HasLanRecording);
        }

        [Test]
        public void Mission5TestSaveImportsChapter1PhoneOnlyWhenNeeded()
        {
            Chapter1SaveData chapter1 = Chapter1SaveData.CreateDefault();
            chapter1.HasLanRecording = true;
            chapter1.EnsureValidDefaults();

            Chapter2SaveData prepared =
                DormitoryMystery.Chapter2.Editor.Chapter2SaveTools
                    .PrepareMission5TestData(
                    Chapter2SaveData.CreateDefault(),
                    chapter1);

            Assert.IsTrue(prepared.Chapter1PhoneDataImported);
            Assert.IsTrue(prepared.PhoneData.HasLanRecording);
            Assert.IsTrue(prepared.HasPhone);
        }

        [Test]
        public void ClassifiedDocumentIsUniqueNonUsableDocumentWithIcon()
        {
            ItemDefinition item =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemPath);

            Assert.NotNull(item);
            Assert.AreEqual("classified_document", item.ItemId);
            Assert.AreEqual("T\u00E0i li\u1EC7u m\u1EADt", item.DisplayName);
            Assert.AreEqual(ItemCategory.Document, item.Category);
            Assert.IsFalse(item.IsStackable);
            Assert.AreEqual(1, item.MaxStack);
            Assert.IsFalse(item.IsDroppable);
            Assert.IsFalse(item.IsUsable);
            Assert.NotNull(item.Icon);
            Assert.AreEqual(IconPath, AssetDatabase.GetAssetPath(item.Icon));

            TextureImporter importer =
                AssetImporter.GetAtPath(IconPath) as TextureImporter;
            Assert.NotNull(importer);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.IsTrue(importer.alphaIsTransparency);
        }

        private static void SetPrivateField(
            Chapter2SaveManager manager,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(Chapter2SaveManager).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Missing field '{fieldName}'.");
            field.SetValue(manager, value);
        }

        private sealed class MemorySaveService : IChapter2SaveService
        {
            private Chapter2SaveData data =
                Chapter2SaveData.CreateDefault();

            public string SavePath => "memory://chapter2";

            public void Save(Chapter2SaveData value)
            {
                data = value?.DeepCopy() ??
                    Chapter2SaveData.CreateDefault();
            }

            public Chapter2SaveData Load()
            {
                return data.DeepCopy();
            }

            public bool HasSave()
            {
                return true;
            }

            public void DeleteSave()
            {
                data = Chapter2SaveData.CreateDefault();
            }
        }
    }
}
