using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    public static class Chapter2CircuitPuzzleBootstrap
    {
        private const string PoliceStationSceneName = "Police_Station";
        private const string ElectricBoxObjectName = "Electric_box";
        private const string JailObstacleObjectName = "JailObstacle";
        private const string InteractionPointName =
            "Chapter2_ElectricInteractionPoint";
        private const string InteractableLayerName = "Interactable";

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
                FindSceneComponent<Chapter2CircuitPuzzleMission>(scene) !=
                null)
            {
                return;
            }

            GameObject electricBox = FindSceneObject(
                scene,
                ElectricBoxObjectName);
            GameObject jailObstacle = FindSceneObject(
                scene,
                JailObstacleObjectName);
            Chapter1InputReader inputReader =
                FindSceneComponent<Chapter1InputReader>(scene);
            if (electricBox == null ||
                jailObstacle == null ||
                inputReader == null)
            {
                Debug.LogError(
                    "[Chapter2Mission02] Police_Station thiếu Electric_box, JailObstacle hoặc Player.");
                return;
            }

            Collider electricCollider =
                electricBox.GetComponent<Collider>();
            Collider jailCollider =
                jailObstacle.GetComponent<Collider>();
            if (electricCollider == null || jailCollider == null)
            {
                Debug.LogError(
                    "[Chapter2Mission02] Electric_box hoặc JailObstacle chưa có Collider.");
                return;
            }

            int interactableLayer =
                LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Chapter2Mission02] Layer '{InteractableLayerName}' không tồn tại.");
                return;
            }

            electricCollider.isTrigger = false;
            electricCollider.enabled = true;
            jailCollider.isTrigger = false;
            jailCollider.enabled = true;
            SetLayerRecursively(
                electricBox.transform,
                interactableLayer);
            Transform interactionPoint = EnsureInteractionPoint(
                electricBox.transform,
                electricCollider.bounds.center,
                interactableLayer);

            Chapter2CircuitBoxInteractable interactable =
                GetOrAdd<Chapter2CircuitBoxInteractable>(electricBox);
            Chapter2SaveManager saveManager =
                Chapter2SaveManager.EnsureForScene(scene);

            GameObject missionObject = new GameObject(
                "Chapter2_Mission02_CircuitPuzzle");
            SceneManager.MoveGameObjectToScene(missionObject, scene);
            Chapter2CircuitPuzzleMission mission =
                missionObject.AddComponent<Chapter2CircuitPuzzleMission>();
            interactable.Configure(mission, interactionPoint);
            mission.Configure(
                saveManager,
                interactable,
                inputReader,
                jailObstacle);
        }

        private static Transform EnsureInteractionPoint(
            Transform electricBox,
            Vector3 worldPosition,
            int layer)
        {
            Transform interactionPoint =
                electricBox.Find(InteractionPointName);
            if (interactionPoint == null)
            {
                GameObject pointObject = new GameObject(
                    InteractionPointName);
                interactionPoint = pointObject.transform;
                interactionPoint.SetParent(electricBox, true);
            }

            interactionPoint.position = worldPosition;
            interactionPoint.rotation = Quaternion.identity;
            interactionPoint.localScale = Vector3.one;
            interactionPoint.gameObject.layer = layer;
            return interactionPoint;
        }

        private static GameObject FindSceneObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex].GetComponentsInChildren<Transform>(
                        true);
                for (int index = 0;
                     index < transforms.Length;
                     index++)
                {
                    Transform candidate = transforms[index];
                    if (candidate != null &&
                        string.Equals(
                            candidate.name,
                            objectName,
                            System.StringComparison.Ordinal))
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] candidates =
                Object.FindObjectsByType<T>(
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
