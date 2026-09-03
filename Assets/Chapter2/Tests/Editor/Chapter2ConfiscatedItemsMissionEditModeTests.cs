using System.IO;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2ConfiscatedItemsMissionEditModeTests
    {
        private const string ScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";

        [Test]
        public void NewChapter2SaveStartsWithOnlyPoliceKey()
        {
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();

            Assert.IsFalse(data.HasPhone);
            Assert.IsTrue(data.HasPoliceStationKey);
            Assert.IsFalse(data.Mission03PhoneRecovered);
            Assert.IsTrue(data.Mission03PoliceKeyRecovered);
            Assert.IsFalse(data.Mission03ClosetUnlocked);
            Assert.IsFalse(data.Mission03Completed);
            Assert.IsFalse(data.Mission02JailObstacleDisabled);
        }

        [Test]
        public void LegacyOwnedFlagsDoNotBypassConfiscatedItemsMission()
        {
            Chapter2SaveData legacy = new Chapter2SaveData
            {
                SaveVersion = 1,
                HasPhone = true,
                HasPoliceStationKey = true,
                Mission02JailObstacleDisabled = true
            };

            legacy.EnsureValidDefaults();

            Assert.IsFalse(legacy.HasPhone);
            Assert.IsTrue(legacy.HasPoliceStationKey);
            Assert.IsTrue(legacy.Mission03PoliceKeyRecovered);
            Assert.IsFalse(legacy.Mission03ClosetUnlocked);
            Assert.IsTrue(legacy.Mission02JailObstacleDisabled);
            Assert.IsTrue(legacy.Mission01ServiceCardCollected);
        }

        [Test]
        public void RecoveryFlagsAreSavedIndividuallyAndRestoreStoryChain()
        {
            Chapter2SaveData partial = new Chapter2SaveData
            {
                Mission03PhoneRecovered = true
            };

            Chapter2SaveData copy = partial.DeepCopy();

            Assert.IsTrue(copy.Mission03PhoneRecovered);
            Assert.IsTrue(copy.Mission03PoliceKeyRecovered);
            Assert.IsTrue(copy.Mission03ClosetUnlocked);
            Assert.IsTrue(copy.HasPhone);
            Assert.IsTrue(copy.HasPoliceStationKey);
            Assert.IsTrue(copy.Mission02JailObstacleDisabled);
            Assert.IsTrue(copy.Mission01ServiceCardCollected);
            Assert.IsTrue(copy.Mission03Completed);
        }

        [Test]
        public void ConfiscatedItemDefinitionsUseExpectedIdsAndIcons()
        {
            ItemDefinition phone =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/Chapter2/Resources/Inventory/Chapter2PhoneItem.asset");
            ItemDefinition policeKey =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/Chapter1/Resources/Inventory/PoliceStationKeyItem.asset");
            ItemDefinition crowbar =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/Chapter2/Resources/Inventory/CrowBarItem.asset");

            Assert.NotNull(phone);
            Assert.NotNull(policeKey);
            Assert.NotNull(crowbar);
            Assert.AreEqual(
                Chapter2ConfiscatedItemsMission.PhoneItemId,
                phone.ItemId);
            Assert.AreEqual(
                Chapter2ConfiscatedItemsMission.PoliceKeyItemId,
                policeKey.ItemId);
            Assert.NotNull(phone.Icon);
            Assert.NotNull(policeKey.Icon);
            Assert.IsFalse(
                policeKey.IsUsable,
                "Chapter 1 key asset must remain unchanged; Chapter 2 enables contextual use through the inventory handler.");
            Assert.NotNull(crowbar.Icon);
            Assert.AreEqual(
                "Assets/Chapter2/Sprites/crowbar.png",
                AssetDatabase.GetAssetPath(crowbar.Icon));

            TextureImporter importer = AssetImporter.GetAtPath(
                "Assets/Chapter2/Sprites/crowbar.png") as TextureImporter;
            Assert.NotNull(importer);
            Assert.AreEqual(TextureImporterType.Sprite,
                importer.textureType);
        }

        [Test]
        public void CrowbarCanBeAddedAndDisplayedByInventory()
        {
            ItemDefinition crowbar =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/Chapter2/Resources/Inventory/CrowBarItem.asset");
            GameObject player = new GameObject("InventoryTestPlayer");
            try
            {
                InventoryController inventory =
                    player.AddComponent<InventoryController>();

                Assert.IsTrue(inventory.AddItem(crowbar));
                Assert.IsTrue(inventory.HasItem(
                    Chapter2ServiceCardMission.CrowbarItemId));
                Assert.AreSame(
                    crowbar.Icon,
                    inventory.GetItem(
                        Chapter2ServiceCardMission.CrowbarItemId)
                        .Definition.Icon);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LockedClosetPromptDoesNotAdvertiseInteraction()
        {
            GameObject closet = new GameObject("ClosetPromptTest");
            try
            {
                Chapter2ClosetInteractable interactable =
                    closet.AddComponent<Chapter2ClosetInteractable>();
                Assert.AreEqual(
                    "Tủ bị khóa",
                    interactable.GetInteractionPrompt(default));
            }
            finally
            {
                Object.DestroyImmediate(closet);
            }
        }

        [Test]
        public void ConfiscatedItemsUiStartsHiddenAndShowsOnlyPhone()
        {
            GameObject owner = new GameObject("Mission03UiTest");
            try
            {
                Chapter2ConfiscatedItemsUI ui =
                    Chapter2ConfiscatedItemsUI.Create(
                        owner.transform);

                Assert.IsFalse(ui.IsVisible);
                Assert.NotNull(FindChild(owner, "PhoneRow"));
                Assert.IsNull(FindChild(owner, "JamesKeyRow"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ClosetKeepsSolidColliderAndSeparateTriggerZone()
        {
            string sceneYaml = File.ReadAllText(ScenePath);
            string solidCollider = ExtractComponentBlock(
                sceneYaml,
                "--- !u!65 &1195724877");
            string triggerCollider = ExtractComponentBlock(
                sceneYaml,
                "--- !u!135 &1195724878");

            StringAssert.Contains("m_IsTrigger: 0", solidCollider);
            StringAssert.Contains("m_IsTrigger: 1", triggerCollider);
            StringAssert.Contains("m_Radius: 1", triggerCollider);
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
