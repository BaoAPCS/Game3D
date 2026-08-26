using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Runtime-installed director for the Nam versus Henry fight. It owns the
    /// health, HUD, input mode and Henry's combat navigation. The existing
    /// scene characters fight at their current world positions; no character
    /// clone or dedicated arena is used.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HenryFightEncounterController : MonoBehaviour
    {
        public enum FightState
        {
            Inactive,
            Starting,
            Pursuing,
            Attacking,
            Recovery,
            PlayerDefeated,
            HenryDefeated,
            PoliceArriving,
            PoliceArrested
        }

        public const float HenryFightSpeed = 6.5f;
        public const float HenryAttackRange = 1.35f;
        public const float HenryAttackRecovery = 0.35f;

        private const float DestinationRefreshInterval = 0.1f;
        private const float HenryStoppingDistance = 1.02f;
        private const float AttackPathLengthTolerance = 0.25f;
        private const float StunnedBeforeGameOverDelay = 1.25f;
        private const float RotationSpeed = 720f;
        private const string GameOverReason =
            "Henry \u0111\u00e3 \u0111\u00e1nh g\u1ee5c b\u1ea1n.";
        private const string HenryDefeatedNotification =
            "B\u1ea1n \u0111\u00e3 \u0111\u00e1nh b\u1ea1i Henry.";
        private const string PoliceArrestedNotification =
            "C\u1ea3nh s\u00e1t \u0111\u00e3 b\u1eaft gi\u1eef b\u1ea1n.";

        [SerializeField] private Chapter1PlayerMotor playerMotor;
        [SerializeField] private Transform henryRoot;

        private Chapter1InputReader inputReader;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackPhoneController;
        private PlayerCombatController playerCombat;
        private CombatHealth playerHealth;
        private PlayerInputLock playerInputLock;

        private HenryChaseController henryChase;
        private HenryRunAnimationPlayer henryAnimation;
        private HenryCombatHitboxController henryCombat;
        private CombatHealth henryHealth;
        private FightCombatHUD fightHud;
        private PoliceArrestSequenceController policeArrest;

        private FightState state;
        private HenryCombatAttack nextAttack = HenryCombatAttack.Punch;
        private float recoveryEndsAt;
        private float nextDestinationRefreshAt;
        private bool initialized;
        private bool encounterActive;
        private bool outcomePending;
        private bool playerDeathObserved;
        private bool henryDeathObserved;
        private bool interactionStateCaptured;
        private bool interactionWasEnabled;
        private bool policeArrivalResolved;
        private bool policeArrivalSucceeded;
        private bool policeRecoveryActive;
        private Coroutine outcomeRoutine;

        public static HenryFightEncounterController Instance { get; private set; }
        public FightState State => state;
        public bool IsFightActive => encounterActive;

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

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            HenryFightEncounterController[] existing =
                FindObjectsByType<HenryFightEncounterController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null &&
                    existing[i].gameObject.scene == scene)
                {
                    Instance = existing[i];
                    return;
                }
            }

            Chapter1PlayerMotor player = FindScenePlayer(scene);
            Transform henry = FindSceneTransform(
                scene,
                HenryTheftInteractable.HenryObjectName);
            if (player == null || henry == null)
            {
                return;
            }

            GameObject directorObject =
                new GameObject(nameof(HenryFightEncounterController));
            SceneManager.MoveGameObjectToScene(directorObject, scene);
            HenryFightEncounterController director =
                directorObject.AddComponent<HenryFightEncounterController>();
            director.Initialize(player, henry);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            state = FightState.Inactive;
        }

        private void OnEnable()
        {
            Chapter1EventBus.HenryCombatReady += HandleHenryCombatReady;
        }

        private IEnumerator Start()
        {
            // Restore the held final pose before the first rendered frame.
            // Waiting here would let Henry flash Idle for one frame on reload.
            if (initialized && Mission3Progress.HenryDefeated)
            {
                ApplyPersistentDefeatState();
                yield break;
            }

            yield return null;
            if (!initialized)
            {
                yield break;
            }

            if (Mission3Progress.CombatPending)
            {
                RequestFightStart();
            }
        }

        private void Update()
        {
            if (!encounterActive || outcomePending ||
                playerMotor == null || henryRoot == null)
            {
                return;
            }

            if ((playerHealth != null && playerHealth.IsDead) ||
                (henryHealth != null && henryHealth.IsDead))
            {
                ScheduleOutcomeResolution();
                return;
            }

            switch (state)
            {
                case FightState.Pursuing:
                    UpdatePursuit();
                    break;
                case FightState.Attacking:
                    UpdateAttack();
                    break;
                case FightState.Recovery:
                    UpdateRecovery();
                    break;
            }
        }

        private void OnDisable()
        {
            Chapter1EventBus.HenryCombatReady -= HandleHenryCombatReady;
        }

        private void OnDestroy()
        {
            UnsubscribeHealthEvents();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Initialize(
            Chapter1PlayerMotor scenePlayer,
            Transform sceneHenry)
        {
            playerMotor = scenePlayer;
            henryRoot = sceneHenry;
            ResolveReferences();
            initialized = playerMotor != null && henryRoot != null;
        }

        private void ResolveReferences()
        {
            if (playerMotor != null)
            {
                if (inputReader == null)
                {
                    inputReader =
                        playerMotor.GetComponent<Chapter1InputReader>();
                }

                if (interactionController == null)
                {
                    interactionController = playerMotor
                        .GetComponent<Chapter1InteractionController>();
                }

                if (backpackPhoneController == null)
                {
                    backpackPhoneController = playerMotor
                        .GetComponent<BackpackPhoneInputController>();
                }

                if (playerCombat == null)
                {
                    playerCombat =
                        playerMotor.GetComponent<PlayerCombatController>();
                }

                if (playerInputLock == null)
                {
                    playerInputLock =
                        playerMotor.GetComponent<PlayerInputLock>();
                }

                playerHealth = playerCombat != null
                    ? playerCombat.CombatHealth
                    : playerMotor.GetComponent<CombatHealth>();
            }

            if (henryRoot == null)
            {
                return;
            }

            if (henryAnimation == null)
            {
                henryAnimation =
                    henryRoot.GetComponent<HenryRunAnimationPlayer>();
                if (henryAnimation == null)
                {
                    henryAnimation = henryRoot.gameObject
                        .AddComponent<HenryRunAnimationPlayer>();
                }
            }

            if (henryChase == null)
            {
                henryChase =
                    henryRoot.GetComponent<HenryChaseController>();
                if (henryChase == null)
                {
                    henryChase = henryRoot.gameObject
                        .AddComponent<HenryChaseController>();
                }
            }

            if (henryCombat == null)
            {
                henryCombat =
                    henryRoot.GetComponent<HenryCombatHitboxController>();
                if (henryCombat == null)
                {
                    henryCombat = henryRoot.gameObject
                        .AddComponent<HenryCombatHitboxController>();
                }
            }

            henryHealth = henryCombat.CombatHealth;
        }

        private void HandleHenryCombatReady()
        {
            RequestFightStart();
        }

        private void RequestFightStart()
        {
            if (!initialized || Mission3Progress.HenryDefeated ||
                encounterActive || state == FightState.Starting ||
                outcomePending)
            {
                if (Mission3Progress.HenryDefeated)
                {
                    ApplyPersistentDefeatState();
                }

                return;
            }

            BeginFight();
        }

        private void BeginFight()
        {
            state = FightState.Starting;
            ResolveReferences();
            if (!HasRequiredReferences())
            {
                AbortFightStart(
                    "Required player or Henry combat components are missing.");
                return;
            }

            NavMeshAgent agent = henryChase.Agent;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                AbortFightStart(
                    "Henry must already be standing on the NavMesh at " +
                    "the end of the warning dialogue.");
                return;
            }

            if (!henryChase.BeginFightControl(
                    playerMotor.transform,
                    HenryFightSpeed))
            {
                AbortFightStart(
                    "HenryChaseController could not enter fight control.");
                return;
            }

            playerCombat.ReleaseForcedStun();
            playerMotor.SetMovementEnabled(true);
            agent.speed = HenryFightSpeed;
            agent.acceleration = 18f;
            agent.angularSpeed = RotationSpeed;
            agent.stoppingDistance = HenryStoppingDistance;
            agent.autoBraking = true;
            agent.isStopped = true;
            agent.ResetPath();

            SubscribeHealthEvents();
            playerHealth.SetMaxHealth(100f, true);
            henryHealth.SetMaxHealth(100f, true);
            henryCombat.EnterCombatMode();

            EnterCombatInputMode();
            fightHud = FightCombatHUD.EnsureRuntimeHUD();
            fightHud.Bind(playerHealth, henryHealth);
            fightHud.Show();

            playerDeathObserved = false;
            henryDeathObserved = false;
            outcomePending = false;
            encounterActive = true;
            nextAttack = HenryCombatAttack.Punch;
            nextDestinationRefreshAt = 0f;
            state = FightState.Pursuing;
            StartHenryPursuit(true);

            Debug.Log(
                "[HenryFight] Fight started at Nam and Henry's current " +
                "world positions.",
                this);
        }

        private bool HasRequiredReferences()
        {
            return playerMotor != null && inputReader != null &&
                   playerCombat != null &&
                   playerHealth != null && henryRoot != null &&
                   henryChase != null && henryAnimation != null &&
                   henryCombat != null && henryHealth != null;
        }

        private void UpdatePursuit()
        {
            if (CanHenryAttackPlayer())
            {
                StopHenryAgent();
                FacePlayer(Time.deltaTime);
                if (henryCombat.TryPlayAttack(nextAttack))
                {
                    nextAttack = GetNextAttack(nextAttack);
                    state = FightState.Attacking;
                }
                else
                {
                    recoveryEndsAt = Time.time + HenryAttackRecovery;
                    state = FightState.Recovery;
                }

                return;
            }

            StartHenryPursuit(false);
        }

        private static HenryCombatAttack GetNextAttack(
            HenryCombatAttack completedAttack)
        {
            switch (completedAttack)
            {
                case HenryCombatAttack.Punch:
                    return HenryCombatAttack.MmaKick;
                case HenryCombatAttack.MmaKick:
                    return HenryCombatAttack.RoundhouseKick;
                default:
                    return HenryCombatAttack.Punch;
            }
        }

        private bool CanHenryAttackPlayer()
        {
            Vector3 toPlayer = GetPlanarDirectionToPlayer();
            if (toPlayer.sqrMagnitude >
                HenryAttackRange * HenryAttackRange)
            {
                return false;
            }

            NavMeshAgent agent = henryChase != null
                ? henryChase.Agent
                : null;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh ||
                agent.pathPending)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    playerMotor.transform.position,
                    out NavMeshHit playerHit,
                    1.5f,
                    agent.areaMask))
            {
                return false;
            }

            // A clear NavMesh ray proves that Henry and Nam are on the same
            // directly reachable surface. This prevents a short world-space
            // distance across a wall from starting an attack through it.
            if (NavMesh.Raycast(
                    agent.nextPosition,
                    playerHit.position,
                    out _,
                    agent.areaMask))
            {
                return false;
            }

            if (agent.hasPath &&
                !float.IsInfinity(agent.remainingDistance) &&
                agent.remainingDistance >
                HenryAttackRange + AttackPathLengthTolerance)
            {
                return false;
            }

            return true;
        }

        private void UpdateAttack()
        {
            StopHenryAgent();
            FacePlayer(Time.deltaTime);
            if (henryCombat.IsAttacking)
            {
                return;
            }

            recoveryEndsAt = Time.time + HenryAttackRecovery;
            state = FightState.Recovery;
        }

        private void UpdateRecovery()
        {
            StopHenryAgent();
            FacePlayer(Time.deltaTime);
            if (Time.time < recoveryEndsAt)
            {
                return;
            }

            state = FightState.Pursuing;
            nextDestinationRefreshAt = 0f;
            StartHenryPursuit(true);
        }

        private void StartHenryPursuit(bool forceDestination)
        {
            NavMeshAgent agent = henryChase != null
                ? henryChase.Agent
                : null;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (forceDestination || Time.time >= nextDestinationRefreshAt)
            {
                nextDestinationRefreshAt =
                    Time.time + DestinationRefreshInterval;
                if (NavMesh.SamplePosition(
                        playerMotor.transform.position,
                        out NavMeshHit playerHit,
                        1.5f,
                        NavMesh.AllAreas))
                {
                    agent.SetDestination(playerHit.position);
                }
            }

            agent.speed = HenryFightSpeed;
            agent.stoppingDistance = HenryStoppingDistance;
            agent.isStopped = false;
            if (!henryAnimation.IsRunPlaying)
            {
                henryAnimation.PlayRun();
            }
        }

        private Vector3 GetPlanarDirectionToPlayer()
        {
            Vector3 direction =
                playerMotor.transform.position - henryRoot.position;
            direction.y = 0f;
            return direction;
        }

        private void FacePlayer(float deltaTime)
        {
            Vector3 direction = GetPlanarDirectionToPlayer();
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
            henryRoot.rotation = Quaternion.RotateTowards(
                henryRoot.rotation,
                target,
                RotationSpeed * Mathf.Max(0f, deltaTime));
        }

        private void StopHenryAgent()
        {
            NavMeshAgent agent = henryChase != null
                ? henryChase.Agent
                : null;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void SubscribeHealthEvents()
        {
            UnsubscribeHealthEvents();
            if (playerHealth != null)
            {
                playerHealth.Died += HandlePlayerDied;
            }

            if (henryHealth != null)
            {
                henryHealth.Died += HandleHenryDied;
            }
        }

        private void UnsubscribeHealthEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }

            if (henryHealth != null)
            {
                henryHealth.Died -= HandleHenryDied;
            }
        }

        private void HandlePlayerDied()
        {
            playerDeathObserved = true;
            ScheduleOutcomeResolution();
        }

        private void HandleHenryDied()
        {
            henryDeathObserved = true;
            ScheduleOutcomeResolution();
        }

        private void ScheduleOutcomeResolution()
        {
            if (!encounterActive && !outcomePending)
            {
                return;
            }

            if (!outcomePending)
            {
                outcomePending = true;
                encounterActive = false;
                StopHenryForOutcome();
                // Both possible outcomes lead into the same police arrival.
                // Quiesce Nam and start the car before yielding one frame to
                // coalesce simultaneous death callbacks.
                playerCombat?.EndAttack();
                EnterPoliceArrestInputMode(false);
                BeginPoliceArrest();
            }

            if (outcomeRoutine == null)
            {
                outcomeRoutine = StartCoroutine(ResolveOutcomeNextFrame());
            }
        }

        private IEnumerator ResolveOutcomeNextFrame()
        {
            // Coalesce both health death callbacks. If both reach zero in the
            // same frame, Nam's defeat is deliberately resolved first.
            yield return null;

            bool playerLost = playerDeathObserved ||
                              (playerHealth != null && playerHealth.IsDead);
            bool henryLost = henryDeathObserved ||
                             (henryHealth != null && henryHealth.IsDead);
            if (playerLost)
            {
                yield return FinishPlayerDefeat();
            }
            else if (henryLost)
            {
                yield return FinishHenryDefeat();
            }

            outcomeRoutine = null;
        }

        private void StopHenryForOutcome()
        {
            StopHenryAgent();
            henryCombat?.CancelAttack(false);
            // The final outcome animation decides the next pose. Returning to
            // Idle here causes a visible one-frame pop before Defeated.
            henryCombat?.ExitCombatMode(false);
        }

        private IEnumerator FinishPlayerDefeat()
        {
            state = FightState.PlayerDefeated;
            henryChase?.EndFightControl(true);

            bool stunned = playerCombat != null &&
                           playerCombat.EnterForcedStun();
            if (stunned)
            {
                // ForcedStun owns Nam's pose and movement lock. Police arrest
                // mode owns the action map, so releasing only this lock keeps
                // the gameplay camera movable while the car approaches.
                playerInputLock?.Unlock(
                    PlayerCombatController.ForcedStunInputLockReason);
            }

            float elapsed = 0f;
            while (elapsed < StunnedBeforeGameOverDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            HideFightPresentation();
            while (!policeArrivalResolved)
            {
                yield return null;
            }

            UnsubscribeHealthEvents();
            outcomePending = false;
            Chapter1EventBus.RaiseGameOver(
                GameOverReason,
                GameOverRestartPolicy.ReloadScene);
        }

        private IEnumerator FinishHenryDefeat()
        {
            state = FightState.HenryDefeated;

            // Release the chase controller without asking it to play Idle.
            // The defeated clip owns Henry's pose from this point onward.
            henryChase?.EndFightControl(false);

            if (!Mission3Progress.TryMarkHenryDefeated())
            {
                Debug.LogError(
                    "[HenryFight] Henry was defeated, but the result could " +
                    "not be stored in Mission 3 progress.",
                    this);
            }

            state = FightState.PoliceArriving;

            bool defeatedAnimationStarted =
                henryAnimation != null && henryAnimation.PlayDefeated();
            if (defeatedAnimationStarted)
            {
                float duration = Mathf.Max(
                    0f,
                    henryAnimation.DefeatedDuration);
                if (duration > 0f)
                {
                    yield return new WaitForSeconds(duration);
                }

                henryAnimation.HoldDefeatedFinalPose();
            }
            else
            {
                Debug.LogError(
                    "[HenryFight] Henry's Defeated animation could not " +
                    "be played.",
                    this);
            }

            HideFightPresentation();
            UnsubscribeHealthEvents();
            Chapter1EventBus.RaiseNotification(
                HenryDefeatedNotification);

            while (!policeArrivalResolved)
            {
                yield return null;
            }

            CompletePoliceArrestAfterVictory();
            Debug.Log("[HenryFight] Henry was defeated.", this);
        }

        private void BeginPoliceArrest()
        {
            policeArrivalResolved = false;
            policeArrivalSucceeded = false;
            policeArrest = PoliceArrestSequenceController.GetOrInstall(
                gameObject.scene);
            if (policeArrest == null)
            {
                Debug.LogError(
                    "[HenryFight] Police arrest controller could not be " +
                    "installed. Resolving the outcome without the car.",
                    this);
                policeArrivalResolved = true;
                return;
            }

            bool accepted = policeArrest.BeginArrest(
                playerMotor != null ? playerMotor.transform : null,
                HandlePoliceArrestCompleted);
            if (!accepted && !policeArrivalResolved)
            {
                policeArrivalResolved = true;
            }
        }

        private void HandlePoliceArrestCompleted(bool arrived)
        {
            policeArrivalSucceeded = arrived;
            policeArrivalResolved = true;
        }

        private void CompletePoliceArrestAfterVictory()
        {
            outcomePending = false;
            if (!policeArrivalSucceeded)
            {
                // Missing/destroyed police_car is a recoverable content
                // failure: keep Nam locked and leave the save pending so a
                // later reload can retry, but never claim he was arrested.
                state = FightState.PoliceArriving;
                Debug.LogError(
                    "[HenryFight] Police arrest ended without police_car " +
                    "reaching Nam. The pending save state will retry after " +
                    "reload.",
                    this);
                return;
            }

            state = FightState.PoliceArrested;
            if (!Mission3Progress.TryMarkPoliceArrestCompleted())
            {
                Debug.LogError(
                    "[HenryFight] Police arrived, but the arrest result " +
                    "could not be stored in Mission 3 progress.",
                    this);
            }

            Chapter1EventBus.RaiseNotification(
                PoliceArrestedNotification);
        }

        private void EnterCombatInputMode()
        {
            if (!interactionStateCaptured)
            {
                interactionWasEnabled =
                    interactionController != null &&
                    interactionController.enabled;
                interactionStateCaptured = true;
            }

            PhoneUIController phone =
                backpackPhoneController != null
                    ? backpackPhoneController.PhoneUIController
                    : null;
            InventoryUIController inventory =
                backpackPhoneController != null
                    ? backpackPhoneController.InventoryUIController
                    : null;
            if (phone != null && phone.IsOpen)
            {
                phone.ClosePhone();
            }

            if (inventory != null && inventory.IsOpen)
            {
                inventory.CloseInventory();
            }

            if (interactionController != null)
            {
                interactionController.SetCombatDoorOnlyMode(true);
                interactionController.enabled = true;
            }

            inputReader?.SetPoliceArrestMode(false);
            inputReader?.SetCombatOnlyMode(true);
        }

        private void ExitCombatInputMode()
        {
            inputReader?.SetPoliceArrestMode(false);
            inputReader?.SetCombatOnlyMode(false);
            if (interactionStateCaptured && interactionController != null)
            {
                if (interactionWasEnabled)
                {
                    interactionController.SetCombatDoorOnlyMode(false);
                    interactionController.enabled = true;
                }
                else
                {
                    interactionController.enabled = false;
                    interactionController.SetCombatDoorOnlyMode(false);
                }
            }

            interactionStateCaptured = false;
        }

        private void EnterPoliceArrestInputMode(bool cancelPlayerAttack)
        {
            if (cancelPlayerAttack)
            {
                playerCombat?.EndAttack();
            }

            PhoneUIController phone =
                backpackPhoneController != null
                    ? backpackPhoneController.PhoneUIController
                    : null;
            InventoryUIController inventory =
                backpackPhoneController != null
                    ? backpackPhoneController.InventoryUIController
                    : null;
            if (phone != null && phone.IsOpen)
            {
                phone.ClosePhone();
            }

            if (inventory != null && inventory.IsOpen)
            {
                inventory.CloseInventory();
            }

            playerMotor?.SetMovementEnabled(false);
            inputReader?.SetPoliceArrestMode(true);
            inputReader?.SetCombatOnlyMode(false);
            if (interactionController != null)
            {
                // OnDisable clears both cached targets and the visible prompt.
                interactionController.enabled = false;
            }
        }

        private void HideFightPresentation()
        {
            fightHud?.Hide();
            fightHud?.Unbind();
        }

        private void ApplyPersistentDefeatState()
        {
            encounterActive = false;
            state = FightState.HenryDefeated;

            FightCombatHUD[] huds =
                FindObjectsByType<FightCombatHUD>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < huds.Length; i++)
            {
                if (huds[i] != null &&
                    huds[i].gameObject.scene == gameObject.scene)
                {
                    huds[i].Hide();
                    huds[i].Unbind();
                }
            }

            ResolveReferences();
            henryCombat?.ExitCombatMode(false);
            if (henryChase != null && henryChase.IsUnderFightControl)
            {
                henryChase.EndFightControl(false);
            }

            // A completed fight restores Henry directly in the last frame of
            // the defeated clip instead of returning him to Idle on reload.
            henryAnimation?.HoldDefeatedFinalPose();
            EnterPoliceArrestInputMode(true);

            policeArrest = PoliceArrestSequenceController.GetOrInstall(
                gameObject.scene);
            if (Mission3Progress.PoliceArrestCompleted)
            {
                state = FightState.PoliceArrested;
                outcomePending = false;
                if (policeArrest == null ||
                    !policeArrest.RestoreTerminalArrestState(
                        playerMotor != null
                            ? playerMotor.transform
                            : null))
                {
                    Debug.LogError(
                        "[HenryFight] The completed police arrest state " +
                        "could not restore police_car.",
                        this);
                }

                return;
            }

            state = FightState.PoliceArriving;
            outcomePending = true;
            BeginPoliceArrest();
            if (!policeRecoveryActive)
            {
                policeRecoveryActive = true;
                StartCoroutine(CompleteRecoveredPoliceArrest());
            }
        }

        private IEnumerator CompleteRecoveredPoliceArrest()
        {
            while (!policeArrivalResolved)
            {
                yield return null;
            }

            CompletePoliceArrestAfterVictory();
            policeRecoveryActive = false;
        }

        private void AbortFightStart(string reason)
        {
            Debug.LogError($"[HenryFight] Fight start aborted: {reason}", this);
            henryCombat?.ExitCombatMode();
            henryChase?.EndFightControl(true);
            HideFightPresentation();
            ExitCombatInputMode();
            encounterActive = false;
            outcomePending = false;
            state = FightState.Inactive;
        }

        private static Chapter1PlayerMotor FindScenePlayer(Scene scene)
        {
            Chapter1PlayerMotor[] players =
                FindObjectsByType<Chapter1PlayerMotor>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null &&
                    players[i].gameObject.scene == scene)
                {
                    return players[i];
                }
            }

            return null;
        }

        private static Transform FindSceneTransform(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i]
                    .GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0;
                     childIndex < transforms.Length;
                     childIndex++)
                {
                    if (string.Equals(
                            transforms[childIndex].name,
                            objectName,
                            StringComparison.Ordinal))
                    {
                        return transforms[childIndex];
                    }
                }
            }

            return null;
        }
    }
}
