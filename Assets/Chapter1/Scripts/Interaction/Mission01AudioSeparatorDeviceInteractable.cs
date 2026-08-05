using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Mission01AudioSeparatorDeviceInteractable : Chapter1Interactable
    {
        [SerializeField] private Mission01AudioSeparatorManager missionManager;
        [SerializeField] private LanRecordingMissionController lanRecordingController;
        [SerializeField] private AudioSeparatorMixerController mixerController;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            ConfigureCollider();
        }

        private void OnValidate()
        {
            ConfigureCollider();
        }

        public override string GetInteractionPrompt(InteractionContext context)
        {
            ResolveReferences();
            if (missionManager == null ||
                missionManager.State < FirstMissionState.EnterDungRoom ||
                missionManager.State >= FirstMissionState.ReturnToMinh)
            {
                return string.Empty;
            }

            return "[F] Dùng máy tách âm";
        }

        public override bool CanInteract(InteractionContext context)
        {
            ResolveReferences();
            return base.CanInteract(context) &&
                missionManager != null &&
                missionManager.State >= FirstMissionState.EnterDungRoom &&
                missionManager.State < FirstMissionState.ReturnToMinh;
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            ResolveReferences();
            if (missionManager == null)
            {
                return InteractionResult.Failed("Chưa tìm thấy Mission 01 Manager.");
            }

            if (missionManager.State < FirstMissionState.EnterDungRoom)
            {
                return InteractionResult.Failed("Chưa cần dùng máy tách âm lúc này.");
            }

            if (missionManager.LanRecordingSeparated)
            {
                return InteractionResult.Succeeded("Đoạn ghi âm đã được xử lý. Mở Ghi âm để nghe lại giọng chị Lan.");
            }

            if (!HasLanRecordingClip())
            {
                return InteractionResult.Failed("Chưa có đoạn ghi âm của Chị Lan trong điện thoại.");
            }

            if (mixerController == null)
            {
                return InteractionResult.Failed("Máy tách âm chưa được cấu hình mixer.");
            }

            return mixerController.TryBeginSession(context);
        }

        public void Configure(
            Mission01AudioSeparatorManager manager,
            LanRecordingMissionController recordingController,
            AudioSeparatorMixerController mixer = null)
        {
            missionManager = manager;
            lanRecordingController = recordingController;
            mixerController = mixer != null ? mixer : GetComponent<AudioSeparatorMixerController>();
            ConfigureCollider();
        }

        private bool HasLanRecordingClip()
        {
            if (lanRecordingController == null)
            {
                lanRecordingController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            Chapter1SaveData data = missionManager != null ? missionManager.Data : Chapter1Manager.Instance?.CurrentData;
            data?.EnsureValidDefaults();
            return lanRecordingController != null &&
                lanRecordingController.LanRecordingClip != null &&
                (data == null || data.HasPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId));
        }

        private void ResolveReferences()
        {
            if (missionManager == null)
            {
                missionManager = Mission01AudioSeparatorManager.Instance;
            }

            if (missionManager == null)
            {
                missionManager = FindAnyObjectByType<Mission01AudioSeparatorManager>(FindObjectsInactive.Include);
            }

            if (lanRecordingController == null)
            {
                lanRecordingController = FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            }

            if (mixerController == null)
            {
                mixerController = GetComponent<AudioSeparatorMixerController>();
            }

            if (mixerController == null)
            {
                mixerController = GetComponentInChildren<AudioSeparatorMixerController>(true);
            }
        }

        private void ConfigureCollider()
        {
            Collider attachedCollider = GetComponent<Collider>();
            if (attachedCollider != null)
            {
                attachedCollider.isTrigger = true;
            }
        }
    }
}
