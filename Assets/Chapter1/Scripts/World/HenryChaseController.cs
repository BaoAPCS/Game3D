using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryChaseController : MonoBehaviour
    {
        private enum ChaseState
        {
            Idle,
            MovingToFoodcart,
            WaitingAtFoodcart,
            ReturningHome,
            Chasing,
            ForcedCatch,
            Escaped,
            Caught
        }

        [Header("Chase")]
        [SerializeField, Min(0.1f)] private float chaseSpeed = 5f;
        [SerializeField, Min(0.1f)] private float acceleration = 18f;
        [SerializeField, Min(1f)] private float angularSpeed = 720f;
        [SerializeField, Min(0.1f)] private float catchDistance = 0.82f;
        [SerializeField, Min(0.01f)]
        private float destinationRefreshInterval = 0.1f;
        [SerializeField, Min(0.5f)] private float forcedCatchTimeout = 4f;

        [Header("Foodcart Distraction")]
        [SerializeField, Min(0.1f)] private float foodcartWaitDuration = 5f;
        [SerializeField, Min(0.1f)]
        private float foodcartApproachPadding = 0.9f;
        [SerializeField, Min(0.05f)]
        private float distractionStoppingDistance = 0.35f;

        [Header("Room Escape")]
        [SerializeField, Min(0.5f)] private float doorwayHalfWidth = 1.1f;
        [SerializeField, Min(0f)] private float insideSideMargin = 0.04f;

        private readonly List<DoorInteractable> trackedDoors =
            new List<DoorInteractable>();
        private HenryRunAnimationPlayer animationPlayer;
        private NavMeshAgent agent;
        private Transform player;
        private DoorInteractable activeEscapeDoor;
        private ChaseState state;
        private Vector3 previousPlayerPosition;
        private float nextDestinationRefreshAt;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 foodcartDestination;
        private float foodcartWaitEndsAt;
        private bool foodcartDistractionActive;
        private bool batterySwapRaceActive;
        private PlayerInputLock forcedCatchInputLock;
        private float forcedCatchDeadline;

        private const string ForcedCatchInputReason = "HenryForcedCatch";

        public bool IsChasing =>
            state == ChaseState.Chasing || state == ChaseState.ForcedCatch;
        public bool IsDistracting =>
            foodcartDistractionActive &&
            (state == ChaseState.MovingToFoodcart ||
             state == ChaseState.WaitingAtFoodcart);
        public bool IsReturningFromFoodcart =>
            foodcartDistractionActive &&
            state == ChaseState.ReturningHome;
        public bool IsBatterySwapRaceActive =>
            batterySwapRaceActive && state == ChaseState.Chasing;
        public bool CanStartDistraction => state == ChaseState.Idle;
        public bool HasEscaped => state == ChaseState.Escaped;
        public bool HasCaughtPlayer => state == ChaseState.Caught;
        public DoorInteractable ActiveEscapeDoor => activeEscapeDoor;
        public NavMeshAgent Agent => agent;

        public event Action ChaseStarted;
        public event Action PlayerEscaped;
        public event Action PlayerCaught;

        private void Awake()
        {
            animationPlayer =
                GetComponent<HenryRunAnimationPlayer>();
            if (animationPlayer == null)
            {
                animationPlayer =
                    gameObject.AddComponent<HenryRunAnimationPlayer>();
            }

            ConfigureAgent();
            homePosition = transform.position;
            homeRotation = transform.rotation;
            animationPlayer.StopAtInitialPose();
        }

        private void Update()
        {
            switch (state)
            {
                case ChaseState.Chasing:
                    UpdateChase();
                    break;
                case ChaseState.ForcedCatch:
                    UpdateForcedCatch();
                    break;
                case ChaseState.MovingToFoodcart:
                    UpdateMoveToFoodcart();
                    break;
                case ChaseState.WaitingAtFoodcart:
                    UpdateWaitAtFoodcart();
                    break;
                case ChaseState.ReturningHome:
                    UpdateReturnHome();
                    break;
            }
        }

        private void UpdateChase()
        {
            if (player == null)
            {
                return;
            }

            Vector3 currentPlayerPosition = player.position;
            TrackRoomDoorCrossings(
                previousPlayerPosition,
                currentPlayerPosition);
            previousPlayerPosition = currentPlayerPosition;

            if (CanEscapeThroughClosedRoomDoor(
                    currentPlayerPosition))
            {
                CompleteEscape();
                return;
            }

            if (CanCatchPlayer(currentPlayerPosition))
            {
                CatchPlayer();
                return;
            }

            RefreshDestination(currentPlayerPosition);
        }

        private void UpdateForcedCatch()
        {
            if (player == null ||
                Time.time >= forcedCatchDeadline ||
                CanCatchPlayer(player.position))
            {
                CatchPlayer();
                return;
            }

            RefreshDestination(player.position);
        }

        private void OnDisable()
        {
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            ReleaseForcedCatchInput();
            StopAgent();
            if (animationPlayer != null)
            {
                animationPlayer.StopAtInitialPose();
            }
        }

        public bool BeginChase(Transform playerTarget)
        {
            if (state != ChaseState.Idle || playerTarget == null)
            {
                return false;
            }

            if (!EnsureAgentOnNavMesh())
            {
                Debug.LogError(
                    "[Henry] Không thể bắt đầu truy đuổi vì Henry " +
                    "không đứng trên NavMesh.",
                    this);
                return false;
            }

            if (animationPlayer == null ||
                !animationPlayer.PlayRun())
            {
                Debug.LogError(
                    "[Henry] Không thể bắt đầu truy đuổi vì " +
                    "Henry_Run_FastRun.anim chưa sẵn sàng.",
                    this);
                return false;
            }

            player = playerTarget;
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            state = ChaseState.Chasing;
            previousPlayerPosition = player.position;
            activeEscapeDoor = null;
            CacheRoomDoors();

            agent.isStopped = false;
            agent.stoppingDistance = GetChaseStoppingDistance();
            nextDestinationRefreshAt = 0f;
            RefreshDestination(player.position, true);

            Debug.Log(
                "[Henry] Người chơi đã ăn trộm. Henry bắt đầu truy đuổi.",
                this);
            ChaseStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// Starts the chase after the player crosses ChaseTrigger with the
        /// swapped battery. Unlike the ordinary theft entry point, this may
        /// interrupt Henry's food-cart visit or his return trip, while keeping
        /// the existing chase, closed-door escape, and return-home flow.
        /// </summary>
        public bool BeginPostSwapChase(Transform playerTarget)
        {
            bool canInterruptCurrentState =
                state == ChaseState.Idle ||
                state == ChaseState.MovingToFoodcart ||
                state == ChaseState.WaitingAtFoodcart ||
                state == ChaseState.ReturningHome;
            if (!canInterruptCurrentState || playerTarget == null)
            {
                return false;
            }

            if (!EnsureAgentOnNavMesh())
            {
                Debug.LogError(
                    "[Henry] Cannot start the post-swap chase because " +
                    "Henry is not on the NavMesh.",
                    this);
                return false;
            }

            if (animationPlayer == null ||
                !animationPlayer.PlayRun())
            {
                Debug.LogError(
                    "[Henry] Cannot start the post-swap chase because " +
                    "the run animation is unavailable.",
                    this);
                return false;
            }

            player = playerTarget;
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            state = ChaseState.Chasing;
            previousPlayerPosition = player.position;
            activeEscapeDoor = null;
            CacheRoomDoors();

            agent.isStopped = false;
            agent.stoppingDistance = GetChaseStoppingDistance();
            nextDestinationRefreshAt = 0f;
            RefreshDestination(player.position, true);

            Debug.Log(
                "[Henry] Phát hiện ắc quy đã bị đánh tráo; " +
                "bắt đầu truy đuổi người chơi.",
                this);
            ChaseStarted?.Invoke();
            return true;
        }

        public bool BeginFoodcartDistraction(Transform foodcart)
        {
            if (!CanStartDistraction || foodcart == null)
            {
                return false;
            }

            if (!EnsureAgentOnNavMesh() ||
                !TryFindFoodcartDestination(
                    foodcart,
                    out foodcartDestination))
            {
                Debug.LogError(
                    "[Henry] No NavMesh route to the foodcart.",
                    this);
                return false;
            }

            if (animationPlayer == null ||
                !animationPlayer.PlayRun())
            {
                Debug.LogError(
                    "[Henry] Foodcart run animation is unavailable.",
                    this);
                return false;
            }

            player = null;
            activeEscapeDoor = null;
            batterySwapRaceActive = false;
            foodcartDistractionActive = true;
            state = ChaseState.MovingToFoodcart;
            agent.isStopped = false;
            agent.stoppingDistance = distractionStoppingDistance;
            nextDestinationRefreshAt = 0f;
            RefreshDestination(foodcartDestination, true);

            Debug.Log(
                "[Henry] Heading to the foodcart distraction point.",
                this);
            return true;
        }

        public bool BeginBatterySwapRace(Transform playerTarget)
        {
            bool canStartRace =
                state == ChaseState.Idle ||
                state == ChaseState.ReturningHome;
            if (!canStartRace || playerTarget == null)
            {
                return false;
            }

            if (!EnsureAgentOnNavMesh() ||
                animationPlayer == null ||
                !animationPlayer.PlayRun())
            {
                return false;
            }

            player = playerTarget;
            foodcartDistractionActive = false;
            batterySwapRaceActive = true;
            state = ChaseState.Chasing;
            previousPlayerPosition = player.position;
            activeEscapeDoor = null;
            CacheRoomDoors();

            agent.isStopped = false;
            agent.stoppingDistance = GetChaseStoppingDistance();
            nextDestinationRefreshAt = 0f;
            RefreshDestination(player.position, true);

            Debug.Log(
                "[Henry] Quay về và phát hiện người chơi đang đánh tráo pin.",
                this);
            ChaseStarted?.Invoke();
            return true;
        }

        public void CompleteBatterySwapRace()
        {
            if (!IsBatterySwapRaceActive)
            {
                return;
            }

            batterySwapRaceActive = false;
            player = null;
            activeEscapeDoor = null;
            foodcartDistractionActive = false;
            BeginReturnHome();
            Debug.Log(
                "[Henry] Người chơi đánh tráo pin thành công trước khi bị phát hiện; Henry tiếp tục quay về.",
                this);
        }

        public void CancelBatterySwapRace()
        {
            if (!IsBatterySwapRaceActive)
            {
                return;
            }

            batterySwapRaceActive = false;
            player = null;
            activeEscapeDoor = null;
            foodcartDistractionActive = false;
            BeginReturnHome();

            Debug.Log(
                "[Henry] Người chơi ngừng đánh tráo pin; Henry tiếp tục quay về.",
                this);
        }

        public void BeginForcedCatch(Transform playerTarget)
        {
            if (state == ChaseState.Caught)
            {
                return;
            }

            player = playerTarget;
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            activeEscapeDoor = null;
            LockPlayerForForcedCatch();

            if (player == null ||
                !EnsureAgentOnNavMesh() ||
                animationPlayer == null ||
                !animationPlayer.PlayRun())
            {
                CatchPlayer();
                return;
            }

            state = ChaseState.ForcedCatch;
            float directTravelSeconds = Vector3.Distance(
                    transform.position,
                    player.position) /
                Mathf.Max(0.1f, chaseSpeed);
            forcedCatchDeadline = Time.time + Mathf.Max(
                forcedCatchTimeout,
                directTravelSeconds * 2f + 3f);
            agent.isStopped = false;
            agent.stoppingDistance = GetChaseStoppingDistance();
            nextDestinationRefreshAt = 0f;
            RefreshDestination(player.position, true);

            Debug.Log(
                "[Henry] Bắt quả tang người chơi đang đánh tráo pin.",
                this);
        }

        private void UpdateMoveToFoodcart()
        {
            if (!HasReachedDestination(
                    foodcartDestination,
                    distractionStoppingDistance))
            {
                RefreshDestination(foodcartDestination);
                return;
            }

            StopAgent();
            animationPlayer?.StopAtInitialPose();
            state = ChaseState.WaitingAtFoodcart;
            foodcartWaitEndsAt = Time.time + foodcartWaitDuration;
        }

        private void UpdateWaitAtFoodcart()
        {
            if (Time.time < foodcartWaitEndsAt)
            {
                return;
            }

            BeginReturnHome();
        }

        private void UpdateReturnHome()
        {
            if (!HasReachedDestination(homePosition, 0.1f))
            {
                RefreshDestination(homePosition);
                return;
            }

            StopAgent();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(homePosition);
            }

            transform.rotation = homeRotation;
            animationPlayer?.StopAtInitialPose();
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            state = ChaseState.Idle;

            Debug.Log(
                "[Henry] Returned to the original position.",
                this);
        }

        private void BeginReturnHome()
        {
            if (!EnsureAgentOnNavMesh())
            {
                foodcartDistractionActive = false;
                state = ChaseState.Idle;
                animationPlayer?.StopAtInitialPose();
                return;
            }

            state = ChaseState.ReturningHome;
            agent.isStopped = false;
            agent.stoppingDistance = 0.1f;
            nextDestinationRefreshAt = 0f;
            animationPlayer?.PlayRun();
            RefreshDestination(homePosition, true);
        }

        private bool HasReachedDestination(
            Vector3 destination,
            float stoppingDistance)
        {
            Vector3 separation = destination - transform.position;
            separation.y = 0f;
            return separation.sqrMagnitude <=
                   Mathf.Pow(stoppingDistance + 0.12f, 2f);
        }

        private bool TryFindFoodcartDestination(
            Transform foodcart,
            out Vector3 destination)
        {
            destination = default;
            Bounds bounds = GetWorldBounds(foodcart);
            Vector3 center = bounds.center;
            center.y = transform.position.y;

            float radius = Mathf.Max(
                bounds.extents.x,
                bounds.extents.z) + foodcartApproachPadding;
            Vector3[] directions =
            {
                Vector3.right,
                Vector3.left,
                Vector3.forward,
                Vector3.back,
                new Vector3(1f, 0f, 1f).normalized,
                new Vector3(-1f, 0f, 1f).normalized,
                new Vector3(1f, 0f, -1f).normalized,
                new Vector3(-1f, 0f, -1f).normalized
            };

            float bestDistance = float.MaxValue;
            NavMeshPath path = new NavMeshPath();
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 candidate = center + directions[i] * radius;
                if (!NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit navHit,
                        2.5f,
                        NavMesh.AllAreas) ||
                    !agent.CalculatePath(navHit.position, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float distance = (navHit.position - transform.position)
                    .sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                destination = navHit.position;
            }

            if (bestDistance < float.MaxValue)
            {
                return true;
            }

            if (NavMesh.SamplePosition(
                    center,
                    out NavMeshHit fallbackHit,
                    5f,
                    NavMesh.AllAreas) &&
                agent.CalculatePath(fallbackHit.position, path) &&
                path.status == NavMeshPathStatus.PathComplete)
            {
                destination = fallbackHit.position;
                return true;
            }

            return false;
        }

        private static Bounds GetWorldBounds(Transform root)
        {
            Collider[] colliders =
                root.GetComponentsInChildren<Collider>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(root.position, Vector3.zero);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled ||
                    collider.isTrigger)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }

            if (hasBounds)
            {
                return bounds;
            }

            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return bounds;
        }

        private float GetChaseStoppingDistance()
        {
            return Mathf.Max(0.35f, catchDistance * 0.7f);
        }

        private void ConfigureAgent()
        {
            HenryChaseNavigation.EnsureBuilt();

            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }

            agent.agentTypeID = 0;
            agent.radius = 0.34f;
            agent.height = 1.8f;
            agent.baseOffset = 0f;
            agent.speed = chaseSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = GetChaseStoppingDistance();
            agent.autoBraking = false;
            agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = 25;

            if (EnsureAgentOnNavMesh())
            {
                agent.isStopped = true;
            }
        }

        private bool EnsureAgentOnNavMesh()
        {
            if (agent != null &&
                agent.enabled &&
                agent.isOnNavMesh)
            {
                return true;
            }

            if (!HenryChaseNavigation.EnsureBuilt())
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit navHit,
                    2.5f,
                    NavMesh.AllAreas))
            {
                return false;
            }

            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            return agent.Warp(navHit.position) && agent.isOnNavMesh;
        }

        private void CacheRoomDoors()
        {
            trackedDoors.Clear();
            DoorInteractable[] doors =
                FindObjectsByType<DoorInteractable>(
                    FindObjectsInactive.Exclude);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                {
                    trackedDoors.Add(doors[i]);
                }
            }
        }

        private void TrackRoomDoorCrossings(
            Vector3 from,
            Vector3 to)
        {
            for (int i = 0; i < trackedDoors.Count; i++)
            {
                DoorInteractable door = trackedDoors[i];
                if (door == null || !door.IsOpen)
                {
                    continue;
                }

                if (door.DidCrossDoorway(
                        from,
                        to,
                        true,
                        doorwayHalfWidth))
                {
                    activeEscapeDoor = door;
                    continue;
                }

                if (door == activeEscapeDoor &&
                    door.DidCrossDoorway(
                        from,
                        to,
                        false,
                        doorwayHalfWidth))
                {
                    activeEscapeDoor = null;
                }
            }
        }

        private void RefreshDestination(
            Vector3 playerPosition,
            bool force = false)
        {
            if (agent == null ||
                !agent.enabled ||
                !agent.isOnNavMesh ||
                agent.isStopped ||
                (!force && Time.time < nextDestinationRefreshAt))
            {
                return;
            }

            nextDestinationRefreshAt =
                Time.time + destinationRefreshInterval;

            if (NavMesh.SamplePosition(
                    playerPosition,
                    out NavMeshHit playerHit,
                    1.5f,
                    NavMesh.AllAreas))
            {
                agent.SetDestination(playerHit.position);
            }
        }

        private bool CanCatchPlayer(Vector3 playerPosition)
        {
            Vector3 separation = playerPosition - transform.position;
            if (Mathf.Abs(separation.y) > 1.35f)
            {
                return false;
            }

            separation.y = 0f;
            if (separation.sqrMagnitude >
                catchDistance * catchDistance)
            {
                return false;
            }

            Vector3 rayStart = transform.position + Vector3.up * 0.9f;
            Vector3 rayEnd = playerPosition + Vector3.up * 0.9f;
            Vector3 ray = rayEnd - rayStart;
            float rayLength = ray.magnitude;
            if (rayLength <= 0.01f)
            {
                return true;
            }

            int obstructionMask =
                LayerMask.GetMask("Environment", "Interactable");
            return !Physics.Raycast(
                rayStart,
                ray / rayLength,
                rayLength,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
        }

        private bool CanEscapeThroughClosedRoomDoor(
            Vector3 playerPosition)
        {
            if (IsPlayerSafeBehindDoor(
                    activeEscapeDoor,
                    playerPosition))
            {
                return true;
            }

            // Crossing can be missed when the player moves across the thin
            // doorway plane in one frame. Closing a door requires standing
            // beside it, so recover the correct escape door from that local
            // position instead of allowing the chase to continue forever.
            float recoveryRadius = doorwayHalfWidth + 0.75f;
            float recoveryRadiusSquared =
                recoveryRadius * recoveryRadius;
            for (int i = 0; i < trackedDoors.Count; i++)
            {
                DoorInteractable door = trackedDoors[i];
                if (!IsPlayerSafeBehindDoor(door, playerPosition))
                {
                    continue;
                }

                Vector3 doorwayOffset =
                    playerPosition - door.DoorwayPoint;
                doorwayOffset.y = 0f;
                if (doorwayOffset.sqrMagnitude >
                    recoveryRadiusSquared)
                {
                    continue;
                }

                activeEscapeDoor = door;
                return true;
            }

            return false;
        }

        private bool IsPlayerSafeBehindDoor(
            DoorInteractable door,
            Vector3 playerPosition)
        {
            return door != null &&
                   !door.IsOpen &&
                   door.IsOnInsideSide(
                       playerPosition,
                       insideSideMargin);
        }

        private void LockPlayerForForcedCatch()
        {
            ReleaseForcedCatchInput();
            if (player == null)
            {
                return;
            }

            forcedCatchInputLock = player.GetComponent<PlayerInputLock>();
            forcedCatchInputLock?.Lock(ForcedCatchInputReason);
        }

        private void ReleaseForcedCatchInput()
        {
            if (forcedCatchInputLock == null)
            {
                return;
            }

            forcedCatchInputLock.Unlock(ForcedCatchInputReason);
            forcedCatchInputLock = null;
        }

        private void CatchPlayer()
        {
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            state = ChaseState.Caught;
            StopAgent();
            animationPlayer?.StopAtInitialPose();
            ReleaseForcedCatchInput();

            Debug.Log("[Henry] Henry đã bắt được người chơi.", this);
            PlayerCaught?.Invoke();
            Chapter1EventBus.RaisePlayerCaught();
        }

        private void CompleteEscape()
        {
            foodcartDistractionActive = false;
            batterySwapRaceActive = false;
            ReleaseForcedCatchInput();
            state = ChaseState.Escaped;
            StopAgent();
            animationPlayer?.StopAtInitialPose();

            Debug.Log(
                "[Henry] Người chơi đã vào phòng và đóng cửa an toàn.",
                this);
            PlayerEscaped?.Invoke();
            HenryGameOverPresenter.Instance?.ShowEscapeMessage();

            player = null;
            activeEscapeDoor = null;
            BeginReturnHome();
        }

        private void StopAgent()
        {
            if (agent == null ||
                !agent.enabled ||
                !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void OnValidate()
        {
            chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            angularSpeed = Mathf.Max(1f, angularSpeed);
            catchDistance = Mathf.Max(0.1f, catchDistance);
            destinationRefreshInterval =
                Mathf.Max(0.01f, destinationRefreshInterval);
            forcedCatchTimeout = Mathf.Max(0.5f, forcedCatchTimeout);
            foodcartWaitDuration = Mathf.Max(0.1f, foodcartWaitDuration);
            foodcartApproachPadding =
                Mathf.Max(0.1f, foodcartApproachPadding);
            distractionStoppingDistance =
                Mathf.Max(0.05f, distractionStoppingDistance);
            doorwayHalfWidth = Mathf.Max(0.5f, doorwayHalfWidth);
            insideSideMargin = Mathf.Max(0f, insideSideMargin);
        }
    }

    internal static class HenryChaseNavigation
    {
        private const string NavigationObjectName =
            "Henry_Chase_Navigation";
        private const string DoorHingeName =
            "Door_Room_Nam_Hinge";

        private static NavMeshSurface surface;

        public static bool EnsureBuilt()
        {
            if (surface != null &&
                surface.navMeshData != null)
            {
                return HasNavMesh();
            }

            int environmentLayer =
                LayerMask.NameToLayer("Environment");
            if (environmentLayer < 0)
            {
                Debug.LogError(
                    "[Henry] Project không có layer Environment.");
                return false;
            }

            InstallDoorObstacles();
            InstallFurnitureObstacles();

            GameObject navigationObject =
                GameObject.Find(NavigationObjectName);
            if (navigationObject == null)
            {
                navigationObject =
                    new GameObject(NavigationObjectName);
            }

            surface =
                navigationObject.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface =
                    navigationObject.AddComponent<NavMeshSurface>();
            }

            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = 1 << environmentLayer;
            surface.useGeometry =
                NavMeshCollectGeometry.PhysicsColliders;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            surface.BuildNavMesh();

            bool built = HasNavMesh();
            if (!built)
            {
                Debug.LogError(
                    "[Henry] Runtime NavMesh không tạo được dữ liệu.");
            }

            return built;
        }

        private static void InstallFurnitureObstacles()
        {
            Transform[] transforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Exclude);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null ||
                    candidate.name.IndexOf(
                        "table",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                Transform[] hierarchy =
                    candidate.GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0;
                     childIndex < hierarchy.Length;
                     childIndex++)
                {
                    Collider[] colliders =
                        hierarchy[childIndex].GetComponents<Collider>();
                    for (int colliderIndex = 0;
                         colliderIndex < colliders.Length;
                         colliderIndex++)
                    {
                        Collider collider = colliders[colliderIndex];
                        if (collider == null ||
                            !collider.enabled ||
                            collider.isTrigger ||
                            !(collider is BoxCollider boxCollider))
                        {
                            continue;
                        }

                        NavMeshObstacle obstacle =
                            collider.GetComponent<NavMeshObstacle>();
                        if (obstacle == null)
                        {
                            obstacle =
                                collider.gameObject
                                    .AddComponent<NavMeshObstacle>();
                        }

                        obstacle.shape = NavMeshObstacleShape.Box;
                        obstacle.center = boxCollider.center;
                        obstacle.size = boxCollider.size;
                        obstacle.carving = true;
                        obstacle.carveOnlyStationary = false;
                        obstacle.carvingMoveThreshold = 0.01f;
                        obstacle.carvingTimeToStationary = 0.05f;
                    }
                }
            }
        }

        private static bool HasNavMesh()
        {
            return NavMesh.CalculateTriangulation()
                .vertices.Length > 0;
        }

        private static void InstallDoorObstacles()
        {
            Transform[] transforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Exclude);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform hinge = transforms[i];
                if (hinge == null ||
                    !string.Equals(
                        hinge.name,
                        DoorHingeName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                BoxCollider leaf =
                    hinge.GetComponentInChildren<BoxCollider>(true);
                if (leaf == null)
                {
                    continue;
                }

                NavMeshObstacle obstacle =
                    leaf.GetComponent<NavMeshObstacle>();
                if (obstacle == null)
                {
                    obstacle =
                        leaf.gameObject.AddComponent<NavMeshObstacle>();
                }

                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = leaf.center;
                obstacle.size = leaf.size;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
                obstacle.carvingMoveThreshold = 0.01f;
                obstacle.carvingTimeToStationary = 0.05f;
            }
        }
    }
}
