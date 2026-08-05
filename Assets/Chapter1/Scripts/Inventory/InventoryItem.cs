using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [Serializable]
    public sealed class InventoryItem
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField, Min(1)] private int quantity = 1;

        public InventoryItem(ItemDefinition definition, int quantity)
        {
            this.definition = definition;
            SetQuantity(quantity);
        }

        public ItemDefinition Definition => definition;
        public int Quantity => Mathf.Max(1, quantity);
        public string ItemId => definition != null ? definition.ItemId : string.Empty;

        internal void SetDefinition(ItemDefinition itemDefinition)
        {
            definition = itemDefinition;
        }

        internal void SetQuantity(int value)
        {
            int maxStack = definition != null ? definition.MaxStack : int.MaxValue;
            quantity = Mathf.Clamp(value, 1, Mathf.Max(1, maxStack));
        }
    }
}
