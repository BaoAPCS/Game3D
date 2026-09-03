using System;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    public static class Chapter2PoliceComputerMissionBootstrap
    {
        public const string PoliceStationSceneName = "Police_Station";
        public const string OfficeEnvironmentName =
            "Office Environment";
        public const string DeskBaseName = "DeskBase";
        public const string DesktopName = "Desktop";
        public const string DesktopCameraName = "Desktop_cam";
        public const string InteractableLayerName = "Interactable";
        public const float MinimumDesktopClickRadius = 0.12f;

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

            if (FindSceneComponent<Chapter2PoliceComputerMission>(
                    scene) != null)
            {
                return true;
            }

            if (!TryFindWorkstation(
                    scene,
                    out GameObject desk,
                    out GameObject desktop,
                    out Camera desktopCamera))
            {
                Debug.LogError(
                    "[Chapter2Mission04] Không tìm thấy đúng cụm DeskBase chứa Desktop và Desktop_cam trong Office Environment.");
                return false;
            }

            Chapter1InputReader inputReader =
                FindSceneComponent<Chapter1InputReader>(scene);
            if (inputReader == null)
            {
                Debug.LogError(
                    "[Chapter2Mission04] Không tìm thấy Player của Nam trong Police_Station.");
                return false;
            }

            int interactableLayer =
                LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Chapter2Mission04] Layer '{InteractableLayerName}' không tồn tại.");
                return false;
            }

            // Only the desk root belongs to the interaction scan. Its many
            // decorative child colliders stay on their authored layers so
            // they cannot steal focus from this single interaction.
            desk.layer = interactableLayer;

            SphereCollider deskSphere =
                GetOrAdd<SphereCollider>(desk);
            deskSphere.enabled = true;
            deskSphere.isTrigger = true;
            if (deskSphere.radius < 0.8f)
            {
                deskSphere.center = Vector3.zero;
                deskSphere.radius = 1.05f;
            }

            SphereCollider desktopSphere =
                GetOrAdd<SphereCollider>(desktop);
            desktopSphere.enabled = true;
            desktopSphere.isTrigger = true;
            desktopSphere.radius = Mathf.Max(
                desktopSphere.radius,
                MinimumDesktopClickRadius);

            Chapter2MissionTriggerZone triggerZone =
                GetOrAdd<Chapter2MissionTriggerZone>(desk);
            triggerZone.Configure(deskSphere, inputReader);

            Chapter2DesktopClickTarget clickTarget =
                GetOrAdd<Chapter2DesktopClickTarget>(desktop);
            clickTarget.Configure(desktopSphere);

            Chapter2DeskComputerInteractable interactable =
                GetOrAdd<Chapter2DeskComputerInteractable>(desk);
            Chapter2SaveManager saveManager =
                Chapter2SaveManager.EnsureForScene(scene);
            PhoneUIController phone =
                FindSceneComponent<PhoneUIController>(scene);

            GameObject missionObject = new GameObject(
                Chapter2PoliceComputerMission.MissionObjectName);
            SceneManager.MoveGameObjectToScene(missionObject, scene);
            Chapter2PoliceComputerMission mission =
                missionObject.AddComponent<
                    Chapter2PoliceComputerMission>();
            interactable.Configure(
                mission,
                triggerZone,
                desktop.transform);
            mission.Configure(
                saveManager,
                interactable,
                triggerZone,
                clickTarget,
                desktopCamera,
                inputReader,
                phone);
            return true;
        }

        public static bool TryFindWorkstation(
            Scene scene,
            out GameObject desk,
            out GameObject desktop,
            out Camera desktopCamera)
        {
            desk = null;
            desktop = null;
            desktopCamera = null;

            GameObject office = FindRoot(scene, OfficeEnvironmentName);
            if (office == null)
            {
                return false;
            }

            Camera[] cameras = office.GetComponentsInChildren<Camera>(
                true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidateCamera = cameras[i];
                if (candidateCamera == null ||
                    !string.Equals(
                        candidateCamera.gameObject.name,
                        DesktopCameraName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Transform deskTransform = candidateCamera.transform.parent;
                while (deskTransform != null &&
                       deskTransform != office.transform &&
                       !string.Equals(
                           deskTransform.name.Trim(),
                           DeskBaseName,
                           StringComparison.Ordinal))
                {
                    deskTransform = deskTransform.parent;
                }

                if (deskTransform == null ||
                    deskTransform == office.transform)
                {
                    continue;
                }

                Transform desktopTransform = FindDescendant(
                    deskTransform,
                    DesktopName);
                if (desktopTransform == null)
                {
                    continue;
                }

                desk = deskTransform.gameObject;
                desktop = desktopTransform.gameObject;
                desktopCamera = candidateCamera;
                return true;
            }

            return false;
        }

        private static GameObject FindRoot(
            Scene scene,
            string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(
                        roots[i].name,
                        rootName,
                        StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static Transform FindDescendant(
            Transform root,
            string objectName)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != root &&
                    string.Equals(
                        candidate.name,
                        objectName,
                        StringComparison.Ordinal))
                {
                    return candidate;
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
