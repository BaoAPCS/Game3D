using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        private const string Archive17Token = "item.Archive17";

        [SerializeField] private Chapter1Manager chapterManager;
        [SerializeField] private bool autoSaveOnChange = true;

        private readonly Dictionary<Chapter1ItemId, int> itemCounts = new Dictionary<Chapter1ItemId, int>();
        private bool initializedFromSave;
        private bool missingManagerLogged;

        public event Action InventoryChanged;
        public event Action<Chapter1ItemId, int> ItemCountChanged;

        public bool HasFlashlight => HasItem(Chapter1ItemId.Flashlight);
        public bool HasFuse => HasItem(Chapter1ItemId.Fuse);
        public bool HasHardDrive => HasItem(Chapter1ItemId.HardDrive);
        public int ThrowableCanCount => GetCount(Chapter1ItemId.ThrowableCan);

        private void Awake()
        {
            ResolveManager();
        }

        private void Start()
        {
            LoadFromManagerIfNeeded();
        }

        public void SetChapterManager(Chapter1Manager manager)
        {
            chapterManager = manager;
            initializedFromSave = false;
            LoadFromManagerIfNeeded();
        }

        public bool HasItem(Chapter1ItemId itemId)
        {
            return GetCount(itemId) > 0;
        }

        public int GetCount(Chapter1ItemId itemId)
        {
            if (itemId == Chapter1ItemId.None)
            {
                return 0;
            }

            return itemCounts.TryGetValue(itemId, out int count) ? Mathf.Max(0, count) : 0;
        }

        public bool AddItem(Chapter1ItemId itemId, int amount = 1)
        {
            LoadFromManagerIfNeeded();
            if (!IsValidChange(itemId, amount))
            {
                return false;
            }

            if (IsUniqueItem(itemId) && HasItem(itemId))
            {
                return false;
            }

            int newCount = IsUniqueItem(itemId) ? 1 : GetCount(itemId) + amount;
            itemCounts[itemId] = newCount;
            CommitInventoryChange(itemId, newCount);
            return true;
        }

        public bool RemoveItem(Chapter1ItemId itemId, int amount = 1)
        {
            LoadFromManagerIfNeeded();
            if (!IsValidChange(itemId, amount))
            {
                return false;
            }

            int currentCount = GetCount(itemId);
            if (currentCount < amount)
            {
                return false;
            }

            int newCount = Mathf.Max(0, currentCount - amount);
            if (newCount == 0)
            {
                itemCounts.Remove(itemId);
            }
            else
            {
                itemCounts[itemId] = newCount;
            }

            CommitInventoryChange(itemId, newCount);
            return true;
        }

        public bool ConsumeItem(Chapter1ItemId itemId, int amount = 1)
        {
            return RemoveItem(itemId, amount);
        }

        public void ClearInventory()
        {
            itemCounts.Clear();
            CommitInventoryChange(Chapter1ItemId.None, 0);
        }

        public void LoadFromSave(Chapter1SaveData data)
        {
            itemCounts.Clear();
            Chapter1SaveData safeData = data ?? Chapter1SaveData.CreateDefault();
            safeData.EnsureValidDefaults();

            SetCountFromSave(Chapter1ItemId.LanRecording, safeData.HasLanRecording ? 1 : 0);
            SetCountFromSave(Chapter1ItemId.Flashlight, safeData.HasFlashlight ? 1 : 0);
            SetCountFromSave(Chapter1ItemId.Fuse, safeData.HasFuse ? 1 : 0);
            SetCountFromSave(Chapter1ItemId.HardDrive, safeData.HasHardDrive ? 1 : 0);
            SetCountFromSave(Chapter1ItemId.ThrowableCan, safeData.ThrowableCanCount);
            SetCountFromSave(Chapter1ItemId.Archive17, safeData.CollectedUniqueItemIds.Contains(Archive17Token) ? 1 : 0);
            initializedFromSave = true;
            RaiseInventoryChanged(Chapter1ItemId.None, 0);
        }

        public void WriteToSave(Chapter1SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.EnsureValidDefaults();
            data.HasLanRecording = HasItem(Chapter1ItemId.LanRecording);
            data.HasFlashlight = HasItem(Chapter1ItemId.Flashlight);
            data.HasFuse = HasItem(Chapter1ItemId.Fuse);
            data.HasHardDrive = HasItem(Chapter1ItemId.HardDrive);
            data.ThrowableCanCount = GetCount(Chapter1ItemId.ThrowableCan);
            SetCollectedToken(data, Archive17Token, HasItem(Chapter1ItemId.Archive17));
        }

        private void LoadFromManagerIfNeeded()
        {
            if (initializedFromSave)
            {
                return;
            }

            ResolveManager();
            if (chapterManager == null)
            {
                if (!missingManagerLogged)
                {
                    missingManagerLogged = true;
                    Debug.LogWarning($"[PlayerInventory] GameObject '{gameObject.name}' chưa có Chapter1Manager, inventory chưa thể đồng bộ save.", this);
                }

                return;
            }

            LoadFromSave(chapterManager.CurrentData);
        }

        private void ResolveManager()
        {
            if (chapterManager == null)
            {
                chapterManager = Chapter1Manager.Instance;
            }
        }

        private static bool IsValidChange(Chapter1ItemId itemId, int amount)
        {
            return itemId != Chapter1ItemId.None && amount > 0;
        }

        private static bool IsUniqueItem(Chapter1ItemId itemId)
        {
            return itemId == Chapter1ItemId.LanRecording
                || itemId == Chapter1ItemId.Flashlight
                || itemId == Chapter1ItemId.Fuse
                || itemId == Chapter1ItemId.HardDrive
                || itemId == Chapter1ItemId.Archive17;
        }

        private void SetCountFromSave(Chapter1ItemId itemId, int count)
        {
            int safeCount = Mathf.Max(0, count);
            if (safeCount > 0)
            {
                itemCounts[itemId] = IsUniqueItem(itemId) ? 1 : safeCount;
            }
        }

        private void CommitInventoryChange(Chapter1ItemId itemId, int newCount)
        {
            ResolveManager();
            if (chapterManager != null)
            {
                WriteToSave(chapterManager.CurrentData);
                if (autoSaveOnChange)
                {
                    chapterManager.SaveChapter();
                }
            }
            else if (!missingManagerLogged)
            {
                missingManagerLogged = true;
                Debug.LogWarning($"[PlayerInventory] GameObject '{gameObject.name}' thay đổi inventory nhưng chưa có Chapter1Manager để lưu.", this);
            }

            RaiseInventoryChanged(itemId, newCount);
        }

        private void RaiseInventoryChanged(Chapter1ItemId itemId, int newCount)
        {
            InventoryChanged?.Invoke();
            ItemCountChanged?.Invoke(itemId, newCount);
            Chapter1EventBus.RaiseInventoryChanged();
        }

        private static void SetCollectedToken(Chapter1SaveData data, string token, bool present)
        {
            data.CollectedUniqueItemIds ??= new List<string>();
            bool contains = data.CollectedUniqueItemIds.Contains(token);
            if (present && !contains)
            {
                data.CollectedUniqueItemIds.Add(token);
            }
            else if (!present && contains)
            {
                data.CollectedUniqueItemIds.Remove(token);
            }
        }
    }
}
