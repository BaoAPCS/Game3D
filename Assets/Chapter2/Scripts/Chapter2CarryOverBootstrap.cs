using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Restores the Chapter 1 carry-over inventory when Police_Station is
    /// loaded. The police-station key remains a story item in Chapter 2 and
    /// deliberately has no interaction with the jail geometry.
    /// </summary>
    public static class Chapter2CarryOverBootstrap
    {
        private const string PoliceStationSceneName = "Police_Station";

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

            Chapter1Manager manager = EnsureChapterManager(scene);
            InventoryController inventory =
                FindSceneComponent<InventoryController>(scene);
            if (inventory == null)
            {
                Debug.LogError(
                    "[Chapter2CarryOver] Không tìm thấy InventoryController " +
                    "của Nam trong scene Police_Station.");
                return;
            }

            Chapter1SaveData data = manager.CurrentData;
            data.EnsureValidDefaults();
            inventory.EnsureStartingItems();

            if (!data.ChapterCompleted ||
                !data.Mission03PoliceArrestCompleted)
            {
                Debug.LogWarning(
                    "[Chapter2CarryOver] Save Chapter 1 chưa hoàn thành; " +
                    "không khôi phục vật phẩm chuyển chương.");
                return;
            }

            RestorePoliceKey(data, inventory);
            VerifyPhone(data, inventory);
        }

        private static Chapter1Manager EnsureChapterManager(Scene scene)
        {
            if (Chapter1Manager.Instance != null)
            {
                return Chapter1Manager.Instance;
            }

            GameObject managerObject = new GameObject(
                "ChapterProgressManager");
            SceneManager.MoveGameObjectToScene(managerObject, scene);
            return managerObject.AddComponent<Chapter1Manager>();
        }

        private static void RestorePoliceKey(
            Chapter1SaveData data,
            InventoryController inventory)
        {
            if (!data.HasChapterCarryOverItem(
                    Chapter1SaveData.PoliceKeyCarryOverItemId))
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
            Chapter1SaveData data,
            InventoryController inventory)
        {
            if (!data.HasChapterCarryOverItem(
                    Chapter1SaveData.PhoneCarryOverItemId))
            {
                return;
            }

            if (!inventory.HasItem(
                    Chapter1SaveData.PhoneCarryOverItemId))
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
