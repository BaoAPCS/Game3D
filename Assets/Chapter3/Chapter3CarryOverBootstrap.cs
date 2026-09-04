using System;
using System.Collections;
using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using DormitoryMystery.Chapter2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter3
{
    [DisallowMultipleComponent]
    public sealed class Chapter3CarryOverBootstrap : MonoBehaviour
    {
        public const string Chapter3SceneName = "Abandoned Hospital";
        public const string RuntimeObjectName =
            "Chapter3_CarryOverBootstrap";

        private Chapter2SaveData chapter2Data;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            InstallForScene(scene);
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != Chapter3SceneName)
            {
                return;
            }

            Chapter3WoodenObstacleController.InstallForScene(scene);

            Chapter3CarryOverBootstrap[] existing =
                FindObjectsByType<Chapter3CarryOverBootstrap>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null &&
                    existing[i].gameObject.scene == scene)
                {
                    return;
                }
            }

            GameObject owner = new GameObject(RuntimeObjectName);
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.AddComponent<Chapter3CarryOverBootstrap>();
        }

        private void Start()
        {
            chapter2Data = new JsonChapter2SaveService().Load();
            chapter2Data.EnsureValidDefaults();
            if (!chapter2Data.Chapter2Completed)
            {
                enabled = false;
                return;
            }

            StartCoroutine(RestoreAfterSceneBootstraps());
        }

        private IEnumerator RestoreAfterSceneBootstraps()
        {
            // Restore repeatedly over the first two frames so the Chapter 1
            // key sync and runtime phone factory cannot overwrite carry-over
            // due to scene-callback ordering.
            RestoreInventoryAndPhone();
            yield return null;
            RestoreInventoryAndPhone();
            yield return null;
            RestoreInventoryAndPhone();
            enabled = false;
        }

        private void RestoreInventoryAndPhone()
        {
            Scene scene = gameObject.scene;
            InventoryController inventory =
                FindSceneComponent<InventoryController>(scene);
            if (inventory == null)
            {
                return;
            }

            Mission3PoliceKeyInventorySync keySync =
                inventory.GetComponent<Mission3PoliceKeyInventorySync>();
            if (keySync != null)
            {
                keySync.enabled = false;
            }

            inventory.SetStartingItems(Array.Empty<ItemDefinition>());
            inventory.EnsureStartingItems();
            inventory.ClearItems();

            List<Chapter2InventoryEntry> snapshot =
                chapter2Data.ChapterEndInventory;
            if (snapshot == null || snapshot.Count == 0)
            {
                snapshot = BuildLegacySnapshot(chapter2Data);
            }

            Dictionary<string, ItemDefinition> definitions =
                LoadInventoryDefinitions();
            for (int i = 0; i < snapshot.Count; i++)
            {
                Chapter2InventoryEntry entry = snapshot[i];
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.ItemId) ||
                    !definitions.TryGetValue(
                        entry.ItemId,
                        out ItemDefinition definition))
                {
                    if (entry != null)
                    {
                        Debug.LogWarning(
                            $"[Chapter3CarryOver] Không tìm thấy ItemDefinition cho '{entry.ItemId}'.",
                            this);
                    }

                    continue;
                }

                inventory.AddItem(
                    definition,
                    Mathf.Max(1, entry.Quantity));
            }

            ConfigurePhone(scene);
        }

        private void ConfigurePhone(Scene scene)
        {
            BackpackPhoneInputController backpack =
                FindSceneComponent<BackpackPhoneInputController>(scene);
            PhoneUIController phone = backpack != null
                ? backpack.PhoneUIController
                : FindSceneComponent<PhoneUIController>(scene);
            if (phone == null)
            {
                return;
            }

            chapter2Data.PhoneData ??= Chapter2PhoneData.CreateDefault();
            phone.ConfigureCarriedPhoneData(
                chapter2Data.PhoneData.ToChapter1SaveData(),
                chapter2Data.Mission04PoliceWifiConnected);
            phone.ConfigureWifiNetwork(
                "Police_Station_Wifi",
                chapter2Data.Mission04PoliceWifiConnected,
                chapter2Data.Mission04WifiPasswordDiscovered,
                null);
            phone.ConfigureMinhMissionMessages(
                chapter2Data.Mission04PoliceWifiConnected,
                chapter2Data.Mission04MinhMessagesRead,
                null);
            phone.ConfigureChapter2EndingConversation(
                chapter2Data.Mission05MinhConversationAvailable,
                chapter2Data.Mission05MinhConversationOpened,
                chapter2Data.Mission05MinhConversationStep,
                null,
                null,
                null);
        }

        private static Dictionary<string, ItemDefinition>
            LoadInventoryDefinitions()
        {
            ItemDefinition[] loaded =
                Resources.LoadAll<ItemDefinition>("Inventory");
            Dictionary<string, ItemDefinition> definitions =
                new Dictionary<string, ItemDefinition>(
                    StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < loaded.Length; i++)
            {
                ItemDefinition definition = loaded[i];
                if (definition != null &&
                    !string.IsNullOrWhiteSpace(definition.ItemId))
                {
                    definitions[definition.ItemId] = definition;
                }
            }

            return definitions;
        }

        private static List<Chapter2InventoryEntry> BuildLegacySnapshot(
            Chapter2SaveData data)
        {
            List<Chapter2InventoryEntry> entries =
                new List<Chapter2InventoryEntry>();
            AddLegacyItem(
                entries,
                Mission3PoliceKeyInventorySync.PoliceKeyItemId,
                data.Mission03PoliceKeyRecovered);
            AddLegacyItem(
                entries,
                Chapter2ConfiscatedItemsMission.PhoneItemId,
                data.Mission03PhoneRecovered);
            AddLegacyItem(
                entries,
                Chapter2ServiceCardMission.CrowbarItemId,
                data.Mission01CrowbarCollected);
            AddLegacyItem(
                entries,
                Chapter2ServiceCardMission.ServiceCardItemId,
                data.Mission01ServiceCardCollected);
            AddLegacyItem(
                entries,
                Chapter2WifiSignalScannerMission
                    .ClassifiedDocumentItemId,
                data.Mission05SecretDocumentCollected);
            return entries;
        }

        private static void AddLegacyItem(
            List<Chapter2InventoryEntry> entries,
            string itemId,
            bool owned)
        {
            if (owned)
            {
                entries.Add(new Chapter2InventoryEntry
                {
                    ItemId = itemId,
                    Quantity = 1
                });
            }
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] candidates = FindObjectsByType<T>(
                FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                T candidate = candidates[i];
                if (candidate != null &&
                    candidate.gameObject.scene == scene)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
