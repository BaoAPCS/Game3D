using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class InventoryController : MonoBehaviour
    {
        [SerializeField] private List<ItemDefinition> startingItems = new List<ItemDefinition>();
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        private bool startingItemsApplied;

        public event Action InventoryChanged;
        public event Action<InventoryItem> ItemAdded;

        public IReadOnlyList<InventoryItem> Items => items;

        private void Awake()
        {
            EnsureStartingItems();
        }

        private void OnValidate()
        {
            RemoveInvalidAndDuplicateItems();
        }

        public void SetStartingItems(IEnumerable<ItemDefinition> definitions)
        {
            startingItems.Clear();
            if (definitions != null)
            {
                foreach (ItemDefinition definition in definitions)
                {
                    if (definition != null && !startingItems.Contains(definition))
                    {
                        startingItems.Add(definition);
                    }
                }
            }
        }

        public void EnsureStartingItems()
        {
            RemoveInvalidAndDuplicateItems();
            if (startingItemsApplied)
            {
                return;
            }

            startingItemsApplied = true;
            bool changed = false;
            for (int i = 0; i < startingItems.Count; i++)
            {
                changed |= AddItemInternal(startingItems[i], 1, false);
            }

            if (changed)
            {
                RaiseInventoryChanged();
            }
        }

        public bool HasItem(string itemId)
        {
            return GetItem(itemId) != null;
        }

        public InventoryItem GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            for (int i = 0; i < items.Count; i++)
            {
                InventoryItem item = items[i];
                if (item != null
                    && item.Definition != null
                    && string.Equals(item.Definition.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        public bool AddItem(ItemDefinition definition, int quantity = 1)
        {
            EnsureStartingItems();
            if (!AddItemInternal(definition, quantity, true))
            {
                return false;
            }

            RaiseInventoryChanged();
            return true;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            EnsureStartingItems();
            InventoryItem item = GetItem(itemId);
            if (item == null || quantity <= 0)
            {
                return false;
            }

            int remaining = item.Quantity - quantity;
            if (remaining <= 0)
            {
                items.Remove(item);
            }
            else
            {
                item.SetQuantity(remaining);
            }

            RaiseInventoryChanged();
            return true;
        }

        public void ClearItems()
        {
            EnsureStartingItems();
            if (items.Count == 0)
            {
                return;
            }

            items.Clear();
            RaiseInventoryChanged();
        }

        private bool AddItemInternal(ItemDefinition definition, int quantity, bool raiseItemAdded)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId) || quantity <= 0)
            {
                return false;
            }

            InventoryItem existing = GetItem(definition.ItemId);
            if (existing != null)
            {
                if (!definition.IsStackable)
                {
                    return false;
                }

                int oldQuantity = existing.Quantity;
                existing.SetQuantity(oldQuantity + quantity);
                return existing.Quantity != oldQuantity;
            }

            InventoryItem item = new InventoryItem(definition, definition.IsStackable ? quantity : 1);
            items.Add(item);
            if (raiseItemAdded)
            {
                ItemAdded?.Invoke(item);
            }

            return true;
        }

        private void RemoveInvalidAndDuplicateItems()
        {
            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = items.Count - 1; i >= 0; i--)
            {
                InventoryItem item = items[i];
                ItemDefinition definition = item != null ? item.Definition : null;
                string itemId = definition != null ? definition.ItemId : string.Empty;
                if (definition == null || string.IsNullOrWhiteSpace(itemId) || seenIds.Contains(itemId))
                {
                    items.RemoveAt(i);
                    continue;
                }

                seenIds.Add(itemId);
                item.SetQuantity(item.Quantity);
            }
        }

        private void RaiseInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }
    }
}
