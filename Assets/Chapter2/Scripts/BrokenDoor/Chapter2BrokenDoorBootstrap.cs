using System;
using DormitoryMystery.Chapter1;
using NavKeypad;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    public static class Chapter2BrokenDoorBootstrap
    {
        public const string PoliceStationSceneName =
            "Police_Station";
        public const string ScifiOfficeName = "ScifiOffice";
        public const string ContractCameraPath =
            "Office 1/Table Set 1 Grey Wood Variant/" +
            "Table Set 1/contract_camera";
        public const string BrokenDoorPath =
            "Office 1/Door Wall Opaque/Broken Door";
        public const string KeypadName = "Keypad";
        public const string KeypadCameraName = "Keypad_cam";
        public const string DoorOneName = "Door1";
        public const string DoorTwoName = "Door2";
        public const string InteractableLayerName =
            "Interactable";

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

            if (!TryFindSetup(
                    scene,
                    out Camera contractCamera,
                    out SphereCollider contractCollider,
                    out Transform keypadTransform,
                    out Keypad keypad,
                    out SphereCollider keypadCollider,
                    out Camera keypadCamera,
                    out Transform doorOnePanel,
                    out Transform doorTwoPanel,
                    out BoxCollider doorwayHeaderCollider))
            {
                Debug.LogError(
                    "[Chapter2BrokenDoor] Không tìm thấy đúng contract_camera, Broken Door/Keypad, Keypad_cam hoặc hai cánh Top.");
                return false;
            }

            DisableCamera(contractCamera);
            DisableCamera(keypadCamera);
            contractCollider.enabled = false;
            keypadCollider.enabled = false;
            keypad.SetCombo(
                Chapter2BrokenDoorMission.CorrectKeypadCombo);

            int interactableLayer =
                LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Chapter2BrokenDoor] Layer '{InteractableLayerName}' không tồn tại.");
                return false;
            }

            contractCamera.gameObject.layer =
                interactableLayer;
            keypadTransform.gameObject.layer =
                interactableLayer;

            Chapter1InputReader inputReader =
                FindSceneComponent<Chapter1InputReader>(scene);
            if (inputReader == null)
            {
                Debug.LogError(
                    "[Chapter2BrokenDoor] Không tìm thấy Player của Nam trong Police_Station.");
                return false;
            }

            Chapter2MissionTriggerZone contractZone =
                GetOrAdd<Chapter2MissionTriggerZone>(
                    contractCamera.gameObject);
            contractZone.Configure(
                contractCollider,
                inputReader);
            Chapter2MissionTriggerZone keypadZone =
                GetOrAdd<Chapter2MissionTriggerZone>(
                    keypadTransform.gameObject);
            keypadZone.Configure(keypadCollider, inputReader);

            Chapter2BrokenDoorInteractable contractInteractable =
                GetOrAdd<Chapter2BrokenDoorInteractable>(
                    contractCamera.gameObject);
            Chapter2BrokenDoorInteractable keypadInteractable =
                GetOrAdd<Chapter2BrokenDoorInteractable>(
                    keypadTransform.gameObject);

            Chapter2SaveManager saveManager =
                Chapter2SaveManager.EnsureForScene(scene);
            PhoneUIController phone =
                FindSceneComponent<PhoneUIController>(scene);
            Chapter2BrokenDoorMission mission =
                FindSceneComponent<Chapter2BrokenDoorMission>(
                    scene);
            if (mission == null)
            {
                GameObject missionObject = new GameObject(
                    Chapter2BrokenDoorMission.MissionObjectName);
                SceneManager.MoveGameObjectToScene(
                    missionObject,
                    scene);
                mission = missionObject.AddComponent<
                    Chapter2BrokenDoorMission>();
            }

            contractInteractable.Configure(
                mission,
                contractZone,
                Chapter2BrokenDoorInspection.Contract,
                contractCamera.transform);
            keypadInteractable.Configure(
                mission,
                keypadZone,
                Chapter2BrokenDoorInspection.Keypad,
                keypadTransform);
            mission.Configure(
                saveManager,
                contractCollider,
                contractZone,
                contractCamera,
                keypadCollider,
                keypadZone,
                keypad,
                keypadCamera,
                doorOnePanel,
                doorTwoPanel,
                doorwayHeaderCollider,
                inputReader,
                phone);
            return true;
        }

        public static bool TryFindSetup(
            Scene scene,
            out Camera contractCamera,
            out SphereCollider contractCollider,
            out Transform keypadTransform,
            out Keypad keypad,
            out SphereCollider keypadCollider,
            out Camera keypadCamera,
            out Transform doorOnePanel,
            out Transform doorTwoPanel,
            out BoxCollider doorwayHeaderCollider)
        {
            contractCamera = null;
            contractCollider = null;
            keypadTransform = null;
            keypad = null;
            keypadCollider = null;
            keypadCamera = null;
            doorOnePanel = null;
            doorTwoPanel = null;
            doorwayHeaderCollider = null;

            GameObject office =
                FindRoot(scene, ScifiOfficeName);
            if (office == null)
            {
                return false;
            }

            Transform contractTransform =
                office.transform.Find(ContractCameraPath);
            Transform brokenDoor =
                office.transform.Find(BrokenDoorPath);
            keypadTransform = brokenDoor != null
                ? FindDirectChild(
                    brokenDoor,
                    KeypadName)
                : null;
            Transform keypadCameraTransform =
                keypadTransform != null
                    ? FindDirectChild(
                        keypadTransform,
                        KeypadCameraName)
                    : null;
            Transform doorOne = brokenDoor != null
                ? FindDirectChild(brokenDoor, DoorOneName)
                : null;
            Transform doorTwo = brokenDoor != null
                ? FindDirectChild(brokenDoor, DoorTwoName)
                : null;

            contractCamera = contractTransform != null
                ? contractTransform.GetComponent<Camera>()
                : null;
            contractCollider = contractTransform != null
                ? contractTransform
                    .GetComponent<SphereCollider>()
                : null;
            keypad = keypadTransform != null
                ? keypadTransform.GetComponent<Keypad>()
                : null;
            keypadCollider = keypadTransform != null
                ? keypadTransform
                    .GetComponent<SphereCollider>()
                : null;
            keypadCamera = keypadCameraTransform != null
                ? keypadCameraTransform
                    .GetComponent<Camera>()
                : null;
            doorOnePanel = FindDoorPanel(doorOne);
            doorTwoPanel = FindDoorPanel(doorTwo);
            doorwayHeaderCollider =
                FindDoorwayHeaderCollider(brokenDoor);

            return contractCamera != null &&
                   contractCollider != null &&
                   keypadTransform != null &&
                   keypad != null &&
                   keypadCollider != null &&
                   keypadCamera != null &&
                   doorOnePanel != null &&
                   doorTwoPanel != null &&
                   doorwayHeaderCollider != null;
        }

        public static void DisableCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.enabled = false;
            AudioListener listener =
                camera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }
        }

        private static Transform FindDoorPanel(
            Transform door)
        {
            if (door == null)
            {
                return null;
            }

            for (int i = 0; i < door.childCount; i++)
            {
                Transform child = door.GetChild(i);
                if (child != null &&
                    child.name.StartsWith(
                        "Top",
                        StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static BoxCollider FindDoorwayHeaderCollider(
            Transform brokenDoor)
        {
            if (brokenDoor == null)
            {
                return null;
            }

            BoxCollider[] colliders =
                brokenDoor.GetComponents<BoxCollider>();
            BoxCollider widestHorizontal = null;
            for (int i = 0; i < colliders.Length; i++)
            {
                BoxCollider candidate = colliders[i];
                if (candidate == null || candidate.isTrigger ||
                    candidate.size.x <= candidate.size.y ||
                    (widestHorizontal != null &&
                     candidate.size.x <=
                     widestHorizontal.size.x))
                {
                    continue;
                }

                widestHorizontal = candidate;
            }

            return widestHorizontal;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName)
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
                        childName,
                        StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindRoot(
            Scene scene,
            string rootName)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null &&
                    string.Equals(
                        root.name,
                        rootName,
                        StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] candidates =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
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
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : target.AddComponent<T>();
        }
    }
}
