using System;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    public static class Chapter2WifiSignalScannerBootstrap
    {
        public const string PoliceStationSceneName = "Police_Station";
        public const string ScifiOfficeName = "ScifiOffice";
        public const string OfficeRootName = "Office 1";
        public const string RouterObjectName = "3d_router";
        public const string RouterCameraName = "router_cam";
        public const string InteractionZoneName =
            "Chapter2_RouterInteractionZone";
        public const string ClassifiedDocumentObjectName =
            "ClassifiedDocument";
        public const string ClassifiedDocumentResourcePath =
            "Inventory/ClassifiedDocumentItem";
        public const string InteractableLayerName = "Interactable";
        public const float InteractionZoneRadius = 1.1f;

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

        public static bool InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(
                    scene.name,
                    PoliceStationSceneName,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryFindRouterSetup(
                    scene,
                    out GameObject router,
                    out Collider signalCollider,
                    out Camera routerCamera))
            {
                Debug.LogError(
                    "[Chapter2Mission05] Không tìm thấy đúng ScifiOffice/Office 1/3d_router, collider hoặc router_cam.");
                return false;
            }

            DisableRouterCamera(routerCamera);
            if (FindSceneComponent<
                    Chapter2WifiSignalScannerMission>(scene) != null)
            {
                return true;
            }

            Chapter1InputReader inputReader =
                FindSceneComponent<Chapter1InputReader>(scene);
            InventoryController inventory = inputReader != null
                ? inputReader.GetComponent<InventoryController>()
                : null;
            if (inputReader == null || inventory == null)
            {
                Debug.LogError(
                    "[Chapter2Mission05] Không tìm thấy Player/InventoryController của Nam trong Police_Station.");
                return false;
            }

            ItemDefinition documentDefinition =
                Resources.Load<ItemDefinition>(
                    ClassifiedDocumentResourcePath);
            if (documentDefinition == null ||
                !string.Equals(
                    documentDefinition.ItemId,
                    Chapter2WifiSignalScannerMission
                        .ClassifiedDocumentItemId,
                    StringComparison.Ordinal))
            {
                Debug.LogError(
                    "[Chapter2Mission05] Thiếu ItemDefinition tài liệu mật tại Resources/" +
                    ClassifiedDocumentResourcePath + ".");
                return false;
            }

            int interactableLayer =
                LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Chapter2Mission05] Layer '{InteractableLayerName}' không tồn tại.");
                return false;
            }

            GameObject zoneObject = EnsureInteractionZone(
                scene,
                signalCollider.bounds.center,
                interactableLayer);
            SphereCollider zoneCollider =
                zoneObject.GetComponent<SphereCollider>();
            Chapter2MissionTriggerZone triggerZone =
                GetOrAdd<Chapter2MissionTriggerZone>(zoneObject);
            triggerZone.Configure(zoneCollider, inputReader);

            Chapter2RouterInteractable interactable =
                GetOrAdd<Chapter2RouterInteractable>(zoneObject);
            GameObject documentVisual =
                EnsureClassifiedDocumentVisual(
                    router.transform,
                    signalCollider,
                    routerCamera);

            Chapter2SaveManager saveManager =
                Chapter2SaveManager.EnsureForScene(scene);
            PhoneUIController phone =
                FindSceneComponent<PhoneUIController>(scene);

            GameObject missionObject = new GameObject(
                Chapter2WifiSignalScannerMission.MissionObjectName);
            SceneManager.MoveGameObjectToScene(missionObject, scene);
            Chapter2WifiSignalScannerMission mission =
                missionObject.AddComponent<
                    Chapter2WifiSignalScannerMission>();
            interactable.Configure(
                mission,
                triggerZone,
                router.transform);
            mission.Configure(
                saveManager,
                router.transform,
                signalCollider,
                routerCamera,
                triggerZone,
                interactable,
                inputReader,
                inventory,
                documentDefinition,
                documentVisual,
                phone);
            return true;
        }

        public static bool TryFindRouterSetup(
            Scene scene,
            out GameObject router,
            out Collider signalCollider,
            out Camera routerCamera)
        {
            router = null;
            signalCollider = null;
            routerCamera = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject scifiOffice = FindRoot(
                scene,
                ScifiOfficeName);
            Transform officeRoot = scifiOffice != null
                ? FindDirectChild(
                    scifiOffice.transform,
                    OfficeRootName)
                : null;
            Transform routerTransform = officeRoot != null
                ? FindDirectChild(officeRoot, RouterObjectName)
                : null;
            if (routerTransform == null)
            {
                return false;
            }

            router = routerTransform.gameObject;
            signalCollider =
                router.GetComponent<SphereCollider>();
            signalCollider ??= router.GetComponent<Collider>();

            Camera[] cameras = router.GetComponentsInChildren<Camera>(
                true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.gameObject.name,
                        RouterCameraName,
                        StringComparison.Ordinal))
                {
                    routerCamera = candidate;
                    break;
                }
            }

            return signalCollider != null && routerCamera != null;
        }

        public static void DisableRouterCamera(Camera routerCamera)
        {
            if (routerCamera == null)
            {
                return;
            }

            AudioListener listener =
                routerCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }

            routerCamera.enabled = false;
        }

        public static GameObject EnsureClassifiedDocumentVisual(
            Transform router,
            Collider signalCollider,
            Camera routerCamera)
        {
            if (router == null)
            {
                return null;
            }

            Transform[] descendants =
                router.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.name,
                        ClassifiedDocumentObjectName,
                        StringComparison.Ordinal))
                {
                    return candidate.gameObject;
                }
            }

            GameObject document = new GameObject(
                ClassifiedDocumentObjectName);
            Transform documentTransform = document.transform;
            documentTransform.SetParent(router, false);

            Bounds routerBounds = CalculateRouterBounds(
                router,
                signalCollider);
            Vector3 worldPosition = routerBounds.center +
                Vector3.down * (routerBounds.extents.y + 0.06f);
            if (routerCamera != null)
            {
                Vector3 towardsCamera =
                    routerCamera.transform.position - worldPosition;
                if (towardsCamera.sqrMagnitude > 0.0001f)
                {
                    documentTransform.rotation = Quaternion.LookRotation(
                        towardsCamera.normalized,
                        Vector3.up);
                    worldPosition += towardsCamera.normalized * 0.025f;
                }
            }

            documentTransform.position = worldPosition;
            Vector3 routerScale = router.lossyScale;
            documentTransform.localScale = new Vector3(
                SafeInverse(routerScale.x),
                SafeInverse(routerScale.y),
                SafeInverse(routerScale.z));

            GameObject paper = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            paper.name = "ClassifiedDocument_Paper";
            paper.transform.SetParent(documentTransform, false);
            paper.transform.localPosition = Vector3.zero;
            paper.transform.localRotation = Quaternion.identity;
            paper.transform.localScale =
                new Vector3(0.38f, 0.25f, 0.018f);
            DisablePrimitiveCollider(paper);
            ApplyRuntimeMaterial(
                paper,
                new Color(0.72f, 0.66f, 0.49f, 1f),
                "Mission05_ClassifiedPaper");

            GameObject redBand = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            redBand.name = "ClassifiedDocument_RedBand";
            redBand.transform.SetParent(documentTransform, false);
            redBand.transform.localPosition =
                new Vector3(0f, 0.052f, -0.012f);
            redBand.transform.localRotation = Quaternion.identity;
            redBand.transform.localScale =
                new Vector3(0.31f, 0.055f, 0.012f);
            DisablePrimitiveCollider(redBand);
            ApplyRuntimeMaterial(
                redBand,
                new Color(0.64f, 0.055f, 0.045f, 1f),
                "Mission05_ClassifiedBand");

            GameObject seal = GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);
            seal.name = "ClassifiedDocument_Seal";
            seal.transform.SetParent(documentTransform, false);
            seal.transform.localPosition =
                new Vector3(0f, -0.045f, -0.02f);
            seal.transform.localRotation =
                Quaternion.Euler(90f, 0f, 0f);
            seal.transform.localScale =
                new Vector3(0.055f, 0.008f, 0.055f);
            DisablePrimitiveCollider(seal);
            ApplyRuntimeMaterial(
                seal,
                new Color(0.52f, 0.035f, 0.03f, 1f),
                "Mission05_ClassifiedSeal");

            document.SetActive(false);
            return document;
        }

        private static GameObject EnsureInteractionZone(
            Scene scene,
            Vector3 worldCenter,
            int layer)
        {
            GameObject zone = FindSceneObject(
                scene,
                InteractionZoneName);
            if (zone == null)
            {
                zone = new GameObject(InteractionZoneName);
                SceneManager.MoveGameObjectToScene(zone, scene);
            }

            zone.layer = layer;
            zone.transform.SetPositionAndRotation(
                worldCenter,
                Quaternion.identity);
            zone.transform.localScale = Vector3.one;

            SphereCollider sphere = GetOrAdd<SphereCollider>(zone);
            sphere.enabled = true;
            sphere.isTrigger = true;
            sphere.center = Vector3.zero;
            sphere.radius = InteractionZoneRadius;
            return zone;
        }

        private static Bounds CalculateRouterBounds(
            Transform router,
            Collider fallbackCollider)
        {
            Renderer[] renderers =
                router.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = fallbackCollider != null
                ? fallbackCollider.bounds
                : new Bounds(router.position, Vector3.one * 0.25f);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    renderer.transform.IsChildOf(router) == false)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static void DisablePrimitiveCollider(GameObject target)
        {
            Collider collider = target != null
                ? target.GetComponent<Collider>()
                : null;
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static void ApplyRuntimeMaterial(
            GameObject target,
            Color color,
            string materialName)
        {
            Renderer renderer = target != null
                ? target.GetComponent<Renderer>()
                : null;
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            if (renderer == null || shader == null)
            {
                return;
            }

            Material material = new Material(shader)
            {
                name = materialName,
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            renderer.sharedMaterial = material;
        }

        private static float SafeInverse(float value)
        {
            return 1f / Mathf.Max(0.0001f, Mathf.Abs(value));
        }

        private static GameObject FindRoot(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null &&
                    string.Equals(
                        root.name,
                        objectName,
                        StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null &&
                    string.Equals(
                        child.name,
                        objectName,
                        StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
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
                            candidate.name,
                            objectName,
                            StringComparison.Ordinal))
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
            T[] candidates = UnityEngine.Object.FindObjectsByType<T>(
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
    }
}
