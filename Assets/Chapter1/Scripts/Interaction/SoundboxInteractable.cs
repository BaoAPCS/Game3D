using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class SoundboxInteractable : Chapter1Interactable
    {
        private const string SoundboxObjectName = "soundbox";
        private const string InteractableLayerName = "Interactable";

        [SerializeField, Min(0.01f)] private float interactionRange = 0.1f;
        [SerializeField] private Collider interactionCollider;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneSoundboxInteraction()
        {
            int interactableLayer = LayerMask.NameToLayer(
                InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogWarning(
                    $"[SoundboxInteractable] Layer " +
                    $"'{InteractableLayerName}' does not exist.");
                return;
            }

            Transform[] sceneTransforms =
                FindObjectsByType<Transform>(
                    FindObjectsInactive.Exclude);
            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                if (candidate == null ||
                    !string.Equals(
                        candidate.name,
                        SoundboxObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                InstallInteraction(candidate.gameObject, interactableLayer);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
        }

        public override string GetInteractionPrompt(InteractionContext context)
        {
            return "Thiết bị tách âm thanh, nhấn [E] để tách âm";
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (!base.CanInteract(context) || context.PlayerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            if (interactionCollider == null)
            {
                return false;
            }

            Vector3 playerPosition = context.PlayerTransform.position;
            Vector3 closestPoint = interactionCollider.ClosestPoint(
                playerPosition);
            playerPosition.y = 0f;
            closestPoint.y = 0f;

            CharacterController playerController =
                context.PlayerObject != null
                    ? context.PlayerObject.GetComponent<CharacterController>()
                    : null;
            float playerRadius = playerController != null
                ? playerController.radius
                : 0f;
            float maximumPlanarDistance = interactionRange + playerRadius;

            return (playerPosition - closestPoint).sqrMagnitude <=
                   maximumPlanarDistance * maximumPlanarDistance;
        }

        public override Transform GetInteractionTransform()
        {
            ResolveReferences();
            return interactionCollider != null
                ? interactionCollider.transform
                : base.GetInteractionTransform();
        }

        private static void InstallInteraction(
            GameObject soundbox,
            int interactableLayer)
        {
            SetLayerRecursively(soundbox.transform, interactableLayer);

            if (!soundbox.TryGetComponent(
                    out SoundboxInteractable interactable))
            {
                interactable = soundbox.AddComponent<SoundboxInteractable>();
            }

            interactable.ResolveReferences();
        }

        private static void SetLayerRecursively(
            Transform parent,
            int layer)
        {
            parent.gameObject.layer = layer;
            for (int i = 0; i < parent.childCount; i++)
            {
                SetLayerRecursively(parent.GetChild(i), layer);
            }
        }

        private void ResolveReferences()
        {
            if (interactionCollider == null)
            {
                interactionCollider = GetComponent<Collider>() ??
                    GetComponentInChildren<Collider>(true);
            }
        }

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0.01f, interactionRange);
            ResolveReferences();
        }
    }
}
