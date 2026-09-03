using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    /// <summary>
    /// Restores Chapter 2's starting inventory from the independent Chapter 2
    /// save. Phone contents are imported from Chapter 1 once, then owned by
    /// the Chapter 2 save and never synchronized back.
    /// </summary>
    public static class Chapter2CarryOverBootstrap
    {
        private const string PoliceStationSceneName = "Police_Station";
        private const string PhoneItemId = "phone";
        private const string PhoneResourcePath =
            "Inventory/Chapter2PhoneItem";

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
            RestoreForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            RestoreForScene(scene);
        }

        private static void RestoreForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != PoliceStationSceneName)
            {
                return;
            }

            Chapter2SaveManager manager =
                Chapter2SaveManager.EnsureForScene(scene);
            ImportChapter1PhoneDataOnce(manager);
            InventoryController inventory =
                FindSceneComponent<InventoryController>(scene);
            if (inventory == null)
            {
                Debug.LogError(
                    "[Chapter2CarryOver] Không tìm thấy InventoryController " +
                    "của Nam trong scene Police_Station.");
                return;
            }

            Chapter2SaveData data = manager.CurrentData;
            data.EnsureValidDefaults();
            inventory.SetStartingItems(
                System.Array.Empty<ItemDefinition>());
            inventory.EnsureStartingItems();

            Mission3PoliceKeyInventorySync chapter1KeySync =
                inventory.GetComponent<
                    Mission3PoliceKeyInventorySync>();
            if (chapter1KeySync != null)
            {
                chapter1KeySync.enabled = false;
            }

            ReconcilePhone(data, inventory);
            ReconcilePoliceKey(data, inventory);
            ConfigurePhoneContents(scene, data);
        }

        private static void ImportChapter1PhoneDataOnce(
            Chapter2SaveManager manager)
        {
            if (manager == null ||
                manager.CurrentData.Chapter1PhoneDataImported)
            {
                return;
            }

            JsonChapter1SaveService chapter1Save =
                new JsonChapter1SaveService();
            if (!chapter1Save.HasSave())
            {
                return;
            }

            manager.ImportChapter1PhoneData(chapter1Save.Load());
        }

        private static void ConfigurePhoneContents(
            Scene scene,
            Chapter2SaveData data)
        {
            PhoneUIController phone =
                FindSceneComponent<PhoneUIController>(scene);
            if (phone == null)
            {
                BackpackPhoneInputController backpackPhone =
                    FindSceneComponent<BackpackPhoneInputController>(
                        scene);
                phone = backpackPhone != null
                    ? backpackPhone.PhoneUIController
                    : null;
            }

            if (phone == null)
            {
                Debug.LogWarning(
                    "[Chapter2CarryOver] Chưa tìm thấy PhoneUIController " +
                    "để cấu hình dữ liệu điện thoại Chapter 2.");
                return;
            }

            data.PhoneData ??= Chapter2PhoneData.CreateDefault();
            phone.ConfigureCarriedPhoneData(
                data.PhoneData.ToChapter1SaveData(),
                data.Mission04PoliceWifiConnected);
        }

        private static void ReconcilePoliceKey(
            Chapter2SaveData data,
            InventoryController inventory)
        {
            inventory.RemoveItem(
                Mission3PoliceKeyInventorySync.PoliceKeyItemId);
            if (!data.Mission03PoliceKeyRecovered)
            {
                return;
            }

            if (!Mission3PoliceKeyInventorySync.TryGrantPoliceKey(
                    inventory.gameObject))
            {
                Debug.LogError(
                    "[Chapter2CarryOver] Không thể khôi phục chìa khóa " +
                    "của James vào balo của Nam.");
            }
        }

        private static void ReconcilePhone(
            Chapter2SaveData data,
            InventoryController inventory)
        {
            inventory.RemoveItem(PhoneItemId);
            if (!data.Mission03PhoneRecovered)
            {
                return;
            }

            ItemDefinition phoneDefinition =
                Resources.Load<ItemDefinition>(PhoneResourcePath);
            if (phoneDefinition == null)
            {
                Debug.LogError(
                    "[Chapter2CarryOver] Không tìm thấy ItemDefinition " +
                    $"tại Resources/{PhoneResourcePath}.");
                return;
            }

            if (!inventory.AddItem(phoneDefinition) &&
                !inventory.HasItem(PhoneItemId))
            {
                Debug.LogError(
                    "[Chapter2CarryOver] Không thể khôi phục điện thoại " +
                    "vào balo Chapter 2 của Nam.");
            }
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] candidates = Object.FindObjectsByType<T>(
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
