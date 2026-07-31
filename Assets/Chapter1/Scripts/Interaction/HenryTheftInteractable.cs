using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class HenryTheftInteractable : Chapter1Interactable
    {
        public const string HenryObjectName =
            "henry_animated_cartoon_character";
        public const string TheftTableObjectName =
            "FFK_TableFreestanding_01_01";
        public const string FoodcartObjectName =
            "avika_street_food_cart";
        public const string TheftPrompt = "[E] Ăn trộm";

        internal const string InteractableLayerName = "Interactable";

        private const string InteractionTriggerName =
            "Henry_Theft_Interaction";

        [SerializeField, Min(0.01f)] private float proximityPadding = 0.15f;
        [SerializeField] private SphereCollider interactionTrigger;
        [SerializeField] private HenryChaseController chaseController;

        private bool theftCommitted;

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
            Transform theftTable = null;
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
                             TheftTableObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    theftTable = candidate;
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

            if (theftTable == null)
            {
                Debug.LogError(
                    $"[Henry] Không tìm thấy bàn '{TheftTableObjectName}'.",
                    henry);
            }
            else
            {
                InstallOnTheftTable(
                    theftTable.gameObject,
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
            return TheftPrompt;
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (theftCommitted ||
                !base.CanInteract(context) ||
                context.PlayerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            return IsWithinInteractionRange(
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
            if (theftCommitted || context.PlayerTransform == null)
            {
                return InteractionResult.Ignored();
            }

            ResolveReferences();
            if (chaseController == null)
            {
                return InteractionResult.Failed(
                    "Henry chưa sẵn sàng.");
            }

            bool stolenDuringDistraction =
                chaseController.IsDistracting;
            if (!stolenDuringDistraction &&
                !chaseController.BeginChase(context.PlayerTransform))
            {
                return InteractionResult.Failed(
                    "Henry chưa thể bắt đầu truy đuổi.");
            }

            theftCommitted = true;
            if (interactionTrigger != null)
            {
                interactionTrigger.enabled = false;
            }

            DisableInteraction();
            return stolenDuringDistraction
                ? InteractionResult.Succeeded(
                    "Henry đang bị đánh lạc hướng. Bạn đã ăn trộm an toàn.")
                : InteractionResult.Succeeded();
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
            if (trigger == null || context.PlayerTransform == null)
            {
                return false;
            }

            Vector3 playerPosition = context.PlayerTransform.position;
            Vector3 closestPoint = trigger.ClosestPoint(playerPosition);
            playerPosition.y = 0f;
            closestPoint.y = 0f;

            CharacterController playerController =
                context.PlayerObject != null
                    ? context.PlayerObject.GetComponent<CharacterController>()
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

        private static void InstallOnTheftTable(
            GameObject theftTable,
            int interactableLayer,
            HenryChaseController chase)
        {
            SphereCollider trigger = CreateInteractionTrigger(
                theftTable.transform,
                InteractionTriggerName,
                interactableLayer,
                0.85f,
                1.35f);

            HenryTheftInteractable interactable =
                theftTable.GetComponent<HenryTheftInteractable>();
            if (interactable == null)
            {
                interactable = theftTable.AddComponent<HenryTheftInteractable>();
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
            return "[E] Nướng thịt";
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

            return InteractionResult.Succeeded();
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
