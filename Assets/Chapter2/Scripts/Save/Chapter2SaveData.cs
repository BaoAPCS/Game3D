using System;
using System.Collections.Generic;

namespace DormitoryMystery.Chapter2
{
    [Serializable]
    public sealed class Chapter2SaveData
    {
        public const int CurrentSaveVersion = 1;
        public const int EndingConversationFinalStep = 4;

        public int SaveVersion = CurrentSaveVersion;
        public bool HasPhone;
        public bool HasPoliceStationKey;
        public bool Mission01CrowbarCollected;
        public bool Mission01ToiletPried;
        public bool Mission01ServiceCardCollected;
        public bool Mission02JailObstacleDisabled;
        public bool Mission03PhoneRecovered;
        public bool Mission03PoliceKeyRecovered;
        public bool Mission03ClosetUnlocked;
        public bool Mission04ComputerUnlocked;
        public bool Mission04WifiPasswordDiscovered;
        public bool Mission04PoliceWifiConnected;
        public bool Mission04MinhMessagesRead;
        public bool Mission05ScannerActivated;
        public bool Mission05RouterInspected;
        public bool Mission05SecretDocumentCollected;
        public bool Mission05BrokenDoorUnlocked;
        public bool Mission05SecretDocumentViewed;
        public bool Mission05MinhConversationAvailable;
        public bool Mission05MinhConversationOpened;
        public int Mission05MinhConversationStep;
        public bool Chapter2Completed;
        public bool Chapter1PhoneDataImported;
        public bool Chapter1CarryOverInventoryApplied;
        public Chapter2PhoneData PhoneData;
        public List<Chapter2InventoryEntry> ChapterEndInventory;

        public bool Mission03Completed =>
            Mission03PhoneRecovered &&
            Mission03PoliceKeyRecovered &&
            Mission03ClosetUnlocked;

        public bool Mission04Completed =>
            Mission04MinhMessagesRead;

        public bool Mission05Completed =>
            Mission05SecretDocumentCollected;

        public static Chapter2SaveData CreateDefault()
        {
            Chapter2SaveData data = new Chapter2SaveData();
            data.EnsureValidDefaults();
            return data;
        }

        public void EnsureValidDefaults()
        {
            SaveVersion = CurrentSaveVersion;
            PhoneData ??= Chapter2PhoneData.CreateDefault();
            PhoneData.EnsureValidDefaults();
            ChapterEndInventory ??= new List<Chapter2InventoryEntry>();
            NormalizeChapterEndInventory();

            Mission05MinhConversationStep = Math.Max(
                0,
                Math.Min(
                    EndingConversationFinalStep,
                    Mission05MinhConversationStep));

            if (Chapter2Completed ||
                Mission05MinhConversationStep >=
                EndingConversationFinalStep)
            {
                Chapter2Completed = true;
                Mission05MinhConversationAvailable = true;
                Mission05MinhConversationOpened = true;
                Mission05MinhConversationStep =
                    EndingConversationFinalStep;
            }

            if (Mission05MinhConversationOpened ||
                Mission05MinhConversationStep > 0)
            {
                Mission05MinhConversationAvailable = true;
            }

            if (Mission05MinhConversationAvailable)
            {
                Mission05SecretDocumentViewed = true;
            }

            if (Mission05SecretDocumentViewed)
            {
                Mission05SecretDocumentCollected = true;
            }

            // Chapter 2 starts after James gave Nam this key. Older Chapter 2
            // saves did not preserve that carry-over item, so migrate them
            // once without treating key ownership as Mission 2 progress.
            if (!Chapter1CarryOverInventoryApplied)
            {
                Mission03PoliceKeyRecovered = true;
                Chapter1CarryOverInventoryApplied = true;
            }

            if (Mission05SecretDocumentCollected)
            {
                Mission05RouterInspected = true;
            }

            if (Mission05BrokenDoorUnlocked ||
                Mission05RouterInspected)
            {
                Mission05ScannerActivated = true;
            }

            if (Mission05ScannerActivated)
            {
                Mission04MinhMessagesRead = true;
            }

            if (Mission04MinhMessagesRead)
            {
                Mission04PoliceWifiConnected = true;
            }

            if (Mission04PoliceWifiConnected)
            {
                Mission04WifiPasswordDiscovered = true;
            }

            if (Mission04WifiPasswordDiscovered)
            {
                Mission04ComputerUnlocked = true;
            }

            if (Mission04ComputerUnlocked)
            {
                Mission03PhoneRecovered = true;
                Mission03PoliceKeyRecovered = true;
                Mission03ClosetUnlocked = true;
            }

            if (Mission03PhoneRecovered)
            {
                Mission03PoliceKeyRecovered = true;
                Mission03ClosetUnlocked = true;
            }

            // Legacy version-1 saves incorrectly marked these confiscated
            // items as owned. The new recovery flags are absent (false) in
            // those files, so they are the authoritative state from now on.
            HasPhone = Mission03PhoneRecovered;
            HasPoliceStationKey = Mission03PoliceKeyRecovered;

            if (Mission03PhoneRecovered ||
                Mission03ClosetUnlocked)
            {
                Mission02JailObstacleDisabled = true;
            }

            if (Mission02JailObstacleDisabled)
            {
                Mission01ServiceCardCollected = true;
            }

            if (Mission01ServiceCardCollected)
            {
                Mission01ToiletPried = true;
            }

            if (Mission01ToiletPried)
            {
                Mission01CrowbarCollected = true;
            }
        }

        public Chapter2SaveData DeepCopy()
        {
            Chapter2SaveData copy = new Chapter2SaveData
            {
                SaveVersion = SaveVersion,
                HasPhone = HasPhone,
                HasPoliceStationKey = HasPoliceStationKey,
                Mission01CrowbarCollected =
                    Mission01CrowbarCollected,
                Mission01ToiletPried = Mission01ToiletPried,
                Mission01ServiceCardCollected =
                    Mission01ServiceCardCollected,
                Mission02JailObstacleDisabled =
                    Mission02JailObstacleDisabled,
                Mission03PhoneRecovered =
                    Mission03PhoneRecovered,
                Mission03PoliceKeyRecovered =
                    Mission03PoliceKeyRecovered,
                Mission03ClosetUnlocked =
                    Mission03ClosetUnlocked,
                Mission04ComputerUnlocked =
                    Mission04ComputerUnlocked,
                Mission04WifiPasswordDiscovered =
                    Mission04WifiPasswordDiscovered,
                Mission04PoliceWifiConnected =
                    Mission04PoliceWifiConnected,
                Mission04MinhMessagesRead =
                    Mission04MinhMessagesRead,
                Mission05ScannerActivated =
                    Mission05ScannerActivated,
                Mission05RouterInspected =
                    Mission05RouterInspected,
                Mission05SecretDocumentCollected =
                    Mission05SecretDocumentCollected,
                Mission05BrokenDoorUnlocked =
                    Mission05BrokenDoorUnlocked,
                Mission05SecretDocumentViewed =
                    Mission05SecretDocumentViewed,
                Mission05MinhConversationAvailable =
                    Mission05MinhConversationAvailable,
                Mission05MinhConversationOpened =
                    Mission05MinhConversationOpened,
                Mission05MinhConversationStep =
                    Mission05MinhConversationStep,
                Chapter2Completed = Chapter2Completed,
                Chapter1PhoneDataImported =
                    Chapter1PhoneDataImported,
                Chapter1CarryOverInventoryApplied =
                    Chapter1CarryOverInventoryApplied,
                PhoneData = PhoneData?.DeepCopy() ??
                    Chapter2PhoneData.CreateDefault(),
                ChapterEndInventory = CopyInventoryEntries(
                    ChapterEndInventory)
            };
            copy.EnsureValidDefaults();
            return copy;
        }

        private void NormalizeChapterEndInventory()
        {
            for (int i = 0; i < ChapterEndInventory.Count; i++)
            {
                Chapter2InventoryEntry entry = ChapterEndInventory[i];
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    ChapterEndInventory.RemoveAt(i--);
                    continue;
                }

                entry.ItemId = entry.ItemId.Trim();
                entry.Quantity = Math.Max(1, entry.Quantity);
                for (int duplicateIndex = i + 1;
                     duplicateIndex < ChapterEndInventory.Count;
                     duplicateIndex++)
                {
                    Chapter2InventoryEntry duplicate =
                        ChapterEndInventory[duplicateIndex];
                    if (duplicate == null ||
                        !string.Equals(
                            entry.ItemId,
                            duplicate.ItemId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    entry.Quantity += Math.Max(1, duplicate.Quantity);
                    ChapterEndInventory.RemoveAt(duplicateIndex--);
                }
            }
        }

        private static List<Chapter2InventoryEntry> CopyInventoryEntries(
            List<Chapter2InventoryEntry> source)
        {
            List<Chapter2InventoryEntry> copy =
                new List<Chapter2InventoryEntry>();
            if (source == null)
            {
                return copy;
            }

            for (int i = 0; i < source.Count; i++)
            {
                Chapter2InventoryEntry entry = source[i];
                if (entry != null)
                {
                    copy.Add(new Chapter2InventoryEntry
                    {
                        ItemId = entry.ItemId,
                        Quantity = entry.Quantity
                    });
                }
            }

            return copy;
        }
    }

    [Serializable]
    public sealed class Chapter2InventoryEntry
    {
        public string ItemId;
        public int Quantity = 1;
    }
}
