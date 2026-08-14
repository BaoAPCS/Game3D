using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Runtime-installed encounter for James, David, and Lewis. Story/UI code
    /// only needs to call BeginHostileEncounter after the hostile dialogue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GangEncounterController : MonoBehaviour
    {
        private const int RequiredValidHits = 6;
        private const float PursuitSpeed = 6.75f;
        private const float AttackRange = 1.45f;
        private const float AttackValidationRange = 1.9f;
        private const float AttackHitNormalizedTime = 0.45f;
        private const float AttackEndNormalizedTime = 0.95f;
        private const float AttackFallbackHitDelay = 0.55f;
        private const float AttackFallbackCycleDuration = 2.5f;
        private const float MinimumAttackSpacing = 0.48f;
        private const float StunnedBeforeGameOverDelay = 1.25f;
        private const string GangGameOverReason =
            "Bạn đã bị James, David và Lewis đánh gục.";

        private static readonly GangMemberSetup[] MemberSetups =
        {
            new GangMemberSetup(
                "James",
                "Base Layer.James_Idle",
                "Base Layer.James_Run",
                "Base Layer.James_Fighting",
                new Vector3(0f, 0f, -0.35f),
                20),
            new GangMemberSetup(
                "David",
                "Base Layer.David_Idle",
                "Base Layer.David_Run",
                "Base Layer.David_Fighting",
                new Vector3(-0.62f, 0f, 0.18f),
                30),
            new GangMemberSetup(
                "Lewis",
                "Base Layer.Lewis_Idle",
                "Base Layer.Lewis_Run",
                "Base Layer.Lewis_Fighting",
                new Vector3(0.62f, 0f, 0.18f),
                40)
        };

        private readonly List<GangMemberActor> members =
            new List<GangMemberActor>(MemberSetups.Length);

        private Transform player;
        private PlayerCombatController playerCombat;
        private bool isHostile;
        private bool isFinishing;
        private int validHitCount;
        private float nextAttackTime;

        public static GangEncounterController Instance { get; private set; }
        public bool IsHostile => isHostile;
        public int ValidHitCount => validHitCount;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

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
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForScene(scene);
        }

        public static bool BeginHostileEncounter()
        {
            if (Instance == null)
            {
                InstallForScene(SceneManager.GetActiveScene());
            }

            return Instance != null && Instance.BeginEncounter();
        }

        private static void InstallForScene(Scene scene)
        {
            if (Instance != null || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Chapter1PlayerMotor playerMotor =
                UnityEngine.Object.FindAnyObjectByType<Chapter1PlayerMotor>(
                    FindObjectsInactive.Exclude);
            if (playerMotor == null || playerMotor.gameObject.scene != scene)
            {
                return;
            }

            Transform[] characters = new Transform[MemberSetups.Length];
            for (int i = 0; i < MemberSetups.Length; i++)
            {
                characters[i] = FindSceneTransform(scene, MemberSetups[i].Name);
                if (characters[i] == null)
                {
                    return;
                }
            }

            GameObject directorObject =
                new GameObject(nameof(GangEncounterController));
            SceneManager.MoveGameObjectToScene(directorObject, scene);
            GangEncounterController director =
                directorObject.AddComponent<GangEncounterController>();
            director.Initialize(playerMotor.transform, characters);
            if (Mission3Progress.GangHostile)
            {
                director.BeginEncounter();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!isHostile || isFinishing || player == null)
            {
                return;
            }

            for (int i = 0; i < members.Count; i++)
            {
                members[i]?.Tick(player, Time.deltaTime);
            }
        }

        private void Initialize(
            Transform playerTransform,
            IReadOnlyList<Transform> characters)
        {
            player = playerTransform;
            playerCombat = player != null
                ? player.GetComponent<PlayerCombatController>()
                : null;

            for (int i = 0; i < MemberSetups.Length; i++)
            {
                Transform character = characters[i];
                GangMemberActor actor = new GangMemberActor(
                    this,
                    character,
                    MemberSetups[i]);
                members.Add(actor);
            }
        }

        private bool BeginEncounter()
        {
            if (isHostile || isFinishing || player == null || members.Count == 0)
            {
                return isHostile;
            }

            if (!Mission3Progress.GangHostile)
            {
                Mission3Progress.MarkGangHostile();
            }

            Mission2HeistProgress.ConcludeHenryEncounter(gameObject.scene);

            if (!HenryChaseNavigation.EnsureBuilt())
            {
                Debug.LogWarning(
                    "[GangEncounter] Không tạo được NavMesh; gang sẽ dùng di chuyển dự phòng.",
                    this);
            }

            isHostile = true;
            validHitCount = 0;
            nextAttackTime = Time.time + 0.35f;
            for (int i = 0; i < members.Count; i++)
            {
                members[i]?.BeginPursuit();
            }

            return true;
        }

        internal bool TryReserveAttack(GangMemberActor member)
        {
            if (!isHostile || isFinishing || Time.time < nextAttackTime)
            {
                return false;
            }

            nextAttackTime = Time.time + MinimumAttackSpacing;
            return true;
        }

        internal void RegisterValidHit(GangMemberActor member)
        {
            if (!isHostile || isFinishing || member == null)
            {
                return;
            }

            validHitCount++;
            if (validHitCount < RequiredValidHits)
            {
                return;
            }

            StartCoroutine(FinishEncounterRoutine());
        }

        private IEnumerator FinishEncounterRoutine()
        {
            isFinishing = true;
            for (int i = 0; i < members.Count; i++)
            {
                members[i]?.StopForDefeat();
            }

            if (playerCombat == null && player != null)
            {
                playerCombat = player.GetComponent<PlayerCombatController>();
            }

            if (playerCombat == null || !playerCombat.EnterForcedStun())
            {
                // The encounter must still terminate cleanly if the player's
                // Animator was misconfigured. Lock input as a safe fallback;
                // the normal prefab path plays Base Layer.Stunned above.
                PlayerInputLock fallbackInputLock = player != null
                    ? player.GetComponent<PlayerInputLock>()
                    : null;
                fallbackInputLock?.Lock("GangDefeat");
            }

            float elapsed = 0f;
            while (elapsed < StunnedBeforeGameOverDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Chapter1EventBus.RaiseGameOver(
                GangGameOverReason,
                GameOverRestartPolicy.ResetChapterThenReload);
        }

        private static Transform FindSceneTransform(Scene scene, string objectName)
        {
            Transform[] transforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Exclude);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null &&
                    candidate.gameObject.scene == scene &&
                    string.Equals(
                        candidate.name,
                        objectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        internal readonly struct GangMemberSetup
        {
            public GangMemberSetup(
                string name,
                string idleState,
                string runState,
                string fightState,
                Vector3 pursuitOffset,
                int avoidancePriority)
            {
                Name = name;
                IdleState = idleState;
                RunState = runState;
                FightState = fightState;
                PursuitOffset = pursuitOffset;
                AvoidancePriority = avoidancePriority;
            }

            public string Name { get; }
            public string IdleState { get; }
            public string RunState { get; }
            public string FightState { get; }
            public Vector3 PursuitOffset { get; }
            public int AvoidancePriority { get; }
        }

        internal sealed class GangMemberActor
        {
            private GangEncounterController director;
            private Transform character;
            private Animator animator;
            private NavMeshAgent agent;
            private GangMemberSetup setup;
            private int idleStateHash;
            private int runStateHash;
            private int fightStateHash;
            private bool pursuing;
            private bool attacking;
            private bool hitReported;
            private float attackElapsed;
            private int currentAnimationHash;

            internal GangMemberActor(
                GangEncounterController encounterDirector,
                Transform characterTransform,
                GangMemberSetup memberSetup)
            {
                director = encounterDirector;
                character = characterTransform;
                setup = memberSetup;
                idleStateHash = Animator.StringToHash(setup.IdleState);
                runStateHash = Animator.StringToHash(setup.RunState);
                fightStateHash = Animator.StringToHash(setup.FightState);
                animator = character.GetComponent<Animator>() ??
                    character.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    PlayState(idleStateHash, 0f, true);
                }
            }

            internal void BeginPursuit()
            {
                pursuing = true;
                attacking = false;
                hitReported = false;
                attackElapsed = 0f;
                EnsureAgent();
                PlayState(runStateHash, 0.08f, false);
            }

            internal void Tick(Transform target, float deltaTime)
            {
                if (!pursuing || target == null)
                {
                    return;
                }

                Vector3 toPlayer = target.position - character.position;
                toPlayer.y = 0f;
                float distance = toPlayer.magnitude;

                if (attacking)
                {
                    StopAgent();
                    FaceDirection(toPlayer, deltaTime);
                    attackElapsed += deltaTime;

                    bool hasAnimationProgress =
                        TryGetFightAnimationProgress(out float progress);
                    bool reachedHitFrame = hasAnimationProgress
                        ? progress >= AttackHitNormalizedTime
                        : attackElapsed >= AttackFallbackHitDelay;
                    if (!hitReported && reachedHitFrame)
                    {
                        hitReported = true;
                        if (distance <= AttackValidationRange)
                        {
                            director.RegisterValidHit(this);
                        }
                    }

                    bool animationFinished = hasAnimationProgress
                        ? progress >= AttackEndNormalizedTime
                        : attackElapsed >= AttackFallbackCycleDuration;
                    if (animationFinished)
                    {
                        attacking = false;
                        attackElapsed = 0f;
                        hitReported = false;
                    }

                    return;
                }

                if (distance <= AttackRange && director.TryReserveAttack(this))
                {
                    BeginAttack();
                    return;
                }

                Vector3 targetPoint = target.position +
                    target.right * setup.PursuitOffset.x +
                    target.forward * setup.PursuitOffset.z;
                Chase(targetPoint, deltaTime);
            }

            internal void StopForDefeat()
            {
                pursuing = false;
                StopAgent();
                PlayState(fightStateHash, 0.06f, true);
            }

            private void BeginAttack()
            {
                attacking = true;
                attackElapsed = 0f;
                hitReported = false;
                StopAgent();
                PlayState(fightStateHash, 0.06f, true);
            }

            private void Chase(Vector3 targetPoint, float deltaTime)
            {
                if (EnsureAgent() && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPoint);
                    PlayState(runStateHash, 0.08f, false);
                    return;
                }

                Vector3 movement = targetPoint - character.position;
                movement.y = 0f;
                if (movement.sqrMagnitude > 0.0025f)
                {
                    Vector3 direction = movement.normalized;
                    character.position += direction * PursuitSpeed * deltaTime;
                    FaceDirection(direction, deltaTime);
                }

                PlayState(runStateHash, 0.08f, false);
            }

            private bool EnsureAgent()
            {
                if (agent == null)
                {
                    agent = character.GetComponent<NavMeshAgent>();
                    if (agent == null)
                    {
                        agent = character.gameObject.AddComponent<NavMeshAgent>();
                    }

                    agent.speed = PursuitSpeed;
                    agent.acceleration = 30f;
                    agent.angularSpeed = 720f;
                    agent.stoppingDistance = AttackRange * 0.82f;
                    agent.radius = 0.32f;
                    agent.height = 1.75f;
                    agent.autoBraking = true;
                    agent.autoRepath = true;
                    agent.obstacleAvoidanceType =
                        ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    agent.avoidancePriority = setup.AvoidancePriority;
                }

                if (agent.enabled && agent.isOnNavMesh)
                {
                    return true;
                }

                if (!NavMesh.SamplePosition(
                        character.position,
                        out NavMeshHit navHit,
                        4f,
                        NavMesh.AllAreas))
                {
                    return false;
                }

                agent.enabled = true;
                return agent.Warp(navHit.position) && agent.isOnNavMesh;
            }

            private void StopAgent()
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
            }

            private void FaceDirection(Vector3 direction, float deltaTime)
            {
                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    return;
                }

                Quaternion targetRotation = Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
                character.rotation = Quaternion.RotateTowards(
                    character.rotation,
                    targetRotation,
                    720f * deltaTime);
            }

            private void PlayState(
                int stateHash,
                float transitionDuration,
                bool restart)
            {
                if (animator == null ||
                    animator.runtimeAnimatorController == null ||
                    !animator.HasState(0, stateHash) ||
                    (!restart &&
                     currentAnimationHash == stateHash &&
                     AnimatorIsPlayingState(stateHash)))
                {
                    return;
                }

                animator.enabled = true;
                animator.CrossFadeInFixedTime(
                    stateHash,
                    transitionDuration,
                    0,
                    0f);
                currentAnimationHash = stateHash;
            }

            private bool TryGetFightAnimationProgress(out float progress)
            {
                progress = 0f;
                if (animator == null || !animator.enabled)
                {
                    return false;
                }

                // When restarting the same fighting state, Unity can report
                // the previous state as current while the restarted copy is
                // the next state. Prefer the next state during that blend.
                if (animator.IsInTransition(0))
                {
                    AnimatorStateInfo next =
                        animator.GetNextAnimatorStateInfo(0);
                    if (next.fullPathHash == fightStateHash)
                    {
                        progress = next.normalizedTime;
                        return true;
                    }
                }

                AnimatorStateInfo current =
                    animator.GetCurrentAnimatorStateInfo(0);
                if (current.fullPathHash != fightStateHash)
                {
                    return false;
                }

                progress = current.normalizedTime;
                return true;
            }

            private bool AnimatorIsPlayingState(int stateHash)
            {
                AnimatorStateInfo current =
                    animator.GetCurrentAnimatorStateInfo(0);
                if (current.fullPathHash == stateHash)
                {
                    return true;
                }

                return animator.IsInTransition(0) &&
                       animator.GetNextAnimatorStateInfo(0).fullPathHash ==
                       stateHash;
            }
        }
    }
}
