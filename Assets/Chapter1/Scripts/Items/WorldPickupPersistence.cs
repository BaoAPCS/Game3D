using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class WorldPickupPersistence : MonoBehaviour
    {
        [SerializeField] private string persistentId;
        [SerializeField] private bool hideWhenCollected = true;

        private ItemPickup itemPickup;

        public string PersistentId => persistentId;

        private void Awake()
        {
            itemPickup = GetComponent<ItemPickup>();
        }

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(persistentId))
            {
                Debug.LogWarning($"[WorldPickupPersistence] GameObject '{gameObject.name}' chưa có persistentId.", this);
                return;
            }

            Chapter1Manager manager = Chapter1Manager.Instance;
            if (manager != null && IsCollected(manager.CurrentData) && hideWhenCollected)
            {
                HideCollectedObject();
            }
        }

        public void SetPersistentId(string id)
        {
            persistentId = id ?? string.Empty;
        }

        public bool IsCollected(Chapter1SaveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(persistentId))
            {
                return false;
            }

            data.EnsureValidDefaults();
            return data.CollectedUniqueItemIds.Contains(persistentId);
        }

        public void RecordCollected(Chapter1Manager manager)
        {
            if (manager == null || string.IsNullOrWhiteSpace(persistentId))
            {
                return;
            }

            Chapter1SaveData data = manager.CurrentData;
            data.EnsureValidDefaults();
            data.CollectedUniqueItemIds ??= new List<string>();
            if (!data.CollectedUniqueItemIds.Contains(persistentId))
            {
                data.CollectedUniqueItemIds.Add(persistentId);
                manager.SaveChapter();
            }
        }

        public void HideCollectedObject()
        {
            if (itemPickup == null)
            {
                itemPickup = GetComponent<ItemPickup>();
            }

            if (itemPickup != null)
            {
                itemPickup.ApplyCollectedState();
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
