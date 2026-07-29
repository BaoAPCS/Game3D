using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class NpcPatrol : MonoBehaviour
    {
        private const float RunLoopRestartNormalizedTime = 0.95f;
        private static readonly int RunStateHash = Animator.StringToHash("Base Layer.Run_F");

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

            FaceDirection(GetTargetPoint() - transform.position);
            PlayRunAnimation();
        }

        private void Update()
        {
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
