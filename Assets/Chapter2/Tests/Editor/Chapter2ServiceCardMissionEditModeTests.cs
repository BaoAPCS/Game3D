using System.IO;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2ServiceCardMissionEditModeTests
    {
        private const string ScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";

        [Test]
        public void PryProgressIncreasesAndClampsAtOne()
        {
            Assert.AreEqual(
                0.52f,
                Chapter2ServiceCardMission.ApplyPryPress(0.4f, 0.12f),
                0.0001f);
            Assert.AreEqual(
                1f,
                Chapter2ServiceCardMission.ApplyPryPress(0.95f, 0.12f));
            Assert.AreEqual(
                0.4f,
                Chapter2ServiceCardMission.ApplyPryPress(0.4f, -1f));
        }

        [Test]
        public void PryProgressDecaysAndClampsAtZero()
        {
            Assert.AreEqual(
                0.24f,
                Chapter2ServiceCardMission.ApplyPryDecay(
                    0.6f,
                    0.18f,
                    2f),
                0.0001f);
            Assert.AreEqual(
                0f,
                Chapter2ServiceCardMission.ApplyPryDecay(
                    0.1f,
                    0.18f,
                    2f));
            Assert.AreEqual(
                0.6f,
                Chapter2ServiceCardMission.ApplyPryDecay(
                    0.6f,
                    -1f,
                    2f));
        }

        [Test]
        public void MissionInventoryDefinitionsAreUniqueQuestItems()
        {
            ItemDefinition crowbar =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/Chapter2/Resources/Inventory/CrowBarItem.asset");
            ItemDefinition serviceCard =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/Chapter2/Resources/Inventory/ServiceCardItem.asset");

            Assert.NotNull(crowbar);
            Assert.NotNull(serviceCard);
            Assert.AreEqual(Chapter2ServiceCardMission.CrowbarItemId,
                crowbar.ItemId);
            Assert.AreEqual(Chapter2ServiceCardMission.ServiceCardItemId,
                serviceCard.ItemId);
            Assert.AreEqual(ItemCategory.Tool, crowbar.Category);
            Assert.AreEqual(ItemCategory.MissionItem, serviceCard.Category);
            Assert.IsFalse(crowbar.IsStackable);
            Assert.IsFalse(crowbar.IsDroppable);
            Assert.IsFalse(serviceCard.IsStackable);
            Assert.IsFalse(serviceCard.IsDroppable);
        }

        [Test]
        public void PoliceStationStartsWithInspectionCameraOffAndToiletSolid()
        {
            string sceneYaml = File.ReadAllText(ScenePath);
            string bedListener = ExtractComponentBlock(
                sceneYaml,
                "--- !u!81 &665260199");
            string bedCamera = ExtractComponentBlock(
                sceneYaml,
                "--- !u!20 &665260200");
            string toiletCollider = ExtractComponentBlock(
                sceneYaml,
                "--- !u!65 &1092700388");

            StringAssert.Contains("m_Enabled: 0", bedListener);
            StringAssert.Contains("m_Enabled: 0", bedCamera);
            StringAssert.Contains("m_IsTrigger: 0", toiletCollider);
        }

        [Test]
        public void Mission02SceneObjectsStartSolidAndObstacleActive()
        {
            string sceneYaml = File.ReadAllText(ScenePath);
            string electricCollider = ExtractComponentBlock(
                sceneYaml,
                "--- !u!65 &1347780889");
            string jailObstacle = ExtractComponentBlock(
                sceneYaml,
                "--- !u!1 &873468386");
            string jailCollider = ExtractComponentBlock(
                sceneYaml,
                "--- !u!65 &873468388");

            StringAssert.Contains("m_IsTrigger: 0", electricCollider);
            StringAssert.Contains("m_Name: JailObstacle", jailObstacle);
            StringAssert.Contains("m_IsActive: 1", jailObstacle);
            StringAssert.Contains("m_IsTrigger: 0", jailCollider);
        }

        [Test]
        public void EmptyMissionPromptDeactivatesItsBlackPanel()
        {
            GameObject owner = new GameObject("Chapter2UiTest");
            try
            {
                Chapter2ServiceCardMissionUI ui =
                    Chapter2ServiceCardMissionUI.Create(
                        owner.transform);
                GameObject promptPanel = FindChild(
                    owner,
                    "MissionPrompt");

                Assert.NotNull(promptPanel);
                Assert.IsFalse(promptPanel.activeSelf);

                ui.SetPrompt("[F] Quan sát");
                Assert.IsTrue(promptPanel.activeSelf);

                ui.SetPrompt(string.Empty);
                Assert.IsFalse(promptPanel.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void CircuitUiStartsHiddenAndBuildsFiveByFiveGrid()
        {
            GameObject owner = new GameObject("Chapter2CircuitUiTest");
            try
            {
                Chapter2CircuitPuzzleUI ui =
                    Chapter2CircuitPuzzleUI.Create(owner.transform);
                Assert.IsFalse(ui.IsVisible);

                int tileCount = 0;
                Transform[] children =
                    owner.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null &&
                        children[i].name.StartsWith(
                            "CircuitTile_",
                            System.StringComparison.Ordinal))
                    {
                        tileCount++;
                    }
                }

                Assert.AreEqual(25, tileCount);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ElectricBoxPromptUsesRequestedActivationText()
        {
            GameObject owner = new GameObject("ElectricBoxPromptTest");
            try
            {
                Chapter2CircuitBoxInteractable interactable =
                    owner.AddComponent<Chapter2CircuitBoxInteractable>();
                Assert.AreEqual(
                    "[F] Kích hoạt",
                    interactable.GetInteractionPrompt(default));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Chapter2SaveIsIndependentFromChapter1Save()
        {
            string testDirectory = Path.Combine(
                Path.GetTempPath(),
                "Chapter2SaveTests_" + System.Guid.NewGuid().ToString("N"));
            string chapter1Path = Path.Combine(
                testDirectory,
                "chapter1_save.json");
            string chapter2Path = Path.Combine(
                testDirectory,
                "chapter2_save.json");
            Directory.CreateDirectory(testDirectory);

            try
            {
                const string chapter1Sentinel =
                    "CHAPTER_1_MUST_NOT_CHANGE";
                File.WriteAllText(chapter1Path, chapter1Sentinel);

                JsonChapter2SaveService service =
                    new JsonChapter2SaveService(chapter2Path);
                Chapter2SaveData data =
                    Chapter2SaveData.CreateDefault();
                data.Mission02JailObstacleDisabled = true;
                service.Save(data);

                Chapter2SaveData loaded = service.Load();
                Assert.IsTrue(loaded.Mission01CrowbarCollected);
                Assert.IsTrue(loaded.Mission01ToiletPried);
                Assert.IsTrue(loaded.Mission01ServiceCardCollected);
                Assert.IsTrue(loaded.Mission02JailObstacleDisabled);
                Assert.AreEqual(
                    chapter1Sentinel,
                    File.ReadAllText(chapter1Path));

                service.DeleteSave();
                Assert.IsFalse(File.Exists(chapter2Path));
                Assert.AreEqual(
                    chapter1Sentinel,
                    File.ReadAllText(chapter1Path));
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        private static GameObject FindChild(
            GameObject root,
            string objectName)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null &&
                    transforms[i].name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static string ExtractComponentBlock(
            string sceneYaml,
            string componentHeader)
        {
            int start = sceneYaml.IndexOf(
                componentHeader,
                System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0,
                $"Missing scene component {componentHeader}.");

            int end = sceneYaml.IndexOf(
                "\n--- !u!",
                start + componentHeader.Length,
                System.StringComparison.Ordinal);
            return end >= 0
                ? sceneYaml.Substring(start, end - start)
                : sceneYaml.Substring(start);
        }
    }
}
