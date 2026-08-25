using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Owns the Task-3 hand-off from James' key reward to Henry's warning.
    /// This controller deliberately stops at HenryCombatReady; the actual
    /// Henry combat encounter is implemented separately.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Mission3CompletionSequenceController : MonoBehaviour
    {
        private const string HenryObjectName =
            "henry_animated_cartoon_character";
        private const string HenryChildCameraName = "Henry_Camera";
        private const string StandaloneHenryCameraName = "HenryCamera";
        private const string SequenceLockReason =
            "Mission3HenryConfrontation";
        private const float HenryStopDistance = 1.6f;
        private const float CameraForwardDistance = 1.1f;
        private const float CameraHeight = 1.45f;
        private const float CameraRightOffset = 0.2f;
        private const float CameraLookHeight = 1.35f;
        private const float HenryCameraFieldOfView = 52f;
        private const float ApproachStartTimeout = 20f;
        private const float ApproachTravelTimeout = 30f;
        private const float CharactersPerSecond = 34f;
        private const float PunctuationPauseMultiplier = 2.5f;
        private const string HenryWarningLine =
            "Nh\u00F3c con, m\u00E0y gan l\u1EAFm m\u1EDBi d\u00E1m " +
            "\u0103n c\u1EAFp \u0111\u1ED3 c\u1EE7a tao";

        private enum SequenceState
        {
            Idle,
            Starting,
            Approaching,
            Dialogue,
            CombatPending
        }

        private static Mission3CompletionSequenceController instance;

        private Transform player;
        private Transform henryRoot;
        private Chapter1PlayerMotor playerMotor;
        private Chapter1InputReader inputReader;
        private PlayerInputLock inputLock;
        private Chapter1InteractionController interactionController;
        private BackpackPhoneInputController backpackController;
        private InventoryUIController inventoryUi;
        private PhoneUIController phoneUi;
        private HenryChaseController henryChase;
        private HenryRunAnimationPlayer henryAnimation;

        private Camera gameplayCamera;
        private Camera henryCamera;
        private Camera standaloneHenryCamera;
        private AudioListener gameplayListener;
        private AudioListener henryListener;

        private Canvas dialogueCanvas;
        private TMP_Text speakerText;
        private TMP_Text lineText;
        private TMP_Text advanceHintText;

        private SequenceState state;
        private Coroutine sequenceRoutine;
        private bool initialized;
        private bool lifecycleStarted;
        private bool approachArrived;
        private bool storyApproachSubscribed;
        private bool sequenceLockHeld;
        private bool gameplayComponentsCaptured;
        private bool interactionWasEnabled;
        private bool backpackWasEnabled;
        private bool gameplayInputStateCaptured;
        private bool gameplayInputWasEnabled;
        private bool cameraStateCaptured;
        private bool gameplayCameraWasEnabled;
        private bool gameplayListenerWasEnabled;
        private bool henryCameraTransformCaptured;
        private Vector3 henryCameraOriginalLocalPosition;
        private Quaternion henryCameraOriginalLocalRotation;
        private float henryCameraOriginalFieldOfView;
        private bool combatReadyRaised;

        public static Mission3CompletionSequenceController Instance =>
            instance;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
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

        /// <summary>
        /// Called by the James reward flow after the key has been persisted
        /// and James has restored the gameplay camera/input state.
        /// </summary>
        public static void NotifyPoliceKeyGranted()
        {
            if (instance == null)
            {
                InstallForScene(SceneManager.GetActiveScene());
            }

            instance?.RequestSequenceStart();
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (instance != null)
            {
                if (instance.gameObject.scene == scene)
                {
                    return;
                }

                instance = null;
            }

            Chapter1PlayerMotor scenePlayer = FindScenePlayer(scene);
            Transform sceneHenry = FindSceneTransform(
                scene,
                HenryObjectName);
            if (scenePlayer == null || sceneHenry == null)
            {
                return;
            }

            GameObject directorObject = new GameObject(
                nameof(Mission3CompletionSequenceController));
            SceneManager.MoveGameObjectToScene(directorObject, scene);
            Mission3CompletionSequenceController director =
                directorObject.AddComponent<
                    Mission3CompletionSequenceController>();
            director.Initialize(scenePlayer, sceneHenry);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            if (lifecycleStarted && initialized)
            {
                StartCoroutine(ResumeAfterEnable());
            }
        }

        private IEnumerator Start()
        {
            lifecycleStarted = true;
            yield return null;

            ResolveRuntimeReferences();
            DisableDedicatedHenryCameras();
            EnsureDialogueUi();

            if (Mission3Progress.CombatPending)
            {
                EstablishCombatPending(true);
            }
            else if (Mission3Progress.PoliceKeyReceived &&
                     !Mission3Progress.HenryConfrontationCompleted)
            {
                RequestSequenceStart();
            }
        }

        private IEnumerator ResumeAfterEnable()
        {
            yield return null;
            ResolveRuntimeReferences();
            DisableDedicatedHenryCameras();

            if (Mission3Progress.CombatPending)
            {
                EstablishCombatPending(true);
            }
            else if (Mission3Progress.PoliceKeyReceived &&
                     !Mission3Progress.HenryConfrontationCompleted)
            {
                RequestSequenceStart();
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            sequenceRoutine = null;
            UnsubscribeFromStoryApproach();

            if (henryChase != null &&
                henryChase.IsStoryApproachActive)
            {
                henryChase.CancelStoryApproach();
            }

            SetDialogueVisible(false);
            RestoreGameplayCamera();
            RestoreGameplayComponents();
            RestoreGameplayInputState();
            ReleaseSequenceLock();
            state = SequenceState.Idle;
            approachArrived = false;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Initialize(
            Chapter1PlayerMotor scenePlayer,
            Transform sceneHenry)
        {
            playerMotor = scenePlayer;
            player = scenePlayer != null ? scenePlayer.transform : null;
            henryRoot = sceneHenry;
            initialized = player != null && henryRoot != null;

            ResolveRuntimeReferences();
            DisableDedicatedHenryCameras();
            EnsureDialogueUi();
            SetDialogueVisible(false);
        }

        private void RequestSequenceStart()
        {
            if (!isActiveAndEnabled ||
                !Mission3Progress.PoliceKeyReceived ||
                Mission3Progress.HenryConfrontationCompleted ||
                Mission3Progress.HenryDefeated ||
                sequenceRoutine != null ||
                state == SequenceState.Approaching ||
                state == SequenceState.Dialogue ||
                state == SequenceState.CombatPending)
            {
                return;
            }

            sequenceRoutine = StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            state = SequenceState.Starting;

            // The reward caller invokes us one frame after James restores its
            // camera and input lock. This additional frame makes the method
            // equally safe when called by another future reward presenter.
            yield return null;
            ResolveRuntimeReferences();
            DisableDedicatedHenryCameras();

            if (!ValidateSequenceReferences())
            {
                state = SequenceState.Idle;
                sequenceRoutine = null;
                yield break;
            }

            SubscribeToStoryApproach();
            approachArrived = false;

            // Task 2 normally sent Henry home when the equipment was handed
            // to Minh. Reassert that invariant here so a very fast player (or
            // a resumed save) cannot make this scene begin while Henry is
            // still returning from the food cart or an old chase.
            henryChase.ConcludeEncounterAndReturnHome();

            bool approachStarted = false;
            float startDeadline = Time.unscaledTime +
                                  ApproachStartTimeout;
            while (!approachStarted &&
                   Time.unscaledTime < startDeadline)
            {
                if (henryChase.IsReadyForStoryApproach)
                {
                    approachStarted = henryChase.BeginStoryApproach(
                        player,
                        HenryStopDistance);
                }

                if (!approachStarted)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    ResolveRuntimeReferences();
                }
            }

            if (!approachStarted)
            {
                Debug.LogError(
                    "[Mission3Completion] Henry could not begin the " +
                    "story approach. Task 3 remains saved before the " +
                    "confrontation so it can resume after reload.",
                    this);
                AbortBeforeCombatPending();
                yield break;
            }

            state = SequenceState.Approaching;
            float travelDeadline = Time.unscaledTime +
                                   ApproachTravelTimeout;
            while (!approachArrived)
            {
                if (henryChase == null)
                {
                    Debug.LogError(
                        "[Mission3Completion] HenryChaseController was " +
                        "lost during the story approach.",
                        this);
                    AbortBeforeCombatPending();
                    yield break;
                }

                if (Time.unscaledTime >= travelDeadline)
                {
                    henryChase.CancelStoryApproach();
                    if (!TryPlaceHenryAtSafeFallback())
                    {
                        Debug.LogError(
                            "[Mission3Completion] Henry's story " +
                            "approach timed out and no safe NavMesh " +
                            "fallback point could be assigned.",
                            this);
                        AbortBeforeCombatPending();
                        yield break;
                    }

                    approachArrived = true;
                    break;
                }

                if (!henryChase.IsStoryApproachActive)
                {
                    Debug.LogError(
                        "[Mission3Completion] Henry's story approach " +
                        "ended before he reached Nam. Task 3 remains " +
                        "saved before the confrontation.",
                        this);
                    AbortBeforeCombatPending();
                    yield break;
                }

                yield return null;
            }

            UnsubscribeFromStoryApproach();

            // Nam remains fully controllable while Henry returns home and
            // runs toward him. Lock controls only for the close-up warning
            // itself, after Henry has actually arrived.
            AcquireSequenceControl(true);
            FaceHenryAndPlayer();
            henryAnimation?.PlayIdle();

            if (!SwitchToHenryCamera())
            {
                Debug.LogError(
                    "[Mission3Completion] Cannot play Henry's warning " +
                    "because the exact child camera " +
                    "'henry_animated_cartoon_character/Henry_Camera' " +
                    "is unavailable. No fallback camera will be used.",
                    this);
                AbortBeforeCombatPending();
                yield break;
            }

            state = SequenceState.Dialogue;
            SetDialogueVisible(true);
            if (advanceHintText != null)
            {
                advanceHintText.text =
                    "[E] Ti\u1EBFp t\u1EE5c (Space / Enter: hi\u1EC7n nhanh)";
            }

            yield return WaitForAdvanceRelease();
            yield return StreamLine(
                "Henry",
                HenryWarningLine,
                true);

            SetDialogueVisible(false);
            RestoreGameplayCamera();
            if (!Mission3Progress.MarkHenryConfrontationCompleted())
            {
                Debug.LogError(
                    "[Mission3Completion] Henry's warning finished, " +
                    "but its persistent completion state could not be " +
                    "saved. CombatPending will not be entered.",
                    this);
                AbortBeforeCombatPending();
                yield break;
            }

            EstablishCombatPending(false);
            sequenceRoutine = null;
        }

        private void HandleStoryApproachArrived()
        {
            approachArrived = true;
        }

        private void EstablishCombatPending(bool restoreHenryPose)
        {
            ResolveRuntimeReferences();
            DisableDedicatedHenryCameras();
            SetDialogueVisible(false);

            // CombatPending is only a story hand-off marker. The future
            // combat system must acquire its own lock when it starts; this
            // sequence must return all controls after Henry's warning.
            RestoreGameplayComponents();
            RestoreGameplayInputState();
            ReleaseSequenceLock();

            state = SequenceState.CombatPending;
            if (restoreHenryPose)
            {
                RestoreCombatPendingPose();
            }
            else
            {
                FaceHenryAndPlayer();
                henryAnimation?.PlayIdle();
            }

            if (!combatReadyRaised)
            {
                combatReadyRaised = true;
                Chapter1EventBus.RaiseHenryCombatReady();
            }
        }

        private void RestoreCombatPendingPose()
        {
            if (player == null || henryRoot == null)
            {
                return;
            }

            Vector3 fromPlayerToHenry = Vector3.ProjectOnPlane(
                henryRoot.position - player.position,
                Vector3.up);
            if (fromPlayerToHenry.sqrMagnitude <= 0.001f)
            {
                fromPlayerToHenry = -Vector3.ProjectOnPlane(
                    player.forward,
                    Vector3.up);
            }

            if (fromPlayerToHenry.sqrMagnitude <= 0.001f)
            {
                fromPlayerToHenry = Vector3.back;
            }

            Vector3 desiredPosition = player.position +
                                      fromPlayerToHenry.normalized *
                                      HenryStopDistance;
            if (NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit hit,
                    2.5f,
                    NavMesh.AllAreas))
            {
                NavMeshAgent agent = henryChase != null
                    ? henryChase.Agent
                    : henryRoot.GetComponent<NavMeshAgent>();
                if (agent != null &&
                    agent.enabled &&
                    agent.isOnNavMesh)
                {
                    agent.Warp(hit.position);
                }
                else
                {
                    henryRoot.position = hit.position;
                }
            }

            FaceHenryAndPlayer();
            henryAnimation?.PlayIdle();
        }

        private bool TryPlaceHenryAtSafeFallback()
        {
            if (player == null || henryRoot == null ||
                henryChase == null)
            {
                return false;
            }

            Vector3 radialDirection = Vector3.ProjectOnPlane(
                henryRoot.position - player.position,
                Vector3.up);
            if (radialDirection.sqrMagnitude <= 0.001f)
            {
                radialDirection = -Vector3.ProjectOnPlane(
                    player.forward,
                    Vector3.up);
            }

            if (radialDirection.sqrMagnitude <= 0.001f)
            {
                radialDirection = Vector3.back;
            }

            Vector3 desiredPosition = player.position +
                                      radialDirection.normalized *
                                      HenryStopDistance;
            if (!NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit hit,
                    2.5f,
                    NavMesh.AllAreas))
            {
                return false;
            }

            NavMeshAgent agent = henryChase.Agent;
            if (agent == null || !agent.enabled ||
                !agent.Warp(hit.position))
            {
                return false;
            }

            FaceHenryAndPlayer();
            henryAnimation?.PlayIdle();
            return true;
        }

        private void AbortBeforeCombatPending()
        {
            UnsubscribeFromStoryApproach();
            SetDialogueVisible(false);
            RestoreGameplayCamera();
            RestoreGameplayComponents();
            RestoreGameplayInputState();
            ReleaseSequenceLock();
            state = SequenceState.Idle;
            sequenceRoutine = null;
            approachArrived = false;
        }

        private bool ValidateSequenceReferences()
        {
            if (player == null || inputReader == null || inputLock == null ||
                interactionController == null)
            {
                Debug.LogError(
                    "[Mission3Completion] Cannot start because Nam's " +
                    "player controllers are incomplete.",
                    this);
                return false;
            }

            if (henryRoot == null || henryChase == null)
            {
                Debug.LogError(
                    "[Mission3Completion] Cannot start because Henry or " +
                    "HenryChaseController is missing.",
                    this);
                return false;
            }

            if (henryCamera == null ||
                henryCamera.transform.parent != henryRoot ||
                !string.Equals(
                    henryCamera.name,
                    HenryChildCameraName,
                    StringComparison.Ordinal))
            {
                Debug.LogError(
                    "[Mission3Completion] Required direct child camera " +
                    "'henry_animated_cartoon_character/Henry_Camera' was " +
                    "not found. Standalone 'HenryCamera' is not a valid " +
                    "fallback.",
                    this);
                return false;
            }

            if (gameplayCamera == null)
            {
                Debug.LogError(
                    "[Mission3Completion] Gameplay camera is missing.",
                    this);
                return false;
            }

            return true;
        }

        private void ResolveRuntimeReferences()
        {
            if (playerMotor == null)
            {
                playerMotor = FindScenePlayer(gameObject.scene);
            }

            if (player == null && playerMotor != null)
            {
                player = playerMotor.transform;
            }

            if (henryRoot == null)
            {
                henryRoot = FindSceneTransform(
                    gameObject.scene,
                    HenryObjectName);
            }

            if (player != null)
            {
                inputReader ??= player.GetComponent<Chapter1InputReader>();
                inputLock ??= player.GetComponent<PlayerInputLock>();
                interactionController ??=
                    player.GetComponent<Chapter1InteractionController>();
                backpackController ??=
                    player.GetComponent<BackpackPhoneInputController>();
            }

            if (inventoryUi == null)
            {
                inventoryUi = FindAnyObjectByType<InventoryUIController>(
                    FindObjectsInactive.Include);
            }

            if (phoneUi == null)
            {
                phoneUi = FindAnyObjectByType<PhoneUIController>(
                    FindObjectsInactive.Include);
            }

            if (henryRoot != null)
            {
                henryChase ??=
                    henryRoot.GetComponent<HenryChaseController>();
                henryAnimation ??=
                    henryRoot.GetComponent<HenryRunAnimationPlayer>();
                ResolveExactHenryChildCamera();
            }

            if (gameplayCamera == null && interactionController != null)
            {
                gameplayCamera = interactionController.GameplayCamera;
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (standaloneHenryCamera == null)
            {
                standaloneHenryCamera = FindSceneCamera(
                    gameObject.scene,
                    StandaloneHenryCameraName);
            }
        }

        private void ResolveExactHenryChildCamera()
        {
            if (henryRoot == null)
            {
                return;
            }

            Transform cameraTransform = henryRoot.Find(
                HenryChildCameraName);
            Camera exactCamera = cameraTransform != null &&
                                 cameraTransform.parent == henryRoot
                ? cameraTransform.GetComponent<Camera>()
                : null;
            if (exactCamera == null)
            {
                henryCamera = null;
                henryListener = null;
                return;
            }

            henryCamera = exactCamera;
            henryListener = FindListener(henryCamera);
            if (!henryCameraTransformCaptured)
            {
                henryCameraOriginalLocalPosition =
                    henryCamera.transform.localPosition;
                henryCameraOriginalLocalRotation =
                    henryCamera.transform.localRotation;
                henryCameraOriginalFieldOfView =
                    henryCamera.fieldOfView;
                henryCameraTransformCaptured = true;
            }
        }

        private void DisableDedicatedHenryCameras()
        {
            if (henryCamera != null)
            {
                SetCameraAndListener(henryCamera, false);
            }

            // This camera is intentionally never selected as a fallback for
            // the confrontation. Keeping it off also prevents a duplicate
            // AudioListener when the child camera is enabled.
            if (standaloneHenryCamera != null)
            {
                SetCameraAndListener(standaloneHenryCamera, false);
            }
        }

        private void AcquireSequenceControl(bool suppressBackpack)
        {
            ResolveRuntimeReferences();

            if (suppressBackpack)
            {
                if (phoneUi != null && phoneUi.IsOpen)
                {
                    phoneUi.ClosePhone();
                }

                if (inventoryUi != null && inventoryUi.IsOpen)
                {
                    inventoryUi.CloseInventory();
                }
            }

            if (!gameplayComponentsCaptured)
            {
                interactionWasEnabled = interactionController != null &&
                                        interactionController.enabled;
                backpackWasEnabled = backpackController != null &&
                                     backpackController.enabled;
                gameplayComponentsCaptured = true;
            }

            if (inputLock != null && !sequenceLockHeld)
            {
                inputLock.AcquireInputLock(SequenceLockReason);
                sequenceLockHeld = true;
            }

            if (interactionController != null)
            {
                interactionController.enabled = false;
            }

            if (backpackController != null)
            {
                backpackController.enabled = !suppressBackpack &&
                                             backpackWasEnabled;
            }

            if (suppressBackpack)
            {
                SuppressGameplayInput();
            }
            else
            {
                RestoreGameplayInputState();
            }
        }

        private void RestoreGameplayComponents()
        {
            if (!gameplayComponentsCaptured)
            {
                return;
            }

            if (interactionController != null)
            {
                interactionController.enabled = interactionWasEnabled;
            }

            if (backpackController != null)
            {
                backpackController.enabled = backpackWasEnabled;
            }

            gameplayComponentsCaptured = false;
        }

        private void SuppressGameplayInput()
        {
            if (inputReader == null)
            {
                return;
            }

            if (!gameplayInputStateCaptured)
            {
                gameplayInputWasEnabled =
                    inputReader.GameplayInputEnabled;
                gameplayInputStateCaptured = true;
            }

            inputReader.SetGameplayInputEnabled(false);
        }

        private void RestoreGameplayInputState()
        {
            if (!gameplayInputStateCaptured)
            {
                return;
            }

            if (inputReader != null)
            {
                inputReader.SetGameplayInputEnabled(
                    gameplayInputWasEnabled);
            }

            gameplayInputStateCaptured = false;
        }

        private void ReleaseSequenceLock()
        {
            if (inputLock != null && sequenceLockHeld)
            {
                inputLock.ReleaseInputLock(SequenceLockReason);
            }

            sequenceLockHeld = false;
        }

        private void SubscribeToStoryApproach()
        {
            if (henryChase == null || storyApproachSubscribed)
            {
                return;
            }

            henryChase.StoryApproachArrived +=
                HandleStoryApproachArrived;
            storyApproachSubscribed = true;
        }

        private void UnsubscribeFromStoryApproach()
        {
            if (henryChase != null && storyApproachSubscribed)
            {
                henryChase.StoryApproachArrived -=
                    HandleStoryApproachArrived;
            }

            storyApproachSubscribed = false;
        }

        private void FaceHenryAndPlayer()
        {
            if (henryRoot == null || player == null)
            {
                return;
            }

            Vector3 toPlayer = Vector3.ProjectOnPlane(
                player.position - henryRoot.position,
                Vector3.up);
            if (toPlayer.sqrMagnitude <= 0.001f)
            {
                return;
            }

            henryRoot.rotation = Quaternion.LookRotation(
                toPlayer.normalized,
                Vector3.up);
            player.rotation = Quaternion.LookRotation(
                -toPlayer.normalized,
                Vector3.up);
        }

        private bool SwitchToHenryCamera()
        {
            ResolveRuntimeReferences();
            if (henryCamera == null || gameplayCamera == null ||
                henryRoot == null)
            {
                return false;
            }

            CaptureGameplayCameraState();
            ConfigureHenryCameraShot();

            // Disable the outgoing listener before enabling the incoming one;
            // there is never a rendered frame with two active listeners.
            SetCameraAndListener(gameplayCamera, false);
            SetCameraAndListener(standaloneHenryCamera, false);
            SetCameraAndListener(henryCamera, true);
            return true;
        }

        private void ConfigureHenryCameraShot()
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                henryRoot.forward,
                Vector3.up);
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            henryCamera.transform.position = henryRoot.position +
                                             forward *
                                             CameraForwardDistance +
                                             Vector3.up * CameraHeight +
                                             right * CameraRightOffset;
            henryCamera.transform.rotation = Quaternion.LookRotation(
                henryRoot.position + Vector3.up * CameraLookHeight -
                henryCamera.transform.position,
                Vector3.up);
            henryCamera.fieldOfView = HenryCameraFieldOfView;
        }

        private void CaptureGameplayCameraState()
        {
            if (cameraStateCaptured || gameplayCamera == null)
            {
                return;
            }

            gameplayListener = FindListener(gameplayCamera);
            gameplayCameraWasEnabled = gameplayCamera.enabled;
            gameplayListenerWasEnabled = gameplayListener != null &&
                                         gameplayListener.enabled;
            cameraStateCaptured = true;
        }

        private void RestoreGameplayCamera()
        {
            if (henryCamera != null)
            {
                SetCameraAndListener(henryCamera, false);
                RestoreHenryCameraTransform();
            }

            SetCameraAndListener(standaloneHenryCamera, false);
            if (cameraStateCaptured && gameplayCamera != null)
            {
                gameplayCamera.enabled = gameplayCameraWasEnabled;
                if (gameplayListener != null)
                {
                    gameplayListener.enabled =
                        gameplayListenerWasEnabled;
                }
            }

            cameraStateCaptured = false;
            gameplayListener = null;
        }

        private void RestoreHenryCameraTransform()
        {
            if (!henryCameraTransformCaptured || henryCamera == null)
            {
                return;
            }

            henryCamera.transform.localPosition =
                henryCameraOriginalLocalPosition;
            henryCamera.transform.localRotation =
                henryCameraOriginalLocalRotation;
            henryCamera.fieldOfView = henryCameraOriginalFieldOfView;
        }

        private IEnumerator StreamLine(
            string speaker,
            string line,
            bool requireEToAdvance = false)
        {
            if (speakerText != null)
            {
                speakerText.text = speaker ?? string.Empty;
                speakerText.color = new Color(1f, 0.58f, 0.32f);
            }

            if (lineText == null)
            {
                yield return requireEToAdvance
                    ? WaitForEAdvance()
                    : WaitForNextLine();
                yield break;
            }

            lineText.text = line ?? string.Empty;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();

            bool skippedTyping = false;
            int characterCount = lineText.textInfo.characterCount;
            for (int i = 0; i < characterCount; i++)
            {
                if (IsAdvancePressed())
                {
                    skippedTyping = true;
                    break;
                }

                lineText.maxVisibleCharacters = i + 1;
                char character =
                    lineText.textInfo.characterInfo[i].character;
                float elapsed = 0f;
                float delay = GetCharacterDelay(character);
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

            yield return requireEToAdvance
                ? WaitForEAdvance()
                : WaitForNextLine();
        }

        private static float GetCharacterDelay(char character)
        {
            float delay = 1f / CharactersPerSecond;
            if (character == '.' || character == '!' || character == '?')
            {
                return delay * PunctuationPauseMultiplier;
            }

            if (character == ',' || character == ';' || character == ':')
            {
                return delay * Mathf.Lerp(
                    1f,
                    PunctuationPauseMultiplier,
                    0.5f);
            }

            return delay;
        }

        private static IEnumerator WaitForNextLine()
        {
            while (!IsAdvancePressed())
            {
                yield return null;
            }

            yield return WaitForAdvanceRelease();
        }

        private static IEnumerator WaitForEAdvance()
        {
            Keyboard keyboard = Keyboard.current;
            while (keyboard == null ||
                   !keyboard.eKey.wasPressedThisFrame)
            {
                yield return null;
                keyboard = Keyboard.current;
            }

            while (Keyboard.current != null &&
                   Keyboard.current.eKey.isPressed)
            {
                yield return null;
            }

            yield return null;
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

        private void EnsureDialogueUi()
        {
            if (dialogueCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "Mission3HenryWarningCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            dialogueCanvas = canvasObject.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 235;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject(
                "DialoguePanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panel =
                panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.offsetMin = new Vector2(48f, 32f);
            panel.offsetMax = new Vector2(-48f, 232f);
            panelObject.GetComponent<Image>().color =
                new Color(0.16f, 0.16f, 0.18f, 0.94f);

            speakerText = CreateText(
                panel,
                "Speaker",
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, -16f),
                new Vector2(-48f, 44f),
                true);
            lineText = CreateText(
                panel,
                "Line",
                34f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, -8f),
                new Vector2(-48f, -98f),
                false);
            advanceHintText = CreateText(
                panel,
                "AdvanceHint",
                18f,
                FontStyles.Italic,
                TextAlignmentOptions.MidlineRight,
                new Vector2(0f, 12f),
                new Vector2(-48f, 32f),
                false,
                true);
            advanceHintText.text =
                "E / Space / Enter: hi\u1EC7n nhanh / ti\u1EBFp t\u1EE5c";
            advanceHintText.color =
                new Color(0.82f, 0.82f, 0.84f, 0.95f);

            SetDialogueVisible(false);
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string objectName,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool topAnchored,
            bool bottomAnchored = false)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect =
                textObject.GetComponent<RectTransform>();
            rect.anchorMin = bottomAnchored
                ? new Vector2(0f, 0f)
                : new Vector2(0f, topAnchored ? 1f : 0f);
            rect.anchorMax = bottomAnchored
                ? new Vector2(1f, 0f)
                : Vector2.one;
            rect.pivot = new Vector2(
                0.5f,
                topAnchored ? 1f : (bottomAnchored ? 0f : 0.5f));
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
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

        private static void SetCameraAndListener(
            Camera camera,
            bool enabled)
        {
            if (camera == null)
            {
                return;
            }

            AudioListener listener = FindListener(camera);
            if (!enabled && listener != null)
            {
                listener.enabled = false;
            }

            camera.enabled = enabled;
            if (enabled && listener != null)
            {
                listener.enabled = true;
            }
        }

        private static AudioListener FindListener(Camera camera)
        {
            return camera != null
                ? camera.GetComponent<AudioListener>() ??
                  camera.GetComponentInChildren<AudioListener>(true)
                : null;
        }

        private static Chapter1PlayerMotor FindScenePlayer(Scene scene)
        {
            Chapter1PlayerMotor[] motors =
                FindObjectsByType<Chapter1PlayerMotor>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < motors.Length; i++)
            {
                if (motors[i] != null &&
                    motors[i].gameObject.scene == scene)
                {
                    return motors[i];
                }
            }

            return null;
        }

        private static Transform FindSceneTransform(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex]
                    .GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (string.Equals(
                            transforms[i].name,
                            objectName,
                            StringComparison.Ordinal))
                    {
                        return transforms[i];
                    }
                }
            }

            return null;
        }

        private static Camera FindSceneCamera(
            Scene scene,
            string cameraName)
        {
            Camera[] cameras = FindObjectsByType<Camera>(
                FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null &&
                    cameras[i].gameObject.scene == scene &&
                    string.Equals(
                        cameras[i].name,
                        cameraName,
                        StringComparison.Ordinal))
                {
                    return cameras[i];
                }
            }

            return null;
        }
    }
}
