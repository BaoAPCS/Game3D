using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Holds PetrolCan in front of the player and provides a camera-directed,
    /// ballistic throw preview while the ThrowCan action is held.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Chapter1InputReader))]
    [RequireComponent(typeof(PlayerInputLock))]
    public sealed class PetrolCanThrower : MonoBehaviour
    {
        private const string HoldPointName = "PetrolCanHoldPoint";
        private const string TrajectoryObjectName = "PetrolCanTrajectory";

        [Header("References")]
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private Chapter1InteractionController
            interactionController;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private LineRenderer trajectoryRenderer;

        [Header("Hold Pose")]
        [SerializeField] private Vector3 holdLocalPosition =
            new Vector3(0.32f, 1.1f, 0.52f);

        [Header("Throw")]
        [SerializeField, Min(0.1f)] private float throwSpeed = 9f;
        [SerializeField, Range(4, 96)] private int trajectorySegments = 36;
        [SerializeField, Min(0.01f)] private float trajectoryTimeStep = 0.05f;
        [SerializeField] private LayerMask trajectoryCollisionMask =
            Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float collisionIgnoreDuration =
            0.4f;
        [SerializeField, Min(0f)] private float pickupAimDelay = 0.12f;

        private readonly List<IgnoredCollisionPair> ignoredCollisionPairs =
            new List<IgnoredCollisionPair>();
        private Vector3[] trajectoryPoints;
        private PetrolCanInteractable heldCan;
        private Material runtimeTrajectoryMaterial;
        private bool isAiming;
        private int pickupFrame = -1;
        private float pickupAimAllowedAt;

        private sealed class IgnoredCollisionPair
        {
            public Collider CanCollider;
            public Collider PlayerCollider;
            public float RestoreAtRealtime;
        }

        internal bool HasCan => heldCan != null;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForScene(scene);
        }

        internal static PetrolCanThrower GetOrInstall(GameObject playerObject)
        {
            if (playerObject == null ||
                playerObject.GetComponent<Chapter1InputReader>() == null)
            {
                return null;
            }

            if (!playerObject.TryGetComponent(
                    out PetrolCanThrower thrower))
            {
                thrower = playerObject.AddComponent<PetrolCanThrower>();
            }

            return thrower;
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Chapter1InputReader[] readers =
                FindObjectsByType<Chapter1InputReader>(
                    FindObjectsInactive.Exclude);
            for (int i = 0; i < readers.Length; i++)
            {
                Chapter1InputReader reader = readers[i];
                if (reader == null || reader.gameObject.scene != scene)
                {
                    continue;
                }

                GameObject candidate = reader.gameObject;
                bool isPlayer =
                    candidate.GetComponent<Chapter1PlayerMotor>() != null ||
                    candidate.GetComponent<Chapter1InteractionController>() !=
                    null;
                if (isPlayer)
                {
                    GetOrInstall(candidate);
                }
            }
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureHoldPoint();
            EnsureTrajectoryRenderer();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToInput();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            CancelAim();
            RestoreAllIgnoredCollisions();
        }

        private void OnDestroy()
        {
            RestoreAllIgnoredCollisions();
            if (runtimeTrajectoryMaterial != null)
            {
                Destroy(runtimeTrajectoryMaterial);
            }
        }

        private void Update()
        {
            RestoreExpiredCollisions();

            if (!isAiming)
            {
                if (heldCan != null &&
                    inputReader != null &&
                    inputReader.ThrowCanHeld &&
                    Time.frameCount > pickupFrame &&
                    Time.unscaledTime >= pickupAimAllowedAt &&
                    CanUseThrowInput())
                {
                    isAiming = true;
                    DrawTrajectory();
                }

                return;
            }

            if (!CanUseThrowInput() ||
                inputReader == null ||
                !inputReader.ThrowCanHeld ||
                heldCan == null)
            {
                CancelAim();
                return;
            }

            DrawTrajectory();
        }

        internal bool TryPickup(PetrolCanInteractable petrolCan)
        {
            if (petrolCan == null ||
                heldCan != null ||
                !CanUseThrowInput())
            {
                return false;
            }

            EnsureHoldPoint();
            if (holdPoint == null)
            {
                return false;
            }

            CancelAim();
            RestoreIgnoredCollisionsFor(petrolCan);
            if (!petrolCan.AttachTo(holdPoint))
            {
                return false;
            }

            heldCan = petrolCan;
            pickupFrame = Time.frameCount;
            pickupAimAllowedAt = Time.unscaledTime + pickupAimDelay;
            return true;
        }

        private void SubscribeToInput()
        {
            UnsubscribeFromInput();
            if (inputReader != null)
            {
                inputReader.ThrowCanPressed += HandleThrowPressed;
                inputReader.ThrowCanReleased += HandleThrowReleased;
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged += HandleLockStateChanged;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (inputReader != null)
            {
                inputReader.ThrowCanPressed -= HandleThrowPressed;
                inputReader.ThrowCanReleased -= HandleThrowReleased;
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged -= HandleLockStateChanged;
            }
        }

        private void HandleThrowPressed()
        {
            if (heldCan == null || !CanUseThrowInput())
            {
                return;
            }

            if (Time.frameCount <= pickupFrame)
            {
                return;
            }

            isAiming = true;
            DrawTrajectory();
        }

        private void HandleThrowReleased()
        {
            if (!isAiming)
            {
                return;
            }

            if (!CanUseThrowInput())
            {
                CancelAim();
                return;
            }

            ThrowHeldCan();
        }

        private void HandleLockStateChanged(bool locked)
        {
            if (locked)
            {
                CancelAim();
            }
        }

        private bool CanUseThrowInput()
        {
            return isActiveAndEnabled &&
                   inputReader != null &&
                   inputReader.GameplayInputEnabled &&
                   (inputLock == null || !inputLock.IsLocked) &&
                   Time.timeScale > 0f;
        }

        private void ThrowHeldCan()
        {
            if (heldCan == null)
            {
                CancelAim();
                return;
            }

            Camera resolvedCamera = ResolveGameplayCamera();
            if (resolvedCamera == null)
            {
                CancelAim();
                return;
            }

            PetrolCanInteractable petrolCan = heldCan;
            Vector3 origin = petrolCan.transform.position;
            Quaternion rotation = petrolCan.transform.rotation;
            Vector3 velocity = CalculateThrowVelocity(resolvedCamera);

            if (!petrolCan.ReleaseForThrow(origin, rotation, velocity))
            {
                CancelAim();
                return;
            }

            heldCan = null;
            CancelAim();
            IgnorePlayerCollisions(petrolCan);
        }

        private Vector3 CalculateThrowVelocity(Camera resolvedCamera)
        {
            Vector3 cameraForward = resolvedCamera != null
                ? resolvedCamera.transform.forward
                : transform.forward;
            if (cameraForward.sqrMagnitude <= 0.0001f)
            {
                cameraForward = transform.forward;
            }

            return cameraForward.normalized * throwSpeed;
        }

        private void DrawTrajectory()
        {
            EnsureTrajectoryRenderer();
            Camera resolvedCamera = ResolveGameplayCamera();
            if (trajectoryRenderer == null ||
                heldCan == null ||
                resolvedCamera == null)
            {
                CancelAim();
                return;
            }

            EnsureTrajectoryPointBuffer();
            Vector3 origin = heldCan.transform.position;
            Vector3 velocity = CalculateThrowVelocity(resolvedCamera);
            Vector3 previousPoint = origin;
            trajectoryPoints[0] = origin;
            int pointCount = 1;
            int collisionMask = trajectoryCollisionMask.value &
                                ~(1 << gameObject.layer);

            for (int i = 1; i <= trajectorySegments; i++)
            {
                float time = i * trajectoryTimeStep;
                Vector3 nextPoint = origin + velocity * time +
                    0.5f * Physics.gravity * time * time;

                if (Physics.Linecast(
                        previousPoint,
                        nextPoint,
                        out RaycastHit hit,
                        collisionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    trajectoryPoints[pointCount++] = hit.point;
                    break;
                }

                trajectoryPoints[pointCount++] = nextPoint;
                previousPoint = nextPoint;
            }

            trajectoryRenderer.positionCount = pointCount;
            for (int i = 0; i < pointCount; i++)
            {
                trajectoryRenderer.SetPosition(i, trajectoryPoints[i]);
            }

            trajectoryRenderer.enabled = true;
        }

        private void CancelAim()
        {
            isAiming = false;
            if (trajectoryRenderer != null)
            {
                trajectoryRenderer.enabled = false;
                trajectoryRenderer.positionCount = 0;
            }
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<Chapter1InputReader>();
            }

            if (inputLock == null)
            {
                inputLock = GetComponent<PlayerInputLock>();
            }

            if (interactionController == null)
            {
                interactionController =
                    GetComponent<Chapter1InteractionController>();
            }

            ResolveGameplayCamera();
        }

        private Camera ResolveGameplayCamera()
        {
            if (interactionController != null &&
                interactionController.GameplayCamera != null)
            {
                gameplayCamera = interactionController.GameplayCamera;
            }
            else if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            return gameplayCamera;
        }

        private void EnsureHoldPoint()
        {
            if (holdPoint == null)
            {
                Transform existing = transform.Find(HoldPointName);
                if (existing != null)
                {
                    holdPoint = existing;
                }
                else
                {
                    GameObject holdObject = new GameObject(HoldPointName);
                    holdPoint = holdObject.transform;
                    holdPoint.SetParent(transform, false);
                }
            }

            holdPoint.localPosition = holdLocalPosition;
            holdPoint.localRotation = Quaternion.identity;
            holdPoint.localScale = Vector3.one;
        }

        private void EnsureTrajectoryRenderer()
        {
            if (trajectoryRenderer == null)
            {
                Transform existing = transform.Find(TrajectoryObjectName);
                GameObject trajectoryObject;
                if (existing != null)
                {
                    trajectoryObject = existing.gameObject;
                }
                else
                {
                    trajectoryObject = new GameObject(TrajectoryObjectName);
                    trajectoryObject.transform.SetParent(transform, false);
                }

                trajectoryRenderer =
                    trajectoryObject.GetComponent<LineRenderer>();
                if (trajectoryRenderer == null)
                {
                    trajectoryRenderer =
                        trajectoryObject.AddComponent<LineRenderer>();
                }
            }

            trajectoryRenderer.useWorldSpace = true;
            trajectoryRenderer.loop = false;
            trajectoryRenderer.widthMultiplier = 0.035f;
            trajectoryRenderer.numCapVertices = 4;
            trajectoryRenderer.numCornerVertices = 4;
            trajectoryRenderer.alignment = LineAlignment.View;
            trajectoryRenderer.textureMode = LineTextureMode.Stretch;
            trajectoryRenderer.shadowCastingMode = ShadowCastingMode.Off;
            trajectoryRenderer.receiveShadows = false;
            trajectoryRenderer.startColor =
                new Color(1f, 0.8f, 0.1f, 0.95f);
            trajectoryRenderer.endColor =
                new Color(1f, 0.25f, 0.05f, 0.55f);

            if (trajectoryRenderer.sharedMaterial == null)
            {
                Shader lineShader = Shader.Find("Sprites/Default") ??
                    Shader.Find("Universal Render Pipeline/Unlit");
                if (lineShader != null)
                {
                    runtimeTrajectoryMaterial = new Material(lineShader)
                    {
                        name = "PetrolCanTrajectory_Runtime",
                        hideFlags = HideFlags.DontSave
                    };
                    trajectoryRenderer.sharedMaterial =
                        runtimeTrajectoryMaterial;
                }
            }

            trajectoryRenderer.enabled = false;
            trajectoryRenderer.positionCount = 0;
        }

        private void EnsureTrajectoryPointBuffer()
        {
            int requiredLength = trajectorySegments + 1;
            if (trajectoryPoints == null ||
                trajectoryPoints.Length != requiredLength)
            {
                trajectoryPoints = new Vector3[requiredLength];
            }
        }

        private void IgnorePlayerCollisions(
            PetrolCanInteractable petrolCan)
        {
            Collider[] canColliders = petrolCan.PhysicsColliders;
            Collider[] playerColliders =
                GetComponentsInChildren<Collider>(true);
            float restoreAt = Time.realtimeSinceStartup +
                              collisionIgnoreDuration;

            for (int i = 0; i < canColliders.Length; i++)
            {
                Collider canCollider = canColliders[i];
                if (canCollider == null || !canCollider.enabled)
                {
                    continue;
                }

                for (int j = 0; j < playerColliders.Length; j++)
                {
                    Collider playerCollider = playerColliders[j];
                    if (playerCollider == null ||
                        playerCollider == canCollider ||
                        petrolCan.OwnsCollider(playerCollider))
                    {
                        continue;
                    }

                    IgnoredCollisionPair existingPair = FindIgnoredPair(
                        canCollider,
                        playerCollider);
                    if (existingPair != null)
                    {
                        existingPair.RestoreAtRealtime = restoreAt;
                        continue;
                    }

                    Physics.IgnoreCollision(
                        canCollider,
                        playerCollider,
                        true);
                    ignoredCollisionPairs.Add(
                        new IgnoredCollisionPair
                        {
                            CanCollider = canCollider,
                            PlayerCollider = playerCollider,
                            RestoreAtRealtime = restoreAt
                        });
                }
            }
        }

        private IgnoredCollisionPair FindIgnoredPair(
            Collider canCollider,
            Collider playerCollider)
        {
            for (int i = 0; i < ignoredCollisionPairs.Count; i++)
            {
                IgnoredCollisionPair pair = ignoredCollisionPairs[i];
                if (pair.CanCollider == canCollider &&
                    pair.PlayerCollider == playerCollider)
                {
                    return pair;
                }
            }

            return null;
        }

        private void RestoreExpiredCollisions()
        {
            float realtimeNow = Time.realtimeSinceStartup;
            for (int i = ignoredCollisionPairs.Count - 1; i >= 0; i--)
            {
                IgnoredCollisionPair pair = ignoredCollisionPairs[i];
                if (pair.RestoreAtRealtime <= realtimeNow)
                {
                    RestoreIgnoredPair(pair);
                    ignoredCollisionPairs.RemoveAt(i);
                }
            }
        }

        private void RestoreIgnoredCollisionsFor(
            PetrolCanInteractable petrolCan)
        {
            for (int i = ignoredCollisionPairs.Count - 1; i >= 0; i--)
            {
                IgnoredCollisionPair pair = ignoredCollisionPairs[i];
                if (petrolCan.OwnsCollider(pair.CanCollider))
                {
                    RestoreIgnoredPair(pair);
                    ignoredCollisionPairs.RemoveAt(i);
                }
            }
        }

        private void RestoreAllIgnoredCollisions()
        {
            for (int i = ignoredCollisionPairs.Count - 1; i >= 0; i--)
            {
                RestoreIgnoredPair(ignoredCollisionPairs[i]);
            }

            ignoredCollisionPairs.Clear();
        }

        private static void RestoreIgnoredPair(IgnoredCollisionPair pair)
        {
            if (pair != null &&
                pair.CanCollider != null &&
                pair.PlayerCollider != null)
            {
                Physics.IgnoreCollision(
                    pair.CanCollider,
                    pair.PlayerCollider,
                    false);
            }
        }

        private void OnValidate()
        {
            throwSpeed = Mathf.Max(0.1f, throwSpeed);
            trajectorySegments = Mathf.Clamp(
                trajectorySegments,
                4,
                96);
            trajectoryTimeStep = Mathf.Max(0.01f, trajectoryTimeStep);
            collisionIgnoreDuration = Mathf.Max(
                0f,
                collisionIgnoreDuration);
            pickupAimDelay = Mathf.Max(0f, pickupAimDelay);

            if (holdPoint != null)
            {
                holdPoint.localPosition = holdLocalPosition;
            }
        }
    }
}
