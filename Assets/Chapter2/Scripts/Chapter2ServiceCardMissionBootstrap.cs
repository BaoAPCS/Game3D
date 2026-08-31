using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    public static class Chapter2ServiceCardMissionBootstrap
    {
        private const string PoliceStationSceneName = "Police_Station";
        private const string BedCameraObjectName = "Bed_cam";
        private const string CrowbarObjectName = "crow_bar";
        private const string ToiletObjectName = "Toilet";
        private const string ServiceCardObjectName = "service_card";
        private const string ToiletInteractionZoneName =
            "Chapter2_ToiletInteractionZone";
        private const string InteractableLayerName = "Interactable";
        private const float ToiletZoneHorizontalPadding = 0.55f;
        private const float ToiletZoneVerticalPadding = 0.15f;
        private const string CrowbarResourcePath =
            "Inventory/CrowBarItem";
        private const string ServiceCardResourcePath =
            "Inventory/ServiceCardItem";

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
                FindSceneComponent<Chapter2ServiceCardMission>(scene) !=
                null)
            {
                return;
            }

            GameObject bedObject = FindSceneObject(
                scene,
                BedCameraObjectName);
            GameObject crowbarObject = FindSceneObject(
                scene,
                CrowbarObjectName);
            GameObject toiletObject = FindSceneObject(
                scene,
                ToiletObjectName);
            GameObject serviceCardObject = FindSceneObject(
                scene,
                ServiceCardObjectName);
            Chapter1InputReader inputReader =
                FindSceneComponent<Chapter1InputReader>(scene);

            if (bedObject == null ||
                crowbarObject == null ||
                toiletObject == null ||
                serviceCardObject == null ||
                inputReader == null)
            {
                Debug.LogError(
                    "[Chapter2Mission01] Police_Station thiếu Bed_cam, crow_bar, Toilet, service_card hoặc Player.");
                return;
            }

            Camera bedCamera = bedObject.GetComponent<Camera>();
            AudioListener bedAudioListener =
                bedObject.GetComponent<AudioListener>();
            if (bedCamera != null)
            {
                bedCamera.enabled = false;
            }

            if (bedAudioListener != null)
            {
                bedAudioListener.enabled = false;
            }

            SphereCollider bedCollider =
                bedObject.GetComponent<SphereCollider>();
            if (bedCollider == null)
            {
                bedCollider = bedObject.AddComponent<SphereCollider>();
                bedCollider.radius = 1.25f;
            }

            bedCollider.isTrigger = true;
            EnsureMinimumWorldRadius(bedCollider, 1.25f);

            Collider toiletCollider =
                toiletObject.GetComponent<Collider>();
            if (toiletCollider == null)
            {
                Debug.LogError(
                    "[Chapter2Mission01] Toilet chưa có Collider.",
                    toiletObject);
                return;
            }

            toiletCollider.isTrigger = false;
            BoxCollider toiletInteractionCollider =
                EnsureToiletInteractionZone(
                    toiletObject,
                    toiletCollider);

            int interactableLayer =
                LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Chapter2Mission01] Layer '{InteractableLayerName}' không tồn tại.");
                return;
            }

            bedObject.layer = interactableLayer;
            SetLayerRecursively(
                serviceCardObject.transform,
                interactableLayer);
            EnsureServiceCardCollider(serviceCardObject);

            ItemDefinition crowbarDefinition =
                Resources.Load<ItemDefinition>(
                    CrowbarResourcePath);
            ItemDefinition serviceCardDefinition =
                Resources.Load<ItemDefinition>(
                    ServiceCardResourcePath);
            if (crowbarDefinition == null ||
                serviceCardDefinition == null)
            {
                Debug.LogError(
                    "[Chapter2Mission01] Thiếu ItemDefinition crow_bar hoặc service_card trong Chapter2/Resources/Inventory.");
                return;
            }

            Chapter2MissionTriggerZone bedZone =
                GetOrAdd<Chapter2MissionTriggerZone>(bedObject);
            Chapter2MissionTriggerZone toiletZone =
                GetOrAdd<Chapter2MissionTriggerZone>(
                    toiletInteractionCollider.gameObject);
            bedZone.Configure(bedCollider, inputReader);
            toiletZone.Configure(
                toiletInteractionCollider,
                inputReader);

            Chapter2BedInspectionInteractable bedInteractable =
                GetOrAdd<Chapter2BedInspectionInteractable>(bedObject);
            Chapter2ServiceCardInteractable cardInteractable =
                GetOrAdd<Chapter2ServiceCardInteractable>(
                    serviceCardObject);

            Camera gameplayCamera = FindGameplayCamera(
                scene,
                bedCamera);
            Chapter2SaveManager saveManager =
                Chapter2SaveManager.EnsureForScene(scene);

            GameObject missionObject = new GameObject(
                "Chapter2_Mission01_ServiceCard");
            SceneManager.MoveGameObjectToScene(missionObject, scene);
            Chapter2ServiceCardMission mission =
                missionObject.AddComponent<
                    Chapter2ServiceCardMission>();
            mission.Configure(
                crowbarObject,
                serviceCardObject,
                bedCamera,
                bedAudioListener,
                toiletCollider,
                bedZone,
                toiletZone,
                bedInteractable,
                cardInteractable,
                inputReader,
                gameplayCamera,
                crowbarDefinition,
                serviceCardDefinition,
                saveManager);
        }

        private static BoxCollider EnsureToiletInteractionZone(
            GameObject toiletObject,
            Collider solidCollider)
        {
            Transform zoneTransform =
                toiletObject.transform.Find(
                    ToiletInteractionZoneName);
            GameObject zoneObject;
            if (zoneTransform == null)
            {
                zoneObject = new GameObject(
                    ToiletInteractionZoneName);
                zoneTransform = zoneObject.transform;
                zoneTransform.SetParent(
                    toiletObject.transform,
                    false);
            }
            else
            {
                zoneObject = zoneTransform.gameObject;
            }

            zoneObject.layer = toiletObject.layer;
            zoneTransform.localPosition = Vector3.zero;
            zoneTransform.localRotation = Quaternion.identity;
            zoneTransform.localScale = Vector3.one;

            BoxCollider zoneCollider =
                GetOrAdd<BoxCollider>(zoneObject);
            Vector3 localScale = toiletObject.transform.lossyScale;
            Vector3 baseCenter;
            Vector3 baseSize;
            if (solidCollider is BoxCollider solidBox)
            {
                baseCenter = solidBox.center;
                baseSize = solidBox.size;
            }
            else
            {
                Bounds bounds = solidCollider.bounds;
                baseCenter = toiletObject.transform.InverseTransformPoint(
                    bounds.center);
                baseSize = new Vector3(
                    bounds.size.x /
                    Mathf.Max(0.0001f, Mathf.Abs(localScale.x)),
                    bounds.size.y /
                    Mathf.Max(0.0001f, Mathf.Abs(localScale.y)),
                    bounds.size.z /
                    Mathf.Max(0.0001f, Mathf.Abs(localScale.z)));
            }

            baseSize.x += 2f * ToiletZoneHorizontalPadding /
                Mathf.Max(0.0001f, Mathf.Abs(localScale.x));
            baseSize.z += 2f * ToiletZoneHorizontalPadding /
                Mathf.Max(0.0001f, Mathf.Abs(localScale.z));
            baseSize.y += 2f * ToiletZoneVerticalPadding /
                Mathf.Max(0.0001f, Mathf.Abs(localScale.y));

            zoneCollider.center = baseCenter;
            zoneCollider.size = baseSize;
            zoneCollider.isTrigger = true;
            zoneCollider.enabled = true;
            return zoneCollider;
        }

        private static void EnsureServiceCardCollider(
            GameObject serviceCardObject)
        {
            Collider[] colliders =
                serviceCardObject.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                SphereCollider sphere =
                    serviceCardObject.AddComponent<SphereCollider>();
                float worldScale = GetLargestWorldScale(
                    serviceCardObject.transform);
                sphere.radius = 0.25f / worldScale;
                sphere.isTrigger = true;
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].isTrigger = true;
                }
            }
        }

        private static float GetLargestWorldScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            return Mathf.Max(
                0.0001f,
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));
        }

        private static void EnsureMinimumWorldRadius(
            SphereCollider sphere,
            float minimumWorldRadius)
        {
            if (sphere == null || minimumWorldRadius <= 0f)
            {
                return;
            }

            Vector3 scale = sphere.transform.lossyScale;
            float smallestAxisScale = Mathf.Max(
                0.0001f,
                Mathf.Min(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z)));
            float requiredLocalRadius =
                minimumWorldRadius / smallestAxisScale;
            sphere.radius = Mathf.Max(
                sphere.radius,
                requiredLocalRadius);
        }

        private static Camera FindGameplayCamera(
            Scene scene,
            Camera excludedCamera)
        {
            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include);
            Camera fallback = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate == null ||
                    candidate == excludedCamera ||
                    candidate.gameObject.scene != scene)
                {
                    continue;
                }

                if (candidate.CompareTag("MainCamera"))
                {
                    return candidate;
                }

                fallback ??= candidate;
            }

            return fallback;
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
                for (int transformIndex = 0;
                     transformIndex < transforms.Length;
                     transformIndex++)
                {
                    Transform candidate = transforms[transformIndex];
                    if (candidate != null &&
                        string.Equals(
                            candidate.gameObject.name,
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
                UnityEngine.Object.FindObjectsByType<T>(
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
