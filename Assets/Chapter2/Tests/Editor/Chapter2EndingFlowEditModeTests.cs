using System.Collections.Generic;
using System.Reflection;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2EndingFlowEditModeTests
    {
        private const string DocumentItemPath =
            "Assets/Chapter2/Resources/Inventory/ClassifiedDocumentItem.asset";
        private const string SecretFilePath =
            "Assets/Chapter2/Sprites/secret_file.png";
        private const string Chapter3ScenePath =
            "Assets/Chapter3/Scene/Abandoned Hospital.unity";

        [Test]
        public void SecretDocumentViewerUsesRequestedImageAndReleasesInput()
        {
            GameObject player = new GameObject("ViewerTestPlayer");
            GameObject root = new GameObject("ViewerTestRoot");
            try
            {
                PlayerInputLock inputLock =
                    player.AddComponent<PlayerInputLock>();
                ItemDefinition document =
                    AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                        DocumentItemPath);
                Assert.NotNull(document);
                Assert.NotNull(document.PreviewImage);

                Chapter2SecretDocumentViewer viewer =
                    Chapter2SecretDocumentViewer.Create(
                        root.transform,
                        inputLock);
                viewer.Show(document.PreviewImage);

                Assert.IsTrue(viewer.IsVisible);
                Assert.IsTrue(inputLock.IsLocked);
                Assert.AreEqual(
                    SecretFilePath,
                    AssetDatabase.GetAssetPath(viewer.DisplayedSprite));
                Assert.NotNull(FindChild(root, "CloseButton"));

                viewer.Close();
                Assert.IsFalse(viewer.IsVisible);
                Assert.IsFalse(inputLock.IsLocked);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void EndingControllerUsesDocumentAndPersistsViewedState()
        {
            GameObject managerObject = new GameObject("EndingSaveTest");
            managerObject.SetActive(false);
            GameObject player = new GameObject("EndingPlayerTest");
            GameObject owner = new GameObject("EndingControllerTest");
            try
            {
                Chapter2SaveData data = Chapter2SaveData.CreateDefault();
                data.Mission05SecretDocumentCollected = true;
                data.EnsureValidDefaults();
                Chapter2SaveManager manager =
                    managerObject.AddComponent<Chapter2SaveManager>();
                SetPrivateField(manager, "currentData", data);
                SetPrivateField(manager, "saveService", new MemorySaveService());

                InventoryController inventory =
                    player.AddComponent<InventoryController>();
                PlayerInputLock inputLock =
                    player.AddComponent<PlayerInputLock>();
                ItemDefinition document =
                    AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                        DocumentItemPath);
                Assert.IsTrue(inventory.AddItem(document));

                Chapter2SecretDocumentViewer viewer =
                    Chapter2SecretDocumentViewer.Create(
                        owner.transform,
                        inputLock);
                Chapter2EndingController controller =
                    owner.AddComponent<Chapter2EndingController>();
                SetPrivateField(controller, "saveManager", manager);
                SetPrivateField(controller, "inventory", inventory);
                SetPrivateField(controller, "inputLock", inputLock);
                SetPrivateField(controller, "documentDefinition", document);
                SetPrivateField(controller, "viewer", viewer);

                InventoryItem item = inventory.GetItem(
                    Chapter2WifiSignalScannerMission
                        .ClassifiedDocumentItemId);
                Assert.IsTrue(controller.CanUseInventoryItem(item));
                Assert.IsTrue(controller.TryUseInventoryItem(item));
                Assert.IsTrue(viewer.IsVisible);
                Assert.IsTrue(data.Mission05SecretDocumentViewed);
                Assert.IsFalse(
                    data.Mission05MinhConversationAvailable,
                    "The Minh message is revealed only after the viewer closes and the 1.5 second delay elapses.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void CompletedSaveCapturesInventoryAndDetachedPhoneData()
        {
            GameObject owner = new GameObject("Chapter2CompleteSaveTest");
            owner.SetActive(false);
            try
            {
                Chapter2SaveManager manager =
                    owner.AddComponent<Chapter2SaveManager>();
                Chapter2SaveData data = Chapter2SaveData.CreateDefault();
                SetPrivateField(manager, "currentData", data);
                SetPrivateField(manager, "saveService", new MemorySaveService());

                InventoryController inventory =
                    owner.AddComponent<InventoryController>();
                ItemDefinition document =
                    AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                        DocumentItemPath);
                inventory.AddItem(document);

                Chapter1SaveData phone = Chapter1SaveData.CreateDefault();
                phone.SavedPhoneRecordingIds.Add("carry_over_test");
                manager.SaveChapter2Completed(inventory, phone);
                phone.SavedPhoneRecordingIds.Clear();

                Assert.IsTrue(data.Chapter2Completed);
                Assert.AreEqual(
                    Chapter2SaveData.EndingConversationFinalStep,
                    data.Mission05MinhConversationStep);
                Assert.AreEqual(1, data.ChapterEndInventory.Count);
                Assert.AreEqual(
                    Chapter2WifiSignalScannerMission
                        .ClassifiedDocumentItemId,
                    data.ChapterEndInventory[0].ItemId);
                CollectionAssert.Contains(
                    data.PhoneData.SavedPhoneRecordingIds,
                    "carry_over_test");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Chapter3HospitalSceneIsEnabledForTransition()
        {
            bool foundEnabled = false;
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == Chapter3ScenePath &&
                    scenes[i].enabled)
                {
                    foundEnabled = true;
                    break;
                }
            }

            Assert.IsTrue(foundEnabled);
            Assert.AreEqual(
                "Abandoned Hospital",
                Chapter2SceneTransitionController.Chapter3SceneName);
        }

        private static GameObject FindChild(
            GameObject root,
            string objectName)
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null &&
                    children[i].name == objectName)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Missing field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private sealed class MemorySaveService : IChapter2SaveService
        {
            private Chapter2SaveData data =
                Chapter2SaveData.CreateDefault();

            public string SavePath => "memory://chapter2-ending";

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
