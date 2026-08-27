using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Save-backed progress for the PSU/UPS battery mission. The public shape
    /// intentionally stays compatible with the original scene-local helper so
    /// the existing pickup and Henry encounter code does not need rewiring.
    /// </summary>
    internal static class Mission2HeistProgress
    {
        private const string BrokenBatteryObjectName = "broken_battery";

        private static ulong sceneHandle = ulong.MaxValue;
        private static GameObject brokenBatteryObject;
        private static Chapter1SaveData fallbackData =
            Chapter1SaveData.CreateDefault();

        public static bool IsStarted => Data.Mission02Started;
        public static bool HasPsu => Data.Mission02HasPsu;
        public static bool HasUps => Data.Mission02HasUps;
        public static bool HasBrokenBattery =>
            Data.Mission02HasBrokenBattery;
        public static bool HasHenryBattery =>
            Data.Mission02HasHenryBattery;
        public static bool HasDeliveredEquipment =>
            Data.Mission02EquipmentDelivered;
        public static bool CanDeliverEquipment =>
            IsStarted && HasPsu && HasUps && !HasDeliveredEquipment;

        private static Chapter1SaveData Data
        {
            get
            {
                Chapter1Manager manager = Chapter1Manager.Instance;
                if (manager != null)
                {
                    return manager.CurrentData;
                }

                fallbackData ??= Chapter1SaveData.CreateDefault();
                return fallbackData;
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            sceneHandle = ulong.MaxValue;
            brokenBatteryObject = null;
            fallbackData = Chapter1SaveData.CreateDefault();
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
            brokenBatteryObject = null;
        }

        public static bool CollectPsu()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission02Started || data.Mission02HasPsu)
            {
                return false;
            }

            data.Mission02HasPsu = true;
            SaveProgress();
            PublishCurrentObjective();
            return true;
        }

        public static bool CollectUps()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission02Started ||
                data.Mission02HasUps ||
                !data.Mission02HasHenryBattery)
            {
                return false;
            }

            data.Mission02HasUps = true;
            SaveProgress();
            PublishCurrentObjective();
            return true;
        }

        public static void CollectBrokenBattery(GameObject pickupObject)
        {
            Chapter1SaveData data = Data;
            if (!data.Mission02Started)
            {
                return;
            }

            data.Mission02HasBrokenBattery = true;
            brokenBatteryObject = pickupObject;
            SaveProgress();
            PublishCurrentObjective();
        }

        public static bool CompleteBatterySwap()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission02Started ||
                data.Mission02HasHenryBattery ||
                !data.Mission02HasBrokenBattery)
            {
                return false;
            }

            data.Mission02HasBrokenBattery = false;
            data.Mission02HasHenryBattery = true;
            SaveProgress();
            PublishCurrentObjective();
            return true;
        }

        public static void BeginMission(Scene scene)
        {
            EnsureScene(scene);
            Chapter1SaveData data = Data;
            if (data.Mission02Started)
            {
                return;
            }

            data.Mission02Started = true;
            SaveProgress();
            PublishCurrentObjective();
        }

        public static bool TryDeliverEquipment()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission02Started ||
                !data.Mission02HasPsu ||
                !data.Mission02HasUps ||
                data.Mission02EquipmentDelivered)
            {
                return false;
            }

            data.Mission02EquipmentDelivered = true;
            data.EnsureValidDefaults();
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Qua nói chuyện với James ở băng nhóm đối diện.");
            return true;
        }

        /// <summary>
        /// Returns Henry to his normal post-Task-2 state before Mission 3 can
        /// begin. This prevents the old distraction/chase encounter from
        /// competing with the gang encounter and its game-over policy.
        /// </summary>
        public static void ConcludeHenryEncounter(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            HenryChaseController[] controllers =
                UnityEngine.Object.FindObjectsByType<HenryChaseController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < controllers.Length; i++)
            {
                HenryChaseController controller = controllers[i];
                if (controller != null &&
                    controller.gameObject.scene == scene)
                {
                    controller.ConcludeEncounterAndReturnHome();
                }
            }
        }

        public static void PlaceBrokenBatteryAt(Transform target)
        {
            if (target == null)
            {
                return;
            }

            if (brokenBatteryObject == null)
            {
                Transform[] candidates = UnityEngine.Object.FindObjectsByType<
                    Transform>(FindObjectsInactive.Include);
                for (int i = 0; i < candidates.Length; i++)
                {
                    Transform candidate = candidates[i];
                    if (candidate != null &&
                        candidate.gameObject.scene == target.gameObject.scene &&
                        string.Equals(
                            candidate.name,
                            BrokenBatteryObjectName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        brokenBatteryObject = candidate.gameObject;
                        break;
                    }
                }
            }

            if (brokenBatteryObject == null)
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

        internal static void RegisterBrokenBatteryObject(
            GameObject pickupObject)
        {
            brokenBatteryObject = pickupObject;
        }

        private static void SaveProgress()
        {
            Chapter1Manager.Instance?.SaveChapter();
        }

        private static void PublishCurrentObjective()
        {
            Chapter1Manager manager = Chapter1Manager.Instance;
            if (manager != null)
            {
                Chapter1EventBus.RaiseObjectiveChanged(
                    manager.GetCurrentObjective());
            }
        }
    }

    /// <summary>
    /// Persistent hand-off between Minh's police lead and the James encounter.
    /// </summary>
    internal static class Mission3Progress
    {
        private static Chapter1SaveData fallbackData =
            Chapter1SaveData.CreateDefault();

        public static bool CanTalkToJames =>
            Data.Mission02EquipmentDelivered;
        public static bool JamesIntroPlayed =>
            Data.Mission03JamesIntroPlayed;
        public static bool ChallengePassed =>
            Data.Mission03ChallengePassed;
        public static bool GangHostile =>
            Data.Mission03GangHostile;
        public static bool PoliceKeyReceived =>
            Data.Mission03PoliceKeyReceived;
        public static bool TaskCompleted => PoliceKeyReceived;
        public static bool HenryConfrontationCompleted =>
            Data.Mission03HenryConfrontationCompleted;
        public static bool HenryDefeated =>
            Data.Mission03HenryDefeated;
        public static bool PoliceArrestCompleted =>
            Data.Mission03PoliceArrestCompleted;
        public static bool CombatPending =>
            HenryConfrontationCompleted &&
            !HenryDefeated &&
            !PoliceArrestCompleted;

        private static Chapter1SaveData Data =>
            Chapter1Manager.Instance != null
                ? Chapter1Manager.Instance.CurrentData
                : fallbackData;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFallbackOnPlayModeStart()
        {
            fallbackData = Chapter1SaveData.CreateDefault();
        }

        public static void MarkJamesIntroPlayed()
        {
            if (!CanTalkToJames || Data.Mission03JamesIntroPlayed)
            {
                return;
            }

            Data.Mission03JamesIntroPlayed = true;
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Quay lại nói chuyện với James để bắt đầu thử thách.");
        }

        public static void MarkChallengePassed()
        {
            if (!CanTalkToJames || Data.Mission03ChallengePassed)
            {
                return;
            }

            Data.Mission03JamesIntroPlayed = true;
            Data.Mission03ChallengePassed = true;
            Data.Mission03GangHostile = false;
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Nhận chìa khóa từ James.");
        }

        public static bool TryMarkPoliceKeyReceived()
        {
            if (!CanTalkToJames ||
                !Data.Mission03ChallengePassed ||
                Data.Mission03GangHostile)
            {
                return false;
            }

            if (Data.Mission03PoliceKeyReceived)
            {
                return true;
            }

            Data.Mission03PoliceKeyReceived = true;
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Henry đang chạy tới chỗ bạn.");
            return true;
        }

        public static bool MarkHenryConfrontationCompleted()
        {
            if (!Data.Mission03PoliceKeyReceived ||
                Data.Mission03GangHostile)
            {
                return false;
            }

            if (Data.Mission03HenryConfrontationCompleted)
            {
                return true;
            }

            Data.Mission03HenryConfrontationCompleted = true;
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Chuẩn bị đối đầu với Henry.");
            return true;
        }

        public static bool TryMarkHenryDefeated()
        {
            if (!Data.Mission03HenryConfrontationCompleted ||
                Data.Mission03GangHostile)
            {
                return false;
            }

            // Idempotent so death callbacks, animation completion and scene
            // teardown can safely converge on the same persistent result.
            if (Data.Mission03HenryDefeated)
            {
                return true;
            }

            Data.Mission03HenryDefeated = true;
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Cảnh sát đang chạy tới chỗ bạn.");
            return true;
        }

        public static bool TryMarkPoliceArrestCompleted()
        {
            if (!Data.Mission03HenryConfrontationCompleted ||
                Data.Mission03GangHostile)
            {
                return false;
            }

            // The terminal arrest follows either result of the Henry fight
            // and is committed only after Police's final dialogue ends.
            bool arrestWasAlreadyCompleted =
                Data.Mission03PoliceArrestCompleted;
            Data.Mission03PoliceArrestCompleted = true;

            Chapter1Manager manager = Chapter1Manager.Instance;
            if (manager != null)
            {
                bool chapterAlreadyCompleted =
                    manager.CurrentData.ChapterCompleted &&
                    manager.CurrentData.CurrentStep ==
                    Chapter1Step.ChapterCompleted;
                if (!chapterAlreadyCompleted &&
                    !manager.AdvanceTo(Chapter1Step.ChapterCompleted))
                {
                    if (!arrestWasAlreadyCompleted)
                    {
                        Data.Mission03PoliceArrestCompleted = false;
                    }

                    return false;
                }

                // AdvanceTo publishes the step, objective and completion
                // events. This explicit save also covers managers whose
                // automatic milestone saving is disabled.
                SaveProgress();
                return true;
            }

            // Keep the fallback store deterministic for isolated runtime and
            // edit-mode use without publishing duplicate terminal events.
            bool fallbackChapterAlreadyCompleted =
                Data.ChapterCompleted &&
                Data.CurrentStep == Chapter1Step.ChapterCompleted;
            Data.CurrentStep = Chapter1Step.ChapterCompleted;
            Data.ChapterCompleted = true;
            if (!fallbackChapterAlreadyCompleted)
            {
                Chapter1EventBus.RaiseStepChanged(
                    Chapter1Step.ChapterCompleted);
                Chapter1EventBus.RaiseObjectiveChanged(
                    "Chương 1 hoàn thành.");
                Chapter1EventBus.RaiseChapterCompleted();
            }

            return true;
        }

        public static void MarkGangHostile()
        {
            if (!CanTalkToJames || Data.Mission03GangHostile)
            {
                return;
            }

            Data.Mission03JamesIntroPlayed = true;
            Data.Mission03ChallengePassed = false;
            Data.Mission03GangHostile = true;
            Data.Mission03PoliceKeyReceived = false;
            Data.Mission03HenryConfrontationCompleted = false;
            Data.Mission03HenryDefeated = false;
            Data.Mission03PoliceArrestCompleted = false;
            SaveProgress();
            Chapter1EventBus.RaiseObjectiveChanged(
                "Chạy thoát khỏi James, David và Lewis.");
        }

        private static void SaveProgress()
        {
            Chapter1Manager.Instance?.SaveChapter();
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
        private const string HenryBatteryObjectName = "battery";
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
            Transform henryBatteryTarget = null;
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include);
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
                else if (string.Equals(
                             candidate.name,
                             HenryBatteryObjectName,
                             StringComparison.OrdinalIgnoreCase))
                {
                    henryBatteryTarget = candidate;
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
                Mission2HeistProgress.RegisterBrokenBatteryObject(
                    brokenBatteryPickup);
                InstallInteraction(
                    brokenBatteryPickup,
                    interactableLayer,
                    PickupKind.BrokenBattery);
            }

            RestoreSavedPickupVisibility(
                psuPickup,
                upsPickup,
                brokenBatteryPickup,
                henryBatteryTarget);
        }

        private static void RestoreSavedPickupVisibility(
            GameObject psuPickup,
            GameObject upsPickup,
            GameObject brokenBatteryPickup,
            Transform henryBatteryTarget)
        {
            if (psuPickup != null && Mission2HeistProgress.HasPsu)
            {
                psuPickup.SetActive(false);
            }

            if (upsPickup != null && Mission2HeistProgress.HasUps)
            {
                upsPickup.SetActive(false);
            }

            if (brokenBatteryPickup == null)
            {
                return;
            }

            if (Mission2HeistProgress.HasHenryBattery &&
                henryBatteryTarget != null)
            {
                Mission2HeistProgress.PlaceBrokenBatteryAt(
                    henryBatteryTarget);
                henryBatteryTarget.gameObject.SetActive(false);
            }
            else if (Mission2HeistProgress.HasBrokenBattery ||
                     Mission2HeistProgress.HasHenryBattery)
            {
                // It is carried, or its saved replacement location cannot be
                // reconstructed safely in this scene.
                brokenBatteryPickup.SetActive(false);
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
