using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Owns the officer phase after police_car reaches Nam: place the authored
    /// Police beside the car, pursue Nam on the NavMesh, switch to the exact
    /// Police_Camera on capture, and finish the two-line arrest dialogue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoliceOfficerArrestController : MonoBehaviour
    {
        public enum PursuitState
        {
            Idle,
            Pursuing,
            Dialogue,
            Completed,
            Failed
        }

        public const string PoliceObjectName = "Police";
        public const string PoliceCameraName = "Police_Camera";
        public const float PoliceRunSpeed = 7.25f;
        public const float CaptureDistance = 1.2f;

        private const string PoliceControllerResource =
            "Police/Police_Auto";
        private const float DestinationRefreshInterval = 0.1f;
        private const float SpawnSampleRadius = 4f;
        private const float TargetSampleRadius = 2.5f;
        private const float TerminalStandDistance = 1.15f;
        private const float AnimatorBlendDuration = 0.12f;
        private const int DialogueCanvasOrder = 32750;
        private const string FirstDialogueLine =
            "Nam, c\u1eadu \u0111\u00e3 b\u1ecb b\u1eaft v\u00ec t\u1ed9i " +
            "\u0111\u1ed1t qu\u00e1n \u0103n v\u00e0 \u0111\u00e1nh ng\u01b0\u1eddi.";
        private const string SecondDialogueLine =
            "M\u1eddi c\u1eadu theo t\u00f4i v\u1ec1 \u0111\u1ed3n.";

        private static readonly int RunStateHash =
            Animator.StringToHash("Base Layer.Run");
        private static readonly int IdleStateHash =
            Animator.StringToHash("Base Layer.Idle");
        private static readonly Vector3 DefaultAuthoredCarOffset =
            new Vector3(0.8f, 0f, 0.84f);

        [SerializeField, Min(0.1f)] private float runSpeed =
            PoliceRunSpeed;
        [SerializeField, Min(0.2f)] private float captureDistance =
            CaptureDistance;
        [SerializeField, Range(10f, 80f)] private float charactersPerSecond =
            34f;
        [SerializeField, Range(1f, 6f)]
        private float punctuationPauseMultiplier = 2.5f;

        private readonly List<Action<bool>> completionCallbacks =
            new List<Action<bool>>();

        private Scene sequenceScene;
        private Transform policeRoot;
        private Transform target;
        private Transform currentPoliceCar;
        private Animator policeAnimator;
        private NavMeshAgent policeAgent;
        private Camera policeCamera;
        private AudioListener policeListener;
        private Camera gameplayCamera;
        private AudioListener gameplayListener;
        private Chapter1PlayerMotor playerMotor;
        private Chapter1InputReader inputReader;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackPhoneController;
        private PlayerCombatController playerCombat;

        private Vector3 authoredPolicePosition;
        private Vector3 authoredCarPosition;
        private Vector3 authoredCarOffset = DefaultAuthoredCarOffset;
        private float nextDestinationRefreshAt;
        private bool authoredPositionsCaptured;
        private bool cameraSwitched;
        private bool gameplayCameraWasEnabled;
        private bool gameplayListenerWasEnabled;
        private bool playerMovementWasEnabled;
        private bool combatOnlyModeWasActive;
        private bool policeArrestModeWasActive;
        private bool interactionWasEnabled;
        private bool interactionDoorOnlyModeWasActive;
        private bool interactionStateCaptured;
        private Coroutine dialogueRoutine;
        private PursuitState state;

        private Canvas dialogueCanvas;
        private TMP_Text speakerText;
        private TMP_Text lineText;
        private TMP_Text advanceHintText;

        public event Action<bool> PursuitCompleted;

        public PursuitState State => state;
        public bool IsPursuing => state == PursuitState.Pursuing;
        public Transform PoliceRoot => policeRoot;
        public Camera PoliceCamera => policeCamera;

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
            InstallForSceneWhenPoliceExists(
                SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForSceneWhenPoliceExists(scene);
        }

        public static PoliceOfficerArrestController GetOrInstall(
            Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            PoliceOfficerArrestController[] existing =
                FindObjectsByType<PoliceOfficerArrestController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                PoliceOfficerArrestController candidate = existing[i];
                if (candidate != null &&
                    candidate.gameObject.scene == scene)
                {
                    candidate.Configure(scene);
                    return candidate;
                }
            }

            GameObject directorObject =
                new GameObject(nameof(PoliceOfficerArrestController));
            SceneManager.MoveGameObjectToScene(directorObject, scene);
            PoliceOfficerArrestController controller =
                directorObject.AddComponent<
                    PoliceOfficerArrestController>();
            controller.Configure(scene);
            return controller;
        }

        public bool BeginPursuit(
            Transform namTarget,
            Transform arrivedPoliceCar,
            Action<bool> onCompleted = null)
        {
            if (state == PursuitState.Completed)
            {
                InvokeCallbackSafely(onCompleted, true);
                return true;
            }

            if (state == PursuitState.Failed)
            {
                InvokeCallbackSafely(onCompleted, false);
                return false;
            }

            if (onCompleted != null)
            {
                completionCallbacks.Add(onCompleted);
            }

            if (state == PursuitState.Pursuing ||
                state == PursuitState.Dialogue)
            {
                return true;
            }

            target = namTarget;
            currentPoliceCar = arrivedPoliceCar;
            if (target == null || currentPoliceCar == null)
            {
                Debug.LogError(
                    "[PoliceOfficer] Cannot pursue: Nam or police_car is " +
                    "missing.",
                    this);
                CompleteSequence(false);
                return false;
            }

            if (!TryResolveAuthoredPolice() ||
                policeCamera == null ||
                policeAnimator == null)
            {
                Debug.LogError(
                    "[PoliceOfficer] Cannot pursue: exact root 'Police', " +
                    "its Animator, or exact child 'Police_Camera' is missing.",
                    this);
                CompleteSequence(false);
                return false;
            }

            ResolvePlayerControllers();
            DisablePoliceCamera();

            if (!HenryChaseNavigation.EnsureBuilt())
            {
                Debug.LogError(
                    "[PoliceOfficer] Runtime NavMesh could not be built " +
                    "before Police spawned.",
                    this);
                CompleteSequence(false);
                return false;
            }

            Vector3 desiredSpawn =
                currentPoliceCar.position + authoredCarOffset;
            if (!NavMesh.SamplePosition(
                    desiredSpawn,
                    out NavMeshHit spawnHit,
                    SpawnSampleRadius,
                    NavMesh.AllAreas))
            {
                Debug.LogError(
                    "[PoliceOfficer] No NavMesh point was found beside the " +
                    "arrived police car.",
                    currentPoliceCar);
                CompleteSequence(false);
                return false;
            }

            policeRoot.gameObject.SetActive(false);
            policeRoot.SetPositionAndRotation(
                spawnHit.position,
                GetFacingRotation(spawnHit.position));
            policeRoot.gameObject.SetActive(true);
            ConfigureAnimator();

            if (!TryConfigureAgent() ||
                !TryPlaceAgentOnNavMesh(spawnHit.position))
            {
                Debug.LogError(
                    "[PoliceOfficer] Police could not be placed on the " +
                    "NavMesh beside police_car.",
                    policeRoot);
                policeRoot.gameObject.SetActive(false);
                CompleteSequence(false);
                return false;
            }

            PlayAnimation(RunStateHash);
            nextDestinationRefreshAt = 0f;
            state = PursuitState.Pursuing;
            RefreshDestination(true);
            return true;
        }

        public bool RestoreTerminalState(
            Transform namTarget,
            Transform arrivedPoliceCar)
        {
            target = namTarget;
            currentPoliceCar = arrivedPoliceCar;
            if (target == null || currentPoliceCar == null ||
                !TryResolveAuthoredPolice() ||
                policeCamera == null)
            {
                Debug.LogError(
                    "[PoliceOfficer] Cannot restore the terminal arrest " +
                    "state because required scene objects are missing.",
                    this);
                return false;
            }

            ResolvePlayerControllers();
            DisablePoliceCamera();

            if (!HenryChaseNavigation.EnsureBuilt())
            {
                Debug.LogError(
                    "[PoliceOfficer] Runtime NavMesh could not be built " +
                    "while restoring the completed arrest.",
                    this);
                return false;
            }

            Vector3 fromNamToCar = Vector3.ProjectOnPlane(
                currentPoliceCar.position - target.position,
                Vector3.up);
            if (fromNamToCar.sqrMagnitude <= 0.001f)
            {
                fromNamToCar = -Vector3.ProjectOnPlane(
                    target.forward,
                    Vector3.up);
            }

            if (fromNamToCar.sqrMagnitude <= 0.001f)
            {
                fromNamToCar = Vector3.back;
            }

            Vector3 desiredPosition = target.position +
                                      fromNamToCar.normalized *
                                      TerminalStandDistance;
            if (!NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit hit,
                    SpawnSampleRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            policeRoot.gameObject.SetActive(false);
            policeRoot.SetPositionAndRotation(
                hit.position,
                GetFacingRotation(hit.position));
            policeRoot.gameObject.SetActive(true);
            ConfigureAnimator();
            if (!TryConfigureAgent() ||
                !TryPlaceAgentOnNavMesh(hit.position))
            {
                Debug.LogError(
                    "[PoliceOfficer] Police could not restore its " +
                    "NavMeshAgent at the completed arrest position.",
                    policeRoot);
                policeRoot.gameObject.SetActive(false);
                return false;
            }

            StopAgent();
            PlayAnimation(IdleStateHash);
            FaceTargetImmediately();
            LockPlayerForArrest();
            if (!SwitchToPoliceCamera())
            {
                RestorePlayerStateAfterFailure();
                return false;
            }

            SetDialogueVisible(false);
            state = PursuitState.Completed;
            completionCallbacks.Clear();
            return true;
        }

        private void Update()
        {
            if (state != PursuitState.Pursuing)
            {
                return;
            }

            if (target == null || policeRoot == null ||
                policeAgent == null || !policeAgent.enabled ||
                !policeAgent.isOnNavMesh)
            {
                Debug.LogError(
                    "[PoliceOfficer] Pursuit lost its target or NavMeshAgent.",
                    this);
                CompleteSequence(false);
                return;
            }

            if (CanCaptureTarget())
            {
                dialogueRoutine = StartCoroutine(PlayArrestDialogue());
                return;
            }

            RefreshDestination(false);
            if (!IsAnimationPlaying(RunStateHash))
            {
                PlayAnimation(RunStateHash);
            }
        }

        private void RefreshDestination(bool force)
        {
            if (!force && Time.time < nextDestinationRefreshAt)
            {
                return;
            }

            nextDestinationRefreshAt =
                Time.time + DestinationRefreshInterval;
            if (!NavMesh.SamplePosition(
                    target.position,
                    out NavMeshHit targetHit,
                    TargetSampleRadius,
                    policeAgent.areaMask))
            {
                return;
            }

            policeAgent.speed = runSpeed;
            policeAgent.isStopped = false;
            policeAgent.SetDestination(targetHit.position);
        }

        private bool CanCaptureTarget()
        {
            Vector3 planarDelta = Vector3.ProjectOnPlane(
                target.position - policeRoot.position,
                Vector3.up);
            if (planarDelta.sqrMagnitude >
                captureDistance * captureDistance)
            {
                return false;
            }

            if (policeAgent.pathPending)
            {
                return false;
            }

            if (policeAgent.hasPath &&
                !float.IsInfinity(policeAgent.remainingDistance) &&
                policeAgent.remainingDistance >
                captureDistance + 0.45f)
            {
                return false;
            }

            if (NavMesh.SamplePosition(
                    target.position,
                    out NavMeshHit targetHit,
                    TargetSampleRadius,
                    policeAgent.areaMask) &&
                NavMesh.Raycast(
                    policeAgent.nextPosition,
                    targetHit.position,
                    out _,
                    policeAgent.areaMask))
            {
                return false;
            }

            return true;
        }

        private IEnumerator PlayArrestDialogue()
        {
            state = PursuitState.Dialogue;
            StopAgent();
            FaceTargetImmediately();
            PlayAnimation(IdleStateHash);
            LockPlayerForArrest();

            if (!SwitchToPoliceCamera())
            {
                CompleteSequence(false);
                dialogueRoutine = null;
                yield break;
            }

            EnsureDialogueUi();
            SetDialogueVisible(true);
            yield return WaitForAdvanceRelease();
            yield return StreamLine(FirstDialogueLine);
            yield return StreamLine(SecondDialogueLine);

            SetDialogueVisible(false);
            dialogueRoutine = null;
            CompleteSequence(true);
        }

        private IEnumerator StreamLine(string line)
        {
            string safeLine = line ?? string.Empty;
            if (speakerText != null)
            {
                speakerText.text = "C\u1ea3nh s\u00e1t";
                speakerText.color = new Color(1f, 0.58f, 0.32f);
            }

            if (lineText == null)
            {
                yield return WaitForNextLine();
                yield break;
            }

            lineText.text = safeLine;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();

            int characterCount = lineText.textInfo.characterCount;
            bool skippedTyping = false;
            for (int i = 0; i < characterCount; i++)
            {
                if (IsAdvancePressed())
                {
                    skippedTyping = true;
                    break;
                }

                lineText.maxVisibleCharacters = i + 1;
                char visibleCharacter =
                    lineText.textInfo.characterInfo[i].character;
                float elapsed = 0f;
                float delay = GetCharacterDelay(visibleCharacter);
                while (elapsed < delay)
                {
                    if (IsAdvancePressed())
                    {
                        skippedTyping = true;
                        break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (skippedTyping)
                {
                    break;
                }
            }

            lineText.maxVisibleCharacters = int.MaxValue;
            if (skippedTyping)
            {
                yield return WaitForAdvanceRelease();
            }

            yield return WaitForNextLine();
        }

        private float GetCharacterDelay(char character)
        {
            float baseDelay =
                1f / Mathf.Max(1f, charactersPerSecond);
            if (character == '.' || character == '!' || character == '?')
            {
                return baseDelay * punctuationPauseMultiplier;
            }

            if (character == ',' || character == ';' || character == ':')
            {
                return baseDelay *
                       Mathf.Lerp(
                           1f,
                           punctuationPauseMultiplier,
                           0.5f);
            }

            return baseDelay;
        }

        private static IEnumerator WaitForNextLine()
        {
            while (!IsAdvancePressed())
            {
                yield return null;
            }

            yield return WaitForAdvanceRelease();
        }

        private static IEnumerator WaitForAdvanceRelease()
        {
            while (IsAdvanceHeld())
            {
                yield return null;
            }

            yield return null;
        }

        private static bool IsAdvancePressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.eKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame);
        }

        private static bool IsAdvanceHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.eKey.isPressed ||
                    keyboard.spaceKey.isPressed ||
                    keyboard.enterKey.isPressed);
        }

        private void Configure(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            sequenceScene = scene;
            TryResolveAuthoredPolice();
            if (state != PursuitState.Dialogue &&
                state != PursuitState.Completed)
            {
                DisablePoliceCamera();
            }
        }

        private bool TryResolveAuthoredPolice()
        {
            if (!sequenceScene.IsValid() || !sequenceScene.isLoaded)
            {
                sequenceScene = gameObject.scene;
            }

            // UnityEngine.Object has a special destroyed-object null state.
            // Do not use ??/??= for cached scene objects because those
            // operators bypass Unity's overloaded equality check.
            if (policeRoot == null)
            {
                policeRoot = FindExactRoot(
                    sequenceScene,
                    PoliceObjectName);
            }
            Transform authoredCar = FindExactRoot(
                sequenceScene,
                PoliceArrestSequenceController.PoliceCarObjectName);
            if (policeRoot == null)
            {
                return false;
            }

            if (!authoredPositionsCaptured)
            {
                authoredPolicePosition = policeRoot.position;
                if (authoredCar != null)
                {
                    authoredCarPosition = authoredCar.position;
                    authoredCarOffset =
                        authoredPolicePosition - authoredCarPosition;
                }

                authoredPositionsCaptured = true;
            }

            if (policeAnimator == null)
            {
                policeAnimator = policeRoot.GetComponent<Animator>();
                if (policeAnimator == null)
                {
                    policeAnimator =
                        policeRoot.GetComponentInChildren<Animator>(true);
                }
            }

            ResolvePoliceCamera();
            return true;
        }

        private void ResolvePoliceCamera()
        {
            if (policeCamera != null || policeRoot == null)
            {
                return;
            }

            Camera[] cameras =
                policeRoot.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null &&
                    string.Equals(
                        cameras[i].name,
                        PoliceCameraName,
                        StringComparison.Ordinal))
                {
                    policeCamera = cameras[i];
                    policeListener =
                        policeCamera.GetComponent<AudioListener>();
                    return;
                }
            }
        }

        private void ConfigureAnimator()
        {
            if (policeAnimator == null)
            {
                return;
            }

            if (policeAnimator.runtimeAnimatorController == null)
            {
                policeAnimator.runtimeAnimatorController =
                    Resources.Load<RuntimeAnimatorController>(
                        PoliceControllerResource);
            }

            policeAnimator.enabled = true;
            policeAnimator.applyRootMotion = false;
            policeAnimator.cullingMode =
                AnimatorCullingMode.AlwaysAnimate;
            policeAnimator.Rebind();
            policeAnimator.Update(0f);
        }

        private bool TryConfigureAgent()
        {
            if (policeRoot == null)
            {
                Debug.LogError(
                    "[PoliceOfficer] Cannot configure NavMeshAgent because " +
                    "the exact Police root is missing.",
                    this);
                policeAgent = null;
                return false;
            }

            try
            {
                // Always reacquire from the live GameObject. A cached Unity
                // component may be a destroyed (fake-null) wrapper while
                // still being non-null to C#'s ?? operator, which previously
                // caused a MissingComponentException on the first property
                // assignment.
                policeAgent = policeRoot.GetComponent<NavMeshAgent>();
                if (policeAgent == null)
                {
                    policeAgent =
                        policeRoot.gameObject.AddComponent<NavMeshAgent>();
                }

                if (policeAgent == null)
                {
                    Debug.LogError(
                        "[PoliceOfficer] Failed to add NavMeshAgent to " +
                        "Police.",
                        policeRoot);
                    return false;
                }

                // Configure while detached from the NavMesh, then enable once
                // all dimensions match the runtime surface's default agent.
                policeAgent.enabled = false;
                policeAgent.agentTypeID = 0;
                policeAgent.radius = 0.35f;
                policeAgent.height = 1.9f;
                policeAgent.baseOffset = 0f;
                policeAgent.speed = runSpeed;
                policeAgent.acceleration = 28f;
                policeAgent.angularSpeed = 720f;
                policeAgent.stoppingDistance =
                    Mathf.Max(0.65f, captureDistance - 0.2f);
                policeAgent.autoBraking = true;
                policeAgent.autoRepath = true;
                policeAgent.updatePosition = true;
                policeAgent.updateRotation = true;
                policeAgent.obstacleAvoidanceType =
                    ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                policeAgent.avoidancePriority = 20;
                policeAgent.enabled = true;
                return true;
            }
            catch (UnityException exception)
            {
                Debug.LogError(
                    "[PoliceOfficer] NavMeshAgent setup failed safely: " +
                    exception.Message,
                    policeRoot);
                policeAgent = null;
                return false;
            }
        }

        private bool TryPlaceAgentOnNavMesh(Vector3 sampledPosition)
        {
            if (policeAgent == null || !policeAgent.enabled)
            {
                return false;
            }

            if (policeAgent.isOnNavMesh)
            {
                return true;
            }

            return policeAgent.Warp(sampledPosition) &&
                   policeAgent.isOnNavMesh;
        }

        private void StopAgent()
        {
            if (policeAgent == null ||
                !policeAgent.enabled ||
                !policeAgent.isOnNavMesh)
            {
                return;
            }

            policeAgent.isStopped = true;
            policeAgent.ResetPath();
        }

        private void PlayAnimation(int stateHash)
        {
            if (policeAnimator == null ||
                policeAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            if (!policeAnimator.HasState(0, stateHash))
            {
                Debug.LogError(
                    "[PoliceOfficer] Police_Auto.controller is missing " +
                    (stateHash == RunStateHash ? "Run." : "Idle."),
                    policeAnimator);
                return;
            }

            policeAnimator.CrossFadeInFixedTime(
                stateHash,
                AnimatorBlendDuration,
                0);
        }

        private bool IsAnimationPlaying(int stateHash)
        {
            if (policeAnimator == null)
            {
                return false;
            }

            AnimatorStateInfo current =
                policeAnimator.GetCurrentAnimatorStateInfo(0);
            if (current.fullPathHash == stateHash)
            {
                return true;
            }

            // Do not restart the same CrossFade every Update while the
            // transition is still in progress. Reissuing it continuously can
            // keep Police visually stuck in Idle even though Run was asked
            // for correctly.
            return policeAnimator.IsInTransition(0) &&
                   policeAnimator.GetNextAnimatorStateInfo(0)
                       .fullPathHash == stateHash;
        }

        private Quaternion GetFacingRotation(Vector3 fromPosition)
        {
            if (target == null)
            {
                return policeRoot != null
                    ? policeRoot.rotation
                    : Quaternion.identity;
            }

            Vector3 direction = Vector3.ProjectOnPlane(
                target.position - fromPosition,
                Vector3.up);
            return direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : policeRoot.rotation;
        }

        private void FaceTargetImmediately()
        {
            if (policeRoot != null && target != null)
            {
                policeRoot.rotation =
                    GetFacingRotation(policeRoot.position);
            }
        }

        private void ResolvePlayerControllers()
        {
            if (target == null)
            {
                return;
            }

            if (playerMotor == null)
            {
                playerMotor = target.GetComponent<Chapter1PlayerMotor>();
            }

            if (inputReader == null)
            {
                inputReader = target.GetComponent<Chapter1InputReader>();
            }

            if (interactionController == null)
            {
                interactionController =
                    target.GetComponent<Chapter1InteractionController>();
            }

            if (backpackPhoneController == null)
            {
                backpackPhoneController =
                    target.GetComponent<BackpackPhoneInputController>();
            }

            if (playerCombat == null)
            {
                playerCombat = target.GetComponent<PlayerCombatController>();
            }
        }

        private void LockPlayerForArrest()
        {
            ResolvePlayerControllers();
            playerCombat?.EndAttack();

            if (!interactionStateCaptured)
            {
                playerMovementWasEnabled =
                    playerMotor == null || playerMotor.MovementEnabled;
                combatOnlyModeWasActive =
                    inputReader != null && inputReader.CombatOnlyMode;
                policeArrestModeWasActive =
                    inputReader != null && inputReader.PoliceArrestMode;
                interactionWasEnabled =
                    interactionController != null &&
                    interactionController.enabled;
                interactionDoorOnlyModeWasActive =
                    interactionController != null &&
                    interactionController.CombatDoorOnlyMode;
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

            playerMotor?.SetMovementEnabled(false);
            inputReader?.SetCombatOnlyMode(false);
            inputReader?.SetPoliceArrestMode(true);
            if (interactionController != null)
            {
                interactionController.enabled = false;
            }
        }

        private void RestorePlayerStateAfterFailure()
        {
            if (!interactionStateCaptured)
            {
                return;
            }

            playerMotor?.SetMovementEnabled(playerMovementWasEnabled);
            if (inputReader != null)
            {
                inputReader.SetPoliceArrestMode(false);
                inputReader.SetCombatOnlyMode(combatOnlyModeWasActive);
                inputReader.SetPoliceArrestMode(
                    policeArrestModeWasActive);
            }

            if (interactionController != null)
            {
                interactionController.SetCombatDoorOnlyMode(
                    interactionDoorOnlyModeWasActive);
                interactionController.enabled = interactionWasEnabled;
            }

            interactionStateCaptured = false;
        }

        private bool SwitchToPoliceCamera()
        {
            if (cameraSwitched)
            {
                return true;
            }

            ResolvePoliceCamera();
            if (policeCamera == null)
            {
                return false;
            }

            gameplayCamera =
                interactionController != null
                    ? interactionController.GameplayCamera
                    : null;
            if (gameplayCamera == null ||
                gameplayCamera == policeCamera)
            {
                Camera[] cameras = FindObjectsByType<Camera>(
                    FindObjectsInactive.Include);
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera candidate = cameras[i];
                    if (candidate != null &&
                        candidate != policeCamera &&
                        candidate.gameObject.scene == sequenceScene &&
                        candidate.enabled &&
                        candidate.gameObject.activeInHierarchy)
                    {
                        gameplayCamera = candidate;
                        break;
                    }
                }
            }

            if (gameplayCamera == null ||
                gameplayCamera == policeCamera)
            {
                Debug.LogError(
                    "[PoliceOfficer] Active gameplay camera could not be " +
                    "resolved before switching to Police_Camera.",
                    this);
                return false;
            }

            gameplayListener =
                gameplayCamera.GetComponent<AudioListener>();
            gameplayCameraWasEnabled = gameplayCamera.enabled;
            gameplayListenerWasEnabled =
                gameplayListener != null && gameplayListener.enabled;

            if (gameplayListener != null)
            {
                gameplayListener.enabled = false;
            }

            gameplayCamera.enabled = false;
            policeCamera.enabled = true;
            if (policeListener != null)
            {
                policeListener.enabled = true;
            }

            cameraSwitched = true;
            return true;
        }

        private void DisablePoliceCamera()
        {
            ResolvePoliceCamera();
            if (policeListener != null)
            {
                policeListener.enabled = false;
            }

            if (policeCamera != null)
            {
                policeCamera.enabled = false;
            }
        }

        private void RestoreGameplayCameraAfterFailure()
        {
            if (!cameraSwitched)
            {
                return;
            }

            DisablePoliceCamera();
            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = gameplayCameraWasEnabled;
            }

            if (gameplayListener != null)
            {
                gameplayListener.enabled =
                    gameplayListenerWasEnabled;
            }

            cameraSwitched = false;
        }

        private void EnsureDialogueUi()
        {
            if (dialogueCanvas != null &&
                speakerText != null &&
                lineText != null)
            {
                if (advanceHintText != null)
                {
                    advanceHintText.text =
                        "E / Space / Enter: hi\u1ec7n nhanh / ti\u1ebfp t\u1ee5c";
                }

                return;
            }

            GameObject canvasObject = new GameObject(
                "PoliceArrestDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            dialogueCanvas = canvasObject.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = DialogueCanvasOrder;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject(
                "DialoguePanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(48f, 32f);
            panelRect.offsetMax = new Vector2(-48f, 232f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
            panelImage.raycastTarget = false;

            speakerText = CreateText(
                "SpeakerText",
                panelRect,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -16f),
                new Vector2(-48f, 44f),
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            lineText = CreateText(
                "LineText",
                panelRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f),
                new Vector2(-48f, -98f),
                34f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            advanceHintText = CreateText(
                "AdvanceHintText",
                panelRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 12f),
                new Vector2(-48f, 32f),
                18f,
                FontStyles.Italic,
                TextAlignmentOptions.MidlineRight);
            advanceHintText.text =
                "E / Space / Enter: hi\u1ec7n nhanh / ti\u1ebfp t\u1ee5c";
            advanceHintText.color =
                new Color(0.72f, 0.76f, 0.82f, 0.9f);
            SetDialogueVisible(false);
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect =
                textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private void SetDialogueVisible(bool visible)
        {
            if (dialogueCanvas != null)
            {
                dialogueCanvas.gameObject.SetActive(visible);
            }
        }

        private void CompleteSequence(bool completed)
        {
            if (state == PursuitState.Completed ||
                state == PursuitState.Failed)
            {
                return;
            }

            StopAgent();
            state = completed
                ? PursuitState.Completed
                : PursuitState.Failed;
            if (!completed)
            {
                SetDialogueVisible(false);
                RestoreGameplayCameraAfterFailure();
                RestorePlayerStateAfterFailure();
            }

            Action<bool>[] callbacks = completionCallbacks.ToArray();
            completionCallbacks.Clear();
            for (int i = 0; i < callbacks.Length; i++)
            {
                InvokeCallbackSafely(callbacks[i], completed);
            }

            Action<bool> handlers = PursuitCompleted;
            if (handlers == null)
            {
                return;
            }

            Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                InvokeCallbackSafely(
                    (Action<bool>)invocationList[i],
                    completed);
            }
        }

        private static void InvokeCallbackSafely(
            Action<bool> callback,
            bool completed)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback(completed);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void InstallForSceneWhenPoliceExists(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                FindExactRoot(scene, PoliceObjectName) == null)
            {
                return;
            }

            GetOrInstall(scene);
        }

        private static Transform FindExactRoot(
            Scene scene,
            string objectName)
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
                        objectName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (!root.activeSelf)
                {
                    return root.transform;
                }

                activeMatch = root.transform;
            }

            return activeMatch;
        }

        private void OnDestroy()
        {
            if (dialogueRoutine != null)
            {
                StopCoroutine(dialogueRoutine);
                dialogueRoutine = null;
            }

            if (state == PursuitState.Pursuing ||
                state == PursuitState.Dialogue)
            {
                CompleteSequence(false);
            }
            else
            {
                completionCallbacks.Clear();
            }
        }

        private void OnValidate()
        {
            runSpeed = Mathf.Max(6.1f, runSpeed);
            captureDistance = Mathf.Max(0.2f, captureDistance);
            charactersPerSecond = Mathf.Clamp(
                charactersPerSecond,
                10f,
                80f);
            punctuationPauseMultiplier = Mathf.Clamp(
                punctuationPauseMultiplier,
                1f,
                6f);
        }
    }
}
