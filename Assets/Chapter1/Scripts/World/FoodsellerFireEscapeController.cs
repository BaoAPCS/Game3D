using UnityEngine;
using UnityEngine.AI;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Makes Foodseller flee from the burning food cart using the Humanoid
    /// Fast Run clip. The component is installed only when the fire starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoodsellerFireEscapeController : MonoBehaviour
    {
        private const string RunControllerResourcePath =
            "Foodcart/FoodsellerFastRun";
        private const float RunLoopRestartNormalizedTime = 0.95f;

        private static readonly int RunStateHash =
            Animator.StringToHash("Base Layer.Fast Run");

        [SerializeField, Min(0.1f)] private float escapeSpeed = 4.8f;
        [SerializeField, Min(1f)] private float escapeDistance = 14f;
        [SerializeField, Min(0.01f)] private float arrivalThreshold = 0.25f;
        [SerializeField, Min(1f)] private float turnSpeed = 720f;

        private Animator animator;
        private NavMeshAgent agent;
        private RuntimeAnimatorController runController;
        private Vector3 escapeDestination;
        private bool escaping;
        private bool usingNavMesh;

        internal static FoodsellerFireEscapeController GetOrInstall(
            GameObject foodseller)
        {
            if (foodseller == null)
            {
                return null;
            }

            FoodsellerFireEscapeController controller =
                foodseller.GetComponent<
                    FoodsellerFireEscapeController>();
            if (controller == null)
            {
                controller = foodseller.AddComponent<
                    FoodsellerFireEscapeController>();
            }

            return controller;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!escaping)
            {
                return;
            }

            KeepRunAnimationPlaying();
            if (usingNavMesh)
            {
                UpdateNavMeshEscape();
            }
            else
            {
                UpdateFallbackEscape();
            }
        }

        internal bool BeginEscape(Transform foodcart)
        {
            if (escaping || foodcart == null)
            {
                return escaping;
            }

            ResolveReferences();
            if (!PrepareRunAnimation())
            {
                return false;
            }

            Vector3 awayDirection = transform.position - foodcart.position;
            awayDirection.y = 0f;
            if (awayDirection.sqrMagnitude <= 0.001f)
            {
                awayDirection = Vector3.back;
            }

            awayDirection.Normalize();
            usingNavMesh = TryStartNavMeshEscape(awayDirection);
            if (!usingNavMesh)
            {
                if (agent != null)
                {
                    agent.enabled = false;
                }

                escapeDestination = transform.position +
                                    awayDirection * escapeDistance;
            }

            escaping = true;
            PlayRunAnimation();
            Debug.Log(
                "[FoodcartFire] Foodseller is running away from the cart.",
                this);
            return true;
        }

        private void ResolveReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>() ??
                    GetComponentInChildren<Animator>(true);
            }
        }

        private bool PrepareRunAnimation()
        {
            if (animator == null)
            {
                Debug.LogError(
                    "[FoodcartFire] Foodseller has no Animator.",
                    this);
                return false;
            }

            if (animator.avatar == null ||
                !animator.avatar.isValid ||
                !animator.avatar.isHuman)
            {
                Debug.LogError(
                    "[FoodcartFire] Foodseller needs a valid Humanoid Avatar.",
                    animator);
                return false;
            }

            if (runController == null)
            {
                runController = Resources.Load<RuntimeAnimatorController>(
                    RunControllerResourcePath);
            }

            if (runController == null)
            {
                Debug.LogError(
                    $"[FoodcartFire] Cannot load " +
                    $"'{RunControllerResourcePath}'.",
                    this);
                return false;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = runController;
            animator.Rebind();
            animator.Update(0f);
            if (!animator.HasState(0, RunStateHash))
            {
                Debug.LogError(
                    "[FoodcartFire] Foodseller Fast Run state is missing.",
                    animator);
                return false;
            }

            return true;
        }

        private bool TryStartNavMeshEscape(Vector3 awayDirection)
        {
            if (!HenryChaseNavigation.EnsureBuilt())
            {
                return false;
            }

            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }

            ConfigureAgent();
            if (!EnsureAgentOnNavMesh())
            {
                return false;
            }

            float[] angleOffsets = { 0f, 30f, -30f, 60f, -60f };
            float[] distanceScales = { 1f, 0.85f, 0.7f };
            NavMeshPath path = new NavMeshPath();
            Vector3 start = transform.position;
            for (int distanceIndex = 0;
                 distanceIndex < distanceScales.Length;
                 distanceIndex++)
            {
                float distance = escapeDistance *
                                 distanceScales[distanceIndex];
                for (int angleIndex = 0;
                     angleIndex < angleOffsets.Length;
                     angleIndex++)
                {
                    Vector3 direction = Quaternion.Euler(
                        0f,
                        angleOffsets[angleIndex],
                        0f) * awayDirection;
                    Vector3 candidate = start + direction * distance;
                    if (!NavMesh.SamplePosition(
                            candidate,
                            out NavMeshHit hit,
                            3.5f,
                            NavMesh.AllAreas))
                    {
                        continue;
                    }

                    Vector3 progress = hit.position - start;
                    progress.y = 0f;
                    if (Vector3.Dot(progress, awayDirection) <
                        distance * 0.45f)
                    {
                        continue;
                    }

                    if (!agent.CalculatePath(hit.position, path) ||
                        path.status != NavMeshPathStatus.PathComplete)
                    {
                        continue;
                    }

                    escapeDestination = hit.position;
                    agent.isStopped = false;
                    return agent.SetDestination(escapeDestination);
                }
            }

            return false;
        }

        private void ConfigureAgent()
        {
            agent.speed = escapeSpeed;
            agent.acceleration = 24f;
            agent.angularSpeed = turnSpeed;
            agent.stoppingDistance = arrivalThreshold;
            agent.radius = 0.25f;
            agent.height = 1.75f;
            agent.autoBraking = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        private bool EnsureAgentOnNavMesh()
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                return true;
            }

            agent.enabled = true;
            if (!NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit startHit,
                    4f,
                    NavMesh.AllAreas))
            {
                return false;
            }

            return agent.Warp(startHit.position) && agent.isOnNavMesh;
        }

        private void UpdateNavMeshEscape()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                usingNavMesh = false;
                return;
            }

            if (agent.pathPending)
            {
                return;
            }

            float stoppingDistance = agent.stoppingDistance +
                                     arrivalThreshold;
            if (agent.remainingDistance <= stoppingDistance)
            {
                CompleteEscape();
            }
        }

        private void UpdateFallbackEscape()
        {
            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                escapeDestination,
                escapeSpeed * Time.deltaTime);
            Vector3 direction = nextPosition - currentPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    direction,
                    Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }

            transform.position = nextPosition;
            Vector3 remaining = escapeDestination - nextPosition;
            remaining.y = 0f;
            if (remaining.sqrMagnitude <=
                arrivalThreshold * arrivalThreshold)
            {
                CompleteEscape();
            }
        }

        private void PlayRunAnimation()
        {
            if (animator != null && animator.HasState(0, RunStateHash))
            {
                animator.Play(RunStateHash, 0, 0f);
            }
        }

        private void KeepRunAnimationPlaying()
        {
            if (animator == null || animator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != RunStateHash ||
                state.normalizedTime >= RunLoopRestartNormalizedTime)
            {
                animator.Play(RunStateHash, 0, 0f);
            }
        }

        private void CompleteEscape()
        {
            escaping = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            Debug.Log(
                "[FoodcartFire] Foodseller escaped from the fire.",
                this);
            gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            escapeSpeed = Mathf.Max(0.1f, escapeSpeed);
            escapeDistance = Mathf.Max(1f, escapeDistance);
            arrivalThreshold = Mathf.Max(0.01f, arrivalThreshold);
            turnSpeed = Mathf.Max(1f, turnSpeed);
        }
    }
}
