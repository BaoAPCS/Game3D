using DormitoryMystery.Chapter1;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2SaveManager : MonoBehaviour
    {
        public static Chapter2SaveManager Instance { get; private set; }

        [SerializeField] private bool autoLoadOnAwake = true;

        private IChapter2SaveService saveService;
        private Chapter2SaveData currentData;

        public Chapter2SaveData CurrentData =>
            currentData ??= Chapter2SaveData.CreateDefault();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError(
                    $"[Chapter2SaveManager] Phát hiện instance trùng trên '{gameObject.name}'. Component trùng sẽ bị disable.",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
            saveService = new JsonChapter2SaveService();
            if (autoLoadOnAwake)
            {
                LoadChapter2();
            }
            else
            {
                currentData = Chapter2SaveData.CreateDefault();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static Chapter2SaveManager EnsureForScene(Scene scene)
        {
            if (Instance != null)
            {
                return Instance;
            }

            Chapter2SaveManager[] managers =
                Object.FindObjectsByType<Chapter2SaveManager>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < managers.Length; i++)
            {
                Chapter2SaveManager manager = managers[i];
                if (manager != null &&
                    manager.gameObject.scene == scene)
                {
                    return manager;
                }
            }

            GameObject managerObject = new GameObject(
                "Chapter2SaveManager");
            SceneManager.MoveGameObjectToScene(managerObject, scene);
            return managerObject.AddComponent<Chapter2SaveManager>();
        }

        public void LoadChapter2()
        {
            EnsureSaveService();
            currentData = saveService.Load();
            currentData.EnsureValidDefaults();
        }

        public void SaveChapter2()
        {
            EnsureSaveService();
            CurrentData.EnsureValidDefaults();
            saveService.Save(CurrentData);
        }

        /// <summary>
        /// Copies Chapter 1's phone payload exactly once. After this point the
        /// Chapter 2 save owns its detached snapshot and no longer depends on
        /// later changes to the Chapter 1 save.
        /// </summary>
        public bool ImportChapter1PhoneData(
            Chapter1SaveData chapter1Data)
        {
            if (chapter1Data == null ||
                CurrentData.Chapter1PhoneDataImported)
            {
                return false;
            }

            CurrentData.PhoneData =
                Chapter2PhoneData.FromChapter1(chapter1Data);
            CurrentData.Chapter1PhoneDataImported = true;
            SaveChapter2();
            return true;
        }

        public void SaveMission01Progress(
            bool crowbarCollected,
            bool toiletPried,
            bool serviceCardCollected)
        {
            CurrentData.Mission01CrowbarCollected =
                crowbarCollected;
            CurrentData.Mission01ToiletPried = toiletPried;
            CurrentData.Mission01ServiceCardCollected =
                serviceCardCollected;
            SaveChapter2();
        }

        public void ResetMission01()
        {
            CurrentData.Mission01CrowbarCollected = false;
            CurrentData.Mission01ToiletPried = false;
            CurrentData.Mission01ServiceCardCollected = false;
            CurrentData.Mission02JailObstacleDisabled = false;
            ResetMission03State();
            SaveChapter2();
        }

        public void ResetMission02()
        {
            CurrentData.Mission02JailObstacleDisabled = false;
            ResetMission03State();
            SaveChapter2();
        }

        public void SaveMission02Completed()
        {
            CurrentData.Mission02JailObstacleDisabled = true;
            SaveChapter2();
        }

        public void SaveMission03Progress(
            bool phoneRecovered,
            bool policeKeyRecovered)
        {
            CurrentData.Mission03PhoneRecovered = phoneRecovered;
            CurrentData.Mission03PoliceKeyRecovered =
                policeKeyRecovered;
            if (phoneRecovered)
            {
                CurrentData.Mission03ClosetUnlocked = true;
            }

            CurrentData.HasPhone = phoneRecovered;
            CurrentData.HasPoliceStationKey = policeKeyRecovered;
            SaveChapter2();
        }

        public void SaveMission03ClosetUnlocked()
        {
            CurrentData.Mission03ClosetUnlocked = true;
            CurrentData.Mission03PoliceKeyRecovered = true;
            CurrentData.HasPoliceStationKey = true;
            SaveChapter2();
        }

        public void ResetMission03()
        {
            ResetMission03State();
            SaveChapter2();
        }

        public void SaveMission04ComputerUnlocked()
        {
            CurrentData.Mission04ComputerUnlocked = true;
            SaveChapter2();
        }

        public void SaveMission04WifiPasswordDiscovered()
        {
            CurrentData.Mission04WifiPasswordDiscovered = true;
            SaveChapter2();
        }

        public void SaveMission04WifiConnected()
        {
            CurrentData.Mission04PoliceWifiConnected = true;
            SaveChapter2();
        }

        public void SaveMission04MinhMessagesRead()
        {
            CurrentData.Mission04MinhMessagesRead = true;
            SaveChapter2();
        }

        public void ResetMission04()
        {
            ResetMission04State();
            SaveChapter2();
        }

        public void SaveMission05RouterInspected()
        {
            CurrentData.Mission05RouterInspected = true;
            SaveChapter2();
        }

        public void SaveMission05ScannerActivated()
        {
            CurrentData.Mission05ScannerActivated = true;
            SaveChapter2();
        }

        public void SaveMission05DocumentCollected()
        {
            CurrentData.Mission05SecretDocumentCollected = true;
            SaveChapter2();
        }

        public void SaveMission05DocumentViewed()
        {
            CurrentData.Mission05SecretDocumentViewed = true;
            SaveChapter2();
        }

        public void SaveMission05MinhConversationAvailable()
        {
            CurrentData.Mission05MinhConversationAvailable = true;
            SaveChapter2();
        }

        public void SaveMission05MinhConversationOpened()
        {
            CurrentData.Mission05MinhConversationOpened = true;
            SaveChapter2();
        }

        public void SaveMission05MinhConversationStep(int step)
        {
            CurrentData.Mission05MinhConversationStep = Mathf.Clamp(
                step,
                0,
                Chapter2SaveData.EndingConversationFinalStep);
            SaveChapter2();
        }

        public void SaveMission05BrokenDoorUnlocked()
        {
            CurrentData.Mission05BrokenDoorUnlocked = true;
            SaveChapter2();
        }

        public void SaveChapter2Completed(
            InventoryController inventory,
            Chapter1SaveData phoneData)
        {
            CurrentData.Mission05MinhConversationAvailable = true;
            CurrentData.Mission05MinhConversationOpened = true;
            CurrentData.Mission05MinhConversationStep =
                Chapter2SaveData.EndingConversationFinalStep;
            CurrentData.Chapter2Completed = true;
            CurrentData.PhoneData =
                Chapter2PhoneData.FromChapter1(phoneData);
            CurrentData.ChapterEndInventory =
                CaptureInventory(inventory);
            SaveChapter2();
        }

        public void ResetMission05()
        {
            ResetMission05State();
            SaveChapter2();
        }

        private void ResetMission03State()
        {
            CurrentData.Mission03PhoneRecovered = false;
            CurrentData.Mission03PoliceKeyRecovered = true;
            CurrentData.Mission03ClosetUnlocked = false;
            CurrentData.HasPhone = false;
            CurrentData.HasPoliceStationKey = true;
            CurrentData.Chapter1CarryOverInventoryApplied = true;
            ResetMission04State();
        }

        private void ResetMission04State()
        {
            CurrentData.Mission04ComputerUnlocked = false;
            CurrentData.Mission04WifiPasswordDiscovered = false;
            CurrentData.Mission04PoliceWifiConnected = false;
            CurrentData.Mission04MinhMessagesRead = false;
            ResetMission05State();
        }

        private void ResetMission05State()
        {
            CurrentData.Mission05ScannerActivated = false;
            CurrentData.Mission05RouterInspected = false;
            CurrentData.Mission05SecretDocumentCollected = false;
            CurrentData.Mission05BrokenDoorUnlocked = false;
            CurrentData.Mission05SecretDocumentViewed = false;
            CurrentData.Mission05MinhConversationAvailable = false;
            CurrentData.Mission05MinhConversationOpened = false;
            CurrentData.Mission05MinhConversationStep = 0;
            CurrentData.Chapter2Completed = false;
            CurrentData.ChapterEndInventory ??=
                new List<Chapter2InventoryEntry>();
            CurrentData.ChapterEndInventory.Clear();
        }

        private static List<Chapter2InventoryEntry> CaptureInventory(
            InventoryController inventory)
        {
            List<Chapter2InventoryEntry> entries =
                new List<Chapter2InventoryEntry>();
            if (inventory == null)
            {
                return entries;
            }

            IReadOnlyList<InventoryItem> items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                InventoryItem item = items[i];
                if (item?.Definition == null ||
                    string.IsNullOrWhiteSpace(
                        item.Definition.ItemId))
                {
                    continue;
                }

                entries.Add(new Chapter2InventoryEntry
                {
                    ItemId = item.Definition.ItemId,
                    Quantity = item.Quantity
                });
            }

            return entries;
        }

        private void EnsureSaveService()
        {
            saveService ??= new JsonChapter2SaveService();
        }
    }
}
