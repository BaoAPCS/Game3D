using System;
using System.Collections;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    /// <summary>
    /// Prepares and owns the short establishing shot shown only when the
    /// Chapter 1 transition loads the police station. It also makes sure the
    /// camera bundled with the imported office asset starts disabled so it
    /// cannot compete with the gameplay camera before Mission 4 uses it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Chapter2JailIntroController : MonoBehaviour
    {
        public const string PoliceStationSceneName = "Police_Station";
        public const string RuntimeObjectName = "Chapter2_JailIntro";
        public const string IntroCameraObjectName =
            "Chapter2_JailIntroCamera";
        public const string EmbeddedOfficeCameraObjectName = "Desktop_cam";
        public const string InputLockReason = "Chapter2.JailIntro";
        public const float ShotHoldDuration = 2.8f;

        private Camera gameplayCamera;
        private Camera introCamera;
        private ThirdPersonCameraRig cameraRig;
        private Chapter1PlayerMotor playerMotor;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;

        private bool gameplayCameraWasEnabled;
        private bool movementWasEnabled;
        private bool gameplayInputWasEnabled;
        private bool interactionWasEnabled;
        private bool gameplayStateCaptured;
        private bool prepared;
        private bool restored;
        private float introStartFieldOfView;
        private float introEndFieldOfView;

        public bool IsPrepared => prepared && !restored;
        public Camera IntroCamera => introCamera;

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
        private static void NormalizeInitialScene()
        {
            DisableEmbeddedOfficeCamera(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            DisableEmbeddedOfficeCamera(scene);
        }

        /// <summary>
        /// Called by the persistent Chapter 1 transition while its black
        /// overlay still covers the newly loaded scene.
        /// </summary>
        public static Chapter2JailIntroController PrepareForTransition(
            Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(
                    scene.name,
                    PoliceStationSceneName,
                    StringComparison.Ordinal))
            {
                return null;
            }

            DisableEmbeddedOfficeCamera(scene);

            Chapter2JailIntroController existing =
                FindSceneComponent<Chapter2JailIntroController>(scene);
            if (existing != null)
            {
                return existing.IsPrepared ? existing : null;
            }

            GameObject owner = new GameObject(RuntimeObjectName);
            SceneManager.MoveGameObjectToScene(owner, scene);
            Chapter2JailIntroController controller =
                owner.AddComponent<Chapter2JailIntroController>();
            if (controller.Prepare(scene))
            {
                return controller;
            }

            Destroy(owner);
            return null;
        }

        /// <summary>
        /// Defensive startup normalization for the camera imported with the
        /// office environment. Mission 4 may intentionally enable it later;
        /// the main player camera and Bed_cam keep their dedicated owners.
        /// </summary>
        public static void DisableEmbeddedOfficeCamera(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(
                    scene.name,
                    PoliceStationSceneName,
                    StringComparison.Ordinal))
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Camera[] cameras = roots[rootIndex]
                    .GetComponentsInChildren<Camera>(true);
                for (int cameraIndex = 0;
                     cameraIndex < cameras.Length;
                     cameraIndex++)
                {
                    Camera candidate = cameras[cameraIndex];
                    if (candidate == null ||
                        !string.Equals(
                            candidate.gameObject.name,
                            EmbeddedOfficeCameraObjectName,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    candidate.enabled = false;
                    AudioListener listener =
                        candidate.GetComponent<AudioListener>();
                    if (listener != null)
                    {
                        listener.enabled = false;
                    }
                }
            }
        }

        public IEnumerator PlayShot()
        {
            if (!IsPrepared || introCamera == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < ShotHoldDuration &&
                   introCamera != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / ShotHoldDuration);
                float easedProgress = progress * progress *
                    (3f - 2f * progress);
                introCamera.fieldOfView = Mathf.Lerp(
                    introStartFieldOfView,
                    introEndFieldOfView,
                    easedProgress);
                yield return null;
            }
        }

        public void CompleteIntro()
        {
            RestoreGameplayState();
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        private bool Prepare(Scene scene)
        {
            playerMotor = FindSceneComponent<Chapter1PlayerMotor>(scene);
            cameraRig = FindSceneComponent<ThirdPersonCameraRig>(scene);
            gameplayCamera = FindGameplayCamera(scene, cameraRig);
            if (playerMotor == null || gameplayCamera == null)
            {
                Debug.LogWarning(
                    "[Chapter2Intro] Khong the quay canh Nam trong phong " +
                    "giam vi scene thieu Player hoac Main Camera.",
                    this);
                return false;
            }

            inputReader =
                playerMotor.GetComponent<Chapter1InputReader>();
            inputLock = playerMotor.GetComponent<PlayerInputLock>();
            interactionController =
                playerMotor.GetComponent<Chapter1InteractionController>();

            gameplayCameraWasEnabled = gameplayCamera.enabled;
            movementWasEnabled = playerMotor.MovementEnabled;
            gameplayInputWasEnabled = inputReader == null ||
                                      inputReader.GameplayInputEnabled;
            interactionWasEnabled = interactionController != null &&
                                    interactionController.enabled;
            gameplayStateCaptured = true;

            inputLock?.Lock(InputLockReason);
            inputReader?.SetGameplayInputEnabled(false);
            playerMotor.SetMovementEnabled(false);
            if (interactionController != null)
            {
                // OnDisable clears a stale interaction prompt before the
                // establishing shot becomes visible.
                interactionController.enabled = false;
            }

            cameraRig?.SnapBehindTarget();
            introCamera = CreateIntroCamera(
                scene,
                gameplayCamera,
                playerMotor.transform);
            if (introCamera == null)
            {
                RestoreGameplayState();
                return false;
            }

            gameplayCamera.enabled = false;
            prepared = true;
            restored = false;
            return true;
        }

        private Camera CreateIntroCamera(
            Scene scene,
            Camera sourceCamera,
            Transform player)
        {
            GameObject cameraObject = new GameObject(
                IntroCameraObjectName,
                typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);

            Camera createdCamera = cameraObject.GetComponent<Camera>();
            createdCamera.CopyFrom(sourceCamera);
            createdCamera.targetTexture = null;
            createdCamera.depth = sourceCamera.depth + 10f;

            Vector3 focusPoint = player.position + Vector3.up * 1.05f;
            Vector3 cameraPosition = sourceCamera.transform.position;
            if ((cameraPosition - focusPoint).sqrMagnitude < 1f)
            {
                cameraPosition = focusPoint - player.forward * 2.4f +
                                 Vector3.up * 1.15f;
            }

            Vector3 lookDirection = focusPoint - cameraPosition;
            Quaternion cameraRotation = lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirection, Vector3.up)
                : sourceCamera.transform.rotation;
            cameraObject.transform.SetPositionAndRotation(
                cameraPosition,
                cameraRotation);

            introStartFieldOfView = Mathf.Clamp(
                sourceCamera.fieldOfView + 4f,
                48f,
                68f);
            introEndFieldOfView = Mathf.Max(
                44f,
                introStartFieldOfView - 7f);
            createdCamera.fieldOfView = introStartFieldOfView;
            createdCamera.enabled = true;
            return createdCamera;
        }

        private void RestoreGameplayState()
        {
            if (restored)
            {
                return;
            }

            restored = true;
            prepared = false;

            if (introCamera != null)
            {
                introCamera.enabled = false;
                Destroy(introCamera.gameObject);
                introCamera = null;
            }

            if (!gameplayStateCaptured)
            {
                return;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = gameplayCameraWasEnabled;
            }

            cameraRig?.SnapBehindTarget();
            playerMotor?.SetMovementEnabled(movementWasEnabled);
            inputReader?.SetGameplayInputEnabled(
                gameplayInputWasEnabled);
            inputLock?.Unlock(InputLockReason);
            if (interactionController != null)
            {
                interactionController.enabled = interactionWasEnabled;
            }
        }

        private void OnDestroy()
        {
            RestoreGameplayState();
        }

        private static Camera FindGameplayCamera(
            Scene scene,
            ThirdPersonCameraRig rig)
        {
            Camera rigCamera = rig != null
                ? rig.GetComponentInChildren<Camera>(true)
                : null;
            if (IsGameplayCameraCandidate(rigCamera, scene))
            {
                return rigCamera;
            }

            Camera fallback = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Camera[] cameras = roots[rootIndex]
                    .GetComponentsInChildren<Camera>(true);
                for (int cameraIndex = 0;
                     cameraIndex < cameras.Length;
                     cameraIndex++)
                {
                    Camera candidate = cameras[cameraIndex];
                    if (!IsGameplayCameraCandidate(candidate, scene))
                    {
                        continue;
                    }

                    if (candidate.CompareTag("MainCamera"))
                    {
                        return candidate;
                    }

                    fallback ??= candidate;
                }
            }

            return fallback;
        }

        private static bool IsGameplayCameraCandidate(
            Camera candidate,
            Scene scene)
        {
            return candidate != null &&
                   candidate.gameObject.scene == scene &&
                   !string.Equals(
                       candidate.gameObject.name,
                       EmbeddedOfficeCameraObjectName,
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       candidate.gameObject.name,
                       "Bed_cam",
                       StringComparison.Ordinal);
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                T component = roots[rootIndex]
                    .GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
