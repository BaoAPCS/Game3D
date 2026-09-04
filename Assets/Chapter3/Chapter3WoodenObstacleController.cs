using System;
using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using DormitoryMystery.Chapter2;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter3
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Chapter3WoodenObstacleController :
        MonoBehaviour,
        IInventoryItemUseHandler
    {
        public const string ObstacleObjectName = "Wooden_obstacle";
        public const string CrowbarItemId =
            Chapter2ServiceCardMission.CrowbarItemId;
        public const string BlockedPrompt = "Lối đi bị chặn";
        public const string PryPrompt = "[Enter] Gỡ vật cản";

        private const string InputLockReason =
            "Chapter3.WoodenObstacle";

        [SerializeField, Range(0.01f, 1f)]
        private float pryProgressPerPress = 0.06f;
        [SerializeField, Min(0f)]
        private float pryDecayPerSecond = 0.42f;

        private readonly HashSet<Collider> playerColliders =
            new HashSet<Collider>();

        private SphereCollider triggerZone;
        private Chapter1InputReader playerInputReader;
        private PlayerInputLock inputLock;
        private BackpackPhoneInputController backpackInput;
        private InventoryUIController inventoryUI;
        private InventoryUIController registeredInventoryUI;
        private Chapter3WoodenObstacleHUD obstacleHUD;

        private float pryProgress;
        private bool isPrying;
        private bool completed;
        private bool ownInputLockHeld;
        private bool modalStateCaptured;
        private bool backpackWasEnabled;

        public float PryProgress => pryProgress;
        public bool IsPrying => isPrying;
        public bool IsCompleted => completed;
        public bool ContainsPlayer => playerColliders.Count > 0;

        internal static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != Chapter3CarryOverBootstrap.Chapter3SceneName)
            {
                return;
            }

            GameObject obstacle = FindSceneObject(
                scene,
                ObstacleObjectName);
            if (obstacle == null)
            {
                Debug.LogWarning(
                    $"[Chapter3WoodenObstacle] Không tìm thấy '{ObstacleObjectName}' trong scene {scene.name}.");
                return;
            }

            SphereCollider zone =
                obstacle.GetComponent<SphereCollider>();
            if (zone == null)
            {
                zone = obstacle.AddComponent<SphereCollider>();
            }

            zone.isTrigger = true;
            zone.enabled = true;

            Chapter3WoodenObstacleController controller =
                obstacle.GetComponent<
                    Chapter3WoodenObstacleController>();
            if (controller == null)
            {
                controller = obstacle.AddComponent<
                    Chapter3WoodenObstacleController>();
            }

            controller.Configure(zone);
        }

        public void Configure(SphereCollider zone)
        {
            triggerZone = zone != null
                ? zone
                : GetComponent<SphereCollider>();
            if (triggerZone != null)
            {
                triggerZone.isTrigger = true;
                triggerZone.enabled = true;
            }

            EnsureHUD();
        }

        private void Awake()
        {
            triggerZone = GetComponent<SphereCollider>();
            EnsureHUD();
        }

        private void OnEnable()
        {
            EnsureHUD();
        }

        private void Update()
        {
            RemoveMissingPlayerColliders();

            if (completed)
            {
                return;
            }

            if (isPrying)
            {
                UpdatePryChallenge();
                return;
            }

            RefreshInventoryHandler();
            RefreshWorldPrompt();
        }

        private void LateUpdate()
        {
            if (!isPrying ||
                (inventoryUI != null && inventoryUI.IsOpen))
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            ClearInventoryHandler();
            ReleaseModalState();
            obstacleHUD?.HideAll();
            playerColliders.Clear();
        }

        private void OnValidate()
        {
            pryProgressPerPress = Mathf.Clamp(
                pryProgressPerPress,
                0.01f,
                1f);
            pryDecayPerSecond = Mathf.Max(
                0f,
                pryDecayPerSecond);
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterPlayerCollider(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // This also handles the case where this controller is installed
            // while Nam is already standing inside the existing trigger.
            RegisterPlayerCollider(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !playerColliders.Remove(other))
            {
                return;
            }

            if (ContainsPlayer)
            {
                return;
            }

            if (isPrying)
            {
                CancelPryChallenge();
            }

            ClearInventoryHandler();
            obstacleHUD?.HideAll();
        }

        public bool CanUseInventoryItem(InventoryItem item)
        {
            return !completed &&
                   !isPrying &&
                   ContainsPlayer &&
                   item != null &&
                   string.Equals(
                       item.ItemId,
                       CrowbarItemId,
                       StringComparison.OrdinalIgnoreCase);
        }

        public bool TryUseInventoryItem(InventoryItem item)
        {
            if (!CanUseInventoryItem(item))
            {
                return false;
            }

            BeginPryChallenge();
            return true;
        }

        public static float ApplyPryPress(
            float currentProgress,
            float progressPerPress)
        {
            return Mathf.Clamp01(
                currentProgress + Mathf.Max(0f, progressPerPress));
        }

        public static float ApplyPryDecay(
            float currentProgress,
            float decayPerSecond,
            float deltaTime)
        {
            return Mathf.Clamp01(
                currentProgress -
                Mathf.Max(0f, decayPerSecond) *
                Mathf.Max(0f, deltaTime));
        }

        private void RegisterPlayerCollider(Collider other)
        {
            Chapter1InputReader inputReader =
                other != null
                    ? other.GetComponentInParent<Chapter1InputReader>()
                    : null;
            if (inputReader == null)
            {
                return;
            }

            playerColliders.Add(other);
            CachePlayerReferences(inputReader);

            if (!isPrying)
            {
                RefreshInventoryHandler();
                RefreshWorldPrompt();
            }
        }

        private void CachePlayerReferences(
            Chapter1InputReader inputReader)
        {
            playerInputReader = inputReader;
            inputLock = playerInputReader.GetComponent<PlayerInputLock>();
            backpackInput = playerInputReader.GetComponent<
                BackpackPhoneInputController>();
            inventoryUI = backpackInput != null
                ? backpackInput.InventoryUIController
                : null;
            if (inventoryUI == null)
            {
                inventoryUI = FindSceneComponent<
                    InventoryUIController>(gameObject.scene);
            }
        }

        private void ResolveRuntimeReferences()
        {
            if (playerInputReader == null)
            {
                playerInputReader = FindSceneComponent<
                    Chapter1InputReader>(gameObject.scene);
            }

            if (playerInputReader != null)
            {
                if (inputLock == null)
                {
                    inputLock = playerInputReader.GetComponent<
                        PlayerInputLock>();
                }

                if (backpackInput == null)
                {
                    backpackInput = playerInputReader.GetComponent<
                        BackpackPhoneInputController>();
                }
            }

            if (inventoryUI == null && backpackInput != null)
            {
                inventoryUI = backpackInput.InventoryUIController;
            }

            if (inventoryUI == null)
            {
                inventoryUI = FindSceneComponent<
                    InventoryUIController>(gameObject.scene);
            }
        }

        private void RefreshInventoryHandler()
        {
            if (!ContainsPlayer || completed || isPrying)
            {
                ClearInventoryHandler();
                return;
            }

            ResolveRuntimeReferences();
            if (inventoryUI == null ||
                registeredInventoryUI == inventoryUI)
            {
                return;
            }

            ClearInventoryHandler();
            inventoryUI.SetItemUseHandler(this);
            registeredInventoryUI = inventoryUI;
        }

        private void ClearInventoryHandler()
        {
            if (registeredInventoryUI == null)
            {
                return;
            }

            registeredInventoryUI.ClearItemUseHandler(this);
            registeredInventoryUI = null;
        }

        private void RefreshWorldPrompt()
        {
            EnsureHUD();
            if (!ContainsPlayer || completed)
            {
                obstacleHUD?.HideAll();
                return;
            }

            ResolveRuntimeReferences();
            if (inventoryUI != null && inventoryUI.IsOpen)
            {
                obstacleHUD?.HideAll();
                return;
            }

            obstacleHUD?.SetPrompt(BlockedPrompt);
            obstacleHUD?.HideProgress();
        }

        private void BeginPryChallenge()
        {
            ResolveRuntimeReferences();
            ClearInventoryHandler();

            isPrying = true;
            pryProgress = 0f;
            CaptureModalState();

            EnsureHUD();
            obstacleHUD?.SetPrompt(PryPrompt);
            obstacleHUD?.SetProgress(pryProgress);
        }

        private void UpdatePryChallenge()
        {
            if (WasEnterPressedThisFrame())
            {
                pryProgress = ApplyPryPress(
                    pryProgress,
                    pryProgressPerPress);
            }
            else
            {
                pryProgress = ApplyPryDecay(
                    pryProgress,
                    pryDecayPerSecond,
                    Time.unscaledDeltaTime);
            }

            obstacleHUD?.SetPrompt(PryPrompt);
            obstacleHUD?.SetProgress(pryProgress);

            if (pryProgress >= 1f)
            {
                CompletePryChallenge();
            }
        }

        private void CompletePryChallenge()
        {
            if (!isPrying)
            {
                return;
            }

            isPrying = false;
            completed = true;
            pryProgress = 1f;

            ClearInventoryHandler();
            ReleaseModalState();
            obstacleHUD?.HideAll();
            Chapter1EventBus.RaiseNotification(
                "Đã gỡ vật cản. Lối đi đã được mở.");

            // Disabling the root removes all six visible planks and their
            // solid child colliders, so Nam can immediately walk through.
            gameObject.SetActive(false);
        }

        private void CancelPryChallenge()
        {
            if (!isPrying)
            {
                return;
            }

            isPrying = false;
            pryProgress = 0f;
            ReleaseModalState();
            obstacleHUD?.HideAll();
            RefreshInventoryHandler();
            RefreshWorldPrompt();
        }

        private void CaptureModalState()
        {
            if (modalStateCaptured)
            {
                return;
            }

            modalStateCaptured = true;
            backpackWasEnabled =
                backpackInput != null && backpackInput.enabled;

            if (inputLock != null)
            {
                inputLock.AcquireInputLock(InputLockReason);
                ownInputLockHeld = true;
            }

            if (backpackInput != null)
            {
                backpackInput.enabled = false;
            }
        }

        private void ReleaseModalState()
        {
            if (modalStateCaptured && backpackInput != null)
            {
                backpackInput.enabled = backpackWasEnabled;
            }

            if (ownInputLockHeld && inputLock != null)
            {
                inputLock.ReleaseInputLock(InputLockReason);
            }

            ownInputLockHeld = false;
            modalStateCaptured = false;

            if (inventoryUI == null || !inventoryUI.IsOpen)
            {
                Chapter1UICursorLock.ApplyAfterClose(inputLock);
            }
        }

        private void EnsureHUD()
        {
            if (obstacleHUD == null)
            {
                obstacleHUD = Chapter3WoodenObstacleHUD.Create(
                    transform);
            }
        }

        private void RemoveMissingPlayerColliders()
        {
            playerColliders.RemoveWhere(
                candidate => candidate == null);
        }

        private static bool WasEnterPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        private static GameObject FindSceneObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex].GetComponentsInChildren<Transform>(
                        true);
                for (int transformIndex = 0;
                     transformIndex < transforms.Length;
                     transformIndex++)
                {
                    Transform candidate = transforms[transformIndex];
                    if (candidate != null &&
                        string.Equals(
                            candidate.gameObject.name,
                            objectName,
                            StringComparison.Ordinal))
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] candidates = FindObjectsByType<T>(
                FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                T candidate = candidates[i];
                if (candidate != null &&
                    candidate.gameObject.scene == scene)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
