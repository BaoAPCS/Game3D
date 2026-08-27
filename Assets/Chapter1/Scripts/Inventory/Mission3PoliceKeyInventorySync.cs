using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Keeps the save-backed Mission 3 key mirrored in Nam's runtime
    /// InventoryController. Inventory contents are scene-local, so this
    /// component restores the key after a scene load without duplicating it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Mission3PoliceKeyInventorySync : MonoBehaviour
    {
        public const string PoliceKeyItemId = "police_station_key";

        private const string PoliceKeyResourcePath =
            "Inventory/PoliceStationKeyItem";

        private static ItemDefinition policeKeyDefinition;
        private InventoryController inventory;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            policeKeyDefinition = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
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
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            InventoryController[] inventories =
                FindObjectsByType<InventoryController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < inventories.Length; i++)
            {
                InventoryController candidate = inventories[i];
                if (candidate == null ||
                    candidate.gameObject.scene != scene)
                {
                    continue;
                }

                Mission3PoliceKeyInventorySync sync =
                    candidate.GetComponent<
                        Mission3PoliceKeyInventorySync>();
                if (sync == null)
                {
                    sync = candidate.gameObject.AddComponent<
                        Mission3PoliceKeyInventorySync>();
                }

                sync.inventory = candidate;
                sync.SynchronizeWithSave();
            }
        }

        private void Awake()
        {
            inventory = GetComponent<InventoryController>();
            SynchronizeWithSave();
        }

        private void OnEnable()
        {
            Chapter1EventBus.ObjectiveChanged +=
                HandleStoryProgressChanged;
            SynchronizeWithSave();
        }

        private void OnDisable()
        {
            Chapter1EventBus.ObjectiveChanged -=
                HandleStoryProgressChanged;
        }

        public static bool TryGrantPoliceKey(
            GameObject playerObject = null)
        {
            return TryGrantPoliceKey(
                playerObject,
                out _);
        }

        public static bool TryGrantPoliceKey(
            GameObject playerObject,
            out bool addedThisAttempt)
        {
            addedThisAttempt = false;
            InventoryController targetInventory =
                FindInventory(playerObject);
            ItemDefinition definition = GetPoliceKeyDefinition();
            if (targetInventory == null || definition == null)
            {
                return false;
            }

            if (!targetInventory.HasItem(PoliceKeyItemId))
            {
                addedThisAttempt = targetInventory.AddItem(definition);
            }

            return targetInventory.HasItem(PoliceKeyItemId);
        }

        public static bool RollbackPoliceKeyGrant(
            GameObject playerObject,
            bool addedThisAttempt)
        {
            if (!addedThisAttempt)
            {
                return false;
            }

            InventoryController targetInventory =
                FindInventory(playerObject);
            return targetInventory != null &&
                   targetInventory.RemoveItem(PoliceKeyItemId);
        }

        private void HandleStoryProgressChanged(string objective)
        {
            SynchronizeWithSave();
        }

        private void SynchronizeWithSave()
        {
            inventory ??= GetComponent<InventoryController>();
            if (inventory == null)
            {
                return;
            }

            bool shouldOwnKey =
                Chapter1Manager.Instance != null &&
                Chapter1Manager.Instance.CurrentData
                    .Mission03PoliceKeyReceived;
            bool ownsKey = inventory.HasItem(PoliceKeyItemId);

            if (shouldOwnKey && !ownsKey)
            {
                ItemDefinition definition = GetPoliceKeyDefinition();
                if (definition != null)
                {
                    inventory.AddItem(definition);
                }
            }
            else if (!shouldOwnKey && ownsKey)
            {
                inventory.RemoveItem(PoliceKeyItemId);
            }
        }

        private static InventoryController FindInventory(
            GameObject playerObject)
        {
            if (playerObject != null)
            {
                InventoryController playerInventory =
                    playerObject.GetComponent<InventoryController>() ??
                    playerObject.GetComponentInParent<
                        InventoryController>();
                if (playerInventory != null)
                {
                    return playerInventory;
                }
            }

            return FindAnyObjectByType<InventoryController>(
                FindObjectsInactive.Include);
        }

        private static ItemDefinition GetPoliceKeyDefinition()
        {
            if (policeKeyDefinition == null)
            {
                policeKeyDefinition = Resources.Load<ItemDefinition>(
                    PoliceKeyResourcePath);
                if (policeKeyDefinition == null)
                {
                    Debug.LogError(
                        "[Mission3Key] Không tìm thấy ItemDefinition tại " +
                        $"Resources/{PoliceKeyResourcePath}.");
                }
            }

            return policeKeyDefinition;
        }
    }
}
