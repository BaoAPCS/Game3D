using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Scene-local progress for the PSU/UPS battery side mission. The state is
    /// deliberately reset when the scene is reloaded so it stays independent
    /// from the main Chapter1Step save flow.
    /// </summary>
    internal static class Mission2HeistProgress
    {
        private static ulong sceneHandle = ulong.MaxValue;
        private static GameObject brokenBatteryObject;

        public static bool IsStarted { get; private set; }
        public static bool HasPsu { get; private set; }
        public static bool HasUps { get; private set; }
        public static bool HasBrokenBattery { get; private set; }
        public static bool HasHenryBattery { get; private set; }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            sceneHandle = ulong.MaxValue;
            ResetProgress();
        }

        public static void EnsureScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            ulong currentSceneHandle = scene.handle.GetRawData();
            if (sceneHandle == currentSceneHandle)
            {
                return;
            }

            sceneHandle = currentSceneHandle;
            ResetProgress();
        }

        public static bool CollectPsu()
        {
            if (!IsStarted || HasPsu)
            {
                return false;
            }

            HasPsu = true;
            return true;
        }

        public static bool CollectUps()
        {
            if (!IsStarted || HasUps || !HasHenryBattery)
            {
                return false;
            }

            HasUps = true;
            return true;
        }

        public static void CollectBrokenBattery(GameObject pickupObject)
        {
            if (!IsStarted)
            {
                return;
            }

            HasBrokenBattery = true;
            brokenBatteryObject = pickupObject;
        }

        public static bool CompleteBatterySwap()
        {
            if (!IsStarted || HasHenryBattery || !HasBrokenBattery)
            {
                return false;
            }

            HasBrokenBattery = false;
            HasHenryBattery = true;
            return true;
        }

        public static void BeginMission(Scene scene)
        {
            EnsureScene(scene);
            IsStarted = true;
        }

        public static void PlaceBrokenBatteryAt(Transform target)
        {
            if (brokenBatteryObject == null || target == null)
            {
                return;
            }

            Transform brokenBatteryTransform = brokenBatteryObject.transform;
            brokenBatteryTransform.SetPositionAndRotation(
                target.position,
                target.rotation);
            brokenBatteryTransform.localScale = target.localScale;
            brokenBatteryObject.SetActive(true);
        }

        private static void ResetProgress()
        {
            IsStarted = false;
            HasPsu = false;
            HasUps = false;
            HasBrokenBattery = false;
            HasHenryBattery = false;
            brokenBatteryObject = null;
        }
    }

    /// <summary>
    /// Runtime-installs the E-key pickup interaction on PSU, UPS, and
    /// broken_battery without requiring manual scene component wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UPSInteractable : Chapter1Interactable
    {
        private enum PickupKind
        {
            Psu,
            BrokenBattery,
            Ups
        }

        private const string PsuObjectName = "PSU";
        private const string UpsObjectName = "UPS";
        private const string LegacyPsuObjectName = "old_radio";
        private const string BrokenBatteryObjectName = "broken_battery";
        private const string InteractableLayerName = "Interactable";

        [SerializeField] private PickupKind pickupKind;
        [SerializeField, Min(0.01f)] private float proximityPadding = 0.5f;
        [SerializeField] private Collider interactionCollider;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

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

        private static void InstallSceneInteractions(Scene scene)
        {
            int interactableLayer = LayerMask.NameToLayer(
                InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogWarning(
                    $"[Mission2] Layer '{InteractableLayerName}' does not exist.");
                return;
            }

            GameObject psuPickup = null;
            GameObject upsPickup = null;
            GameObject brokenBatteryPickup = null;
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude);
            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                if (candidate == null || candidate.gameObject.scene != scene)
                {
                    continue;
                }

                if (string.Equals(
                        candidate.name,
                        PsuObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    psuPickup = candidate.gameObject;
                }
                else if (psuPickup == null &&
                         string.Equals(
                             candidate.name,
                             LegacyPsuObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    psuPickup = candidate.gameObject;
                }
                else if (string.Equals(
                             candidate.name,
                             UpsObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    upsPickup = candidate.gameObject;
                }
                else if (string.Equals(
                             candidate.name,
                             BrokenBatteryObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    brokenBatteryPickup = candidate.gameObject;
                }
            }

            if (psuPickup == null &&
                upsPickup == null &&
                brokenBatteryPickup == null)
            {
                return;
            }

            Mission2HeistProgress.EnsureScene(scene);
            if (psuPickup != null)
            {
                InstallInteraction(
                    psuPickup,
                    interactableLayer,
                    PickupKind.Psu);
            }

            if (upsPickup != null)
            {
                InstallInteraction(
                    upsPickup,
                    interactableLayer,
                    PickupKind.Ups);
            }

            if (brokenBatteryPickup != null)
            {
                InstallInteraction(
                    brokenBatteryPickup,
                    interactableLayer,
                    PickupKind.BrokenBattery);
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
            if (pickupKind == PickupKind.BrokenBattery)
            {
                return "[E] Nhặt ắc quy hỏng";
            }

            if (pickupKind == PickupKind.Ups)
            {
                return "[E] Nhặt UPS";
            }

            return "[E] Nhặt PSU";
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (!Mission2HeistProgress.IsStarted ||
                !base.CanInteract(context) ||
                context.PlayerTransform == null ||
                IsAlreadyCollected())
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
            float maximumPlanarDistance = proximityPadding + playerRadius;

            return (playerPosition - closestPoint).sqrMagnitude <=
                   maximumPlanarDistance * maximumPlanarDistance;
        }

        public override Transform GetInteractionTransform()
        {
            // Let the interaction controller use the actual collider center.
            // Imported PSU/UPS models can have pivots far below or beside the
            // visible mesh, causing a clear pickup to look obstructed.
            return null;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            if (!Mission2HeistProgress.IsStarted ||
                IsAlreadyCollected())
            {
                return InteractionResult.Ignored();
            }

            string message;
            if (pickupKind == PickupKind.BrokenBattery)
            {
                Mission2HeistProgress.CollectBrokenBattery(gameObject);
                message = "Đã nhặt ắc quy hỏng";
            }
            else if (pickupKind == PickupKind.Ups)
            {
                if (!Mission2HeistProgress.HasHenryBattery)
                {
                    return InteractionResult.Failed(
                        "UPS cần ắc quy. Hãy tìm ắc quy");
                }

                if (!Mission2HeistProgress.CollectUps())
                {
                    return InteractionResult.Ignored();
                }

                message = "Đã nhặt UPS";
            }
            else
            {
                if (!Mission2HeistProgress.CollectPsu())
                {
                    return InteractionResult.Ignored();
                }

                message = "Đã nhặt PSU";
            }

            DisableInteraction();
            gameObject.SetActive(false);
            return InteractionResult.Succeeded(message);
        }

        private bool IsAlreadyCollected()
        {
            if (pickupKind == PickupKind.BrokenBattery)
            {
                return Mission2HeistProgress.HasBrokenBattery ||
                       Mission2HeistProgress.HasHenryBattery;
            }

            return pickupKind == PickupKind.Ups
                ? Mission2HeistProgress.HasUps
                : Mission2HeistProgress.HasPsu;
        }

        private static void InstallInteraction(
            GameObject pickup,
            int interactableLayer,
            PickupKind kind)
        {
            SetLayerRecursively(pickup.transform, interactableLayer);

            if (!pickup.TryGetComponent(
                    out UPSInteractable interactable))
            {
                interactable = pickup.AddComponent<UPSInteractable>();
            }

            interactable.pickupKind = kind;
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
            proximityPadding = Mathf.Max(0.01f, proximityPadding);
            ResolveReferences();
        }
    }
}
