using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Runtime-installed pickup interaction for the scene PetrolCan. The can
    /// stays outside inventory/save state so it can be thrown and collected
    /// again as many times as needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetrolCanInteractable : Chapter1Interactable
    {
        private const string PetrolCanObjectName = "PetrolCan";
        private const string InteractableLayerName = "Interactable";

        [SerializeField, Min(0.01f)] private float proximityPadding = 0.5f;
        [SerializeField] private Rigidbody canRigidbody;
        [SerializeField] private Collider interactionCollider;

        private Collider[] physicsColliders = Array.Empty<Collider>();
        private bool[] colliderEnabledStates = Array.Empty<bool>();
        private Transform originalParent;
        private Quaternion heldLocalRotation;
        private bool rigidbodyWasKinematic;
        private bool rigidbodyUsedGravity;
        private bool rigidbodyDetectedCollisions;
        private bool restPoseCaptured;
        private bool isHeld;
        private bool ignitionArmed;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        internal bool IsHeld => isHeld;
        internal Rigidbody CanRigidbody => canRigidbody;
        internal Collider[] PhysicsColliders => physicsColliders;

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
            InstallSceneInteractions(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallSceneInteractions(scene);
        }

        protected override void Awake()
        {
            base.Awake();
            CaptureRestPose();
            ResolveReferences();
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return "[E] Nhặt can xăng";
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (!Mission2HeistProgress.IsStarted ||
                Mission2HeistProgress.HasDeliveredEquipment ||
                isHeld ||
                !base.CanInteract(context) ||
                context.PlayerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            if (canRigidbody == null || interactionCollider == null)
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
            float maximumPlanarDistance = proximityPadding + playerRadius;

            return (playerPosition - closestPoint).sqrMagnitude <=
                   maximumPlanarDistance * maximumPlanarDistance;
        }

        public override Transform GetInteractionTransform()
        {
            // Imported models can have a misleading root pivot. Let the
            // interaction controller use the collider center instead.
            return null;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            if (!Mission2HeistProgress.IsStarted ||
                Mission2HeistProgress.HasDeliveredEquipment ||
                isHeld ||
                context.PlayerObject == null)
            {
                return InteractionResult.Ignored();
            }

            PetrolCanThrower thrower =
                PetrolCanThrower.GetOrInstall(context.PlayerObject);
            if (thrower == null || !thrower.TryPickup(this))
            {
                return InteractionResult.Ignored();
            }

            return InteractionResult.Succeeded();
        }

        internal bool AttachTo(Transform holdPoint)
        {
            if (isHeld || holdPoint == null)
            {
                return false;
            }

            CaptureRestPose();
            ResolveReferences();
            if (canRigidbody == null || physicsColliders.Length == 0)
            {
                Debug.LogWarning(
                    "[PetrolCan] Cannot pick up PetrolCan without a " +
                    "Rigidbody and Collider.",
                    this);
                return false;
            }

            CapturePhysicsState();
            isHeld = true;
            ignitionArmed = false;
            DisableInteraction();

            canRigidbody.linearVelocity = Vector3.zero;
            canRigidbody.angularVelocity = Vector3.zero;
            canRigidbody.isKinematic = true;
            canRigidbody.useGravity = false;
            canRigidbody.detectCollisions = false;

            for (int i = 0; i < physicsColliders.Length; i++)
            {
                if (physicsColliders[i] != null)
                {
                    physicsColliders[i].enabled = false;
                }
            }

            transform.SetParent(holdPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = heldLocalRotation;
            return true;
        }

        internal bool ReleaseForThrow(
            Vector3 origin,
            Quaternion rotation,
            Vector3 velocity)
        {
            if (!isHeld || canRigidbody == null)
            {
                return false;
            }

            transform.SetParent(originalParent, true);
            transform.SetPositionAndRotation(origin, rotation);

            canRigidbody.isKinematic = rigidbodyWasKinematic;
            canRigidbody.useGravity = rigidbodyUsedGravity;
            canRigidbody.detectCollisions = rigidbodyDetectedCollisions;

            for (int i = 0; i < physicsColliders.Length; i++)
            {
                Collider physicsCollider = physicsColliders[i];
                if (physicsCollider != null)
                {
                    physicsCollider.enabled =
                        i < colliderEnabledStates.Length
                            ? colliderEnabledStates[i]
                            : true;
                }
            }

            canRigidbody.linearVelocity = velocity;
            canRigidbody.angularVelocity = Vector3.zero;
            isHeld = false;
            ignitionArmed = true;
            EnableInteraction();
            return true;
        }

        internal bool OwnsCollider(Collider candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            for (int i = 0; i < physicsColliders.Length; i++)
            {
                if (physicsColliders[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InstallSceneInteractions(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            int interactableLayer = LayerMask.NameToLayer(
                InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogWarning(
                    $"[PetrolCan] Layer '{InteractableLayerName}' does not " +
                    "exist.");
                return;
            }

            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude);
            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                if (candidate == null ||
                    candidate.gameObject.scene != scene ||
                    !string.Equals(
                        candidate.name,
                        PetrolCanObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SetLayerRecursively(candidate, interactableLayer);
                if (!candidate.TryGetComponent(
                        out PetrolCanInteractable interactable))
                {
                    interactable = candidate.gameObject.AddComponent<
                        PetrolCanInteractable>();
                }

                interactable.ResolveReferences();
            }
        }

        private static void SetLayerRecursively(Transform parent, int layer)
        {
            parent.gameObject.layer = layer;
            for (int i = 0; i < parent.childCount; i++)
            {
                SetLayerRecursively(parent.GetChild(i), layer);
            }
        }

        private void CaptureRestPose()
        {
            if (restPoseCaptured)
            {
                return;
            }

            originalParent = transform.parent;
            heldLocalRotation = transform.localRotation;
            restPoseCaptured = true;
        }

        private void ResolveReferences()
        {
            if (canRigidbody == null)
            {
                canRigidbody = GetComponent<Rigidbody>() ??
                    GetComponentInChildren<Rigidbody>(true);
            }

            if (physicsColliders == null || physicsColliders.Length == 0)
            {
                physicsColliders = GetComponentsInChildren<Collider>(true);
                colliderEnabledStates =
                    new bool[physicsColliders.Length];
            }

            if (interactionCollider == null)
            {
                interactionCollider = GetComponent<Collider>() ??
                    GetComponentInChildren<Collider>(true);
            }
        }

        private void CapturePhysicsState()
        {
            rigidbodyWasKinematic = canRigidbody.isKinematic;
            rigidbodyUsedGravity = canRigidbody.useGravity;
            rigidbodyDetectedCollisions = canRigidbody.detectCollisions;

            if (colliderEnabledStates.Length != physicsColliders.Length)
            {
                colliderEnabledStates =
                    new bool[physicsColliders.Length];
            }

            for (int i = 0; i < physicsColliders.Length; i++)
            {
                colliderEnabledStates[i] =
                    physicsColliders[i] != null &&
                    physicsColliders[i].enabled;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryIgniteFoodcart(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            TryIgniteFoodcart(collision);
        }

        private void TryIgniteFoodcart(Collision collision)
        {
            if (!ignitionArmed || isHeld || collision == null)
            {
                return;
            }

            int contactCount = collision.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (TryIgniteFromCollider(
                        contact.otherCollider,
                        contact) ||
                    TryIgniteFromCollider(
                        contact.thisCollider,
                        contact))
                {
                    ignitionArmed = false;
                    return;
                }
            }
        }

        private static bool TryIgniteFromCollider(
            Collider contactedCollider,
            ContactPoint contact)
        {
            if (contactedCollider == null)
            {
                return false;
            }

            FoodcartGrillIgnitionTarget target =
                contactedCollider.GetComponent<
                    FoodcartGrillIgnitionTarget>() ??
                contactedCollider.GetComponentInParent<
                    FoodcartGrillIgnitionTarget>();
            return target != null &&
                   target.TryIgnite(contactedCollider, contact);
        }

        private void OnValidate()
        {
            proximityPadding = Mathf.Max(0.01f, proximityPadding);
            ResolveReferences();
        }
    }
}
