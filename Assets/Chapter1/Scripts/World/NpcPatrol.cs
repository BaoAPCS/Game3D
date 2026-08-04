using UnityEngine;
using UnityEngine.AI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class NpcPatrol : MonoBehaviour
    {
        private const float RunLoopRestartNormalizedTime = 0.95f;
        private static readonly int RunStateHash = Animator.StringToHash("Base Layer.Run_F");
        private static readonly int IdleStateHash =
            Animator.StringToHash("Base Layer.Pose_Idle");

        [Header("Pavement route (world-space X positions)")]
        [SerializeField] private Vector3 endpointA = new Vector3(-10f, -0.013f, -17.575f);
        [SerializeField] private Vector3 endpointB = new Vector3(10f, -0.013f, -17.575f);

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float runSpeed = 2.5f;
        [SerializeField, Min(0f)] private float turnDuration = 0.2f;
        [SerializeField, Min(0.001f)] private float arrivalThreshold = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool drawRoute = true;

        private Animator animator;
        private float routeY;
        private float routeZ;
        private int targetEndpointIndex;
        private bool turning;
        private float turnElapsed;
        private Quaternion turnStartRotation;
        private Quaternion turnTargetRotation;
        private Vector3 emergencyDestination;
        private NavMeshAgent emergencyAgent;
        private bool emergencyRunning;
        private bool usingEmergencyNavMesh;
        private bool holdingAtFoodcart;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (Mathf.Abs(endpointA.x - endpointB.x) <= arrivalThreshold)
            {
                Debug.LogWarning("NpcPatrol needs two different X endpoints.", this);
                enabled = false;
                return;
            }

            // The route is deliberately constrained to the pavement line where the NPC starts.
            // This prevents small endpoint Y/Z edits from making the NPC leave the pavement.
            routeY = transform.position.y;
            routeZ = transform.position.z;
            targetEndpointIndex = Mathf.Abs(transform.position.x - endpointA.x) <= arrivalThreshold ? 1 : 0;
            turning = false;
            turnElapsed = 0f;
            emergencyRunning = false;
            usingEmergencyNavMesh = false;
            holdingAtFoodcart = false;

            FaceDirection(GetTargetPoint() - transform.position);
            PlayRunAnimation();
        }

        private void Update()
        {
            if (holdingAtFoodcart)
            {
                return;
            }

            if (emergencyRunning)
            {
                UpdateEmergencyRun();
                return;
            }

            KeepRunAnimationPlaying();

            if (turning)
            {
                UpdateTurn();
                return;
            }

            Vector3 target = GetTargetPoint();
            float nextX = Mathf.MoveTowards(transform.position.x, target.x, runSpeed * Time.deltaTime);
            transform.position = new Vector3(nextX, routeY, routeZ);

            FaceDirection(target - transform.position);

            if (Mathf.Abs(transform.position.x - target.x) <= arrivalThreshold)
            {
                transform.position = target;
                BeginTurn();
            }
        }

        internal bool BeginEmergencyRun(Vector3 destination)
        {
            if (holdingAtFoodcart || emergencyRunning)
            {
                return false;
            }

            emergencyDestination = new Vector3(
                destination.x,
                transform.position.y,
                destination.z);
            turning = false;
            emergencyRunning = true;
            usingEmergencyNavMesh =
                TryStartEmergencyNavigation(ref emergencyDestination);
            holdingAtFoodcart = false;
            FaceDirection(emergencyDestination - transform.position);
            PlayRunAnimation();
            return true;
        }

        private void UpdateEmergencyRun()
        {
            KeepRunAnimationPlaying();
            if (usingEmergencyNavMesh)
            {
                UpdateEmergencyNavigation();
                return;
            }

            Vector3 currentPosition = transform.position;
            float emergencySpeed = Mathf.Max(runSpeed, 3.5f);
            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                emergencyDestination,
                emergencySpeed * Time.deltaTime);
            transform.position = nextPosition;
            FaceDirection(emergencyDestination - nextPosition);

            Vector3 remaining = emergencyDestination - nextPosition;
            remaining.y = 0f;
            float stopThreshold = Mathf.Max(arrivalThreshold, 0.15f);
            if (remaining.sqrMagnitude > stopThreshold * stopThreshold)
            {
                return;
            }

            transform.position = emergencyDestination;
            FinishEmergencyRun();
        }

        private bool TryStartEmergencyNavigation(
            ref Vector3 destination)
        {
            if (!HenryChaseNavigation.EnsureBuilt())
            {
                return false;
            }

            emergencyAgent = GetComponent<NavMeshAgent>();
            if (emergencyAgent == null)
            {
                emergencyAgent = gameObject.AddComponent<NavMeshAgent>();
            }

            float emergencySpeed = Mathf.Max(runSpeed, 3.5f);
            emergencyAgent.agentTypeID = 0;
            emergencyAgent.radius = 0.3f;
            emergencyAgent.height = 1.8f;
            emergencyAgent.baseOffset = 0f;
            emergencyAgent.speed = emergencySpeed;
            emergencyAgent.acceleration = 16f;
            emergencyAgent.angularSpeed = 720f;
            emergencyAgent.stoppingDistance =
                Mathf.Max(arrivalThreshold, 0.2f);
            emergencyAgent.autoBraking = true;
            emergencyAgent.updatePosition = true;
            emergencyAgent.updateRotation = true;
            emergencyAgent.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            emergencyAgent.avoidancePriority = 35;

            if (!emergencyAgent.enabled)
            {
                emergencyAgent.enabled = true;
            }

            if (!emergencyAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(
                        transform.position,
                        out NavMeshHit startHit,
                        2.5f,
                        NavMesh.AllAreas) ||
                    !emergencyAgent.Warp(startHit.position))
                {
                    emergencyAgent.enabled = false;
                    return false;
                }
            }

            if (!NavMesh.SamplePosition(
                    destination,
                    out NavMeshHit destinationHit,
                    3f,
                    NavMesh.AllAreas))
            {
                emergencyAgent.enabled = false;
                return false;
            }

            destination = destinationHit.position;
            emergencyAgent.isStopped = false;
            if (emergencyAgent.SetDestination(destination))
            {
                return true;
            }

            emergencyAgent.enabled = false;
            return false;
        }

        private void UpdateEmergencyNavigation()
        {
            if (emergencyAgent == null ||
                !emergencyAgent.enabled ||
                !emergencyAgent.isOnNavMesh)
            {
                usingEmergencyNavMesh = false;
                return;
            }

            if (emergencyAgent.pathPending)
            {
                return;
            }

            if (emergencyAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                emergencyAgent.isStopped = true;
                emergencyAgent.enabled = false;
                usingEmergencyNavMesh = false;
                return;
            }

            float stopThreshold = Mathf.Max(
                emergencyAgent.stoppingDistance,
                arrivalThreshold);
            if (emergencyAgent.remainingDistance > stopThreshold)
            {
                return;
            }

            FinishEmergencyRun();
        }

        private void FinishEmergencyRun()
        {
            if (emergencyAgent != null && emergencyAgent.enabled)
            {
                if (emergencyAgent.isOnNavMesh)
                {
                    emergencyAgent.isStopped = true;
                    emergencyAgent.ResetPath();
                }

                emergencyAgent.enabled = false;
            }

            emergencyRunning = false;
            usingEmergencyNavMesh = false;
            holdingAtFoodcart = true;
            PlayIdleAnimation();
        }

        private void BeginTurn()
        {
            int nextEndpointIndex = 1 - targetEndpointIndex;
            Vector3 nextTarget = GetPoint(nextEndpointIndex);
            Vector3 direction = nextTarget - transform.position;
            direction.y = 0f;

            targetEndpointIndex = nextEndpointIndex;
            turnElapsed = 0f;
            turnStartRotation = transform.rotation;
            turnTargetRotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : transform.rotation;

            if (turnDuration <= 0f)
            {
                transform.rotation = turnTargetRotation;
                turning = false;
                PlayRunAnimation();
                return;
            }

            turning = true;
        }

        private void UpdateTurn()
        {
            turnElapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(turnElapsed / turnDuration);
            transform.rotation = Quaternion.Slerp(turnStartRotation, turnTargetRotation, normalizedTime);

            if (normalizedTime >= 1f)
            {
                turning = false;
                PlayRunAnimation();
            }
        }

        private Vector3 GetTargetPoint()
        {
            return GetPoint(targetEndpointIndex);
        }

        private Vector3 GetPoint(int index)
        {
            Vector3 endpoint = index == 0 ? endpointA : endpointB;
            return new Vector3(endpoint.x, routeY, routeZ);
        }

        private void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void PlayRunAnimation()
        {
            if (animator == null)
            {
                return;
            }

            if (!animator.HasState(0, RunStateHash))
            {
                Debug.LogWarning("NpcPatrol could not find the 'Base Layer.Run_F' Animator state.", animator);
                return;
            }

            animator.CrossFadeInFixedTime(RunStateHash, 0.1f, 0, 0f);
        }

        private void KeepRunAnimationPlaying()
        {
            if (animator == null || animator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool leftRunState = state.fullPathHash != RunStateHash;
            bool runCycleIsEnding =
                state.fullPathHash == RunStateHash &&
                state.normalizedTime >= RunLoopRestartNormalizedTime;

            if (leftRunState || runCycleIsEnding)
            {
                animator.Play(RunStateHash, 0, 0f);
            }
        }

        private void PlayIdleAnimation()
        {
            if (animator == null || !animator.HasState(0, IdleStateHash))
            {
                return;
            }

            animator.CrossFadeInFixedTime(
                IdleStateHash,
                0.15f,
                0,
                0f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawRoute)
            {
                return;
            }

            Vector3 start = new Vector3(endpointA.x, transform.position.y, transform.position.z);
            Vector3 end = new Vector3(endpointB.x, transform.position.y, transform.position.z);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.2f);
            Gizmos.DrawWireSphere(end, 0.2f);
        }

        private void OnValidate()
        {
            runSpeed = Mathf.Max(0.01f, runSpeed);
            turnDuration = Mathf.Max(0f, turnDuration);
            arrivalThreshold = Mathf.Max(0.001f, arrivalThreshold);
        }
    }
}
