using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    /// <summary>
    /// Restores Chapter 2's starting inventory from the independent Chapter 2
    /// save. No Chapter 1 save data is read or modified in Police_Station.
    /// </summary>
    public static class Chapter2CarryOverBootstrap
    {
        private const string PoliceStationSceneName = "Police_Station";
        private const string PhoneItemId = "phone";

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
            inventory.EnsureStartingItems();

            Mission3PoliceKeyInventorySync chapter1KeySync =
                inventory.GetComponent<
                    Mission3PoliceKeyInventorySync>();
            if (chapter1KeySync != null)
            {
                chapter1KeySync.enabled = false;
            }

            RestorePoliceKey(data, inventory);
            VerifyPhone(data, inventory);
        }

        private static void RestorePoliceKey(
            Chapter2SaveData data,
            InventoryController inventory)
        {
            if (!data.HasPoliceStationKey)
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

        private static void VerifyPhone(
            Chapter2SaveData data,
            InventoryController inventory)
        {
            if (!data.HasPhone)
            {
                return;
            }

            if (!inventory.HasItem(PhoneItemId))
            {
                Debug.LogError(
                    "[Chapter2CarryOver] Player prefab không khôi phục " +
                    "được điện thoại khởi đầu vào balo của Nam.");
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
