using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Chapter1InputReader))]
    [RequireComponent(typeof(PlayerInputLock))]
    public sealed class Chapter1InteractionController : MonoBehaviour
    {
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Chapter1Manager chapterManager;
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private float sphereRadius = 0.2f;
        [SerializeField] private LayerMask interactionMask;
        [SerializeField] private LayerMask obstructionMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private float interactionCooldown = 0.1f;
        [SerializeField] private bool drawDebugGizmos;
        [SerializeField] private bool showInteractionDebug;
        [SerializeField] private bool logFocusChanges;

        private readonly Collider[] overlapBuffer = new Collider[24];
        private IChapter1Interactable currentFocusedInteractable;
        private IChapter1Interactable currentInteractTarget;
        private IChapter1Interactable currentTalkTarget;
        private string currentPrompt = string.Empty;
        private float nextAllowedInteractionTime;
        private int lastInteractionFrame = -1;

        private struct InteractionCandidate
        {
            public IChapter1Interactable Interactable;
            public string Prompt;
            public float Distance;
            public float Priority;
        }

        public event Action<IChapter1Interactable> FocusChanged;
        public event Action<string> PromptChanged;
        public event Action<IChapter1Interactable, InteractionResult> InteractionPerformed;

        public IChapter1Interactable CurrentFocusedInteractable => currentFocusedInteractable;
        public string CurrentPrompt => currentPrompt;
        public bool HasGameplayCamera => gameplayCamera != null;
        public Camera GameplayCamera => gameplayCamera;
        public int InteractionMaskValue => interactionMask.value;
        public float InteractionDistance => interactionDistance;
        public float SphereRadius => sphereRadius;
        public bool ShowInteractionDebug => showInteractionDebug;
        public string CurrentTargetName { get; private set; } = string.Empty;
        public string LastHitName { get; private set; } = string.Empty;
        public float LastHitDistance { get; private set; }

        private void Awake()
        {
            ResolveLocalReferences();
            if (chapterManager == null)
            {
                chapterManager = Chapter1Manager.Instance;
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            ResolveLocalReferences();
            if (inputReader != null)
            {
                inputReader.InteractPressed += HandleInteractPressed;
                inputReader.TalkPressed += HandleTalkPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.InteractPressed -= HandleInteractPressed;
                inputReader.TalkPressed -= HandleTalkPressed;
            }

            currentInteractTarget = null;
            currentTalkTarget = null;
            SetFocusedInteractable(null, string.Empty);
        }

        private void Update()
        {
            ScanForInteractable();
        }

        private void OnValidate()
        {
            interactionDistance = Mathf.Max(0.1f, interactionDistance);
            sphereRadius = Mathf.Max(0.01f, sphereRadius);
            interactionCooldown = Mathf.Max(0f, interactionCooldown);
        }

        public void SetGameplayCamera(Camera camera)
        {
            gameplayCamera = camera;
        }

        public void SetChapterManager(Chapter1Manager manager)
        {
            chapterManager = manager;
        }

        public void SetInventory(PlayerInventory playerInventory)
        {
            inventory = playerInventory;
        }

        public void SetReferences(Chapter1InputReader reader, PlayerInputLock lockReference, PlayerInventory playerInventory, Chapter1Manager manager)
        {
            inputReader = reader;
            inputLock = lockReference;
            inventory = playerInventory;
            chapterManager = manager;
        }

        private void ResolveLocalReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<Chapter1InputReader>();
            }

            if (inputLock == null)
            {
                inputLock = GetComponent<PlayerInputLock>();
            }

            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
            }
        }

        private void ScanForInteractable()
        {
            Vector3 origin = transform.position;
            InteractionContext context = CreateContext();
            InteractionCandidate interactCandidate = default;
            InteractionCandidate talkCandidate = default;
            Collider nearestCandidateCollider = null;
            float nearestCandidateDistance = float.MaxValue;

            int hitCount = Physics.OverlapSphereNonAlloc(origin, interactionDistance, overlapBuffer, interactionMask, triggerInteraction);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = overlapBuffer[i];
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                IChapter1Interactable interactable = hitCollider.GetComponentInParent<IChapter1Interactable>();
                if (interactable == null)
                {
                    continue;
                }

                Transform interactionTransform = interactable.GetInteractionTransform();
                Vector3 targetPosition = interactionTransform != null ? interactionTransform.position : hitCollider.bounds.center;
                float targetDistance = Vector3.Distance(origin, targetPosition);
                if (targetDistance < nearestCandidateDistance)
                {
                    nearestCandidateCollider = hitCollider;
                    nearestCandidateDistance = targetDistance;
                }

                if (!interactable.IsInteractionEnabled || !interactable.CanInteract(context))
                {
                    continue;
                }

                float priority = interactable is Chapter1Interactable chapterInteractable ? chapterInteractable.InteractionPriority : 0f;
                if (interactable.InteractionInput == Chapter1InteractionInput.Talk)
                {
                    ConsiderCandidate(
                        ref talkCandidate,
                        interactable,
                        context,
                        targetPosition,
                        targetDistance,
                        priority);
                }
                else
                {
                    ConsiderCandidate(
                        ref interactCandidate,
                        interactable,
                        context,
                        targetPosition,
                        targetDistance,
                        priority);
                }
            }

            currentInteractTarget = interactCandidate.Interactable;
            currentTalkTarget = talkCandidate.Interactable;

            InteractionCandidate displayedCandidate =
                talkCandidate.Interactable != null
                    ? talkCandidate
                    : interactCandidate;
            string combinedPrompt = CombinePrompts(
                talkCandidate.Prompt,
                interactCandidate.Prompt);

            SetLastHit(nearestCandidateCollider, nearestCandidateDistance);
            SetFocusedInteractable(
                displayedCandidate.Interactable,
                combinedPrompt);
        }

        private static string CombinePrompts(
            string talkPrompt,
            string interactPrompt)
        {
            bool hasTalkPrompt = !string.IsNullOrWhiteSpace(talkPrompt);
            bool hasInteractPrompt =
                !string.IsNullOrWhiteSpace(interactPrompt);

            if (hasTalkPrompt && hasInteractPrompt)
            {
                return $"{talkPrompt}\n{interactPrompt}";
            }

            if (hasTalkPrompt)
            {
                return talkPrompt;
            }

            return hasInteractPrompt ? interactPrompt : string.Empty;
        }

        private void ConsiderCandidate(
            ref InteractionCandidate bestCandidate,
            IChapter1Interactable interactable,
            InteractionContext context,
            Vector3 targetPosition,
            float targetDistance,
            float priority)
        {
            bool nearer = targetDistance < bestCandidate.Distance;
            bool sameDistanceHigherPriority =
                Mathf.Approximately(targetDistance, bestCandidate.Distance) &&
                priority > bestCandidate.Priority;

            if (bestCandidate.Interactable != null &&
                !nearer &&
                !sameDistanceHigherPriority)
            {
                return;
            }

            bool closePickup =
                interactable is UPSInteractable &&
                targetDistance <= 1.5f;
            if (!closePickup && IsObstructed(targetPosition))
            {
                return;
            }

            bestCandidate.Interactable = interactable;
            bestCandidate.Prompt = interactable.GetInteractionPrompt(context);
            bestCandidate.Distance = targetDistance;
            bestCandidate.Priority = priority;
        }

        private bool IsObstructed(Vector3 targetPosition)
        {
            Vector3 origin = gameplayCamera != null ? gameplayCamera.transform.position : transform.position + Vector3.up * 1.4f;
            float targetDistance = Vector3.Distance(origin, targetPosition);
            if (obstructionMask.value == 0 || targetDistance <= 0.01f)
            {
                return false;
            }

            Vector3 direction = (targetPosition - origin).normalized;
            return Physics.Raycast(origin, direction, targetDistance - 0.01f, obstructionMask, QueryTriggerInteraction.Ignore);
        }

        private void SetFocusedInteractable(IChapter1Interactable interactable, string prompt)
        {
            prompt ??= string.Empty;
            if (ReferenceEquals(currentFocusedInteractable, interactable) && string.Equals(currentPrompt, prompt, StringComparison.Ordinal))
            {
                return;
            }

            string previousTargetName = CurrentTargetName;
            if (currentFocusedInteractable is Chapter1Interactable previousInteractable)
            {
                previousInteractable.SetFocused(false);
            }

            currentFocusedInteractable = interactable;
            currentPrompt = prompt;
            CurrentTargetName = GetInteractableName(currentFocusedInteractable);

            if (currentFocusedInteractable is Chapter1Interactable nextInteractable)
            {
                nextInteractable.SetFocused(true);
            }

            if (logFocusChanges)
            {
                string nextTargetName = string.IsNullOrEmpty(CurrentTargetName) ? "<none>" : CurrentTargetName;
                string oldTargetName = string.IsNullOrEmpty(previousTargetName) ? "<none>" : previousTargetName;
                Debug.Log($"[Chapter1InteractionController] Focus changed: {oldTargetName} -> {nextTargetName}.", this);
            }

            FocusChanged?.Invoke(currentFocusedInteractable);
            PromptChanged?.Invoke(currentPrompt);
        }

        private void SetLastHit(Collider hitCollider, float hitDistance)
        {
            LastHitName = hitCollider != null ? hitCollider.gameObject.name : string.Empty;
            LastHitDistance = hitCollider != null ? hitDistance : 0f;
        }

        private static string GetInteractableName(IChapter1Interactable interactable)
        {
            return interactable is Component component ? component.gameObject.name : string.Empty;
        }

        private void HandleInteractPressed()
        {
            TryPerformInteraction(Chapter1InteractionInput.Interact);
        }

        private void HandleTalkPressed()
        {
            TryPerformInteraction(Chapter1InteractionInput.Talk);
        }

        private void TryPerformInteraction(Chapter1InteractionInput pressedInput)
        {
            IChapter1Interactable target = pressedInput ==
                Chapter1InteractionInput.Talk
                    ? currentTalkTarget
                    : currentInteractTarget;

            if (target == null)
            {
                return;
            }

            if (lastInteractionFrame == Time.frameCount || Time.unscaledTime < nextAllowedInteractionTime)
            {
                return;
            }

            lastInteractionFrame = Time.frameCount;
            nextAllowedInteractionTime = Time.unscaledTime + interactionCooldown;

            if (inputLock != null && inputLock.IsLocked)
            {
                Chapter1EventBus.RaiseNotification("Không thể tương tác lúc này.");
                return;
            }

            InteractionContext context = CreateContext();
            InteractionResult result = target.Interact(context);
            if (!result.ConsumeInteractionInput)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                Chapter1EventBus.RaiseNotification(result.Message);
            }

            InteractionPerformed?.Invoke(target, result);
            ScanForInteractable();
        }

        private InteractionContext CreateContext()
        {
            return new InteractionContext(gameObject, transform, inventory, chapterManager, this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos && !showInteractionDebug)
            {
                return;
            }

            Vector3 origin = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, interactionDistance);

            if (currentFocusedInteractable != null)
            {
                Transform target = currentFocusedInteractable.GetInteractionTransform();
                if (target != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(origin, target.position);
                    Gizmos.DrawWireSphere(target.position, Mathf.Max(0.15f, sphereRadius * 1.5f));
                }
            }
        }
    }
}
