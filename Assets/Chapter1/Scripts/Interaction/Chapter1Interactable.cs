using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public abstract class Chapter1Interactable : MonoBehaviour, IChapter1Interactable
    {
        [SerializeField] private string displayName = "vật thể";
        [SerializeField] private string interactionVerb = "Tương tác";
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private bool oneShot;
        [SerializeField] private bool alreadyUsed;
        [SerializeField] private float interactionPriority;
        [SerializeField] private Chapter1Step requiredStep;
        [SerializeField] private bool requireExactStep;
        [SerializeField] private bool allowInteractionWithoutStepRequirement = true;
        [SerializeField] private string unavailableMessage = "Không thể tương tác lúc này.";
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private GameObject highlightObject;

        public bool IsInteractionEnabled => interactionEnabled && (!oneShot || !alreadyUsed);
        public float InteractionPriority => interactionPriority;

        protected virtual void Awake()
        {
            SetFocused(false);
        }

        protected virtual void OnDisable()
        {
            SetFocused(false);
        }

        public virtual string GetInteractionPrompt(InteractionContext context)
        {
            string safeVerb = string.IsNullOrWhiteSpace(interactionVerb) ? "Tương tác" : interactionVerb.Trim();
            string safeName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();
            return $"[F] {safeVerb} {safeName}";
        }

        public virtual bool CanInteract(InteractionContext context)
        {
            return IsInteractionEnabled && MeetsStepRequirement(context);
        }

        public InteractionResult Interact(InteractionContext context)
        {
            if (!interactionEnabled)
            {
                return InteractionResult.Failed(GetUnavailableMessage());
            }

            if (oneShot && alreadyUsed)
            {
                return InteractionResult.Ignored();
            }

            if (!MeetsStepRequirement(context))
            {
                return InteractionResult.Failed(GetUnavailableMessage());
            }

            InteractionResult result = PerformInteraction(context);
            if (result.Success && oneShot)
            {
                alreadyUsed = true;
                interactionEnabled = false;
                SetFocused(false);
            }

            return result;
        }

        public virtual Transform GetInteractionTransform()
        {
            return interactionPoint != null ? interactionPoint : transform;
        }

        public void EnableInteraction()
        {
            interactionEnabled = true;
        }

        public void DisableInteraction()
        {
            interactionEnabled = false;
            SetFocused(false);
        }

        public void ResetInteraction()
        {
            alreadyUsed = false;
            interactionEnabled = true;
            SetFocused(false);
        }

        public void SetFocused(bool focused)
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(focused && IsInteractionEnabled);
            }
        }

        protected virtual InteractionResult PerformInteraction(InteractionContext context)
        {
            return InteractionResult.Ignored();
        }

        protected string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();
        }

        private bool MeetsStepRequirement(InteractionContext context)
        {
            if (allowInteractionWithoutStepRequirement)
            {
                return true;
            }

            if (context.ChapterManager == null)
            {
                return false;
            }

            Chapter1Step currentStep = context.ChapterManager.CurrentStep;
            return requireExactStep ? currentStep == requiredStep : currentStep >= requiredStep;
        }

        private string GetUnavailableMessage()
        {
            return string.IsNullOrWhiteSpace(unavailableMessage) ? "Không thể tương tác lúc này." : unavailableMessage;
        }
    }
}
