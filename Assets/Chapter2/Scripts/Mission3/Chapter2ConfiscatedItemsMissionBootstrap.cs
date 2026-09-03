using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    public static class Chapter2ConfiscatedItemsMissionBootstrap
    {
        private const string PoliceStationSceneName = "Police_Station";
        private const string OfficeEnvironmentName =
            "Office Environment";
        private const string ClosetObjectName = "Closet";
        private const string InteractableLayerName = "Interactable";
        private const string PhoneResourcePath =
            "Inventory/Chapter2PhoneItem";
        private const string PoliceKeyResourcePath =
            "Inventory/PoliceStationKeyItem";

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
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForScene(scene);
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                scene.name != PoliceStationSceneName ||
                FindSceneComponent<
                    Chapter2ConfiscatedItemsMission>(scene) != null)
            {
                return;
            }

            GameObject closet = FindCloset(scene);
            Chapter1InputReader inputReader =
                FindSceneComponent<Chapter1InputReader>(scene);
            InventoryController inventory = inputReader != null
                ? inputReader.GetComponent<InventoryController>()
                : null;
            if (closet == null || inputReader == null ||
                inventory == null)
            {
                Debug.LogError(
                    "[Chapter2Mission03] Police_Station thiếu Closet trong Office Environment hoặc Player.");
                return;
            }

            int interactableLayer =
                LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Chapter2Mission03] Layer '{InteractableLayerName}' không tồn tại.");
                return;
            }

            SphereCollider sphere =
                closet.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = closet.AddComponent<SphereCollider>();
                sphere.radius = 1.1f;
            }

            sphere.enabled = true;
            sphere.isTrigger = true;

            BoxCollider solidCollider =
                closet.GetComponent<BoxCollider>();
            if (solidCollider != null)
            {
                solidCollider.enabled = true;
                solidCollider.isTrigger = false;
            }

            SetLayerRecursively(closet.transform, interactableLayer);

            ItemDefinition phoneDefinition =
                Resources.Load<ItemDefinition>(PhoneResourcePath);
            ItemDefinition policeKeyDefinition =
                Resources.Load<ItemDefinition>(PoliceKeyResourcePath);
            if (phoneDefinition == null || policeKeyDefinition == null)
            {
                Debug.LogError(
                    "[Chapter2Mission03] Thiếu ItemDefinition điện thoại hoặc chìa khóa trong Resources.");
                return;
            }

            Chapter2MissionTriggerZone triggerZone =
                GetOrAdd<Chapter2MissionTriggerZone>(closet);
            triggerZone.Configure(sphere, inputReader);

            Chapter2ClosetInteractable interactable =
                GetOrAdd<Chapter2ClosetInteractable>(closet);
            Chapter2SaveManager saveManager =
                Chapter2SaveManager.EnsureForScene(scene);

            GameObject missionObject = new GameObject(
                "Chapter2_Mission03_ConfiscatedItems");
            SceneManager.MoveGameObjectToScene(missionObject, scene);
            Chapter2ConfiscatedItemsMission mission =
                missionObject.AddComponent<
                    Chapter2ConfiscatedItemsMission>();
            interactable.Configure(mission, triggerZone);
            mission.Configure(
                saveManager,
                interactable,
                triggerZone,
                inputReader,
                inventory,
                phoneDefinition,
                policeKeyDefinition);
        }

        private static GameObject FindCloset(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!string.Equals(
                        roots[i].name,
                        OfficeEnvironmentName,
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject closet = FindDescendant(
                    roots[i].transform,
                    ClosetObjectName);
                if (closet != null)
                {
                    return closet;
                }
            }

            return null;
        }

        private static GameObject FindDescendant(
            Transform root,
            string objectName)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.name,
                        objectName,
                        System.StringComparison.Ordinal))
                {
                    return candidate.gameObject;
                }
            }

            return null;
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

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null
                ? existing
                : target.AddComponent<T>();
        }

        private static void SetLayerRecursively(
            Transform root,
            int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }
    }
}
