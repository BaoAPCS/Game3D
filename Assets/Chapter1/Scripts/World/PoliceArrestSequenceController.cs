using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Drives the existing inactive police car along its road lane after the
    /// Nam-versus-Henry fight. The target X coordinate is captured when the
    /// sequence begins; the car never follows the player or leaves its
    /// original Y/Z lane.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoliceArrestSequenceController : MonoBehaviour
    {
        public enum SequenceState
        {
            Idle,
            Approaching,
            Arrived,
            Failed
        }

        public const string PoliceCarObjectName = "police_car";
        public const float PoliceCarSpeed = 12f;
        public const float PoliceCarStopOffset = 4.1f;
        public const float PoliceCarTimeout = 15f;
        public const float ArrivalThreshold = 0.1f;

        [SerializeField, Min(0.1f)] private float approachSpeed =
            PoliceCarSpeed;
        [SerializeField, Min(0.5f)] private float stopOffset =
            PoliceCarStopOffset;
        [SerializeField, Min(1f)] private float timeoutSeconds =
            PoliceCarTimeout;
        [SerializeField, Min(0.01f)] private float arrivalThreshold =
            ArrivalThreshold;

        private readonly List<Action<bool>> completionCallbacks =
            new List<Action<bool>>();

        private Scene sequenceScene;
        private Transform policeCar;
        private AudioSource sirenSource;
        private Vector3 initialPosition;
        private Vector3 destination;
        private float sequenceStartedAt;
        private bool initialPositionCaptured;
        private bool missingAudioWarningLogged;
        private SequenceState state;

        /// <summary>
        /// Raised once when an actively running sequence finishes. The bool
        /// is true when the car reached (or was safely snapped to) its stop.
        /// </summary>
        public event Action<bool> ArrestCompleted;

        public SequenceState State => state;
        public bool IsRunning => state == SequenceState.Approaching;
        public bool HasArrived => state == SequenceState.Arrived;
        public Transform PoliceCar => policeCar;
        public Vector3 Destination => destination;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForInitiallyLoadedScene()
        {
            InstallForSceneWhenPoliceCarExists(
                SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForSceneWhenPoliceCarExists(scene);
        }

        /// <summary>
        /// Returns the scene's existing controller or creates one. Unlike the
        /// automatic installer, this still returns a controller when the car
        /// is missing, so BeginArrest can report failure and invoke its
        /// completion callback instead of leaving the caller waiting.
        /// </summary>
        public static PoliceArrestSequenceController GetOrInstall(
            Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            PoliceArrestSequenceController[] existing =
                FindObjectsByType<PoliceArrestSequenceController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                PoliceArrestSequenceController candidate = existing[i];
                if (candidate != null &&
                    candidate.gameObject.scene == scene)
                {
                    candidate.Configure(scene);
                    return candidate;
                }
            }

            GameObject directorObject =
                new GameObject(nameof(PoliceArrestSequenceController));
            SceneManager.MoveGameObjectToScene(directorObject, scene);
            PoliceArrestSequenceController controller =
                directorObject.AddComponent<
                    PoliceArrestSequenceController>();
            controller.Configure(scene);
            return controller;
        }

        /// <summary>
        /// Starts the one-shot arrival. Repeated calls while it is moving add
        /// their callback to the same sequence; calls after completion receive
        /// the already-known result immediately.
        /// </summary>
        public bool BeginArrest(
            Transform target,
            Action<bool> onCompleted = null)
        {
            if (state == SequenceState.Arrived)
            {
                InvokeCallbackSafely(onCompleted, true);
                return true;
            }

            if (state == SequenceState.Failed)
            {
                InvokeCallbackSafely(onCompleted, false);
                return false;
            }

            if (onCompleted != null)
            {
                completionCallbacks.Add(onCompleted);
            }

            if (state == SequenceState.Approaching)
            {
                return true;
            }

            if (target == null)
            {
                Debug.LogError(
                    "[PoliceArrest] Cannot begin: Nam target is missing.",
                    this);
                CompleteSequence(false);
                return false;
            }

            if (!TryResolvePoliceCar())
            {
                Debug.LogError(
                    "[PoliceArrest] Cannot begin: exact inactive root " +
                    "'police_car' was not found in the scene.",
                    this);
                CompleteSequence(false);
                return false;
            }

            CaptureDestination(target.position.x);
            policeCar.gameObject.SetActive(true);
            NormalizeRigidbodies();
            StartSiren();

            sequenceStartedAt = Time.time;
            state = SequenceState.Approaching;

            if (Mathf.Abs(policeCar.position.x - destination.x) <=
                arrivalThreshold)
            {
                FinishAtDestination();
            }

            return true;
        }

        /// <summary>
        /// Restores the final arrested tableau without replaying the siren or
        /// publishing a new arrival event. Intended for an already-completed
        /// save loaded into a fresh scene.
        /// </summary>
        public bool RestoreTerminalArrestState(Transform target)
        {
            if (target == null)
            {
                Debug.LogError(
                    "[PoliceArrest] Cannot restore: Nam target is missing.",
                    this);
                return false;
            }

            if (!TryResolvePoliceCar())
            {
                Debug.LogError(
                    "[PoliceArrest] Cannot restore: exact root " +
                    "'police_car' was not found in the scene.",
                    this);
                return false;
            }

            CaptureDestination(target.position.x);
            policeCar.gameObject.SetActive(true);
            NormalizeRigidbodies();
            StopSiren();
            policeCar.position = destination;

            if (state == SequenceState.Approaching)
            {
                CompleteSequence(true);
            }
            else
            {
                state = SequenceState.Arrived;
                completionCallbacks.Clear();
            }

            return true;
        }

        private static void InstallForSceneWhenPoliceCarExists(Scene scene)
        {
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                FindExactPoliceCarRoot(scene) == null)
            {
                return;
            }

            GetOrInstall(scene);
        }

        private static Transform FindExactPoliceCarRoot(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            Transform activeMatch = null;
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null ||
                    !string.Equals(
                        root.name,
                        PoliceCarObjectName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                // Prefer the authored inactive root. Retain an active exact
                // match so reload restoration can still resolve it.
                if (!root.activeSelf)
                {
                    return root.transform;
                }

                activeMatch = root.transform;
            }

            return activeMatch;
        }

        private void Configure(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            sequenceScene = scene;
            TryResolvePoliceCar();
        }

        private bool TryResolvePoliceCar()
        {
            if (policeCar != null)
            {
                return true;
            }

            if (!sequenceScene.IsValid() || !sequenceScene.isLoaded)
            {
                sequenceScene = gameObject.scene;
            }

            policeCar = FindExactPoliceCarRoot(sequenceScene);
            if (policeCar == null)
            {
                return false;
            }

            if (!initialPositionCaptured)
            {
                initialPosition = policeCar.position;
                initialPositionCaptured = true;
            }

            sirenSource = policeCar.GetComponent<AudioSource>();
            if (sirenSource == null)
            {
                sirenSource = policeCar.GetComponentInChildren<
                    AudioSource>(true);
            }

            if (sirenSource != null)
            {
                sirenSource.playOnAwake = false;
                sirenSource.loop = true;
                sirenSource.spatialBlend = 1f;
                sirenSource.minDistance = 5f;
                sirenSource.maxDistance = 120f;
            }

            return true;
        }

        private void CaptureDestination(float targetX)
        {
            float approachSide = initialPosition.x >= targetX ? 1f : -1f;
            destination = new Vector3(
                targetX + approachSide * stopOffset,
                initialPosition.y,
                initialPosition.z);
        }

        private void Update()
        {
            if (state != SequenceState.Approaching)
            {
                return;
            }

            if (policeCar == null)
            {
                Debug.LogError(
                    "[PoliceArrest] police_car was destroyed while " +
                    "approaching. Completing with failure.",
                    this);
                CompleteSequence(false);
                return;
            }

            if (Time.time - sequenceStartedAt >= timeoutSeconds)
            {
                Debug.LogWarning(
                    "[PoliceArrest] Arrival exceeded 15 seconds; " +
                    "snapping police_car to its safe road stop.",
                    policeCar);
                FinishAtDestination();
                return;
            }

            Vector3 current = policeCar.position;
            float nextX = Mathf.MoveTowards(
                current.x,
                destination.x,
                approachSpeed * Time.deltaTime);
            policeCar.position = new Vector3(
                nextX,
                initialPosition.y,
                initialPosition.z);

            if (Mathf.Abs(nextX - destination.x) <= arrivalThreshold)
            {
                FinishAtDestination();
            }
        }

        private void NormalizeRigidbodies()
        {
            Rigidbody[] bodies =
                policeCar.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null)
                {
                    continue;
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
            }
        }

        private void StartSiren()
        {
            if (sirenSource == null && policeCar != null)
            {
                sirenSource = policeCar.GetComponent<AudioSource>();
                if (sirenSource == null)
                {
                    sirenSource = policeCar.GetComponentInChildren<
                        AudioSource>(true);
                }
            }

            if (sirenSource == null || sirenSource.clip == null)
            {
                if (!missingAudioWarningLogged)
                {
                    missingAudioWarningLogged = true;
                    Debug.LogWarning(
                        "[PoliceArrest] police_car has no configured " +
                        "AudioSource/clip; arrival will continue silently.",
                        policeCar);
                }

                return;
            }

            sirenSource.loop = true;
            if (!sirenSource.enabled)
            {
                sirenSource.enabled = true;
            }

            if (!sirenSource.isPlaying)
            {
                sirenSource.Play();
            }
        }

        private void StopSiren()
        {
            if (sirenSource != null && sirenSource.isPlaying)
            {
                sirenSource.Stop();
            }
        }

        private void FinishAtDestination()
        {
            if (policeCar != null)
            {
                policeCar.position = destination;
            }

            StopSiren();
            CompleteSequence(true);
        }

        private void CompleteSequence(bool arrived)
        {
            if (state == SequenceState.Arrived ||
                state == SequenceState.Failed)
            {
                return;
            }

            state = arrived
                ? SequenceState.Arrived
                : SequenceState.Failed;
            StopSiren();

            Action<bool>[] callbacks = completionCallbacks.ToArray();
            completionCallbacks.Clear();
            for (int i = 0; i < callbacks.Length; i++)
            {
                InvokeCallbackSafely(callbacks[i], arrived);
            }

            Action<bool> handlers = ArrestCompleted;
            if (handlers == null)
            {
                return;
            }

            Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                InvokeCallbackSafely(
                    (Action<bool>)invocationList[i],
                    arrived);
            }
        }

        private static void InvokeCallbackSafely(
            Action<bool> callback,
            bool arrived)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback(arrived);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            if (state == SequenceState.Approaching)
            {
                CompleteSequence(false);
                return;
            }

            StopSiren();
            completionCallbacks.Clear();
        }

        private void OnValidate()
        {
            approachSpeed = Mathf.Max(0.1f, approachSpeed);
            stopOffset = Mathf.Max(0.5f, stopOffset);
            timeoutSeconds = Mathf.Max(1f, timeoutSeconds);
            arrivalThreshold = Mathf.Max(0.01f, arrivalThreshold);
        }
    }
}
