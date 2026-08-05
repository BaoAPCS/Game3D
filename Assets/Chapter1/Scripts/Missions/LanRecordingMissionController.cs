using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class LanRecordingMissionController : MonoBehaviour
    {
        [SerializeField] private LanRecordingMissionState state = LanRecordingMissionState.NotStarted;
        [SerializeField] private AudioClip lanRecordingClip;
        [SerializeField] private PhoneMessageData lanVoiceMessage;
        [SerializeField] private string expectedAudioAssetPath = LanAudioRecordingCatalog.MixedPath;

        public LanRecordingMissionState State => state;
        public PhoneMessageData LanVoiceMessage => lanVoiceMessage;
        public AudioClip LanRecordingClip => lanRecordingClip != null ? lanRecordingClip : lanVoiceMessage != null ? lanVoiceMessage.AudioClip : null;
        public bool HasRealLanRecordingClip => LanRecordingClip != null;

        private void Awake()
        {
            if (!HasRealLanRecordingClip)
            {
                Debug.LogWarning(
                    $"[LanRecordingMissionController] Missing real Lan recording AudioClip. Expected asset path: {expectedAudioAssetPath}. No fake AudioClip will be created.",
                    this);
            }
        }

        public void ConfigureLanRecording(AudioClip clip, PhoneMessageData voiceMessage)
        {
            lanRecordingClip = clip;
            lanVoiceMessage = voiceMessage;
            expectedAudioAssetPath = LanAudioRecordingCatalog.MixedPath;
        }

        public void SetState(LanRecordingMissionState nextState)
        {
            if ((int)nextState < (int)state)
            {
                Debug.LogWarning($"[LanRecordingMissionController] Cannot move mission state backward from {state} to {nextState}.", this);
                return;
            }

            state = nextState;
        }

        public void MarkRecordingDownloaded()
        {
            if ((int)state < (int)LanRecordingMissionState.RecordingDownloaded)
            {
                state = LanRecordingMissionState.RecordingDownloaded;
            }

            NotifyLanRecordingSaved();
        }

        public void NotifyLanRecordingSaved()
        {
            Mission01AudioSeparatorManager firstMissionManager = Mission01AudioSeparatorManager.Instance;
            if (firstMissionManager != null)
            {
                firstMissionManager.NotifyLanRecordingSaved();
            }
        }
    }
}
