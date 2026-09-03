using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [CreateAssetMenu(
        fileName = "PhoneRecordingAudioLibrary",
        menuName = "Dormitory Mystery/Phone/Recording Audio Library")]
    public sealed class PhoneRecordingAudioLibrary : ScriptableObject
    {
        public const string DefaultResourcePath = "Phone/PhoneRecordingAudioLibrary";

        [Header("Original recording")]
        [SerializeField] private AudioClip mixedRecording;

        [Header("Separated recordings")]
        [SerializeField] private AudioClip voice;
        [SerializeField] private AudioClip policeSiren;
        [SerializeField] private AudioClip rain;
        [SerializeField] private AudioClip trafficHorn;
        [SerializeField] private AudioClip wind;
        [SerializeField] private AudioClip thunder;

        public static PhoneRecordingAudioLibrary LoadDefault()
        {
            return Resources.Load<PhoneRecordingAudioLibrary>(DefaultResourcePath);
        }

        public AudioClip ResolveClip(string recordingId)
        {
            if (string.Equals(
                    recordingId,
                    LanAudioRecordingCatalog.MixedRecordingId,
                    System.StringComparison.Ordinal))
            {
                return mixedRecording;
            }

            if (!LanAudioRecordingCatalog.TryGetStemFromRecordingId(
                    recordingId,
                    out LanAudioStemId stem))
            {
                return null;
            }

            switch (stem)
            {
                case LanAudioStemId.Voice:
                    return voice;
                case LanAudioStemId.Police:
                    return policeSiren;
                case LanAudioStemId.Rain:
                    return rain;
                case LanAudioStemId.Horns:
                    return trafficHorn;
                case LanAudioStemId.Wind:
                    return wind;
                case LanAudioStemId.Thunder:
                    return thunder;
                default:
                    return null;
            }
        }
    }
}
