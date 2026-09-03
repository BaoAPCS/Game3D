namespace DormitoryMystery.Chapter1
{
    public interface IInventoryItemUseHandler
    {
        bool CanUseInventoryItem(InventoryItem item);
        bool TryUseInventoryItem(InventoryItem item);
    }
}
