using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public static class LanAudioRecordingCatalog
    {
        public const string AudioFolder = "Assets/Chapter1/Audio/Phone";
        public const string MixedRecordingId = "Lan_LastRecording_Mixed";
        public const string MixedDisplayName = "Đoạn ghi âm cuối của chị Lan";
        public const string MixedFileName = "Lan_LastRecording_Mixed.mp3";
        public const string LegacyMixedFileName = "Lan_LastRecord_Mixed.MP3";
        public const string OldLanRecordingFileName = "Lan_LastRecording.MP3";
        public const string OldLanVoiceFileName = "LanVoice.mp3";
        public const string OldPoliceSirenFileName = "police_siren.mp3";

        public static readonly string MixedPath = AudioFolder + "/" + MixedFileName;
        public static readonly string LegacyMixedPath = AudioFolder + "/" + LegacyMixedFileName;
        public static readonly string OldLanRecordingPath = AudioFolder + "/" + OldLanRecordingFileName;
        public static readonly string OldLanVoicePath = AudioFolder + "/" + OldLanVoiceFileName;
        public static readonly string OldPoliceSirenPath = AudioFolder + "/" + OldPoliceSirenFileName;

        public static readonly LanAudioStemId[] StemOrder =
        {
            LanAudioStemId.Voice,
            LanAudioStemId.Police,
            LanAudioStemId.Rain,
            LanAudioStemId.Horns,
            LanAudioStemId.Wind,
            LanAudioStemId.Thunder
        };

        public static int StemCount => StemOrder.Length;

        public static string GetStemFileName(LanAudioStemId stem)
        {
            switch (stem)
            {
                case LanAudioStemId.Voice:
                    return "Lan_Stem_Voice.mp3";
                case LanAudioStemId.Police:
                    return "Lan_Stem_PoliceSiren.mp3";
                case LanAudioStemId.Rain:
                    return "Lan_Stem_Rain.mp3";
                case LanAudioStemId.Horns:
                    return "Lan_Stem_TrafficHorn.mp3";
                case LanAudioStemId.Wind:
                    return "Lan_Stem_Wind.mp3";
                case LanAudioStemId.Thunder:
                    return "Lan_Stem_Thunder.mp3";
                default:
                    return string.Empty;
            }
        }

        public static string GetStemPath(LanAudioStemId stem)
        {
            return AudioFolder + "/" + GetStemFileName(stem);
        }

        public static string GetStemDisplayName(LanAudioStemId stem)
        {
            for (int i = 0; i < StemOrder.Length; i++)
            {
                if (StemOrder[i] == stem)
                {
                    return $"Âm thanh {i + 1}";
                }
            }

            return stem.ToString();
        }

        public static string GetOutputRecordingId(LanAudioStemId stem)
        {
            switch (stem)
            {
                case LanAudioStemId.Voice:
                    return "Lan_Voice_Isolated";
                case LanAudioStemId.Police:
                    return "Lan_PoliceSiren_Isolated";
                case LanAudioStemId.Rain:
                    return "Lan_Rain_Isolated";
                case LanAudioStemId.Horns:
                    return "Lan_TrafficHorn_Isolated";
                case LanAudioStemId.Wind:
                    return "Lan_Wind_Isolated";
                case LanAudioStemId.Thunder:
                    return "Lan_Thunder_Isolated";
                default:
                    return string.Empty;
            }
        }

        public static bool TryGetStemFromRecordingId(string recordingId, out LanAudioStemId stem)
        {
            for (int i = 0; i < StemOrder.Length; i++)
            {
                LanAudioStemId candidate = StemOrder[i];
                if (string.Equals(recordingId, GetOutputRecordingId(candidate), StringComparison.Ordinal))
                {
                    stem = candidate;
                    return true;
                }
            }

            stem = default;
            return false;
        }

        public static bool IsKnownRecordingId(string recordingId)
        {
            if (string.Equals(recordingId, MixedRecordingId, StringComparison.Ordinal))
            {
                return true;
            }

            return TryGetStemFromRecordingId(recordingId, out _);
        }

        public static string GetRecordingDisplayName(string recordingId)
        {
            if (string.Equals(recordingId, MixedRecordingId, StringComparison.Ordinal))
            {
                return MixedDisplayName;
            }

            return TryGetStemFromRecordingId(recordingId, out LanAudioStemId stem)
                ? GetStemDisplayName(stem)
                : recordingId;
        }

        public static AudioClip ResolveClip(string recordingId, LanRecordingMissionController lanRecordingController, AudioSeparatorMixerController mixerController)
        {
            AudioClip sceneClip;
            if (string.Equals(recordingId, MixedRecordingId, StringComparison.Ordinal))
            {
                sceneClip = lanRecordingController != null
                    ? lanRecordingController.LanRecordingClip
                    : null;
            }
            else
            {
                sceneClip = TryGetStemFromRecordingId(recordingId, out LanAudioStemId stem) &&
                            mixerController != null
                    ? mixerController.GetStemClip(stem)
                    : null;
            }

            if (sceneClip != null)
            {
                return sceneClip;
            }

            PhoneRecordingAudioLibrary library = PhoneRecordingAudioLibrary.LoadDefault();
            return library != null ? library.ResolveClip(recordingId) : null;
        }
    }
}
