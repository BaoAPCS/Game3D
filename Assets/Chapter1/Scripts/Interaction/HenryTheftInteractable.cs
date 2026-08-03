using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryTheftInteractable : Chapter1Interactable
    {
        public const string HenryObjectName =
            "henry_animated_cartoon_character";
        public const string TheftBatteryObjectName =
            "battery";
        public const string FoodcartObjectName =
            "avika_street_food_cart";
        public const string TheftPrompt =
            "[E] L\u1EA5y ắc quy c\u1EE7a Henry";

        private const string HoldSwapPrompt =
            "[Gi\u1EEF E] \u0110\u00E1nh tr\u00E1o ắc quy c\u1EE7a Henry";
        private const string NewBatteryLabel = "Ắc quy m\u1EDBi";

        internal const string InteractableLayerName = "Interactable";

        private const string InteractionTriggerName =
            "Henry_Theft_Interaction";

        [SerializeField, Min(0.01f)] private float proximityPadding = 0.15f;
        [SerializeField, Min(0.5f)] private float batterySwapDuration = 3.5f;
        [SerializeField] private SphereCollider interactionTrigger;
        [SerializeField] private HenryChaseController chaseController;

        private bool theftCommitted;
        private bool swapInProgress;
        private float swapEndsAt;
        private Coroutine swapRoutine;
        private Chapter1InputReader swapInputReader;
        private Transform swapPlayer;
        private GameObject swapPlayerObject;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;
        public bool TheftCommitted => theftCommitted;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallHenryEncounter(scene);
        }

        private static void InstallHenryEncounter(Scene scene)
        {
            int interactableLayer = LayerMask.NameToLayer(
                InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogError(
                    $"[Henry] Layer '{InteractableLayerName}' không tồn tại.");
                return;
            }

            Transform henry = null;
            Transform theftBattery = null;
            Transform foodcart = null;
            Transform[] transforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate.gameObject.scene != scene)
                {
                    continue;
                }

                if (string.Equals(
                        candidate.name,
                        HenryObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    henry = candidate;
                }
                else if (string.Equals(
                             candidate.name,
                             TheftBatteryObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    theftBattery = candidate;
                }
                else if (string.Equals(
                             candidate.name,
                             FoodcartObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    foodcart = candidate;
                }
            }

            if (henry == null)
            {
                return;
            }

            HenryChaseController chase = EnsureHenryController(
                henry.gameObject);
            if (chase == null)
            {
                return;
            }

            if (theftBattery == null)
            {
                Debug.LogError(
                    $"[Henry] Không tìm thấy pin '{TheftBatteryObjectName}'.",
                    henry);
            }
            else
            {
                InstallOnTheftBattery(
                    theftBattery.gameObject,
                    interactableLayer,
                    chase);
            }

            if (foodcart == null)
            {
                Debug.LogError(
                    $"[Henry] Không tìm thấy foodcart '{FoodcartObjectName}'.",
                    henry);
            }
            else
            {
                HenryFoodcartInteractable.InstallOnFoodcart(
                    foodcart.gameObject,
                    interactableLayer,
                    chase);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            if (!Mission2HeistProgress.HasBrokenBattery)
            {
                return NewBatteryLabel;
            }

            if (swapInProgress)
            {
                int remainingSeconds = Mathf.Max(
                    1,
                    Mathf.CeilToInt(swapEndsAt - Time.time));
                return $"[Gi\u1EEF E] \u0110ang \u0111\u00E1nh tr\u00E1o ắc quy... {remainingSeconds}s";
            }

            return HoldSwapPrompt;
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (theftCommitted ||
                Mission2HeistProgress.HasHenryBattery ||
                !base.CanInteract(context) ||
                context.PlayerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            return chaseController != null &&
                   IsWithinInteractionRange(
                       context,
                       interactionTrigger,
                       proximityPadding);
        }

        public override Transform GetInteractionTransform()
        {
            ResolveReferences();
            return interactionTrigger != null
                ? interactionTrigger.transform
                : transform;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            if (theftCommitted ||
                Mission2HeistProgress.HasHenryBattery ||
                context.PlayerTransform == null)
            {
                return InteractionResult.Ignored();
            }

            if (!Mission2HeistProgress.HasBrokenBattery)
            {
                return InteractionResult.Ignored();
            }

            ResolveReferences();
            if (chaseController == null)
            {
                return InteractionResult.Failed(
                    "Henry ch\u01B0a s\u1EB5n s\u00E0ng.");
            }

            if (swapInProgress)
            {
                return InteractionResult.Ignored();
            }

            Chapter1InputReader inputReader = context.PlayerObject != null
                ? context.PlayerObject.GetComponent<Chapter1InputReader>()
                : null;
            if (inputReader == null || !inputReader.TalkHeld)
            {
                return InteractionResult.Failed(
                    "H\u00E3y gi\u1EEF ph\u00EDm E li\u00EAn t\u1EE5c \u0111\u1EC3 \u0111\u00E1nh tr\u00E1o ắc quy.");
            }

            if (!chaseController.IsDistracting &&
                !chaseController.IsBatterySwapRaceActive &&
                !chaseController.BeginBatterySwapRace(
                    context.PlayerTransform))
            {
                chaseController.BeginForcedCatch(context.PlayerTransform);
                return InteractionResult.Succeeded();
            }

            StartBatterySwap(context, inputReader);
            return InteractionResult.Succeeded();
        }

        protected override void OnDisable()
        {
            CancelBatterySwap(true, false);
            base.OnDisable();
        }

        private void StartBatterySwap(
            InteractionContext context,
            Chapter1InputReader inputReader)
        {
            swapInProgress = true;
            swapEndsAt = Time.time + batterySwapDuration;
            swapInputReader = inputReader;
            swapPlayer = context.PlayerTransform;
            swapPlayerObject = context.PlayerObject;
            swapInputReader.TalkReleased += HandleSwapTalkReleased;
            swapRoutine = StartCoroutine(BatterySwapRoutine());
        }

        private IEnumerator BatterySwapRoutine()
        {
            while (Time.time < swapEndsAt)
            {
                if (swapInputReader == null ||
                    !swapInputReader.TalkHeld ||
                    swapPlayer == null ||
                    !IsWithinInteractionRange(
                        swapPlayer,
                        swapPlayerObject,
                        interactionTrigger,
                        proximityPadding))
                {
                    CancelBatterySwap(false, true);
                    yield break;
                }

                if (!CanContinueBatterySwap())
                {
                    CatchPlayerDuringSwap();
                    yield break;
                }

                yield return null;
            }

            if (swapInputReader == null ||
                !swapInputReader.TalkHeld ||
                swapPlayer == null ||
                !IsWithinInteractionRange(
                    swapPlayer,
                    swapPlayerObject,
                    interactionTrigger,
                    proximityPadding))
            {
                CancelBatterySwap(false, true);
                yield break;
            }

            if (!CanContinueBatterySwap())
            {
                CatchPlayerDuringSwap();
                yield break;
            }

            if (!CommitBatterySwap(
                    "\u0110\u00E1nh tr\u00E1o ắc quy th\u00E0nh c\u00F4ng. H\u00E3y quay v\u1EC1 nh\u1EB7t UPS."))
            {
                ClearSwapState(false);
                Chapter1EventBus.RaiseNotification(
                    "Kh\u00F4ng th\u1EC3 ho\u00E0n t\u1EA5t vi\u1EC7c \u0111\u00E1nh tr\u00E1o ắc quy.");
            }
        }

        private bool CanContinueBatterySwap()
        {
            if (chaseController == null)
            {
                return false;
            }

            if (chaseController.IsReturningFromFoodcart &&
                !chaseController.BeginBatterySwapRace(swapPlayer))
            {
                return false;
            }

            return chaseController.IsDistracting ||
                   chaseController.IsBatterySwapRaceActive;
        }

        private bool CommitBatterySwap(string notification = "")
        {
            if (!Mission2HeistProgress.CompleteBatterySwap())
            {
                return false;
            }

            Mission2HeistProgress.PlaceBrokenBatteryAt(transform);
            chaseController?.CompleteBatterySwapRace();
            ClearSwapState(false);
            theftCommitted = true;
            if (interactionTrigger != null)
            {
                interactionTrigger.enabled = false;
            }

            DisableInteraction();
            if (!string.IsNullOrWhiteSpace(notification))
            {
                Chapter1EventBus.RaiseNotification(notification);
            }

            gameObject.SetActive(false);
            return true;
        }

        private void CatchPlayerDuringSwap()
        {
            Transform caughtPlayer = swapPlayer;
            ClearSwapState(false);

            if (chaseController != null)
            {
                chaseController.BeginForcedCatch(caughtPlayer);
            }
            else
            {
                Chapter1EventBus.RaisePlayerCaught();
            }
        }

        private void HandleSwapTalkReleased()
        {
            if (swapInProgress)
            {
                CancelBatterySwap(true, true);
            }
        }

        private void CancelBatterySwap(
            bool stopCoroutine,
            bool showNotification)
        {
            bool wasBatterySwapRace =
                chaseController != null &&
                chaseController.IsBatterySwapRaceActive;
            ClearSwapState(stopCoroutine);

            if (wasBatterySwapRace)
            {
                chaseController.CancelBatterySwapRace();
            }

            if (showNotification)
            {
                Chapter1EventBus.RaiseNotification(
                    "\u0110\u00E3 h\u1EE7y \u0111\u00E1nh tr\u00E1o ắc quy.");
            }
        }

        private void ClearSwapState(bool stopCoroutine)
        {
            Coroutine activeRoutine = swapRoutine;
            Chapter1InputReader activeInputReader = swapInputReader;
            swapRoutine = null;
            swapInProgress = false;
            swapEndsAt = 0f;
            swapInputReader = null;
            swapPlayer = null;
            swapPlayerObject = null;

            if (activeInputReader != null)
            {
                activeInputReader.TalkReleased -= HandleSwapTalkReleased;
            }

            if (stopCoroutine && activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }
        }

        internal static SphereCollider CreateInteractionTrigger(
            Transform owner,
            string triggerName,
            int interactableLayer,
            float minimumRadius,
            float maximumRadius)
        {
            Transform triggerTransform = owner.Find(triggerName);
            if (triggerTransform == null)
            {
                GameObject triggerObject = new GameObject(triggerName);
                triggerTransform = triggerObject.transform;
                triggerTransform.SetParent(owner, true);
            }

            Bounds bounds = GetWorldBounds(owner);
            triggerTransform.position = bounds.center;
            triggerTransform.rotation = Quaternion.identity;
            triggerTransform.localScale = Vector3.one;
            triggerTransform.gameObject.layer = interactableLayer;

            SphereCollider trigger =
                triggerTransform.GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = triggerTransform.gameObject
                    .AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.radius = Mathf.Clamp(
                Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.2f,
                minimumRadius,
                maximumRadius);
            return trigger;
        }

        internal static bool IsWithinInteractionRange(
            InteractionContext context,
            Collider trigger,
            float extraRange)
        {
            return IsWithinInteractionRange(
                context.PlayerTransform,
                context.PlayerObject,
                trigger,
                extraRange);
        }

        internal static bool IsWithinInteractionRange(
            Transform playerTransform,
            GameObject playerObject,
            Collider trigger,
            float extraRange)
        {
            if (trigger == null || playerTransform == null)
            {
                return false;
            }

            Vector3 playerPosition = playerTransform.position;
            Vector3 closestPoint = trigger.ClosestPoint(playerPosition);
            playerPosition.y = 0f;
            closestPoint.y = 0f;

            CharacterController playerController =
                playerObject != null
                    ? playerObject.GetComponent<CharacterController>()
                    : null;
            float playerRadius = playerController != null
                ? playerController.radius
                : 0f;
            float maximumDistance = extraRange + playerRadius;
            return (playerPosition - closestPoint).sqrMagnitude <=
                   maximumDistance * maximumDistance;
        }

        private static HenryChaseController EnsureHenryController(
            GameObject henry)
        {
            HenryRunAnimationPlayer animationPlayer =
                henry.GetComponent<HenryRunAnimationPlayer>();
            if (animationPlayer == null)
            {
                henry.AddComponent<HenryRunAnimationPlayer>();
            }

            HenryChaseController chase =
                henry.GetComponent<HenryChaseController>();
            if (chase == null)
            {
                chase = henry.AddComponent<HenryChaseController>();
            }

            return chase;
        }

        private static void InstallOnTheftBattery(
            GameObject theftBattery,
            int interactableLayer,
            HenryChaseController chase)
        {
            SphereCollider trigger = CreateInteractionTrigger(
                theftBattery.transform,
                InteractionTriggerName,
                interactableLayer,
                0.45f,
                0.65f);

            HenryTheftInteractable interactable =
                theftBattery.GetComponent<HenryTheftInteractable>();
            if (interactable == null)
            {
                interactable = theftBattery.AddComponent<HenryTheftInteractable>();
            }

            interactable.interactionTrigger = trigger;
            interactable.chaseController = chase;
            interactable.ResolveReferences();
        }

        private static Bounds GetWorldBounds(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(root.position, Vector3.zero);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
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

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
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

        private void ResolveReferences()
        {
            if (interactionTrigger == null)
            {
                Transform triggerTransform = transform.Find(
                    InteractionTriggerName);
                if (triggerTransform != null)
                {
                    interactionTrigger = triggerTransform
                        .GetComponent<SphereCollider>();
                }
            }

            if (chaseController == null)
            {
                chaseController = FindAnyObjectByType<
                    HenryChaseController>();
            }
        }

        private void OnValidate()
        {
            proximityPadding = Mathf.Max(0.01f, proximityPadding);
            batterySwapDuration = Mathf.Max(0.5f, batterySwapDuration);
            ResolveReferences();
        }
    }
}

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryFoodcartInteractable : Chapter1Interactable
    {
        private const string InteractionTriggerName =
            "Henry_Foodcart_Interaction";

        [SerializeField, Min(0.01f)] private float proximityPadding = 0.15f;
        [SerializeField] private SphereCollider interactionTrigger;
        [SerializeField] private HenryChaseController chaseController;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return "[E] N\u01B0\u1EDBng th\u1ECBt";
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (!base.CanInteract(context) ||
                context.PlayerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            return chaseController != null &&
                   chaseController.CanStartDistraction &&
                   HenryTheftInteractable.IsWithinInteractionRange(
                       context,
                       interactionTrigger,
                       proximityPadding);
        }

        public override Transform GetInteractionTransform()
        {
            ResolveReferences();
            return interactionTrigger != null
                ? interactionTrigger.transform
                : transform;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            ResolveReferences();
            if (chaseController == null ||
                !chaseController.BeginFoodcartDistraction(transform))
            {
                return InteractionResult.Failed(
                    "Henry chưa thể bị đánh lạc hướng lúc này.");
            }

            return InteractionResult.Succeeded(
                "Henry \u0111ang ch\u1EA1y t\u1EDBi qu\u1EA7y \u0111\u1ED3 \u0103n");
        }

        internal static void InstallOnFoodcart(
            GameObject foodcart,
            int interactableLayer,
            HenryChaseController chase)
        {
            SphereCollider trigger =
                HenryTheftInteractable.CreateInteractionTrigger(
                    foodcart.transform,
                    InteractionTriggerName,
                    interactableLayer,
                    1f,
                    1.6f);

            HenryFoodcartInteractable interactable =
                foodcart.GetComponent<HenryFoodcartInteractable>();
            if (interactable == null)
            {
                interactable = foodcart.AddComponent<
                    HenryFoodcartInteractable>();
            }

            interactable.interactionTrigger = trigger;
            interactable.chaseController = chase;
            interactable.ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (interactionTrigger == null)
            {
                Transform triggerTransform = transform.Find(
                    InteractionTriggerName);
                if (triggerTransform != null)
                {
                    interactionTrigger = triggerTransform
                        .GetComponent<SphereCollider>();
                }
            }

            if (chaseController == null)
            {
                chaseController = FindAnyObjectByType<
                    HenryChaseController>();
            }
        }

        private void OnValidate()
        {
            proximityPadding = Mathf.Max(0.01f, proximityPadding);
            ResolveReferences();
        }
    }
}
