using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public enum ItemCategory
    {
        Phone,
        Document,
        Key,
        Tool,
        Quest,
        MissionItem,
        Other
    }

    [CreateAssetMenu(menuName = "Dormitory Mystery/Chapter 1/Inventory/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite previewImage;
        [SerializeField, TextArea(2, 6)] private string description;
        [SerializeField] private ItemCategory category = ItemCategory.Other;
        [SerializeField] private bool isStackable;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private bool isDroppable = true;
        [SerializeField] private bool isUsable;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Sprite PreviewImage => previewImage != null
            ? previewImage
            : icon;
        public string Description => description;
        public ItemCategory Category => category;
        public bool IsStackable => isStackable;
        public int MaxStack => Mathf.Max(1, maxStack);
        public bool IsDroppable => isDroppable;
        public bool IsUsable => isUsable;

        private void OnValidate()
        {
            itemId = (itemId ?? string.Empty).Trim();
            displayName = (displayName ?? string.Empty).Trim();
            maxStack = Mathf.Max(1, maxStack);
            if (!isStackable)
            {
                maxStack = 1;
            }
        }
    }
}
