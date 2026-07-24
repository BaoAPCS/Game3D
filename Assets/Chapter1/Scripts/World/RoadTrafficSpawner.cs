using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class RoadTrafficSpawner : MonoBehaviour
    {
        private const string RiderMotorcycleName = "motorcycle_with_rider";
        private const string MotorcytressName = "motorcytress";

        [Serializable]
        private sealed class VehicleRoute
        {
            [SerializeField] private string routeName = "Vehicle";
            [SerializeField] private GameObject sourceObject;
            [SerializeField] private Transform destination;
            [SerializeField, Min(0.01f)] private float speed = 8f;
            [SerializeField, Min(0f)] private float initialDelay;
            [SerializeField] private Vector2 spawnIntervalRange = new Vector2(4f, 7f);
            [SerializeField, Min(1)] private int maximumActiveVehicles = 4;
            [SerializeField] private bool preserveStartHeight = true;
            [SerializeField] private bool hideSourceOnPlay = true;

            [NonSerialized] private Vector3 startPosition;
            [NonSerialized] private Quaternion startRotation;
            [NonSerialized] private Vector3 startWorldScale;
            [NonSerialized] private bool sourceWasActive;
            [NonSerialized] private bool isReady;
            [NonSerialized] private float nextSpawnTime;
            [NonSerialized] private int activeVehicleCount;
            [NonSerialized] private Vector3 automaticDestination;

            public VehicleRoute()
            {
            }

            public VehicleRoute(string defaultName)
            {
                routeName = defaultName;
            }

            public string DisplayName =>
                string.IsNullOrWhiteSpace(routeName) ? "Unnamed route" : routeName;

            public GameObject SourceObject => sourceObject;
            public bool IsReady => isReady;
            public float Speed => speed;
            public float NextSpawnTime => nextSpawnTime;
            public int ActiveVehicleCount => activeVehicleCount;
            public int MaximumActiveVehicles => maximumActiveVehicles;
            public Vector3 StartPosition => startPosition;
            public Quaternion StartRotation => startRotation;
            public Vector3 StartWorldScale => startWorldScale;

            public void TryAssignSourceByName()
            {
                if (sourceObject == null && !string.IsNullOrWhiteSpace(routeName))
                {
                    sourceObject = GameObject.Find(routeName);
                }
            }

            public bool MatchesSourceName(string objectName)
            {
                return string.Equals(routeName, objectName, StringComparison.Ordinal) ||
                       (sourceObject != null &&
                        string.Equals(sourceObject.name, objectName, StringComparison.Ordinal));
            }

            public void Initialize(
                float currentTime,
                float automaticRoadEndX,
                UnityEngine.Object logContext)
            {
                isReady = false;
                activeVehicleCount = 0;

                if (sourceObject == null)
                {
                    Debug.LogWarning(
                        $"[RoadTrafficSpawner] Route '{DisplayName}' has no Source Object.",
                        logContext);
                    return;
                }

                startPosition = sourceObject.transform.position;
                startRotation = sourceObject.transform.rotation;
                startWorldScale = sourceObject.transform.lossyScale;
                sourceWasActive = sourceObject.activeSelf;

                float roadEndX = Mathf.Max(
                    Mathf.Abs(automaticRoadEndX),
                    Mathf.Abs(startPosition.x) + 1f);
                automaticDestination = new Vector3(
                    startPosition.x >= 0f ? -roadEndX : roadEndX,
                    startPosition.y,
                    startPosition.z);

                nextSpawnTime = currentTime + initialDelay;
                isReady = true;
            }

            public void HideSource()
            {
                if (isReady && hideSourceOnPlay && sourceObject != null)
                {
                    sourceObject.SetActive(false);
                }
            }

            public void RestoreSource()
            {
                if (sourceObject != null && hideSourceOnPlay)
                {
                    sourceObject.SetActive(sourceWasActive);
                }

                isReady = false;
                activeVehicleCount = 0;
            }

            public Vector3 GetDestinationPosition()
            {
                Vector3 target = destination != null
                    ? destination.position
                    : automaticDestination;

                if (preserveStartHeight)
                {
                    target.y = startPosition.y;
                }

                return target;
            }

            public Vector3 GetEditorDestinationPosition(float automaticRoadEndX)
            {
                if (destination != null)
                {
                    Vector3 assignedTarget = destination.position;
                    if (preserveStartHeight && sourceObject != null)
                    {
                        assignedTarget.y = sourceObject.transform.position.y;
                    }

                    return assignedTarget;
                }

                if (sourceObject == null)
                {
                    return Vector3.zero;
                }

                Vector3 start = sourceObject.transform.position;
                float roadEndX = Mathf.Max(
                    Mathf.Abs(automaticRoadEndX),
                    Mathf.Abs(start.x) + 1f);

                return new Vector3(
                    start.x >= 0f ? -roadEndX : roadEndX,
                    start.y,
                    start.z);
            }

            public void ScheduleNextSpawn(float currentTime)
            {
                float minimum = Mathf.Max(
                    0.1f,
                    Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y));
                float maximum = Mathf.Max(
                    minimum,
                    Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y));

                nextSpawnTime = currentTime + UnityEngine.Random.Range(minimum, maximum);
            }

            public void RegisterSpawn()
            {
                activeVehicleCount++;
            }

            public void RegisterRemoval()
            {
                activeVehicleCount = Mathf.Max(0, activeVehicleCount - 1);
            }

            public void Validate()
            {
                speed = Mathf.Max(0.01f, speed);
                initialDelay = Mathf.Max(0f, initialDelay);
                spawnIntervalRange.x = Mathf.Max(0.1f, spawnIntervalRange.x);
                spawnIntervalRange.y = Mathf.Max(0.1f, spawnIntervalRange.y);
                maximumActiveVehicles = Mathf.Max(1, maximumActiveVehicles);
            }
        }

        private sealed class ActiveVehicle
        {
            public ActiveVehicle(GameObject instance, VehicleRoute route)
            {
                Instance = instance;
                Transform = instance.transform;
                Route = route;
            }

            public GameObject Instance { get; }
            public Transform Transform { get; }
            public VehicleRoute Route { get; }
        }

        [Header("Traffic Routes")]
        [SerializeField] private List<VehicleRoute> routes = new List<VehicleRoute>
        {
            new VehicleRoute("motorcycle_with_rider"),
            new VehicleRoute("motorcytress")
        };

        [Header("Movement")]
        [SerializeField, Min(1f)] private float automaticRoadEndX = 32f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.15f;
        [SerializeField] private bool disableAnimatorRootMotion = true;
        [SerializeField] private bool makeRigidbodiesKinematic = true;

        [Header("Debug")]
        [SerializeField] private Color routeGizmoColor = new Color(0.1f, 0.9f, 1f, 0.9f);

        private readonly List<ActiveVehicle> activeVehicles = new List<ActiveVehicle>();
        private bool trafficRunning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeSpawnerExists()
        {
            RoadTrafficSpawner existingSpawner =
                UnityEngine.Object.FindAnyObjectByType<RoadTrafficSpawner>();

            if (existingSpawner != null && existingSpawner.isActiveAndEnabled)
            {
                return;
            }

            if (GameObject.Find(RiderMotorcycleName) == null &&
                GameObject.Find(MotorcytressName) == null)
            {
                return;
            }

            GameObject trafficSystem = new GameObject("RoadTrafficSystem (Auto)");
            trafficSystem.AddComponent<RoadTrafficSpawner>();
        }

        private void Reset()
        {
            EnsureDefaultRoutes();

            foreach (VehicleRoute route in routes)
            {
                route?.TryAssignSourceByName();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                BeginTraffic();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                StopTraffic();
            }
        }

        private void Update()
        {
            if (!trafficRunning)
            {
                return;
            }

            SpawnDueVehicles();
            MoveActiveVehicles();
        }

        private void BeginTraffic()
        {
            if (trafficRunning)
            {
                return;
            }

            activeVehicles.Clear();
            EnsureDefaultRoutes();

            float currentTime = Time.time;
            foreach (VehicleRoute route in routes)
            {
                if (route == null)
                {
                    continue;
                }

                route.TryAssignSourceByName();
                route.Initialize(currentTime, automaticRoadEndX, this);
            }

            foreach (VehicleRoute route in routes)
            {
                route?.HideSource();
            }

            trafficRunning = true;
        }

        private void StopTraffic()
        {
            trafficRunning = false;

            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                RemoveVehicleAt(i, true);
            }

            if (routes == null)
            {
                return;
            }

            foreach (VehicleRoute route in routes)
            {
                route?.RestoreSource();
            }
        }

        private void SpawnDueVehicles()
        {
            float currentTime = Time.time;

            foreach (VehicleRoute route in routes)
            {
                if (route == null ||
                    !route.IsReady ||
                    currentTime < route.NextSpawnTime ||
                    route.ActiveVehicleCount >= route.MaximumActiveVehicles)
                {
                    continue;
                }

                SpawnVehicle(route);
                route.ScheduleNextSpawn(currentTime);
            }
        }

        private void SpawnVehicle(VehicleRoute route)
        {
            GameObject instance = Instantiate(
                route.SourceObject,
                route.StartPosition,
                route.StartRotation);

            instance.name = $"{route.SourceObject.name}_Traffic";
            instance.transform.localScale = route.StartWorldScale;

            PrepareSpawnedVehicle(instance);
            instance.SetActive(true);

            route.RegisterSpawn();
            activeVehicles.Add(new ActiveVehicle(instance, route));
        }

        private void PrepareSpawnedVehicle(GameObject instance)
        {
            if (disableAnimatorRootMotion)
            {
                Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
                foreach (Animator animator in animators)
                {
                    animator.applyRootMotion = false;
                }
            }

            if (makeRigidbodiesKinematic)
            {
                Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
                foreach (Rigidbody body in rigidbodies)
                {
                    body.useGravity = false;
                    body.isKinematic = true;
                }
            }
        }

        private void MoveActiveVehicles()
        {
            float maximumArrivalDistanceSquared = arrivalDistance * arrivalDistance;

            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                ActiveVehicle vehicle = activeVehicles[i];
                if (vehicle.Instance == null)
                {
                    RemoveVehicleAt(i, false);
                    continue;
                }

                Vector3 target = vehicle.Route.GetDestinationPosition();
                vehicle.Transform.position = Vector3.MoveTowards(
                    vehicle.Transform.position,
                    target,
                    vehicle.Route.Speed * Time.deltaTime);

                if ((vehicle.Transform.position - target).sqrMagnitude <=
                    maximumArrivalDistanceSquared)
                {
                    RemoveVehicleAt(i, true);
                }
            }
        }

        private void RemoveVehicleAt(int index, bool destroyInstance)
        {
            ActiveVehicle vehicle = activeVehicles[index];
            vehicle.Route.RegisterRemoval();

            if (destroyInstance && vehicle.Instance != null)
            {
                Destroy(vehicle.Instance);
            }

            activeVehicles.RemoveAt(index);
        }

        private void OnValidate()
        {
            automaticRoadEndX = Mathf.Max(1f, automaticRoadEndX);
            arrivalDistance = Mathf.Max(0.01f, arrivalDistance);

            if (routes == null)
            {
                return;
            }

            foreach (VehicleRoute route in routes)
            {
                route?.Validate();
            }
        }

        private void EnsureDefaultRoutes()
        {
            if (routes == null)
            {
                routes = new List<VehicleRoute>();
            }

            EnsureRouteExists(RiderMotorcycleName);
            EnsureRouteExists(MotorcytressName);
        }

        private void EnsureRouteExists(string sourceName)
        {
            foreach (VehicleRoute route in routes)
            {
                if (route != null && route.MatchesSourceName(sourceName))
                {
                    return;
                }
            }

            routes.Add(new VehicleRoute(sourceName));
        }

        private void OnDrawGizmosSelected()
        {
            if (routes == null)
            {
                return;
            }

            Gizmos.color = routeGizmoColor;

            foreach (VehicleRoute route in routes)
            {
                if (route?.SourceObject == null)
                {
                    continue;
                }

                Vector3 start = route.SourceObject.transform.position;
                Vector3 end = route.GetEditorDestinationPosition(automaticRoadEndX);

                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(start, 0.25f);
                Gizmos.DrawWireSphere(end, 0.25f);
            }
        }
    }
}
